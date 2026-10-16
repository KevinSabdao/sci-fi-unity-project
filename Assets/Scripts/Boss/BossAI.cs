using UnityEngine;
using UnityEngine.AI;

// brain of the boss. waits in place until it notices the player, roars and then fights until it dies
// chases on the NavMesh and attacks in range. no patrol and no losing the player once the fight starts
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(EnemyHealth))]
public class BossAI : MonoBehaviour
{
    enum State { Waiting, Intro, Chase, Attack, Recover, Dead }

    // one move the boss can pick. ranges are flat distances from his feet
    [System.Serializable]
    public class Attack
    {
        // inspector label only
        public string label = "Attack";

        // must match a Trigger parameter in the Animator Controller
        public string triggerName = "ClawCombo";

        // the player must be between these distances
        public float minRange = 0f;
        public float maxRange = 2.2f;

        // and within this many degrees of his front
        public float maxAngle = 45f;

        // seconds before this move can be picked again
        public float cooldown = 0f;

        // length of the clip. the move ends after it
        [ReadOnly]
        public float duration = 2.625f;

        // seconds into the clip where he turns to the player: x start and y end
        // turning stops at the end of each window so the player can dodge
        [ReadOnly]
        public Vector2[] turnWindows = new Vector2[0];

        // degrees per second inside the turn windows
        public float turnSpeed = 240f;

        // inside the turn windows he steps in until the player is this close
        public float idealRange = 1.5f;

        // metres per second of that step. 0 keeps him planted
        public float lungeSpeed = 4.5f;

        // aims where the player will be this many seconds ahead. 0 aims at the player
        public float leadTime = 0.3f;

        // where the player should end up in degrees from his front, by distance. + is his right
        // the claw does not always strike straight ahead. the Swipe lands to his left
        public AnimationCurve aimAngleByDistance = AnimationCurve.Constant(0f, 10f, 0f);

        // seconds into the clip where he checks the distance again, between hits
        [ReadOnly]
        public float[] cancelPoints = new float[0];

        // past this distance at a check he drops the rest of the move and chases
        public float cancelRange = 3.5f;

        // takes the forward step baked in the clip
        [ReadOnly]
        public bool rootMotion;

        [System.NonSerialized]
        public float readyTime;
    }

    // who to fight. empty finds the object tagged Player
    [SerializeField] Transform target;

    [Header("Sight")]
    // notices the player in any direction inside this distance
    [SerializeField] float detectionRange = 20f;

    // layers that block sight. leave the player, enemy and boss hitbox layers out
    [SerializeField] LayerMask sightBlockers = ~0;

    // height of his eyes above his feet
    [SerializeField] float eyeHeight = 2.5f;

    // height of the player's eyes above the player's position
    [SerializeField] float targetEyeHeight = 1.5f;

    [Header("Movement")]
    // above the Run clip's own speed the run animation plays faster to match
    [SerializeField] float chaseSpeed = 3.9f;

    // speed the feet move at in the Run clip: the blend tree's top threshold
    [ReadOnly]
    [SerializeField] float runClipSpeed = 3.9f;

    // degrees per second during the roar, the recovery and close in the chase
    [SerializeField] float turnSpeed = 160f;

    [Header("Intro")]
    // length of the Roar clip
    [ReadOnly]
    [SerializeField] float roarDuration = 3.958f;

    [Header("Attacks")]
    [SerializeField] Attack[] attacks =
    {
        new Attack
        {
            label = "Claw Combo",
            triggerName = "ClawCombo",
            minRange = 0f,
            maxRange = 2.2f,
            maxAngle = 45f,
            cooldown = 0f,
            duration = 2.625f,
            // aims before hit 1, between hits 1 and 2 (frames 12-16) and 2 and 3 (frames 24-31)
            turnWindows = new[]
            {
                new Vector2(0f, 0.21f),
                new Vector2(0.46f, 0.63f),
                new Vector2(0.96f, 1.25f),
            },
            turnSpeed = 240f,
            idealRange = 1.5f,
            lungeSpeed = 4.5f,
            leadTime = 0.3f,
            // right after hits 1 and 2
            cancelPoints = new[] { 0.46f, 0.96f },
            cancelRange = 3.5f,
            rootMotion = true,
        },
        new Attack
        {
            label = "Swipe",
            triggerName = "Swipe",
            // the claw lands 2.8 to 4.3 m out. closer it passes overhead
            minRange = 2.4f,
            maxRange = 4f,
            maxAngle = 60f,
            cooldown = 4f,
            duration = 2.667f,
            // aims through the raised wind up. commits at frame 27, before the strike at 32
            turnWindows = new[] { new Vector2(0f, 1.1f) },
            turnSpeed = 180f,
            idealRange = 3.3f,
            // planted: a heavy telegraphed blow the player is meant to dodge
            lungeSpeed = 0f,
            leadTime = 0.25f,
            // measured on the clip: the tips cross body height at -20 deg 4.2 m, -40 deg 3.7 m and -60 deg 2.9 m
            aimAngleByDistance = new AnimationCurve(
                new Keyframe(1.8f, -65f),
                new Keyframe(2.85f, -60f),
                new Keyframe(3.7f, -40f),
                new Keyframe(4.2f, -20f)),
            cancelPoints = new float[0],
            cancelRange = 0f,
            rootMotion = false,
        },
    };

    // seconds standing still after an attack, turning to the player
    [SerializeField] float recoverTime = 0.8f;

    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int AnimSpeedHash = Animator.StringToHash("AnimSpeed");
    static readonly int CancelHash = Animator.StringToHash("CancelAttack");

    NavMeshAgent agent;
    Animator animator;
    EnemyHealth health;

    // optional: the claw damage windows
    BossClawDamage claws;

    // keeps the player from walking through him. turned off once the body falls
    CapsuleCollider bodyBlocker;

    State state = State.Waiting;
    Attack current;
    float stateStartTime;
    float stateEndTime;

    // cancel points of the current move already checked
    int cancelChecksDone;

    // measured each frame, for aiming ahead of the player
    Vector3 targetVelocity;
    Vector3 lastTargetPosition;

    public bool IsFighting => state != State.Waiting && state != State.Dead;

    void Reset()
    {
        sightBlockers = ~LayerMask.GetMask("Player", "Enemy", "BossHitBox", "Ignore Raycast");
    }

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        health = GetComponent<EnemyHealth>();
        claws = GetComponent<BossClawDamage>();
        bodyBlocker = GetComponent<CapsuleCollider>();
    }

    void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
                target = player.transform;
        }

        if (target == null)
        {
            Debug.LogError($"{nameof(BossAI)}: no target. Assign one, or tag " +
                           "the player object as Player.", this);
            enabled = false;
            return;
        }

        health.DamageBlocked += HandleDamageBlocked;
        health.OnDied.AddListener(HandleDied);

        agent.speed = chaseSpeed;
        lastTargetPosition = target.position;

        EnterWaiting();
    }

    void OnDestroy()
    {
        if (health == null)
            return;

        health.DamageBlocked -= HandleDamageBlocked;
        health.OnDied.RemoveListener(HandleDied);
    }

    void Update()
    {
        TrackTargetVelocity();

        switch (state)
        {
            case State.Waiting:
                if (NoticesTarget())
                    EnterIntro();
                break;

            case State.Intro:
                TurnTowards(target.position, turnSpeed);

                if (Time.time >= stateEndTime)
                    EnterChase();
                break;

            case State.Chase:
                UpdateChase();
                break;

            case State.Attack:
                UpdateAttack();
                break;

            case State.Recover:
                TurnTowards(target.position, turnSpeed);

                if (Time.time >= stateEndTime)
                    EnterChase();
                break;
        }

        ReportSpeed();
    }

    void UpdateChase()
    {
        if (agent.isOnNavMesh)
            agent.SetDestination(target.position);

        Attack chosen = PickAttack();

        if (chosen != null)
        {
            EnterAttack(chosen);
            return;
        }

        // the agent stops short of the player and stops turning, so he aims himself
        if (FlatDistance() <= agent.stoppingDistance + 0.1f)
            TurnTowards(target.position, turnSpeed);
    }

    void UpdateAttack()
    {
        float clipTime = Time.time - stateStartTime;

        // the player got away between hits. the rest of the move would hit air
        if (ShouldCancel(clipTime))
        {
            current.readyTime = Time.time + current.cooldown;
            CloseHitWindow();
            animator.SetTrigger(CancelHash);
            EnterChase();
            return;
        }

        if (InTurnWindow(clipTime))
        {
            Vector3 aim = target.position + targetVelocity * current.leadTime;

            TurnTowards(AimFacing(aim), current.turnSpeed);
            Lunge(aim);
        }

        if (Time.time < stateEndTime)
            return;

        current.readyTime = Time.time + current.cooldown;
        CloseHitWindow();
        EnterRecover();
    }

    // a point to face so the aim point sits at the move's strike angle
    Vector3 AimFacing(Vector3 aim)
    {
        Vector3 direction = aim - transform.position;
        direction.y = 0f;

        float strikeAngle = current.aimAngleByDistance.Evaluate(direction.magnitude);

        // the player sits strikeAngle off his front, so his front turns the other way
        return transform.position + Quaternion.Euler(0f, -strikeAngle, 0f) * direction;
    }

    // a move that ends early never reaches its ClawHitEnd event
    void CloseHitWindow()
    {
        if (claws != null)
            claws.ClawHitEnd();
    }

    bool InTurnWindow(float clipTime)
    {
        foreach (Vector2 window in current.turnWindows)
        {
            if (clipTime >= window.x && clipTime <= window.y)
                return true;
        }

        return false;
    }

    // each cancel point is checked once, when the clip passes it
    bool ShouldCancel(float clipTime)
    {
        if (cancelChecksDone >= current.cancelPoints.Length)
            return false;

        if (clipTime < current.cancelPoints[cancelChecksDone])
            return false;

        cancelChecksDone++;

        return FlatDistance() > current.cancelRange;
    }

    // steps towards the aim point until the player is within the ideal range
    void Lunge(Vector3 aim)
    {
        if (current.lungeSpeed <= 0f || !agent.enabled)
            return;

        Vector3 direction = aim - transform.position;
        direction.y = 0f;

        float gap = direction.magnitude - current.idealRange;

        if (gap <= 0f)
            return;

        float step = Mathf.Min(current.lungeSpeed * Time.deltaTime, gap);

        // through the agent so the step stays on the NavMesh
        agent.Move(direction.normalized * step);
    }

    // smoothed so a single jittery frame does not swing the aim
    void TrackTargetVelocity()
    {
        if (Time.deltaTime <= 0f)
            return;

        Vector3 velocity = (target.position - lastTargetPosition) / Time.deltaTime;
        velocity.y = 0f;

        targetVelocity = Vector3.Lerp(targetVelocity, velocity, 10f * Time.deltaTime);
        lastTargetPosition = target.position;
    }

    // random pick among the moves that fit the player's distance and angle
    Attack PickAttack()
    {
        float distance = FlatDistance();
        float angle = AngleToTarget();

        Attack chosen = null;
        int fits = 0;

        foreach (Attack attack in attacks)
        {
            if (Time.time < attack.readyTime)
                continue;

            if (distance < attack.minRange || distance > attack.maxRange)
                continue;

            if (angle > attack.maxAngle)
                continue;

            // each fitting move has the same chance
            fits++;

            if (Random.Range(0, fits) == 0)
                chosen = attack;
        }

        return chosen;
    }

    void EnterWaiting()
    {
        state = State.Waiting;

        // no damage before the fight. a blocked shot still wakes him
        health.Invulnerable = true;

        Stop();
    }

    void EnterIntro()
    {
        state = State.Intro;
        stateEndTime = Time.time + roarDuration;

        // no damage and no hit flash while he roars
        health.Invulnerable = true;

        Stop();
        animator.SetTrigger("Roar");
    }

    void EnterChase()
    {
        state = State.Chase;
        health.Invulnerable = false;

        if (agent.isOnNavMesh)
            agent.isStopped = false;
    }

    void EnterAttack(Attack attack)
    {
        state = State.Attack;
        current = attack;
        stateStartTime = Time.time;
        stateEndTime = Time.time + attack.duration;
        cancelChecksDone = 0;

        Stop();

        // a cancel left over from the last move would cut this one short
        animator.ResetTrigger(CancelHash);
        animator.SetTrigger(attack.triggerName);
    }

    void EnterRecover()
    {
        state = State.Recover;
        stateEndTime = Time.time + recoverTime;
    }

    // a shot starts the fight from any distance
    void HandleDamageBlocked()
    {
        if (state == State.Waiting)
            EnterIntro();
    }

    void HandleDied()
    {
        state = State.Dead;

        // a trigger set just before death would fire after it
        animator.ResetTrigger("Roar");
        animator.ResetTrigger(CancelHash);

        foreach (Attack attack in attacks)
            animator.ResetTrigger(attack.triggerName);

        animator.SetTrigger("Death");

        CloseHitWindow();
        Stop();
        agent.enabled = false;

        // the body falls 1.5 m behind where he stood
        if (bodyBlocker != null)
            bodyBlocker.enabled = false;
    }

    void Stop()
    {
        if (!agent.isOnNavMesh)
            return;

        agent.isStopped = true;
        agent.velocity = Vector3.zero;
    }

    // replaces the Animator's own root motion. only attacks that step use it
    void OnAnimatorMove()
    {
        if (state != State.Attack || !current.rootMotion || !agent.enabled)
            return;

        Vector3 step = animator.deltaPosition;
        step.y = 0f;

        // through the agent so the step stays on the NavMesh
        agent.Move(step);
    }

    void TurnTowards(Vector3 point, float degreesPerSecond)
    {
        Vector3 direction = point - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion wanted = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, wanted,
                                                      degreesPerSecond * Time.deltaTime);
    }

    bool NoticesTarget()
    {
        if (FlatDistance() > detectionRange)
            return false;

        Vector3 eye = transform.position + Vector3.up * eyeHeight;
        Vector3 targetEye = target.position + Vector3.up * targetEyeHeight;

        return !Physics.Linecast(eye, targetEye, sightBlockers, QueryTriggerInteraction.Ignore);
    }

    float FlatDistance()
    {
        Vector3 offset = target.position - transform.position;
        offset.y = 0f;
        return offset.magnitude;
    }

    float AngleToTarget()
    {
        Vector3 direction = target.position - transform.position;
        direction.y = 0f;
        return Vector3.Angle(transform.forward, direction);
    }

    void ReportSpeed()
    {
        // real speed. the blend tree thresholds are the clips' own speeds
        float speed = agent.enabled ? agent.velocity.magnitude : 0f;
        animator.SetFloat(SpeedHash, speed, 0.1f, Time.deltaTime);

        // past the Run clip's speed the blend tree is all Run, so the clip speeds up instead
        animator.SetFloat(AnimSpeedHash, Mathf.Max(1f, speed / runClipSpeed), 0.1f, Time.deltaTime);
    }

    // red sight, orange attack ranges
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = new Color(1f, 0.5f, 0f, 1f);

        foreach (Attack attack in attacks)
            Gizmos.DrawWireSphere(transform.position, attack.maxRange);
    }
}

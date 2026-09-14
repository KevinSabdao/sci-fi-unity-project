using UnityEngine;
using UnityEngine.AI;

namespace COMP602
{
    // Enemy movement brain. Idles and wanders slowly around its spawn point
    // until it notices the player, then switches to a faster chase. Attacks
    // are not handled here: EnemyAttackState decides those, and this script
    // stays out of the way while one is running.
    //
    // DETECTION
    // Three independent ways of noticing the player, any one is enough:
    // sight, which is a cone limited by View Angle and Detection Range;
    // hearing, a smaller sphere that works in any direction, standing in for
    // footsteps; and damage, which alerts instantly from any range and
    // pursues for Alert Duration even if the shot came from far away.
    // Staying behind the enemy and outside Hearing Range keeps you unseen.
    //
    // The cone follows the head bone on a humanoid rig, so the idle head
    // sweep changes what the enemy can see. Head Forward Offset corrects
    // the bone axis, which on Mixamo rigs rarely points at the face.
    //
    // SETUP
    // Goes on the enemy root. Needs a NavMeshAgent, an Animator with a Float
    // parameter named "Speed", and a baked NavMesh in the scene. The player
    // must be tagged "Player" unless Target is filled in.
    // EnemyHealth is optional: with it present, being hit alerts the enemy.
    // Without it, the other two senses still work.
    //
    // TROUBLESHOOTING
    // Enemy never moves: no baked NavMesh, or it spawned off the mesh.
    // Enemy notices through walls: turn on Require Line Of Sight and set
    // Sight Blockers to the layers walls live on.
    // Enemy flickers between walk and run: Lose Sight Range is too close to
    // Detection Range. Keep a few metres between them.
    // Sneaking never works: Hearing Range may be larger than the room, or
    // View Angle is near 360.
    // Shot from far away is ignored: Alert Duration is too short for the
    // enemy to cover the distance before the chase times out.
    // Cone points the wrong way: adjust Head Forward Offset. 180 on Y for
    // the nape, 90 on X for the top of the head.
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyAI : MonoBehaviour
    {
        enum State { Idle, Wander, Chase }

        // who to chase, empty finds the object tagged Player
        [SerializeField] Transform target;

        [Header("Sight")]
        // how far the enemy can see inside its view cone
        [SerializeField] float detectionRange = 15f;

        // total width of the view cone in degrees, 360 sees everything
        [Range(0f, 360f)]
        [SerializeField] float viewAngle = 110f;

        // larger than detectionRange, so the edge does not flip the state
        [SerializeField] float loseSightRange = 22f;

        // off lets the enemy see through walls, handy in a test scene
        [SerializeField] bool requireLineOfSight;

        // layers that block sight, leave the enemy and player layers out
        [SerializeField] LayerMask sightBlockers = ~0;

        // fallback eye height, used only when there is no head bone
        [SerializeField] float eyeHeight = 1.5f;

        // correction for the head bone forward axis, set it by eye
        [SerializeField] Vector3 headForwardOffset = Vector3.zero;

        [Header("Hearing")]
        // noticed at this distance regardless of facing, keep it small
        [SerializeField] float hearingRange = 4f;

        [Header("Alert")]
        // guaranteed pursuit after being hit, ignoring range
        [SerializeField] float alertDuration = 8f;

        [Header("Speeds")]
        [SerializeField] float wanderSpeed = 0.6f;
        [SerializeField] float chaseSpeed = 3.5f;

        [Header("Wander")]
        // how far from its spawn point the enemy will roam
        [SerializeField] float wanderRadius = 12f;

        // seconds walking towards a point before giving up on it
        [SerializeField] float wanderTimeout = 15f;

        [Header("Idle")]
        // seconds standing still between wander points
        [SerializeField] float idleDurationMin = 2f;
        [SerializeField] float idleDurationMax = 6f;

        NavMeshAgent agent;
        Animator animator;
        EnemyAttackState attackState;
        EnemyHealth health;

        // null on a non-humanoid rig, the body is used instead
        Transform head;

        State state = State.Idle;
        Vector3 spawnPoint;

        // when the current Idle ends, or the current Wander gives up
        float stateEndTime;

        // below this the chase cannot be broken by range
        float alertUntil;

        // public so the debug drawing matches what is actually tested
        public Vector3 EyePosition => head != null
            ? head.position
            : transform.position + Vector3.up * eyeHeight;

        public Vector3 EyeForward
        {
            get
            {
                if (head == null)
                    return transform.forward;

                Vector3 forward = Quaternion.Euler(headForwardOffset) * head.forward;

                // flattened, looking up or down must not narrow the cone
                forward.y = 0f;

                return forward.sqrMagnitude < 0.001f
                    ? transform.forward
                    : forward.normalized;
            }
        }

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();
            attackState = GetComponent<EnemyAttackState>();
            health = GetComponent<EnemyHealth>();

            if (animator != null && animator.isHuman)
                head = animator.GetBoneTransform(HumanBodyBones.Head);

            spawnPoint = transform.position;
        }

        void Start()
        {
            // optional, being shot alerts the enemy
            if (health != null)
                health.OnDamaged.AddListener(HandleDamaged);

            if (target == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");

                if (player != null)
                    target = player.transform;
            }

            if (target == null)
            {
                Debug.LogError($"{nameof(EnemyAI)}: no target. Assign one, or tag " +
                               "the player object as Player.", this);
                enabled = false;
                return;
            }

            EnterIdle();
        }

        void OnDestroy()
        {
            if (health != null)
                health.OnDamaged.RemoveListener(HandleDamaged);
        }

        void Update()
        {
            if (attackState != null && attackState.IsAttacking)
            {
                // chase speed whatever the state, reporting zero would make
                // the blend tree climb from idle once the attack releases
                ReportSpeed(chaseSpeed);

                // keeps the path current, so the agent is not a frame behind
                if (state == State.Chase && agent.isOnNavMesh && !agent.pathPending)
                    agent.SetDestination(target.position);

                return;
            }

            switch (state)
            {
                case State.Idle:
                    UpdateIdle();
                    break;

                case State.Wander:
                    UpdateWander();
                    break;

                case State.Chase:
                    UpdateChase();
                    break;
            }
        }

        void UpdateIdle()
        {
            if (NoticesTarget())
            {
                EnterChase();
                return;
            }

            ReportSpeed(0f);

            if (Time.time < stateEndTime)
                return;

            EnterWander();
        }

        void UpdateWander()
        {
            if (NoticesTarget())
            {
                EnterChase();
                return;
            }

            ReportSpeed(agent.velocity.magnitude);

            // unreachable point or a blocked path, stop waiting on it
            if (Time.time >= stateEndTime)
            {
                EnterIdle();
                return;
            }

            if (agent.pathPending)
                return;

            if (agent.remainingDistance > agent.stoppingDistance)
                return;

            EnterIdle();
        }

        void UpdateChase()
        {
            // the alert window holds the chase open regardless of distance
            bool alerted = Time.time < alertUntil;

            if (!alerted &&
                Vector3.Distance(transform.position, target.position) > loseSightRange)
            {
                EnterIdle();
                return;
            }

            agent.SetDestination(target.position);

            // intended speed rather than measured, the Animator must not dip
            // through the walk range while the agent accelerates
            ReportSpeed(chaseSpeed);
        }

        void EnterIdle()
        {
            state = State.Idle;

            agent.speed = wanderSpeed;

            // clearing the path stops the agent drifting during the pause
            if (agent.isOnNavMesh)
                agent.ResetPath();

            stateEndTime = Time.time + Random.Range(idleDurationMin, idleDurationMax);
        }

        void EnterWander()
        {
            Vector3 random = spawnPoint + Random.insideUnitSphere * wanderRadius;

            // the random point is almost never on the NavMesh, so this snaps
            // it to the nearest walkable spot, failing is normal near an edge
            if (!NavMesh.SamplePosition(random, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
            {
                EnterIdle();
                return;
            }

            state = State.Wander;

            agent.speed = wanderSpeed;
            agent.SetDestination(hit.position);

            stateEndTime = Time.time + wanderTimeout;
        }

        void EnterChase()
        {
            state = State.Chase;
            agent.speed = chaseSpeed;
        }

        // being hit alerts the enemy wherever the shot came from, and opens
        // the alert window so the chase survives the distance check
        void HandleDamaged()
        {
            alertUntil = Time.time + alertDuration;

            if (state == State.Chase)
                return;

            EnterChase();
        }

        // sight inside the cone, or hearing in any direction
        bool NoticesTarget()
        {
            float distance = Vector3.Distance(transform.position, target.position);

            if (distance <= hearingRange)
                return HasLineOfSight();

            if (distance > detectionRange)
                return false;

            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;

            // half the cone on each side of where the head is facing
            if (Vector3.Angle(EyeForward, toTarget) > viewAngle * 0.5f)
                return false;

            return HasLineOfSight();
        }

        bool HasLineOfSight()
        {
            if (!requireLineOfSight)
                return true;

            Vector3 eye = EyePosition;
            Vector3 targetEye = target.position + Vector3.up * eyeHeight;

            return !Physics.Linecast(eye, targetEye, sightBlockers);
        }

        void ReportSpeed(float value)
        {
            // damped rather than set, so the blend tree eases between clips
            if (animator != null)
            {
                float normalised = chaseSpeed > 0f ? value / chaseSpeed : 0f;
                animator.SetFloat("Speed", normalised, 0.15f, Time.deltaTime);
            }
        }

        // Scene view only, see EnemyDebugRanges for the Game view version.
        // Sight cone in red, lose-sight in yellow, hearing in magenta and
        // wander area in cyan.
        void OnDrawGizmosSelected()
        {
            Vector3 eye = Application.isPlaying
                ? EyePosition
                : transform.position + Vector3.up * eyeHeight;

            Vector3 forward = Application.isPlaying ? EyeForward : transform.forward;

            Gizmos.color = Color.red;

            Vector3 left = Quaternion.Euler(0f, -viewAngle * 0.5f, 0f) * forward;
            Vector3 right = Quaternion.Euler(0f, viewAngle * 0.5f, 0f) * forward;

            Gizmos.DrawRay(eye, left * detectionRange);
            Gizmos.DrawRay(eye, right * detectionRange);
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, loseSightRange);

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, hearingRange);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(Application.isPlaying ? spawnPoint : transform.position,
                                  wanderRadius);
        }
    }
}

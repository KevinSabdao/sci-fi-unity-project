using UnityEngine;
using UnityEngine.AI;

namespace COMP602
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyAI : MonoBehaviour
    {
        enum State { Idle, Wander, Chase, Return }

        [Tooltip("Who to chase. Empty finds the object tagged Player.")]
        [SerializeField] Transform target;

        [Header("Sight")]
        [Tooltip("How far he sees inside the view cone.")]
        [SerializeField] float detectionRange = 15f;

        [Range(0f, 360f)]
        [Tooltip("Width of the view cone in degrees. 360 is all round.")]
        [SerializeField] float viewAngle = 110f;

        [Tooltip("Distance at which he drops a chase.")]
        [SerializeField] float loseSightRange = 22f;

        [Tooltip("Off lets him see through walls.")]
        [SerializeField] bool requireLineOfSight;

        [Tooltip("Layers that block sight. Leave the enemy and player layers out.")]
        [SerializeField] LayerMask sightBlockers = ~0;

        [Tooltip("Eye height on the player. Also his own, with no head bone.")]
        [SerializeField] float eyeHeight = 1.5f;

        [Tooltip("Corrects the head bone forward axis.")]
        [SerializeField] Vector3 headForwardOffset = Vector3.zero;

        [Header("Hearing")]
        [Tooltip("Heard at this distance in any direction.")]
        [SerializeField] float hearingRange = 4f;

        [Header("Alert")]
        [Tooltip("Pursuit window opened by a hit. Ignores range and the leash.")]
        [SerializeField] float alertDuration = 8f;

        [Header("Speeds")]
        [Tooltip("Patrol speed.")]
        [SerializeField] float wanderSpeed = 0.6f;
        [Tooltip("Chase and run home speed.")]
        [SerializeField] float chaseSpeed = 3.5f;

        [Header("Leash")]
        [Tooltip("He drops a chase this far from his spawn point.")]
        [SerializeField] float leashRange = 25f;

        // stops him hesitating at the edge
        [Tooltip("Seconds ignoring the player after the leash ends a chase.")]
        [SerializeField] float leashIgnoreDuration = 1.5f;

        // off: getting close to the player also breaks it
        [Tooltip("On, only a chase opened by damage can break the leash.")]
        [SerializeField] bool leashBrokenByDamageOnly;

        // seconds the player must stay out of Hearing Range to restore the leash
        [Tooltip("Seconds of distance before a broken leash is restored.")]
        [SerializeField] float caughtUpTimeout = 6f;

        [Header("Stuck")]
        // covers a player where the NavMesh can't reach
        [Tooltip("Seconds of chasing without closing in before he gives up.")]
        [SerializeField] float stuckTimeout = 4f;

        [Tooltip("Seconds ignoring an unreachable player. A shot cuts it short.")]
        [SerializeField] float stuckIgnoreDuration = 10f;

        [Header("Return")]
        [Tooltip("Distance from spawn at which he runs home instead of wandering.")]
        [SerializeField] float runHomeDistance = 15f;

        [Header("Wander")]
        [Tooltip("How far from its spawn point he will roam.")]
        [SerializeField] float wanderRadius = 12f;

        [Tooltip("Seconds walking to a patrol point before dropping it.")]
        [SerializeField] float wanderTimeout = 15f;

        [Header("Idle")]
        [Tooltip("Shortest pause between patrol points.")]
        [SerializeField] float idleDurationMin = 2f;
        [Tooltip("Longest pause between patrol points.")]
        [SerializeField] float idleDurationMax = 6f;

        NavMeshAgent agent;
        Animator animator;
        EnemyAttackState attackState;
        EnemyHealth health;

        // null on non-humanoid rigs, uses the body
        Transform head;

        State state = State.Idle;
        Vector3 spawnPoint;

        // end of the current Idle or Wander
        float stateEndTime;

        // chase ignores range and leash until this time
        float alertUntil;

        // when the chase stopped moving, NegativeInfinity while moving
        float stuckSince = float.NegativeInfinity;

        // player ignored until this time
        float ignoreTargetUntil;

        // got close during this chase, suspends the leash
        bool caughtUp;

        // last time the player was in Hearing Range during a chase
        float lastCloseTime;

        public bool IsChasing => state == State.Chase;

        // chase stalled for Stuck Timeout
        public bool IsStuckChasing => state == State.Chase
            && stuckSince > float.NegativeInfinity
            && Time.time >= stuckSince + stuckTimeout;

        // public so the debug drawing uses the same values
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

                // flattened so looking up or down keeps the cone width
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
            // being shot alerts the enemy
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
                // keeps chase speed so the blend tree doesn't restart from idle
                // after the attack
                ReportSpeed(chaseSpeed);

                // keeps the path updated during the attack
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

                case State.Return:
                    UpdateReturn();
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

            // timed out, unreachable point or blocked path
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
            // alert keeps the chase going at any distance
            bool alerted = Time.time < alertUntil;

            if (!alerted &&
                Vector3.Distance(transform.position, target.position) > loseSightRange)
            {
                GiveUpChase(0f);
                return;
            }

            bool close = Vector3.Distance(transform.position, target.position) <= hearingRange;

            // getting close commits him to the chase
            if (close && (alerted || !leashBrokenByDamageOnly))
            {
                caughtUp = true;
                lastCloseTime = Time.time;
            }
            else if (close)
            {
                lastCloseTime = Time.time;
            }

            // commitment ends once the player stays away for Caught Up Timeout
            if (caughtUp && Time.time >= lastCloseTime + caughtUpTimeout)
                caughtUp = false;

            // leash keeps him near spawn, skipped while alerted or caught up
            if (!alerted && !caughtUp &&
                Vector3.Distance(transform.position, spawnPoint) > leashRange)
            {
                GiveUpChase(leashIgnoreDuration);
                return;
            }

            agent.SetDestination(target.position);

            // intended speed so the animation doesn't dip into walk while
            // accelerating
            ReportSpeed(chaseSpeed);

            TrackProgress();
        }

        // gives up when the chase stops moving
        // the agent waits at the NavMesh edge when the player is out of reach
        void TrackProgress()
        {
            bool closingIn = agent.velocity.sqrMagnitude > 0.04f;

            if (closingIn || agent.pathPending)
            {
                stuckSince = float.NegativeInfinity;
                return;
            }

            if (stuckSince <= float.NegativeInfinity)
                stuckSince = Time.time;

            if (Time.time < stuckSince + stuckTimeout)
                return;

            GiveUpChase(stuckIgnoreDuration);
        }

        // ends the chase, runs home if far from spawn
        // the ignore window stops an instant re-chase
        void GiveUpChase(float ignoreDuration)
        {
            if (ignoreDuration > 0f)
                ignoreTargetUntil = Time.time + ignoreDuration;

            if (Vector3.Distance(transform.position, spawnPoint) > runHomeDistance)
            {
                EnterReturn();
                return;
            }

            EnterIdle();
        }

        // runs back to spawn, then patrols again
        void UpdateReturn()
        {
            if (NoticesTarget())
            {
                EnterChase();
                return;
            }

            ReportSpeed(chaseSpeed);

            if (agent.pathPending)
                return;

            if (agent.remainingDistance > agent.stoppingDistance)
                return;

            EnterIdle();
        }

        void EnterIdle()
        {
            state = State.Idle;
            stuckSince = float.NegativeInfinity;
            caughtUp = false;
            lastCloseTime = Time.time;

            agent.speed = wanderSpeed;

            // clears the path so he stops in place
            if (agent.isOnNavMesh)
                agent.ResetPath();

            stateEndTime = Time.time + Random.Range(idleDurationMin, idleDurationMax);
        }

        void EnterWander()
        {
            Vector3 random = spawnPoint + Random.insideUnitSphere * wanderRadius;

            // snaps the random point to the NavMesh, can fail near edges
            if (!NavMesh.SamplePosition(random, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
            {
                EnterIdle();
                return;
            }

            state = State.Wander;
            stuckSince = float.NegativeInfinity;
            caughtUp = false;
            lastCloseTime = Time.time;

            agent.speed = wanderSpeed;
            agent.SetDestination(hit.position);

            stateEndTime = Time.time + wanderTimeout;
        }

        void EnterChase()
        {
            state = State.Chase;
            stuckSince = float.NegativeInfinity;
            caughtUp = false;
            lastCloseTime = Time.time;
            agent.speed = chaseSpeed;
        }

        void EnterReturn()
        {
            state = State.Return;
            stuckSince = float.NegativeInfinity;
            caughtUp = false;
            lastCloseTime = Time.time;

            agent.speed = chaseSpeed;
            agent.SetDestination(spawnPoint);
        }

        // a hit alerts him from any range
        void HandleDamaged()
        {
            alertUntil = Time.time + alertDuration;

            // a shot ends the ignore window
            ignoreTargetUntil = 0f;

            if (state == State.Chase)
                return;

            EnterChase();
        }

        // sight inside the cone, or hearing in any direction
        bool NoticesTarget()
        {
            if (Time.time < ignoreTargetUntil)
                return false;

            float distance = Vector3.Distance(transform.position, target.position);

            if (distance <= hearingRange)
                return HasLineOfSight();

            if (distance > detectionRange)
                return false;

            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;

            // half the cone on each side of the head
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
            // damped so the blend tree eases between clips
            if (animator != null)
            {
                float normalised = chaseSpeed > 0f ? value / chaseSpeed : 0f;
                animator.SetFloat("Speed", normalised, 0.15f, Time.deltaTime);
            }
        }

        // warns about values that conflict
        void OnValidate()
        {
            if (leashRange <= wanderRadius)
            {
                Debug.LogWarning($"{nameof(EnemyAI)}: Leash Range should be above Wander " +
                                 "Radius, or he gives up chasing inside his own patrol " +
                                 "area.", this);
            }

            if (leashRange <= loseSightRange)
            {
                Debug.LogWarning($"{nameof(EnemyAI)}: Leash Range should be above Lose " +
                                 "Sight Range, or the leash ends ordinary chases instead " +
                                 "of only the long ones.", this);
            }

            if (loseSightRange <= detectionRange)
            {
                Debug.LogWarning($"{nameof(EnemyAI)}: Lose Sight Range should sit a few " +
                                 "metres above Detection Range, or he flips between " +
                                 "chasing and patrolling at the edge.", this);
            }
        }

        // scene view only, EnemyDebugRanges draws in the Game view
        // red sight, yellow lose sight, magenta hearing, cyan wander, green leash
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

            Vector3 home = Application.isPlaying ? spawnPoint : transform.position;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(home, wanderRadius);

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(home, leashRange);
        }
    }
}
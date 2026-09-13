using UnityEngine;
using UnityEngine.AI;

namespace COMP602
{
    // Turns the enemy towards the player while it is attacking or staggering.
    // The NavMeshAgent normally handles rotation, but it stops doing so while
    // isStopped is true, which is exactly when the attacks run.
    public class EnemyFaceTarget : MonoBehaviour
    {
        // who to face, empty finds the object tagged Player
        [SerializeField] Transform target;

        [Header("Turning")]
        // degrees per second, low values let the player circle away
        [SerializeField] float turnSpeed = 120f;

        // close enough to count as already facing the player
        [SerializeField] float angleTolerance = 2f;

        EnemyAttackState attackState;
        NavMeshAgent agent;

        void Awake()
        {
            attackState = GetComponent<EnemyAttackState>();
            agent = GetComponent<NavMeshAgent>();
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
                Debug.LogError($"{nameof(EnemyFaceTarget)}: no target. Assign one, or tag " +
                               "the player object as Player.", this);
                enabled = false;
            }
        }

        void Update()
        {
            if (!ShouldTurn())
                return;

            Vector3 direction = target.position - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f)
                return;

            Quaternion wanted = Quaternion.LookRotation(direction);

            if (Quaternion.Angle(transform.rotation, wanted) < angleTolerance)
                return;

            transform.rotation = Quaternion.RotateTowards(transform.rotation, wanted,
                                                          turnSpeed * Time.deltaTime);
        }

        bool ShouldTurn()
        {
            // only while an attack or a stagger holds the gate
            if (attackState != null && attackState.IsAttacking)
                return true;

            // without EnemyAttackState, a stopped agent is the only cue
            if (attackState == null && agent != null && agent.isStopped)
                return true;

            return false;
        }
    }
}

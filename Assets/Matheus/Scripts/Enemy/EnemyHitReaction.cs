using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace COMP602
{
    // Plays a stagger animation when this enemy takes damage, and holds it
    // still while it plays. Shares the gate with the attacks, so it never
    // cuts into one.
    public class EnemyHitReaction : MonoBehaviour
    {
        [Header("Timing")]
        // held this long, should match the stagger clip
        [SerializeField] float staggerDuration = 0.6f;

        // absorbs bursts from multi-pellet weapons
        [SerializeField] float cooldown = 1.5f;
        
        [Header("Chance")]
        // odds of staggering on a qualifying hit
        [Range(0f, 1f)]
        [SerializeField] float staggerChance = 0.33f;

        [Header("Behaviour")]
        // keeps the stagger from sliding across the floor
        [SerializeField] bool stopWhileStaggering = true;

        EnemyHealth health;
        EnemyAttackState attackState;
        NavMeshAgent agent;
        Animator animator;

        float nextStaggerTime;
        bool staggering;

        void Awake()
        {
            health = GetComponent<EnemyHealth>();
            attackState = GetComponent<EnemyAttackState>();
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();
        }

        void OnEnable()
        {
            if (health != null)
                health.OnDamaged.AddListener(HandleDamaged);
        }

        void OnDisable()
        {
            if (health != null)
                health.OnDamaged.RemoveListener(HandleDamaged);

            // releases the gate if this enemy dies mid-stagger
            if (staggering && attackState != null)
                attackState.End(this);
        }

        void HandleDamaged()
        {
            if (staggering)
                return;

            if (Time.time < nextStaggerTime)
                return;

            // rolled before reserving, a failed roll must not block an attack
            if (Random.value > staggerChance)
                return;

            // busy attacking or recovering, no stagger this time
            if (attackState != null && !attackState.TryReserve(this))
                return;

            StartCoroutine(Stagger());
        }

        IEnumerator Stagger()
        {
            staggering = true;
            nextStaggerTime = Time.time + cooldown;

            if (stopWhileStaggering && agent != null)
                agent.isStopped = true;

            if (animator != null)
                animator.SetTrigger("HitReaction");

            yield return new WaitForSeconds(staggerDuration);

            if (stopWhileStaggering && agent != null)
                agent.isStopped = false;

            staggering = false;

            if (attackState != null)
                attackState.End(this);
        }
    }
}

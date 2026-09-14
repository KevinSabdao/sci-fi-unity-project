using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace COMP602
{
    // Melee moves for one enemy. Holds them as variations and runs whichever
    // one EnemyAttackState picks, so this script never attacks on its own.
    public class EnemyMeleeAttack : MonoBehaviour, IEnemyAttack
    {
        // One melee move. Referenced by index from EnemyAttackState.
        [System.Serializable]
        public class Variation
        {
            // inspector label only
            public string label = "Melee";

            // must match a Trigger parameter in the Animator Controller
            public string triggerName = "MeleeAttack";

            public float damage = 10f;

            // seconds from the animation starting until the hit lands
            [ReadOnly]
            public float damageDelay = 1f;

            // total length of the move
            [ReadOnly]
            public float duration = 2.6f;

            // extra seconds planted for a recovery animation such as a roar
            [ReadOnly]
            public float recoveryTime = 2.8f;

            // push applied on hit in metres per second, 0 for none
            public float knockback = 3f;
        }

        // who to attack, empty finds the object tagged Player
        [SerializeField] Transform target;

        [Header("Variations")]
        [SerializeField] Variation[] variations = new Variation[0];

        [Header("Range")]
        // checked when the hit lands, backing off mid-swing dodges it
        [SerializeField] float damageRange = 2.5f;

        PlayerHealth targetHealth;
        FirstPersonMovement targetMovement;
        EnemyAttackState attackState;
        NavMeshAgent agent;
        Animator animator;

        bool attacking;

        void Start()
        {
            animator = GetComponent<Animator>();
            agent = GetComponent<NavMeshAgent>();
            attackState = GetComponent<EnemyAttackState>();

            if (variations.Length == 0)
            {
                Debug.LogError($"{nameof(EnemyMeleeAttack)}: no variations set up. Add at " +
                               "least one in the Inspector.", this);
                enabled = false;
                return;
            }

            if (target == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");

                if (player != null)
                    target = player.transform;
            }

            if (target == null)
            {
                Debug.LogError($"{nameof(EnemyMeleeAttack)}: no target. Assign one, or tag " +
                               "the player object as Player.", this);
                enabled = false;
                return;
            }

            targetHealth = target.GetComponentInParent<PlayerHealth>();
            targetMovement = target.GetComponentInParent<FirstPersonMovement>();

            if (targetHealth == null)
            {
                Debug.LogError($"{nameof(EnemyMeleeAttack)}: target has no PlayerHealth.", this);
                enabled = false;
            }
        }

        // entry point called by EnemyAttackState
        public void BeginAttack(int variantIndex)
        {
            if (attacking)
                return;

            if (variantIndex < 0 || variantIndex >= variations.Length)
            {
                Debug.LogError($"{nameof(EnemyMeleeAttack)}: variant index {variantIndex} " +
                               $"does not exist. There are {variations.Length}.", this);

                if (attackState != null)
                    attackState.End(this);

                return;
            }

            StartCoroutine(Attack(variations[variantIndex]));
        }

        IEnumerator Attack(Variation move)
        {
            attacking = true;

            if (agent != null)
                agent.isStopped = true;

            if (animator != null)
                animator.SetTrigger(move.triggerName);

            yield return new WaitForSeconds(move.damageDelay);

            float distance = Vector3.Distance(transform.position, target.position);

            if (distance <= damageRange)
            {
                targetHealth.TakeDamage(move.damage);
                PushPlayer(move.knockback);
            }

            // the rest of the move plays out before movement resumes
            yield return new WaitForSeconds(Mathf.Max(move.duration - move.damageDelay, 0f));
            
            // the recovery animation needs the body to stay still
            yield return new WaitForSeconds(move.recoveryTime);

            if (agent != null)
                agent.isStopped = false;

            attacking = false;

            if (attackState != null)
                attackState.End(this);
        }

        void PushPlayer(float force)
        {
            if (targetMovement == null || force <= 0f)
                return;

            // flattened before normalising, the push stays horizontal
            Vector3 direction = target.position - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f)
                return;

            targetMovement.AddImpulse(direction.normalized * force);
        }

        // frees the gate if this enemy dies mid-swing
        void OnDisable()
        {
            if (attackState != null)
                attackState.End(this);
        }

        // draws the damage range in the Scene view
        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 1f);
            Gizmos.DrawWireSphere(transform.position, damageRange);
        }
    }
}

using UnityEngine;

namespace COMP602
{
    // Implemented by every attack this enemy can perform.
    public interface IEnemyAttack
    {
        // variantIndex picks the move, for scripts holding more than one
        void BeginAttack(int variantIndex);
    }

    // Picks which attack runs and when. Acts as a gate that only one thing may
    // hold at a time, so attacks and hit reactions never overlap.
    public class EnemyAttackState : MonoBehaviour
    {
        [System.Serializable]
        public class Option
        {
            // inspector label only
            public string label = "Attack";

            // the same component can appear more than once, with another variant
            public MonoBehaviour attack;

            // which move inside that component, 0 if it only has one
            public int variantIndex;

            // higher values are picked more often, 0 disables
            public float weight = 1f;
        }

        // who to attack, empty finds the object tagged Player
        [SerializeField] Transform target;

        [Header("Attacks")]
        [SerializeField] Option[] options = new Option[0];

        [Header("Range")]
        // how close the player must be for any attack to start
        [SerializeField] float attackRange = 2f;

        [Header("Timing")]
        // seconds between attacks
        [SerializeField] float cooldown = 2f;

        // the gate frees itself if an attack never calls End
        [SerializeField] float maxAttackDuration = 8f;

        float nextAttackTime;
        float attackStartTime;
        MonoBehaviour owner;

        public bool IsAttacking { get; private set; }

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
                Debug.LogError($"{nameof(EnemyAttackState)}: no target. Assign one, or tag " +
                               "the player object as Player.", this);
                enabled = false;
                return;
            }

            // caught here rather than silently doing nothing at runtime
            foreach (Option o in options)
            {
                if (o.attack == null)
                    continue;

                if (o.attack is IEnemyAttack)
                    continue;

                Debug.LogError($"{nameof(EnemyAttackState)}: '{o.label}' points at " +
                               $"{o.attack.GetType().Name}, which is not an attack.", this);
            }
        }

        void Update()
        {
            if (IsAttacking)
            {
                WatchForStuckAttack();
                return;
            }

            if (Time.time < nextAttackTime)
                return;

            float distance = Vector3.Distance(transform.position, target.position);

            if (distance > attackRange)
                return;

            Option chosen = Pick();

            if (chosen == null)
                return;

            Begin(chosen);
        }

        // weighted random pick, null when nothing is usable
        Option Pick()
        {
            float total = 0f;

            foreach (Option o in options)
            {
                if (o.attack is IEnemyAttack)
                    total += Mathf.Max(o.weight, 0f);
            }

            if (total <= 0f)
                return null;

            float roll = Random.Range(0f, total);
            float running = 0f;

            foreach (Option o in options)
            {
                if (!(o.attack is IEnemyAttack))
                    continue;

                running += Mathf.Max(o.weight, 0f);

                if (roll <= running)
                    return o;
            }

            return null;
        }
        
        // reserves the gate for a non-attack, such as a hit reaction
        public bool TryReserve(MonoBehaviour user)
        {
            if (IsAttacking)
                return false;

            if (Time.time < nextAttackTime)
                return false;

            IsAttacking = true;
            attackStartTime = Time.time;
            owner = user;

            return true;
        }

        void Begin(Option option)
        {
            IsAttacking = true;
            attackStartTime = Time.time;
            owner = option.attack;

            ((IEnemyAttack)option.attack).BeginAttack(option.variantIndex);
        }

        // ignored unless the caller owns the gate, so late calls are harmless
        public void End(MonoBehaviour attack)
        {
            if (owner != attack)
                return;

            Free();
        }

        void WatchForStuckAttack()
        {
            if (Time.time < attackStartTime + maxAttackDuration)
                return;

            Debug.LogWarning($"{nameof(EnemyAttackState)}: attack by {owner} never " +
                             "released the gate. Freeing it.", this);

            Free();
        }

        void Free()
        {
            IsAttacking = false;
            nextAttackTime = Time.time + cooldown;
            owner = null;
        }

        // draws the attack range in the Scene view
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}

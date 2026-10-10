using UnityEngine;

namespace COMP602
{
    // fires a projectile from an Animation Event on the scream clip
    // the scream plays after every attack, so it only fires when armed
    // arms itself after a run of missed melee hits
    public class EnemySpitAttack : MonoBehaviour
    {
        [SerializeField] Transform target;

        [Header("Spit")]
        [SerializeField] GameObject projectilePrefab;

        // where the projectile appears, a child of the head bone
        [SerializeField] Transform spawnPoint;

        [Header("Trigger")]
        // missed melee hits in a row before he spits
        // a new value is rolled between these after every spit
        [SerializeField] int missesBeforeSpitMin = 3;
        [SerializeField] int missesBeforeSpitMax = 5;

        EnemyMeleeAttack melee;

        // missed melee hits since the last hit or spit
        int missStreak;

        // rolled in Awake, then again after every spit
        int missesBeforeSpit;

        bool armed;

        void Awake()
        {
            melee = GetComponent<EnemyMeleeAttack>();

            RollMissesBeforeSpit();
        }

        void OnEnable()
        {
            if (melee != null)
                melee.HitResolved += HandleHitResolved;
        }

        void OnDisable()
        {
            if (melee != null)
                melee.HitResolved -= HandleHitResolved;
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
                Debug.LogError($"{nameof(EnemySpitAttack)}: no target. Assign one, or tag " +
                               "the player object as Player.", this);
                enabled = false;
            }
        }

        // the scream after this hit spits once enough misses pile up
        void HandleHitResolved(bool connected)
        {
            missStreak = connected ? 0 : missStreak + 1;

            if (missStreak < missesBeforeSpit)
                return;

            missStreak = 0;
            RollMissesBeforeSpit();

            armed = true;
        }

        void RollMissesBeforeSpit()
        {
            int low = Mathf.Max(missesBeforeSpitMin, 1);
            int high = Mathf.Max(missesBeforeSpitMax, low);

            missesBeforeSpit = Random.Range(low, high + 1);
        }

        // called by an Animation Event on the scream clip
        public void FireProjectile()
        {
            if (!armed)
                return;

            armed = false;

            if (projectilePrefab == null || spawnPoint == null || target == null)
                return;

            Vector3 direction = target.position + Vector3.up * 1.2f - spawnPoint.position;

            Instantiate(projectilePrefab, spawnPoint.position, Quaternion.LookRotation(direction));
        }
    }
}
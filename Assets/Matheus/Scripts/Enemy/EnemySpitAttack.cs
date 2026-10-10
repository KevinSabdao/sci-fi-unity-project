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

        [Header("Infection")]
        // chance for each spit hit to infect, 0 to rely on the guarantee only
        [SerializeField, Range(0f, 100f)] float infectionChancePercent = 10f;

        // dodged spits in a row before the next hit is certain to infect
        // a new value is rolled between these after every infection, 0 and 0 turns it off
        [SerializeField] int missesBeforeInfectionMin = 3;
        [SerializeField] int missesBeforeInfectionMax = 5;

        EnemyMeleeAttack melee;

        // missed melee hits since the last hit or spit
        int missStreak;

        // rolled in Awake, then again after every spit
        int missesBeforeSpit;

        // dodged spits in a row, any hit resets it
        int spitMisses;

        // rolled in Awake, then again after every infection, 0 when turned off
        int missesBeforeInfection;

        bool armed;

        void Awake()
        {
            melee = GetComponent<EnemyMeleeAttack>();

            RollMissesBeforeSpit();
            RollMissesBeforeInfection();
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

        // called by EnemySpitProjectile when it misses the player
        public void HandleSpitMissed()
        {
            spitMisses++;

            // TEMP infection debug
            Debug.Log($"[Infection] spit missed, dodge streak {spitMisses}/{missesBeforeInfection}", this);
        }

        // called by EnemySpitProjectile when it hits the player
        public void HandleSpitHit(PlayerStatusEffects status)
        {
            // no status component on the player, nothing to infect
            if (status == null)
                return;

            // dodging too many spits in a row makes the next hit certain
            bool guaranteed = missesBeforeInfection > 0 && spitMisses >= missesBeforeInfection;
            float roll = Random.value * 100f;
            bool lucky = roll < infectionChancePercent;

            // TEMP infection debug
            Debug.Log($"[Infection] spit hit, roll {roll:0.0} vs chance {infectionChancePercent}%, " +
                      $"dodge streak {spitMisses}/{missesBeforeInfection} -> " +
                      (guaranteed ? "INFECTED by dodge streak" : lucky ? "INFECTED by chance" : "not infected"), this);

            // the hit breaks the dodge streak either way
            spitMisses = 0;

            if (!guaranteed && !lucky)
                return;

            RollMissesBeforeInfection();

            status.Infect();
        }

        void RollMissesBeforeInfection()
        {
            if (missesBeforeInfectionMax <= 0)
            {
                missesBeforeInfection = 0;
                return;
            }

            int low = Mathf.Max(missesBeforeInfectionMin, 1);
            int high = Mathf.Max(missesBeforeInfectionMax, low);

            missesBeforeInfection = Random.Range(low, high + 1);
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

            GameObject projectile = Instantiate(projectilePrefab, spawnPoint.position,
                                                Quaternion.LookRotation(direction));

            if (projectile.TryGetComponent(out EnemySpitProjectile spit))
                spit.Init(this);
        }
    }
}
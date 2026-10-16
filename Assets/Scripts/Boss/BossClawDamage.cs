using UnityEngine;

// talon damage, only between the ClawHitStart and ClawHitEnd events of each attack
public class BossClawDamage : MonoBehaviour
{
    // one damaging window, opened by ClawHitStart with the same name
    [System.Serializable]
    public class Hit
    {
        // must match the string parameter of the ClawHitStart event
        public string name = "Claw1";

        public float damage = 10f;

        // push applied on hit in metres per second, 0 for none
        public float knockback = 2f;

        // chance for a hit to open a deep wound on the player, such as the Swipe. 0 never
        [Range(0f, 100f)]
        public float deepWoundChancePercent;
    }

    [System.Serializable]
    public class Talon
    {
        public Transform bone;

        // the tip sits this far along the bone's Y axis
        [ReadOnly]
        public float length = 1f;
    }

    [Header("Talons")]
    [SerializeField] Talon[] talons = new Talon[0];

    // points along each talon that cut, as shares of its length, 0.5 and up is the outer half
    [SerializeField] float[] cuttingPoints = { 0.5f, 0.75f, 1f };

    // thickness of the cutting edge in metres
    [SerializeField] float edgeRadius = 0.1f;

    // only these layers take damage, the player's
    [SerializeField] LayerMask targetLayers;

    [Header("Hits")]
    [SerializeField] Hit[] hits =
    {
        new Hit { name = "Claw1", damage = 8f, knockback = 2f },
        new Hit { name = "Claw2", damage = 12f, knockback = 2.5f },
        new Hit { name = "Claw3", damage = 18f, knockback = 4f },
        new Hit { name = "Swipe", damage = 25f, knockback = 6f, deepWoundChancePercent = 50f },
    };

    // closes a window whose end event never came
    [SerializeField] float maxWindow = 0.5f;

    Hit active;
    float activeUntil;

    // one hit per window, however many talons pass through the player
    bool landed;

    Vector3[] lastPoints;
    readonly Collider[] overlaps = new Collider[4];

    void Reset()
    {
        targetLayers = LayerMask.GetMask("Player");
    }

    void Awake()
    {
        lastPoints = new Vector3[talons.Length * cuttingPoints.Length];
    }

    // called by an Animation Event as the claw starts to cut
    public void ClawHitStart(string hitName)
    {
        active = null;

        foreach (Hit hit in hits)
        {
            if (hit.name == hitName)
                active = hit;
        }

        if (active == null)
        {
            Debug.LogWarning($"{nameof(BossClawDamage)}: no hit named '{hitName}'. Add it under " +
                             "Hits or fix the event parameter.", this);
            return;
        }

        landed = false;
        activeUntil = Time.time + maxWindow;

        RecordPoints();
    }

    // called by an Animation Event as the claw stops cutting, and by BossAI when a move ends early
    public void ClawHitEnd()
    {
        active = null;
    }

    // after the Animator has posed the bones for this frame
    void LateUpdate()
    {
        if (active == null)
            return;

        if (Time.time > activeUntil)
        {
            active = null;
            return;
        }

        if (!landed)
            Sweep();

        RecordPoints();
    }

    // the tips move up to 1 m a frame, so each point sweeps from where it was the frame before
    void Sweep()
    {
        int i = 0;

        foreach (Talon talon in talons)
        {
            foreach (float share in cuttingPoints)
            {
                Vector3 from = lastPoints[i++];
                Vector3 to = CuttingPoint(talon, share);

                if (TryHit(from, to))
                    return;
            }
        }
    }

    bool TryHit(Vector3 from, Vector3 to)
    {
        Vector3 path = to - from;
        float distance = path.magnitude;

        if (distance > 0.0001f &&
            Physics.SphereCast(from, edgeRadius, path / distance, out RaycastHit hit, distance,
                               targetLayers, QueryTriggerInteraction.Ignore))
        {
            return Land(hit.collider);
        }

        // a cast that starts inside the player does not report it
        int count = Physics.OverlapSphereNonAlloc(to, edgeRadius, overlaps, targetLayers,
                                                  QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            if (Land(overlaps[i]))
                return true;
        }

        return false;
    }

    bool Land(Collider other)
    {
        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();

        if (health == null)
            return false;

        landed = true;

        health.TakeDamage(active.damage);
        Push(other.GetComponentInParent<FirstPersonMovement>());

        PlayerStatusEffects status = other.GetComponentInParent<PlayerStatusEffects>();

        if (status != null && Random.value * 100f < active.deepWoundChancePercent)
            status.ApplyDeepWound();

        // bleeding on the last combo hit goes here once it exists

        return true;
    }

    void Push(FirstPersonMovement movement)
    {
        if (movement == null || active.knockback <= 0f)
            return;

        // away from the boss and flattened, the push stays horizontal
        Vector3 direction = movement.transform.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        movement.AddImpulse(direction.normalized * active.knockback);
    }

    void RecordPoints()
    {
        int i = 0;

        foreach (Talon talon in talons)
        {
            foreach (float share in cuttingPoints)
                lastPoints[i++] = CuttingPoint(talon, share);
        }
    }

    static Vector3 CuttingPoint(Talon talon, float share)
    {
        return talon.bone.TransformPoint(0f, talon.length * share, 0f);
    }

    // the cutting part of each talon, red while a window is open
    void OnDrawGizmosSelected()
    {
        Gizmos.color = active != null ? Color.red : new Color(1f, 0.6f, 0f, 1f);

        foreach (Talon talon in talons)
        {
            if (talon.bone == null)
                continue;

            foreach (float share in cuttingPoints)
                Gizmos.DrawWireSphere(CuttingPoint(talon, share), edgeRadius);
        }
    }
}

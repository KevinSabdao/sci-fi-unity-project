using System;
using UnityEngine;
using UnityEngine.Events;

// Hit points for one enemy. Raises events on damage and death, so sounds,
// drops and score can react without this script knowing about them.
public class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] float maxHealth = 100f;

    [Header("Death")]
    // off for enemies that stay as a body, such as the boss
    [SerializeField] bool destroyOnDeath = true;

    // keeps the object alive while the death animation plays
    [SerializeField] float destroyDelay = 0f;

    // wired in the Inspector for sounds, drops and score
    public UnityEvent OnDied;
    public UnityEvent OnDamaged;

    // for listeners not tied to one enemy, such as UI
    public static event Action<EnemyHealth, float> AnyDamaged;
    public static event Action<EnemyHealth> AnyDied;

    public float Current { get; private set; }
    public float Max => maxHealth;
    public bool IsAlive => Current > 0f;

    // ignores all damage while on, set from code such as the boss before the fight and during its opening roar
    public bool Invulnerable { get; set; }

    // a hit that Invulnerable ignored. no damage and no OnDamaged, so no hit flash
    public event Action DamageBlocked;

    // DamageZone multiplier of the last hit, read by UI on OnDamaged
    public float LastDamageMultiplier { get; private set; } = 1f;

    // the last hit landed on a DamageZone marked critical, such as the boss eye
    public bool LastHitCritical { get; private set; }

    void Awake()
    {
        Current = maxHealth;
    }

    public void TakeDamage(float amount) => TakeDamage(amount, null);

    // hitCollider is the collider the weapon struck, its DamageZone scales the damage
    public void TakeDamage(float amount, Collider hitCollider)
    {
        if (!IsAlive)
            return;

        if (Invulnerable)
        {
            DamageBlocked?.Invoke();
            return;
        }

        float multiplier = 1f;
        bool critical = false;

        if (hitCollider != null && hitCollider.TryGetComponent(out DamageZone zone))
        {
            multiplier = zone.Multiplier;
            critical = zone.Critical;
        }

        amount *= multiplier;

        if (amount <= 0f)
            return;

        LastDamageMultiplier = multiplier;
        LastHitCritical = critical;

        Current = Mathf.Max(Current - amount, 0f);

        OnDamaged?.Invoke();
        AnyDamaged?.Invoke(this, amount);

        if (Current <= 0f)
            Die();
    }

    public void Heal(float amount)
    {
        if (!IsAlive)
            return;

        if (amount <= 0f)
            return;

        Current = Mathf.Min(Current + amount, maxHealth);
    }

    void Die()
    {
        OnDied?.Invoke();
        AnyDied?.Invoke(this);

        if (destroyOnDeath)
            Destroy(gameObject, destroyDelay);
    }
}

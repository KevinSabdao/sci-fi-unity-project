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

    // ignores all damage while on, set from code such as the boss during its opening roar
    public bool Invulnerable { get; set; }

    // DamageZone multiplier of the last hit, above 1 means a weak point, read by UI on OnDamaged
    public float LastDamageMultiplier { get; private set; } = 1f;

    void Awake()
    {
        Current = maxHealth;
    }

    public void TakeDamage(float amount) => TakeDamage(amount, null);

    // hitCollider is the collider the weapon struck, its DamageZone scales the damage
    public void TakeDamage(float amount, Collider hitCollider)
    {
        if (!IsAlive || Invulnerable)
            return;

        float multiplier = 1f;

        if (hitCollider != null && hitCollider.TryGetComponent(out DamageZone zone))
            multiplier = zone.Multiplier;

        amount *= multiplier;

        if (amount <= 0f)
            return;

        LastDamageMultiplier = multiplier;

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

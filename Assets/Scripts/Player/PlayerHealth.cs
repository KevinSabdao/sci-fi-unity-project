using System;
using UnityEngine;
using UnityEngine.Events;

// Hit points for the player. Running out respawns instead of ending the
// game, and raises events so UI and sounds can react.
public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] float maxHealth = 100f;

    [Header("Respawn")]
    // empty respawns at the position the player started in
    [SerializeField] Transform respawnPoint;

    public UnityEvent OnDamaged;
    public UnityEvent OnRespawned;

    // for listeners not attached to the player, such as UI
    public static event Action<PlayerHealth, float> AnyDamaged;
    public static event Action<PlayerHealth> AnyRespawned;

    // raised by any heal above zero, even at full health
    public event Action Healed;

    // max health taken by effects such as infection, RestoreMax gives it back
    float maxHealthLoss;

    public float Current { get; private set; }
    public float Max => maxHealth - maxHealthLoss;
    public bool IsAlive => Current > 0f;

    // max health with nothing taken away
    public float BaseMax => maxHealth;

    CharacterController controller;
    Vector3 startPosition;

    void Awake()
    {
        Current = maxHealth;
        startPosition = transform.position;
        controller = GetComponent<CharacterController>();

        // added for the unit tests: a player built from code has these empty. a scene or prefab fills them in
        // without them PlayerStatusEffects never hears about a respawn or a hit
        OnDamaged ??= new UnityEvent();
        OnRespawned ??= new UnityEvent();
    }

    public void TakeDamage(float amount)
    {
        if (!IsAlive)
            return;

        if (amount <= 0f)
            return;

        Current = Mathf.Max(Current - amount, 0f);

        OnDamaged?.Invoke();
        AnyDamaged?.Invoke(this, amount);

        if (Current <= 0f)
            Respawn();
    }

    public void Heal(float amount)
    {
        if (!IsAlive)
            return;

        if (amount <= 0f)
            return;

        Current = Mathf.Min(Current + amount, Max);

        Healed?.Invoke();
    }

    // lowers max health, never below 1, current health above it is cut down
    public void ReduceMax(float amount)
    {
        if (amount <= 0f)
            return;

        maxHealthLoss = Mathf.Min(maxHealthLoss + amount, maxHealth - 1f);
        Current = Mathf.Min(Current, Max);
    }

    // gives back all max health taken by ReduceMax, current health stays the same
    public void RestoreMax()
    {
        maxHealthLoss = 0f;
    }

    // health lost to status effects, skips OnDamaged so hit feedback only
    // plays on real hits
    public void Drain(float amount)
    {
        if (!IsAlive)
            return;

        if (amount <= 0f)
            return;

        Current = Mathf.Max(Current - amount, 0f);

        if (Current <= 0f)
            Respawn();
    }

    void Respawn()
    {
        Vector3 target = respawnPoint != null ? respawnPoint.position : startPosition;

        // the controller overrides transform changes, so it goes off first
        if (controller != null)
            controller.enabled = false;

        transform.position = target;

        if (controller != null)
            controller.enabled = true;

        maxHealthLoss = 0f;
        Current = maxHealth;

        OnRespawned?.Invoke();
        AnyRespawned?.Invoke(this);
    }
}

using UnityEngine;

// stamina for sprinting, drains while sprinting and recovers after a short rest
// running out blocks sprint until it climbs back to Recover Percent
[RequireComponent(typeof(FirstPersonMovement))]
public class PlayerStamina : MonoBehaviour
{
    // off gives endless sprint and hides the bar
    [SerializeField] bool useStamina = true;

    [SerializeField] float maxStamina = 100f;

    // used per second of sprinting
    [SerializeField] float drainPerSecond = 20f;

    // recovered per second once recovery starts
    [SerializeField] float regenPerSecond = 25f;

    // seconds after the last sprint before it starts to recover
    [SerializeField] float regenDelay = 1f;

    // after running out, sprint comes back at this share of the max
    [SerializeField, Range(0f, 100f)] float recoverPercent = 30f;

    FirstPersonMovement movement;
    PlayerHealth health;

    float lastSprintTime = float.NegativeInfinity;

    public bool UseStamina => useStamina;
    public float Current { get; private set; }
    public float Max => maxStamina;

    // 0 to 1, for the bar
    public float Fraction => maxStamina > 0f ? Current / maxStamina : 0f;

    // ran out and has not climbed back to Recover Percent yet
    public bool IsExhausted { get; private set; }

    public float RecoverFraction => recoverPercent / 100f;

    public bool CanSprint => !useStamina || (!IsExhausted && Current > 0f);

    void Awake()
    {
        movement = GetComponent<FirstPersonMovement>();
        health = GetComponent<PlayerHealth>();

        Refill();
    }

    void OnEnable()
    {
        if (health != null)
            health.OnRespawned?.AddListener(Refill);
    }

    void OnDisable()
    {
        if (health != null)
            health.OnRespawned?.RemoveListener(Refill);
    }

    public void Refill()
    {
        Current = maxStamina;
        IsExhausted = false;
    }

    void Update()
    {
        if (!useStamina)
        {
            Refill();
            return;
        }

        // only real sprinting drains, holding the key while standing still does not
        if (movement.IsSprinting)
        {
            Current = Mathf.Max(Current - drainPerSecond * Time.deltaTime, 0f);
            lastSprintTime = Time.time;

            if (Current <= 0f)
                IsExhausted = true;

            return;
        }

        if (Time.time < lastSprintTime + regenDelay)
            return;

        Current = Mathf.Min(Current + regenPerSecond * Time.deltaTime, maxStamina);

        if (IsExhausted && Fraction >= RecoverFraction)
            IsExhausted = false;
    }
}

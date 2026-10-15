using System;
using UnityEngine;

// timed effects on the player: infection and deep wound
// each effect gets its own settings class and section in the inspector
[RequireComponent(typeof(PlayerHealth))]
public class PlayerStatusEffects : MonoBehaviour
{
    [Serializable]
    public class InfectionSettings
    {
        // max health lost on each tick, current health only drops once it is at max
        public float maxHealthLossPerTick = 1f;

        // seconds between ticks
        public float lossInterval = 10f;

        // max health stops dropping at this share of the original
        [Range(0f, 100f)]
        public float maxHealthFloorPercent = 50f;

        // health the vaccine gives back on each tick, until all the infection took is back
        public float vaccineHealPerTick = 1f;

        // seconds between vaccine ticks
        public float vaccineHealInterval = 2f;

        // PlayerScreenEffects entry shown while infected
        public ScreenEffect screenEffect = ScreenEffect.Infected;
    }

    [SerializeField] InfectionSettings infection = new InfectionSettings();

    // the HUD shows health draining over the timer but nothing is lost unless it runs out
    // holding the mend key cures it with no loss
    [Serializable]
    public class DeepWoundSettings
    {
        // share of max health lost when the timer runs out, locked in when the wound is dealt
        [Range(0f, 100f)]
        public float lossPercent = 30f;

        // seconds to mend before the loss is real
        public float duration = 15f;

        // seconds the key has to be held in total. letting go keeps the progress
        public float mendTime = 4f;

        // seconds without mending before the progress starts to drop
        public float mendDecayDelay = 2f;

        // seconds of progress lost per second once it drops
        public float mendDecayPerSecond = 1f;

        public KeyCode mendKey = KeyCode.X;

        // how far the camera sinks while mending, in metres
        public float mendCameraDip = 0.25f;

        // PlayerScreenEffects entry shown while wounded
        public ScreenEffect screenEffect = ScreenEffect.DeepWound;
    }

    [SerializeField] DeepWoundSettings deepWound = new DeepWoundSettings();

    PlayerHealth health;

    // optional, without it infection has no screen effect
    PlayerScreenEffects screenEffects;

    // optional, holds the player still while mending
    FirstPersonMovement movement;

    // optional, sinks while mending
    Transform cameraTransform;
    Vector3 cameraRestPosition;
    float cameraDip;

    float nextLossTime;

    // current health taken by this infection, the vaccine gives it back
    float healthTaken;

    // health the vaccine still has to give back
    float vaccineHealLeft;
    float nextVaccineHealTime;

    // lasts until max health hits the floor, the vaccine or a respawn
    public bool IsInfected { get; private set; }

    // infection reached the floor and ended, max health stays low and no new infection takes until the vaccine
    public bool IsInfectionBurnedOut { get; private set; }

    // no new infection while the vaccine is still giving health back
    public bool IsImmune => vaccineHealLeft > 0f;

    // for the HUD countdown
    public float SecondsToNextLoss => Mathf.Max(nextLossTime - Time.time, 0f);

    // raised with true when infection starts, false when it ends
    public event Action<bool> InfectionChanged;

    // raised when the vaccine cures, for the HUD message
    public event Action Vaccinated;

    // health the deep wound takes if the timer runs out
    float deepWoundLoss;
    float deepWoundTimeLeft;
    float mendProgress;

    // a hit stops the mend until the key is pressed again
    bool mendBlocked;

    float lastMendTime;

    public bool IsDeepWounded { get; private set; }

    // the mend key is held and counting
    public bool IsMending { get; private set; }

    public float DeepWoundSecondsLeft => IsDeepWounded ? Mathf.Max(deepWoundTimeLeft, 0f) : 0f;

    // 0 to 1, for the mend circle
    public float MendFraction => deepWound.mendTime > 0f ? Mathf.Clamp01(mendProgress / deepWound.mendTime) : 0f;

    public KeyCode MendKey => deepWound.mendKey;

    // the part of the loss the HUD shows as already gone, it grows as the timer runs
    public float DeepWoundShownLoss =>
        IsDeepWounded && deepWound.duration > 0f
            ? deepWoundLoss * (1f - deepWoundTimeLeft / deepWound.duration)
            : 0f;

    // raised with true when a deep wound starts, false when it ends either way
    public event Action<bool> DeepWoundChanged;

    // raised when the wound is mended in time, for the HUD message
    public event Action DeepWoundMended;

    float MaxHealthFloor => health.BaseMax * infection.maxHealthFloorPercent / 100f;

    void Awake()
    {
        health = GetComponent<PlayerHealth>();
        screenEffects = GetComponent<PlayerScreenEffects>();
        movement = GetComponent<FirstPersonMovement>();

        Camera playerCamera = GetComponentInChildren<Camera>();

        if (playerCamera != null)
        {
            cameraTransform = playerCamera.transform;
            cameraRestPosition = cameraTransform.localPosition;
        }
    }

    void OnEnable()
    {
        health.OnRespawned?.AddListener(CureAll);
        health.OnDamaged?.AddListener(InterruptMend);
    }

    void OnDisable()
    {
        health.OnRespawned?.RemoveListener(CureAll);
        health.OnDamaged?.RemoveListener(InterruptMend);

        CureAll();
    }

    // every effect at once, such as on respawn
    public void CureAll()
    {
        Cure();
        EndDeepWound();
    }

    [ContextMenu("Infect")]
    public void Infect()
    {
        if (!health.IsAlive)
            return;

        // a new infection on top of one, after one burned out or while immune does nothing
        if (IsInfected || IsInfectionBurnedOut || IsImmune)
            return;

        IsInfected = true;
        healthTaken = 0f;
        nextLossTime = Time.time + infection.lossInterval;

        if (screenEffects != null)
            screenEffects.SetActive(infection.screenEffect, true);

        InfectionChanged?.Invoke(true);
    }

    // the only cure, gives max health back at once and what the infection took from health slowly
    // false when not infected or burned out, so a vaccine item is not used up
    public bool Vaccinate()
    {
        if (!IsInfected && !IsInfectionBurnedOut)
            return false;

        float taken = healthTaken;

        Cure();

        vaccineHealLeft = taken;
        nextVaccineHealTime = Time.time + infection.vaccineHealInterval;

        Vaccinated?.Invoke();

        return true;
    }

    // ends the infection and gives max health back, health taken stays lost
    public void Cure()
    {
        vaccineHealLeft = 0f;

        if (!IsInfected && !IsInfectionBurnedOut)
            return;

        IsInfectionBurnedOut = false;
        healthTaken = 0f;

        health.RestoreMax();

        EndInfection();
    }

    // stops the losses and hides the infection, max health stays where it is
    void EndInfection()
    {
        if (!IsInfected)
            return;

        IsInfected = false;

        if (screenEffects != null)
            screenEffects.SetActive(infection.screenEffect, false);

        InfectionChanged?.Invoke(false);
    }

    // inspector menu entry, for testing without a vaccine item
    [ContextMenu("Vaccinate")]
    void VaccinateFromMenu() => Vaccinate();

    // a second wound on top of one does nothing
    [ContextMenu("Deep Wound")]
    public void ApplyDeepWound()
    {
        if (!health.IsAlive || IsDeepWounded)
            return;

        IsDeepWounded = true;
        deepWoundLoss = health.Max * deepWound.lossPercent / 100f;
        deepWoundTimeLeft = deepWound.duration;
        mendProgress = 0f;
        mendBlocked = false;

        if (screenEffects != null)
            screenEffects.SetActive(deepWound.screenEffect, true);

        DeepWoundChanged?.Invoke(true);
    }

    // ends the wound with no loss
    void EndDeepWound()
    {
        StopMending();

        if (!IsDeepWounded)
            return;

        IsDeepWounded = false;
        deepWoundLoss = 0f;

        if (screenEffects != null)
            screenEffects.SetActive(deepWound.screenEffect, false);

        DeepWoundChanged?.Invoke(false);
    }

    void Update()
    {
        if (IsInfected && Time.time >= nextLossTime)
            InfectionTick();

        if (vaccineHealLeft > 0f && Time.time >= nextVaccineHealTime)
            VaccineTick();

        if (IsDeepWounded)
            UpdateDeepWound();
    }

    void UpdateDeepWound()
    {
        bool held = Input.GetKey(deepWound.mendKey) && !FirstPersonLook.UiHasCursor;

        // letting go after a hit lets the next press mend again
        if (!held)
            mendBlocked = false;

        if (held && !mendBlocked)
            StartMending();
        else
            StopMending();

        // the timer stops while mending
        if (IsMending)
        {
            mendProgress += Time.deltaTime;
            lastMendTime = Time.time;

            if (mendProgress >= deepWound.mendTime)
            {
                EndDeepWound();
                DeepWoundMended?.Invoke();
            }

            return;
        }

        // progress left alone too long starts to slip away
        if (Time.time > lastMendTime + deepWound.mendDecayDelay)
            mendProgress = Mathf.Max(mendProgress - deepWound.mendDecayPerSecond * Time.deltaTime, 0f);

        deepWoundTimeLeft -= Time.deltaTime;

        if (deepWoundTimeLeft > 0f)
            return;

        // the timer ran out. the loss is real and can kill
        float loss = deepWoundLoss;

        EndDeepWound();
        health.Drain(loss);
    }

    void StartMending()
    {
        if (IsMending)
            return;

        IsMending = true;

        // standing still while mending
        if (movement != null)
            movement.SetSpeedModifier(this, 0f);

        // hands busy: the weapons are put away and cannot fire, the same lock the zombie grab uses
        PlayerHands.Lock();
    }

    void StopMending()
    {
        if (!IsMending)
            return;

        IsMending = false;

        if (movement != null)
            movement.RemoveSpeedModifier(this);

        PlayerHands.Unlock();
    }

    // a hit while mending stops it. the progress is kept
    void InterruptMend()
    {
        if (!IsMending)
            return;

        mendBlocked = true;
        StopMending();
    }

    // after the look script, the camera sinks while mending and comes back after
    void LateUpdate()
    {
        if (cameraTransform == null)
            return;

        float wanted = IsMending ? deepWound.mendCameraDip : 0f;

        if (Mathf.Approximately(cameraDip, wanted) && wanted == 0f)
            return;

        cameraDip = Mathf.MoveTowards(cameraDip, wanted, Time.deltaTime);
        cameraTransform.localPosition = cameraRestPosition + Vector3.down * cameraDip;
    }

    void InfectionTick()
    {
        nextLossTime += infection.lossInterval;

        // never takes max health below the floor
        float amount = Mathf.Min(infection.maxHealthLossPerTick, health.Max - MaxHealthFloor);

        if (amount > 0.001f)
        {
            // current health only drops when the new max cuts it down
            float before = health.Current;

            health.ReduceMax(amount);

            healthTaken += before - health.Current;
        }

        // at the floor it burns out. the vaccine still gives everything back
        if (health.Max <= MaxHealthFloor + 0.001f)
        {
            IsInfectionBurnedOut = true;
            EndInfection();
        }
    }

    void VaccineTick()
    {
        nextVaccineHealTime += infection.vaccineHealInterval;

        float amount = Mathf.Min(infection.vaccineHealPerTick, vaccineHealLeft);

        vaccineHealLeft -= amount;
        health.Heal(amount);
    }
}

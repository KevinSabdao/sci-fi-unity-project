using System;
using UnityEngine;

// timed effects on the player, infection for now
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

    PlayerHealth health;

    // optional, without it infection has no screen effect
    PlayerScreenEffects screenEffects;

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

    float MaxHealthFloor => health.BaseMax * infection.maxHealthFloorPercent / 100f;

    void Awake()
    {
        health = GetComponent<PlayerHealth>();
        screenEffects = GetComponent<PlayerScreenEffects>();
    }

    void OnEnable()
    {
        health.OnRespawned?.AddListener(Cure);
    }

    void OnDisable()
    {
        health.OnRespawned?.RemoveListener(Cure);

        Cure();
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

    void Update()
    {
        if (IsInfected && Time.time >= nextLossTime)
            InfectionTick();

        if (vaccineHealLeft > 0f && Time.time >= nextVaccineHealTime)
            VaccineTick();
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

        // at the floor it burns out, the vaccine still gives everything back
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

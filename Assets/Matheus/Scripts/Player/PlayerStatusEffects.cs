using System;
using UnityEngine;

namespace COMP602
{
    // timed effects on the player, infection for now
    // each effect gets its own settings class and section in the inspector
    [RequireComponent(typeof(PlayerHealth))]
    public class PlayerStatusEffects : MonoBehaviour
    {
        [Serializable]
        public class InfectionSettings
        {
            // health lost on each tick
            public float damagePerTick = 1f;

            // seconds between ticks
            public float tickInterval = 2f;

            // seconds before it wears off, a new infection restarts it
            public float duration = 60f;

            // stops draining at this share of max health and cures itself
            [Range(0f, 100f)]
            public float healthFloorPercent = 30f;

            // PlayerScreenEffects entry shown while infected
            public ScreenEffect screenEffect = ScreenEffect.Infected;
        }

        [SerializeField] InfectionSettings infection = new InfectionSettings();

        PlayerHealth health;

        // optional, without it infection has no screen effect
        PlayerScreenEffects screenEffects;

        float infectedUntil;
        float nextTickTime;

        public bool IsInfected { get; private set; }

        // raised with true when infection starts, false when it ends
        public event Action<bool> InfectionChanged;

        float HealthFloor => health.Max * infection.healthFloorPercent / 100f;

        void Awake()
        {
            health = GetComponent<PlayerHealth>();
            screenEffects = GetComponent<PlayerScreenEffects>();
        }

        void OnEnable()
        {
            // any heal cures, and so does respawning
            health.Healed += Cure;
            health.OnRespawned?.AddListener(Cure);
        }

        void OnDisable()
        {
            health.Healed -= Cure;
            health.OnRespawned?.RemoveListener(Cure);

            Cure();
        }

        public void Infect()
        {
            if (!health.IsAlive)
                return;

            // already at the floor, it would cure on the first tick
            if (health.Current <= HealthFloor)
                return;

            infectedUntil = Time.time + infection.duration;

            if (IsInfected)
                return;

            IsInfected = true;
            nextTickTime = Time.time + infection.tickInterval;

            if (screenEffects != null)
                screenEffects.SetActive(infection.screenEffect, true);

            InfectionChanged?.Invoke(true);
        }

        public void Cure()
        {
            if (!IsInfected)
                return;

            IsInfected = false;

            if (screenEffects != null)
                screenEffects.SetActive(infection.screenEffect, false);

            InfectionChanged?.Invoke(false);
        }

        void Update()
        {
            if (!IsInfected)
                return;

            if (Time.time >= infectedUntil)
            {
                Cure();
                return;
            }

            if (Time.time < nextTickTime)
                return;

            nextTickTime += infection.tickInterval;

            // never drains past the floor
            float amount = Mathf.Min(infection.damagePerTick, health.Current - HealthFloor);

            if (amount > 0f)
                health.Drain(amount);

            if (health.Current <= HealthFloor + 0.001f)
                Cure();
        }
    }
}

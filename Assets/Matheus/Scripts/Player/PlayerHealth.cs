using System;
using UnityEngine;
using UnityEngine.Events;

namespace COMP602
{
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

        public float Current { get; private set; }
        public float Max => maxHealth;
        public bool IsAlive => Current > 0f;

        CharacterController controller;
        Vector3 startPosition;

        void Awake()
        {
            Current = maxHealth;
            startPosition = transform.position;
            controller = GetComponent<CharacterController>();
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

            Current = Mathf.Min(Current + amount, maxHealth);
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

            Current = maxHealth;

            OnRespawned?.Invoke();
            AnyRespawned?.Invoke(this);
        }
    }
}

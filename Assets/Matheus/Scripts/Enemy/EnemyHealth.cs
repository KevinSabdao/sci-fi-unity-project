using System;
using UnityEngine;
using UnityEngine.Events;

namespace COMP602
{
    // Hit points for one enemy. Raises events on damage and death, so sounds,
    // drops and score can react without this script knowing about them.
    public class EnemyHealth : MonoBehaviour
    {
        [Header("Health")]
        [SerializeField] float maxHealth = 100f;

        [Header("Death")]
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

        void Awake()
        {
            Current = maxHealth;
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

            Destroy(gameObject, destroyDelay);
        }
    }
}

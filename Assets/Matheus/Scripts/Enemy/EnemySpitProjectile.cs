using UnityEngine;

namespace COMP602
{
    // flies forward and damages the player on contact
    // spawned by EnemySpitAttack, destroys itself either way
    [RequireComponent(typeof(Rigidbody))]
    public class EnemySpitProjectile : MonoBehaviour
    {
        [SerializeField] float speed = 8f;

        [SerializeField] float damage = 10f;

        // push applied on hit in metres per second, 0 for none
        [SerializeField] float knockback = 4f;

        // seconds before it gives up and disappears
        [SerializeField] float lifetime = 5f;

        // the enemy that fired it, told whether it hit or missed the player
        EnemySpitAttack owner;

        public void Init(EnemySpitAttack shooter) => owner = shooter;

        void Start()
        {
            GetComponent<Rigidbody>().linearVelocity = transform.forward * speed;

            Invoke(nameof(Expire), lifetime);
        }

        void OnTriggerEnter(Collider other)
        {
            // the enemy that fired it should not be hit by it
            if (other.GetComponentInParent<EnemyHealth>() != null)
                return;

            PlayerHealth health = other.GetComponentInParent<PlayerHealth>();

            if (health == null)
            {
                // hit a wall or the floor
                Miss();
                return;
            }

            health.TakeDamage(damage);

            Push(other.GetComponentInParent<FirstPersonMovement>());

            // the shooter may have died while it was in the air
            if (owner != null)
                owner.HandleSpitHit(health.GetComponent<PlayerStatusEffects>());

            Destroy(gameObject);
        }

        // flew for its whole lifetime without hitting anything
        void Expire() => Miss();

        void Miss()
        {
            if (owner != null)
                owner.HandleSpitMissed();

            Destroy(gameObject);
        }

        void Push(FirstPersonMovement movement)
        {
            if (movement == null || knockback <= 0f)
                return;

            // flattened before normalising, the push stays horizontal
            Vector3 direction = transform.forward;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f)
                return;

            movement.AddImpulse(direction.normalized * knockback);
        }
    }
}

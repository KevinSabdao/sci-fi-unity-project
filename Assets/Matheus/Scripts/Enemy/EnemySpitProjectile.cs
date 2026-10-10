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

        // seconds before it gives up and disappears
        [SerializeField] float lifetime = 5f;

        void Start()
        {
            GetComponent<Rigidbody>().linearVelocity = transform.forward * speed;

            Destroy(gameObject, lifetime);
        }

        void OnTriggerEnter(Collider other)
        {
            // the enemy that fired it should not be hit by it
            if (other.GetComponentInParent<EnemyHealth>() != null)
                return;

            PlayerHealth health = other.GetComponentInParent<PlayerHealth>();

            if (health != null)
                health.TakeDamage(damage);

            Destroy(gameObject);
        }
    }
}
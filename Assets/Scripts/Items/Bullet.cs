using COMP602;
using UnityEngine;

// damage carrier for a fired round. the weapon that spawns it sets the amount,
// so one prefab serves every gun

public class Bullet : MonoBehaviour
{
    // overwritten by the Weapon that fires this bullet
    public float damage = 25f;

    // one round hurts an enemy once, a single collision can report several
    // contacts in the same frame
    private bool hasHit;

    private void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;

        // the muzzle sits on the player and the guns are on his layer too
        if (collision.collider.gameObject.layer == LayerMask.NameToLayer("Player")) return;

        // a burst spawns its rounds on the same spot in the same frame, so they
        // touch each other before anything else
        if (collision.collider.GetComponentInParent<Bullet>()) return;

        // health lives on the enemy root, the collider that was hit may be on
        // a child such as a limb
        EnemyHealth health = collision.collider.GetComponentInParent<EnemyHealth>();
        if (!health) return;

        hasHit = true;
        health.TakeDamage(damage);
    }
}

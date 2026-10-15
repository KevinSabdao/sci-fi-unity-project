using UnityEngine;

// damage multiplier for the collider on this object, read by EnemyHealth when it is hit
// above 1 for a weak point, below 1 for armour
public class DamageZone : MonoBehaviour
{
    [SerializeField] float multiplier = 1f;

    public float Multiplier => multiplier;
}

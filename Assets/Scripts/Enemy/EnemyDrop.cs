using UnityEngine;

// leaves an item where the enemy died
[RequireComponent(typeof(EnemyHealth))]
public class EnemyDrop : MonoBehaviour
{
    [SerializeField] GameObject dropPrefab;

    // chance to drop on each death
    [SerializeField, Range(0f, 100f)] float dropChancePercent = 100f;

    // metres above the enemy's feet, keeps the item off the floor
    [SerializeField] float dropHeight = 0.5f;

    EnemyHealth health;

    void Awake()
    {
        health = GetComponent<EnemyHealth>();
    }

    void OnEnable()
    {
        health.OnDied?.AddListener(Drop);
    }

    void OnDisable()
    {
        health.OnDied?.RemoveListener(Drop);
    }

    void Drop()
    {
        if (dropPrefab == null)
            return;

        if (Random.value * 100f >= dropChancePercent)
            return;

        Instantiate(dropPrefab, transform.position + Vector3.up * dropHeight, Quaternion.identity);
    }
}

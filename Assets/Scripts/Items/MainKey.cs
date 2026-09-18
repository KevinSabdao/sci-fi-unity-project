using UnityEngine;

public class MainKey : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        Inventory.AddKey("Key", 1);
        Destroy(gameObject);
    }
}

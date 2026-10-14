using COMP602;
using UnityEngine;

public class HealthItem : MonoBehaviour
{
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void OnTriggerEnter(Collider other)
    {

            PlayerHealth targetScript = other.GetComponent<PlayerHealth>();
            if (targetScript != null)
            {
                targetScript.Heal(20);
                Destroy(gameObject);
            }

        
    }
}

using System;
using UnityEngine;

public class HealthItem : MonoBehaviour
{
    
        [SerializeField] int HealAmount = 20;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void OnTriggerEnter(Collider other)
        {
            //trys to get the PlayerHeath script from collided object
            PlayerHealth targetScript = other.GetComponent<PlayerHealth>();
            //checks if the collided object contains player health script
            if (targetScript != null && targetScript.Current != targetScript.Max)
            {
                targetScript.Heal(HealAmount);
                Destroy(gameObject);
            }


        }
    
}

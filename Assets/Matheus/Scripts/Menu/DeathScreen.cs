using UnityEngine;

namespace COMP602
{
    public class DeathScreen : MonoBehaviour
    {
        [SerializeField] GameObject deathPanel;

        PlayerHealth deadPlayer;

        void OnEnable()
        {
            PlayerHealth.AnyDamaged += HandleDamaged;
        }

        void OnDisable()
        {
            PlayerHealth.AnyDamaged -= HandleDamaged;
        }

        void HandleDamaged(PlayerHealth health, float amount)
        {
            if (health.IsAlive)
                return;

            deadPlayer = health;
            deathPanel.SetActive(true);
            Time.timeScale = 0f;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            FirstPersonLook look = deadPlayer.GetComponent<FirstPersonLook>();
            if (look != null)
                look.enabled = false;
        }

        public void OnRespawnButtonPressed()
        {
            Time.timeScale = 1f;
            deadPlayer.Respawn();
            deathPanel.SetActive(false);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            FirstPersonLook look = deadPlayer.GetComponent<FirstPersonLook>();
            if (look != null)
                look.enabled = true;
        }

        public void OnQuitButtonPressed()
        {
            Debug.Log("Quit requested");
            Application.Quit();
        }
    }
}
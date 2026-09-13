using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public static bool isPaused = false;
    [SerializeField] GameObject gameUi;
    public GameObject pauseMenuUi;
    [SerializeField] GameObject player;
    [SerializeField] GameObject pauseCamera;


    //Previous implementation of invertoryList to print inventory 
    //[SerializeField] GameObject inventoryList;
    
    
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                Resume();
            }
            else
            {
                //Previous implementation of invertoryList to print inventory
                //inventoryList.GetComponent<TMPro.TMP_Text>().text = "";
                //for (int i = 0; i < Inventory.inventoryCounter; i++)
                //{
                //    inventoryList.GetComponent<TMPro.TMP_Text>().text += Inventory.inventoryList[i].itemName + " : " + Inventory.inventoryList[i].itemCount + "\n";
                //}
                Pause();
            }
        }

    }
    public void Resume()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        pauseCamera.SetActive(false);
        pauseMenuUi.SetActive(false);
        Time.timeScale = 1f;
        AudioListener.pause = false;
        isPaused = false;
        gameUi.SetActive(true);
    }
    public void Pause()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        pauseCamera.SetActive(true);
        Vector3 pausePos = player.transform.position;

        pausePos.y += 2;
        pausePos.x += 5;
        pauseCamera.transform.position = pausePos;

        pauseMenuUi.SetActive(true);
        Time.timeScale = 0f;
        AudioListener.pause = true;
        isPaused = true;
        gameUi.SetActive(false);
    }
    //Old system to go switch to main menu
    //public void LoadMainMenu(String sceneName)
    //{
    //    Time.timeScale = 1f;
    //    AudioListener.pause = false;
    //    isPaused = false;
    //    SceneManager.LoadScene(sceneName);
    //}

}

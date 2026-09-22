using System;
using COMP602;
using UnityEngine;
using UnityEngine.SceneManagement;



public class PauseMenu : MonoBehaviour
{
    public static bool isPaused = false;

    [SerializeField] GameObject player;
    [SerializeField] GameObject pauseCamera;
    

    //Old code from upskilling, Print inventory on pause as well as change ui
    //[SerializeField] GameObject inventoryList;
    //[SerializeField] GameObject gameUi;
    //public GameObject pauseMenuUi;

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
                //Old code from upskilling print inventory list
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
        // FirstPersonLook owns the cursor and Time.timeScale, see Inventory for
        // why neither is set directly here
        FirstPersonLook.PopUiModal();
        pauseCamera.SetActive(false);
        AudioListener.pause = false;
        isPaused = false;
        

        //Old code from upskilling change ui
        //pauseMenuUi.SetActive(false);
        //gameUi.SetActive(true);
    }
    public void Pause()
    {
        FirstPersonLook.PushUiModal();
        pauseCamera.SetActive(true);
        Vector3 pausePos = player.transform.position;

        pausePos.y += 2;
        pausePos.x += 5;
        pauseCamera.transform.position = pausePos;


        AudioListener.pause = true;
        isPaused = true;
        

        //Old code from upskilling change ui
        //pauseMenuUi.SetActive(true);
        //gameUi.SetActive(false);
    }
    
    //Old code from upskilling load main menu
    //public void LoadMainMenu(String sceneName)
    //{
    //    Time.timeScale = 1f;
    //    AudioListener.pause = false;
    //    isPaused = false;
    //    SceneManager.LoadScene(sceneName);
    //}

}



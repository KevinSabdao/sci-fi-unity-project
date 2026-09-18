using UnityEngine;

public class OpenDoor : MonoBehaviour
{
   

    public static bool key = false;
    void Update()
    {
        for (int i = 0; i < Inventory.keyCounter; i++)
        {
            if (Inventory.keyList[i].keyName == "Key")
            {
                key = true;
            }
        }
        if (Raycasting.distanceFromTarget < 5 && Raycasting.target == gameObject && Input.GetKeyDown(KeyCode.E) && key == true)
        {
            gameObject.GetComponentInParent<Animator>().Play("DoorOpen");
        }
    }
}

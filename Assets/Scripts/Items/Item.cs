using System.Collections;
using UnityEngine;

public abstract class Item : MonoBehaviour
{
    public bool isActive;
    private readonly string itemSlotLeft = "LeftArm"; 
    private readonly string itemSlotRight = "RightArm"; 

    public Vector3 spawnPosition; 
    public Vector3 spawnRotation; 

    internal KeyCode keyPressRequired { get; set; }

    internal void checkRequiredKeyPress()
    {
        if (this.transform.parent.gameObject.name == itemSlotLeft)
        {
            keyPressRequired = KeyCode.Mouse0;
        }
        else if (this.transform.parent.gameObject.name == itemSlotRight)
        {
            keyPressRequired = KeyCode.Mouse1;
        }
        else
        {
            return;
        }
    }
}

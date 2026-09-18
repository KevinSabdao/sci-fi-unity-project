using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public abstract class Item : MonoBehaviour
{
    public bool isActive { get; set; }
    public Sprite inventoryIcon;

    public string itemName;
    [TextArea]
    public string description;

    public Vector3 spawnPosition;
    public Vector3 spawnRotation;

    internal KeyCode keyPressRequired { get; set; }
}

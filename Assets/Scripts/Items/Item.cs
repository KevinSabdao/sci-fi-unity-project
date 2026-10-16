using System.Collections;
using UnityEngine;
using UnityEngine.UI;

internal enum ItemType
{
    Weapon,
    Cube
}

public abstract class Item : MonoBehaviour
{
    public bool isActive { get; set; }
    public Sprite inventoryIcon;

    public string itemName;
    [TextArea]
    public string description;
    [SerializeField]
    internal ItemType itemType;

    [SerializeField]
    internal int stackCountMax = 1;
    [SerializeField]
    internal int stackCount = 1;

    public Vector3 spawnPosition;
    public Vector3 spawnRotation;

    internal KeyCode keyPressRequired { get; set; }
}

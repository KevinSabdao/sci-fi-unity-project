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

    // Where the item sits in the hand, relative to the arm (mirrored for the left arm)
    public Vector3 holdPosition;
    public Vector3 holdRotation = new Vector3(0, 180, 0);

    internal KeyCode keyPressRequired { get; set; }

    // Mirror the hold for the left arm so both hands look the same
    private bool InLeftArm => transform.parent && transform.parent.localPosition.x < 0;

    internal Vector3 HoldLocalPosition()
    {
        return InLeftArm ? new Vector3(-holdPosition.x, holdPosition.y, holdPosition.z) : holdPosition;
    }

    internal Quaternion HoldLocalRotation()
    {
        Quaternion rotation = Quaternion.Euler(holdRotation);
        return InLeftArm ? new Quaternion(rotation.x, -rotation.y, -rotation.z, rotation.w) : rotation;
    }
}

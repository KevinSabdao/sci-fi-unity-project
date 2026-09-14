using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public abstract class Item : MonoBehaviour
{
    private class ItemSlot
    {
        internal readonly string name;
        internal readonly KeyCode keyPress;

        public ItemSlot(string name, KeyCode keyPress)
        {
            this.name = name;
            this.keyPress = keyPress;
        }
    }

    public bool isActive { get; set; }
    public Sprite inventoryIcon;

    public string itemName;
    [TextArea]
    public string description;

    // Names of the left and right arms (for held items)
    private readonly ItemSlot itemSlotLeft = new ItemSlot("LeftArm", KeyCode.Mouse0);
    private readonly ItemSlot itemSlotRight = new ItemSlot("RightArm", KeyCode.Mouse1);

    public Vector3 spawnPosition;
    public Vector3 spawnRotation;

    internal KeyCode keyPressRequired { get; set; }

    // Set required key press depending on if item is held on the left or right
    internal void CheckRequiredKeyPress()
    {
        if (this.transform.parent.gameObject.name == itemSlotLeft.name)
        {
            keyPressRequired = itemSlotLeft.keyPress;
        }
        else if (this.transform.parent.gameObject.name == itemSlotRight.name)
        {
            keyPressRequired = itemSlotRight.keyPress;
        }
    }
}

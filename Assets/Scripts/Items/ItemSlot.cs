using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ItemSlot : MonoBehaviour, IPointerClickHandler
{
    internal GameObject item;

    // Item slot specific information
    internal GameObject selectedSlot;
    internal bool thisItemSelected;
    internal bool isArm = false;
    internal int storedIndex;

    // Item slot shared information
    internal static bool itemIsSelected = false;
    private static ItemSlot itemSelected;

    // Item functionality attributes
    private Image image;
    internal KeyCode keyPress; // Key press to activate item slot

    private Inventory inventory;

    public void Start()
    {
        this.selectedSlot = this.transform.GetChild(0).gameObject;
        this.inventory = Inventory.Instance;
    }
    
    internal void PickupItem(GameObject pickedUpItemObject)
    {
        this.item = pickedUpItemObject;
        Item pickedUpItemItem = pickedUpItemObject.GetComponent<Item>();
        
        // Make the item act on the button corresponding to the item slot
        pickedUpItemItem.keyPressRequired = keyPress;

        // Store the item's image for later use
        this.image = this.transform.GetChild(1).GetComponent<Image>();
        this.image.sprite = pickedUpItemItem.inventoryIcon;
        this.image.enabled = true;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            OnLeftClick();
        }
    }

    public void OnLeftClick()
    {
        if (!this.item) { return; } // Immediately back out if no item is selected

        // Deselect if clicking on the same item
        if (itemIsSelected && this == itemSelected)
        {
            inventory.DeselectAllSlots();
        }
        
        // Swap items if clicking on two different items where one is an arm
        else if (itemIsSelected && this.isArm)
        {
            inventory.SwapItems(this, itemSelected);
        }
        else if (itemIsSelected && itemSelected.isArm)
        {
            inventory.SwapItems(itemSelected, this);
        }

        // Select item
        else
        {
            inventory.DeselectAllSlots();
            inventory.SetDescription(this.item, this.image);

            this.selectedSlot.SetActive(true);
            this.thisItemSelected = true;
            itemIsSelected = true;
            itemSelected = this;
        }
    }
}

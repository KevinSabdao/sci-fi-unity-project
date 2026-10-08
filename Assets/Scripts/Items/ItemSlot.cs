using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ItemSlot : MonoBehaviour, IPointerClickHandler
{
    [SerializeField]
    internal GameObject itemObject;
    internal Item itemItem;

    // Item slot specific information
    internal GameObject selectedSlot;
    internal bool thisItemSelected;
    internal bool isArm = false;
    internal int storedIndex;
    internal int itemStackSize;

    // Item slot shared information
    internal static bool itemIsSelected = false;
    public static ItemSlot itemSelected;

    // Item functionality attributes
    private Image image;
    internal KeyCode keyPress; // Key press to activate item slot

    private Inventory inventory;

    public void Awake()
    {
        this.selectedSlot = this.transform.GetChild(0).gameObject;
        this.image = this.transform.GetChild(1).GetComponent<Image>();
        this.inventory = Inventory.Instance;
    }

    public void Update()
    {
        // Delete image from slot if it becomes empty
        if (!itemObject)
        {
            this.image.sprite = null;
            this.image.enabled = false;
        }
    }
    
    internal void PickupItem(GameObject pickedUpItem)
    {
        this.itemObject = pickedUpItem;

        if (pickedUpItem != null)
        {
            this.itemItem = pickedUpItem.GetComponent<Item>();
        
            // Make the item act on the button corresponding to the item slot
            this.itemItem.keyPressRequired = keyPress;

            // Store the item's image for later use
            this.image.sprite = this.itemItem.inventoryIcon;
            this.image.enabled = true;
        }
        else
        {
            // Reset the item slot if the item is empty
            this.image.sprite = null;
            this.image.enabled = false;
        }

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
        if (!itemIsSelected && !this.itemObject) { return; } // Immediately back out if no item is selected

        // Deselect if clicking on the same item
        if (itemIsSelected && this == itemSelected)
        {
            inventory.DeselectAllSlots();
        }
        
        // Swap items if clicking on two different items where one is an arm
        else if (itemIsSelected)
        {
            inventory.SwapItems(itemSelected, this);
        }

        // Select item
        else
        {
            inventory.DeselectAllSlots();
            inventory.SetDescription(this.itemItem, this.image);

            this.selectedSlot.SetActive(true);
            this.thisItemSelected = true;
            itemIsSelected = true;
            itemSelected = this;
        }
    }
}

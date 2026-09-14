using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ItemSlot : MonoBehaviour, IPointerClickHandler
{
    private string itemName;
    private KeyCode keyPress;
    private string description;
    internal GameObject item;

    private Image image;

    public GameObject selectedSlot;
    public bool thisItemSelected;

    internal static Image ItemDescriptionImage;
    internal static TMP_Text ItemDescriptionName;
    internal static TMP_Text ItemDescriptionText;

    internal bool isArm = false;
    internal int storedIndex;
    internal static bool itemIsSelected = false;
    private static ItemSlot itemSelected;

    public void Start()
    {
        this.selectedSlot = this.transform.GetChild(0).gameObject;
        ItemDescriptionImage = GameObject.Find("ItemImage").GetComponent<Image>();
        ItemDescriptionImage.enabled = false;
        ItemDescriptionName = GameObject.Find("ItemDescriptionNameText").GetComponent<TMP_Text>();
        ItemDescriptionText = GameObject.Find("ItemDescriptionText").GetComponent<TMP_Text>();
    }
    
    public ItemSlot(string name, KeyCode keyPress)
    {
        this.name = name;
        this.keyPress = keyPress;
    }

    public void PickupItem(GameObject pickedUpItem)
    {
        this.name = pickedUpItem.GetComponent<Item>().itemName;
        this.item = pickedUpItem;
        this.description = pickedUpItem.GetComponent<Item>().description;

        this.image = this.transform.GetChild(1).GetComponent<Image>();
        this.image.sprite = pickedUpItem.GetComponent<Item>().inventoryIcon;
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
        if (!this.item) { return; }

        if (itemIsSelected && this == itemSelected)
        {
            Inventory.Instance.DeselectAllSlots();
        }
        else if (itemIsSelected && (this.isArm || itemSelected.isArm))
        {
            if (this.isArm)
            {
                Inventory.Instance.SwapItems(this, itemSelected);
            }
            else if (itemSelected.isArm)
            {
                Inventory.Instance.SwapItems(itemSelected, this);
            }
        }
        else
        {
            Inventory.Instance.DeselectAllSlots();
            this.selectedSlot.SetActive(true);
            this.thisItemSelected = true;

            ItemDescriptionName.text = this.name;
            ItemDescriptionText.text = this.description;
            if (this.image) { ItemDescriptionImage.sprite = this.image.sprite; }
            ItemDescriptionImage.enabled = true;

            itemIsSelected = true;
            itemSelected = this;
        }
    }
}

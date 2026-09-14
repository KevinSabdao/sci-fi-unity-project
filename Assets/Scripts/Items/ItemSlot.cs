using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ItemSlot : MonoBehaviour, IPointerClickHandler
{
    internal string name;
    internal KeyCode keyPress;
    internal GameObject item;
    internal string description;

    private bool isFull;
    private Image image;

    public GameObject selectedSlot;
    public bool thisItemSelected;

    public Image ItemDescriptionImage;
    public TMP_Text ItemDescriptionName;
    public TMP_Text ItemDescriptionText;

    public bool isArm = false;
    public int storedIndex;
    private static bool itemIsSelected = false;
    private static ItemSlot itemSelected;

    public void Start()
    {
        this.selectedSlot = this.transform.GetChild(0).gameObject;
        this.ItemDescriptionImage = GameObject.Find("ItemImage").GetComponent<Image>();
        this.ItemDescriptionImage.enabled = false;
        this.ItemDescriptionName = GameObject.Find("ItemDescriptionNameText").GetComponent<TMP_Text>();
        this.ItemDescriptionText = GameObject.Find("ItemDescriptionText").GetComponent<TMP_Text>();
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
        this.isFull = true;
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
        if (itemIsSelected && this.isArm)
        {
            Inventory.Instance.SwapItems(this, itemSelected);
            itemIsSelected = false;
        }
        else
        {
            Inventory.Instance.DeselectAllSlots();
            selectedSlot.SetActive(true);
            thisItemSelected = true;

            this.ItemDescriptionName.text = this.name;
            this.ItemDescriptionText.text = this.description;
            if (this.image) { this.ItemDescriptionImage.sprite = this.image.sprite; }
            this.ItemDescriptionImage.enabled = true;

            itemIsSelected = true;
            itemSelected = this;
        }
    }
}

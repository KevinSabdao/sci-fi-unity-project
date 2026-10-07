using System.Collections.Generic;
using COMP602;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Inventory : MonoBehaviour
{
    // Singleton
    public static Inventory Instance { get; set; }

    // Inventory slots
    [Header("Inventory:")]
    [SerializeField]
    private List<GameObject> startingItems;

    // Inventory physical objects
    [SerializeField]
    private List<GameObject> ArmObjects;
    [SerializeField]
    private int maxStorageSlots; // Maximum the UI can handle is 15 :/

    // Inventory UI
    private GameObject InventoryUI;
    [SerializeField]
    private GameObject ItemSlotPrefab;
    [SerializeField]
    private GameObject ArmSlots;
    private GameObject StorageSlots;
    [SerializeField]
    private List<ItemSlot> itemSlotsActive;
    [SerializeField]
    private List<ItemSlot> itemSlotsInactive;

    public static List<Keys> keyList = new List<Keys>();
    public static int keyCounter = 0;

    private bool menuActivated = false;
    internal Image itemDescriptionImage;
    internal TMP_Text itemDescriptionName;
    internal TMP_Text itemDescriptionText;

    // Keybinds
    [Header("Keybinds:")]
    [SerializeField]
    private KeyCode openInventory;
    [SerializeField]
    private KeyCode useLeftArm;
    [SerializeField]
    private KeyCode useRightArm;

    
    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        // Set internal inventory parameters
        this.ArmSlots = GameObject.Find("ArmSlots");
        this.StorageSlots = GameObject.Find("StorageSlots");
        this.InventoryUI = GameObject.Find("InventoryUI");
        this.ArmObjects = new List<GameObject>();
        this.itemSlotsActive = new List<ItemSlot>();
        this.itemSlotsInactive = new List<ItemSlot>();
        this.itemDescriptionImage = GameObject.Find("ItemImage").GetComponent<Image>();
        this.itemDescriptionImage.enabled = false;
        this.itemDescriptionName = GameObject.Find("ItemDescriptionNameText").GetComponent<TMP_Text>();
        this.itemDescriptionText = GameObject.Find("ItemDescriptionText").GetComponent<TMP_Text>();

        // Set arm objects
        foreach(Transform itemSlot in GameObject.Find("InventoryStorage").transform)
        {
            ArmObjects.Add(itemSlot.gameObject);
        }

        // Set active item slots
        ItemSlot itemSlotAdded;
        for (int i = 0; i < this.transform.GetChild(0).childCount; i++)
        {
            (Instantiate(ItemSlotPrefab) as GameObject).transform.SetParent(ArmSlots.transform);
            itemSlotAdded = ArmSlots.transform.GetChild(i).GetComponent<ItemSlot>();
            itemSlotAdded.isArm = true;
            itemSlotAdded.keyPress = (i == 0) ? useLeftArm : useRightArm;
            itemSlotsActive.Add(itemSlotAdded);
        }
        // With this new change, all slots should be active slots

        // Fill up inventory with pre-selected items
        foreach (GameObject item in startingItems)
        {
            PickupItem(item);
        }

        // Hide inventory
        this.InventoryUI.SetActive(menuActivated);
    }

    private void Update()
    {
        // Pick up item
        if (Input.GetKeyDown(openInventory))
        {
            menuActivated = !menuActivated;
            InventoryUI.SetActive(menuActivated);

            if (menuActivated)
            {
                // FirstPersonLook owns the cursor and Time.timeScale, asking it
                // keeps the pointer free AND visible, and stops a click on a
                // slot from being read as "click to resume"
                FirstPersonLook.PushUiModal();
            }
            else
            {
                FirstPersonLook.PopUiModal();
                DeselectAllSlots();
            }
        }
    }

    public void PickupItem(GameObject pickedUpItemObject)
    {
        if (itemSlotsInactive.Count > maxStorageSlots) { return; } // If no more items can go into the inventory

        Item pickedUpItemItem = pickedUpItemObject.GetComponent<Item>();

        // Put item into next active slot
        for (int i = 0; i < itemSlotsActive.Count; i++)
        {
            if (itemSlotsActive[i].item == null)
            {
                AddItemIntoSlot(pickedUpItemItem, i, true);
                return;
            }
        }

        // Put item into storage if no active slots are available
        (Instantiate(ItemSlotPrefab) as GameObject).transform.SetParent(StorageSlots.transform, false);
        itemSlotsInactive.Add(StorageSlots.transform.GetChild(itemSlotsInactive.Count).GetComponent<ItemSlot>());
        AddItemIntoSlot(pickedUpItemItem, itemSlotsInactive.Count - 1, false);
    }

    private void AddItemIntoSlot(Item itemAdded, int index, bool inActiveSlot)
    {
        // Add physical item corresponding inventory location
        // itemAdded.transform.SetParent(slot.transform, false);

        // Add UI item to corresponding inventory location
        ItemSlot itemSlot;
        if (inActiveSlot)
        {
            itemSlot = itemSlotsActive[index].GetComponent<ItemSlot>();
        }
        else
        {
            itemSlot = itemSlotsInactive[index].GetComponent<ItemSlot>();
        }
        itemSlot.PickupItem(itemAdded);
        itemSlot.storedIndex = index;
        itemSlot.isArm = inActiveSlot;

        // Set active state of item
        itemAdded.isActive = inActiveSlot;

        // Store position and rotation of the item for future dropping feature
        // itemAdded.transform.localPosition = new Vector3(item.spawnPosition.x, item.spawnPosition.y, item.spawnPosition.z);
        // itemAdded.transform.localRotation = Quaternion.Euler(item.spawnRotation.x, item.spawnRotation.y, item.spawnRotation.z);
    }

    public void SwapItems(ItemSlot itemArm, ItemSlot itemStorage)
    {
        // Create separate objects to avoid broken reference
        Item itemStorageItem = itemStorage.item;
        Item itemArmItem = itemArm.item;
        int storageIndex = itemStorage.storedIndex;
        int armIndex = itemArm.storedIndex;

        // Add item from storage to arm
        AddItemIntoSlot(itemStorageItem, armIndex, true);

        // Add item to either arm or storage
        if (itemStorage.isArm)
        {
            AddItemIntoSlot(itemArmItem, storageIndex, true);
        }
        else // Put item into storage if no active slots are available
        {
            AddItemIntoSlot(itemArmItem, storageIndex, false);
            // (Instantiate(ItemSlotPrefab) as GameObject).transform.SetParent(StorageSlots.transform, false);
            // itemSlotsInactive.Add(StorageSlots.transform.GetChild(itemSlotsInactive.Count).GetComponent<ItemSlot>());
            // AddItemIntoSlot(itemArmItem, itemSlotsInactive.Count - 1, false);

            // Destroy(itemSlotsInactive[storageIndex].gameObject);
            // itemSlotsInactive.RemoveAt(storageIndex);
        }

        // Fix stored indexes
        // for (int i = 0; i < itemSlotsInactive.Count; i++)
        // {
        //     itemSlotsInactive[i].GetComponent<ItemSlot>().storedIndex = i;
        // }

        DeselectAllSlots();
    }

    public void DeselectAllSlots()
    {
        // ItemSlot itemSlot;

        // Deselect all arm slots
        foreach (ItemSlot slot in itemSlotsActive)
        {
            // itemSlot = slot.GetComponent<ItemSlot>();
            if (slot.selectedSlot) { slot.selectedSlot.SetActive(false); }
            slot.thisItemSelected = false;
        }

        // Deselect all storage slots
        foreach (ItemSlot slot in itemSlotsInactive)
        {
            // itemSlot = slot.GetComponent<ItemSlot>();
            if (slot.selectedSlot) { slot.selectedSlot.SetActive(false); }
            slot.thisItemSelected = false;
        }

        // Remove name and description text
        itemDescriptionImage.enabled = false;
        itemDescriptionName.text = "";
        itemDescriptionText.text = "";

        ItemSlot.itemIsSelected = false;
    }

    public void SetDescription(Item item, Image itemImage)
    {
        // Item itemItem = itemObject.GetComponent<Item>();
        itemDescriptionName.text = item.itemName;
        itemDescriptionText.text = item.description;
        if (itemImage) { itemDescriptionImage.sprite = itemImage.sprite; }
        itemDescriptionImage.enabled = true;
    }

    public static void AddKey(string name, int count)
    {
        Keys existingKey = keyList.Find(x => x.keyName == name);
        if (existingKey != null)
        {
            existingKey.keyCount++;

        }
        else
        {
            keyCounter++;
            Keys key = new Keys(name, count);
            keyList.Add(key);
        }
    }


    // private void DropCurrentItem(GameObject pickedUpItem)
    // {
    //     if (itemSlotsActive.transform.childCount > 0)
    //     {
    //         var itemToDrop = itemSlotsActive.transform.GetChild(0).gameObject;

    //         itemToDrop.GetComponent<Item>().isActive = false;

    //         itemToDrop.transform.SetParent(pickedUpItem.transform.parent);
    //         itemToDrop.transform.localPosition = pickedUpItem.transform.localPosition;
    //         itemToDrop.transform.localRotation = pickedUpItem.transform.localRotation;
    //     }
    // }
}

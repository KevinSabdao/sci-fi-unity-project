using COMP602;
using System.Collections.Generic;
using System.Linq;
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
    [SerializeField]
    private int maxStorageSlots; // Maximum the UI can handle is 15 :/

    // Inventory physical objects
    private List<GameObject> ArmObjects;
    private GameObject inactiveItems;

    // Inventory internal item slots
    private List<ItemSlot> itemSlotsActive;
    private List<ItemSlot> itemSlotsInactive;

    // Inventory UI
    private GameObject InventoryUI;
    [SerializeField]
    private GameObject ItemSlotPrefab;
    private GameObject ArmSlots;
    private GameObject StorageSlots;

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
        this.inactiveItems = GameObject.Find("InactiveItems");
        this.inactiveItems.SetActive(false);
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
            itemSlotAdded.storedIndex = i;
            itemSlotsActive.Add(itemSlotAdded);
        }

        // Set inactive item slots
        for (int i = 0; i < this.maxStorageSlots; i++)
        {
            (Instantiate(ItemSlotPrefab) as GameObject).transform.SetParent(StorageSlots.transform);
            itemSlotAdded = StorageSlots.transform.GetChild(i).GetComponent<ItemSlot>();
            itemSlotAdded.isArm = false;
            itemSlotAdded.storedIndex = i;
            itemSlotsInactive.Add(itemSlotAdded);
        }

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

    // private bool canAddItem()
    // {
    //     // Check for an empty item slot
    //     foreach (ItemSlot itemSlot in itemSlotsActive.Concat(itemSlotsInactive))
    //     {
    //         if (itemSlot.itemObject == null) { return true; }
    //     }

    //     return false;
    // }

    public void PickupItem(GameObject pickedUpItem)
    {
        bool addedToInventory;

        // Put item into next active slot
        addedToInventory = CheckItemSlots(itemSlotsActive, true, pickedUpItem);

        // Put item into storage if no active slots are available
        if (!addedToInventory)
        {
            CheckItemSlots(itemSlotsInactive, false, pickedUpItem);
        }
    }

    private bool CheckItemSlots(List<ItemSlot> itemSlotsChecking, bool isActiveSlot, GameObject item)
    {
        for (int i = 0; i < itemSlotsChecking.Count; i++)
        {
            if (itemSlotsChecking[i].itemItem == null) // if slot is empty
            {
                AddItemIntoSlot(item, i, isActiveSlot);
                return true;
            }
        }

        return false;
    }

    private void AddItemIntoSlot(GameObject itemAddedObject, int index, bool inActiveSlot)
    {
        Item itemAddedItem = itemAddedObject ? itemAddedObject.GetComponent<Item>() : null;

        // Add item to correct inventory location
        ItemSlot itemSlot;
        if (inActiveSlot)
        {
            itemAddedItem?.transform.SetParent(ArmObjects[index].transform, false);
            itemSlot = itemSlotsActive[index].GetComponent<ItemSlot>();
        }
        else
        {
            itemAddedItem?.transform.SetParent(inactiveItems.transform, false);
            itemSlot = itemSlotsInactive[index].GetComponent<ItemSlot>();
        }

        // Store basic bookkeeping
        itemSlot.PickupItem(itemAddedObject ? itemAddedObject : null);
        itemSlot.storedIndex = index;
        itemSlot.isArm = inActiveSlot;
        if (itemAddedItem) { itemAddedItem.isActive = inActiveSlot; }

        // Store position and rotation of the item for future dropping feature
        if (itemAddedObject)
        {
            itemAddedObject.transform.localPosition = new Vector3(itemAddedItem.spawnPosition.x, itemAddedItem.spawnPosition.y, itemAddedItem.spawnPosition.z);
            itemAddedObject.transform.localRotation = Quaternion.Euler(itemAddedItem.spawnRotation.x, itemAddedItem.spawnRotation.y, itemAddedItem.spawnRotation.z);
        }
    }

    public void SwapItems(ItemSlot itemArm, ItemSlot itemStorage)
    {
        // Create separate objects to avoid broken reference
        GameObject itemStorageObject = itemStorage.itemObject;
        GameObject itemArmObject = itemArm.itemObject;
        int storageIndex = itemStorage.storedIndex;
        int armIndex = itemArm.storedIndex;

        // Actually swap items
        AddItemIntoSlot(itemStorageObject, armIndex, itemArm.isArm);
        AddItemIntoSlot(itemArmObject, storageIndex, itemStorage.isArm);

        DeselectAllSlots();
    }

    public void DeselectAllSlots()
    {
        // Deselect all item slots
        foreach (ItemSlot slot in itemSlotsActive.Concat(itemSlotsInactive))
        {
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

    public void DeleteSelectedItem()
    {
        Destroy(ItemSlot.itemSelected.itemObject);
        DeselectAllSlots();
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

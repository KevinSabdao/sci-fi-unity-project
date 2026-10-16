using System.Collections.Generic;
using System.Linq;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Inventory : MonoBehaviour
{
    // Singleton for the player inventory
    public static Inventory Instance { get; set; }

    // Inventory slots
    [Header("Inventory:")]
    [SerializeField]
    private List<GameObject> startingItems;
    [SerializeField]
    private int maxStorageSlots; // Maximum the UI can handle is 15 :/
    [SerializeField]
    private int maxExternalSlots; // Maximum the UI can handle is 20 :/

    // Inventory physical objects
    private List<GameObject> ArmObjects;
    private List<GameObject> itemStorages;
    private GameObject inactiveItems;
    private readonly Vector3 holdPosition = new Vector3(0, 0, 0);
    private readonly Vector3 holdRotation = new Vector3(0, 180, 0);
    private readonly Vector3 dropRotation = new Vector3(0, -90, -90);

    // Inventory internal item slots
    private List<ItemSlot> itemSlotsActive;
    private List<ItemSlot> itemSlotsInactive;
    private List<ItemSlot> itemSlotsExternal;

    // Inventory UI GameObjects
    private GameObject InventoryUI;
    [SerializeField]
    private GameObject ItemSlotPrefab;
    private GameObject ArmSlots;
    private GameObject StorageSlots;
    private GameObject ExternalSlots;
    private static GameObject switchStorageButtons;
    private static GameObject itemControlButtons;

    // Inventory description items
    internal static Image itemDescriptionImage;
    internal static TMP_Text itemDescriptionName;
    internal static TMP_Text itemDescriptionText;

    // Other inventory values
    public static bool menuActivated = false;
    public static int storageOpen;
    private const int STORAGE_MAIN = 0;

    // Not sure what this does - it's not mine and I'm too scared to touch it :/
    public static List<Keys> keyList = new List<Keys>();
    public static int keyCounter = 0;

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

        // Set correct inventory item slots
        this.ArmSlots = GameObject.Find("ArmSlots");
        this.itemSlotsActive = new List<ItemSlot>();
        this.StorageSlots = GameObject.Find("StorageSlots");
        this.itemSlotsInactive = new List<ItemSlot>();
        this.ExternalSlots = GameObject.Find("ExternalSlots");
        this.itemSlotsExternal = new List<ItemSlot>();

        // Set other inventory components
        this.InventoryUI = GameObject.Find("InventoryUI");
        this.inactiveItems = GameObject.Find("InactiveItems");
        this.inactiveItems.SetActive(false);
        this.ArmObjects = new List<GameObject>();
        this.itemStorages = new List<GameObject>();

        // Set inventory description items
        itemDescriptionImage = GameObject.Find("ItemImage").GetComponent<Image>();
        itemDescriptionImage.enabled = false;
        itemDescriptionName = GameObject.Find("ItemDescriptionNameText").GetComponent<TMP_Text>();
        itemDescriptionText = GameObject.Find("ItemDescriptionText").GetComponent<TMP_Text>();
        switchStorageButtons = GameObject.Find("InventorySwitchButtons");
        itemControlButtons = GameObject.Find("ItemControlButtons");

        // Set arm objects
        foreach(Transform itemSlot in GameObject.Find("InventoryStorage").transform)
        {
            ArmObjects.Add(itemSlot.gameObject);
        }

        foreach (Transform itemSlotDisplay in GameObject.Find("InventorySlotsDisplay").transform)
        {
            itemStorages.Add(itemSlotDisplay.gameObject);
        }

        // Create all item slots
        AddItemSlots(ArmSlots, itemSlotsActive, this.transform.GetChild(0).childCount, STORAGE_MAIN);
        AddItemSlots(StorageSlots, itemSlotsInactive, this.maxStorageSlots, STORAGE_MAIN);
        AddItemSlots(ExternalSlots, itemSlotsExternal, this.maxExternalSlots, 1);

        // Fill up inventory with pre-selected items
        foreach (GameObject item in startingItems)
        {
            PickupItem(item);
        }

        // Hide all option buttons
        switchStorageButtons.SetActive(true);
        itemControlButtons.SetActive(false);

        // Set the open storage to the inventory (should always be 0)
        storageOpen = 0;

        // Hide inventory
        this.InventoryUI.SetActive(menuActivated);
    }

    private void AddItemSlots(GameObject slotObject, List<ItemSlot> slotList, int slotCount, int storage)
    {
        ItemSlot itemSlotAdded;
        for (int i = 0; i < slotCount; i++)
        {
            // Create item slot from prefab
            (Instantiate(ItemSlotPrefab) as GameObject).transform.SetParent(slotObject.transform);
            itemSlotAdded = slotObject.transform.GetChild(i).GetComponent<ItemSlot>();
            itemSlotAdded.storedIndex = i;
            itemSlotAdded.storedStorage = storage;

            // Set slot-dependent parameters
            if (slotObject == this.ArmSlots)
            {
                itemSlotAdded.isArm = true;
                itemSlotAdded.keyPress = (i == 0) ? useLeftArm : useRightArm;
            }
            else
            {
                itemSlotAdded.isArm = false;
            }

            // Add the slot to a list for easy access
            slotList.Add(itemSlotAdded);
        }
    }

    private void Update()
    {
        // Open inventory
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
                storageOpen = STORAGE_MAIN;
            }
        }

        // Display the currently selected storage
        for (int i = 0; i < itemStorages.Count; i++)
        {
            itemStorages[i].SetActive(i == storageOpen);
        }
    }

    public void PickupItem(GameObject pickedUpItem)
    {
        bool addedToInventory;

        // Put item into next available stack (if one exists)
        addedToInventory = ExistingStackExists(pickedUpItem);

        // Put item into next active slot
        if (!addedToInventory) addedToInventory = AttemptAddToInventory(itemSlotsActive, true, pickedUpItem);

        // Put item into storage if no active slots are available
        if (!addedToInventory) addedToInventory = AttemptAddToInventory(itemSlotsInactive, false, pickedUpItem);
    }

    private bool ExistingStackExists(GameObject item)
    {
        // Get item components to compare stack sizes
        Item itemFrom = item.GetComponent<Item>();
        Item itemTo;

        // Check if a slot has an incomplete stack of the same item type
        foreach(ItemSlot itemSlot in itemSlotsActive.Concat(itemSlotsInactive))
        {
            if (itemSlot.itemObject == null) { continue; }
        
            itemTo = itemSlot.itemItem;
            if (
                    itemTo.itemType == itemFrom.itemType &&
                    itemTo.stackCount < itemTo.stackCountMax
            )
            {
                // If the transfer stack was successful (no remainder)
                if (TransferItemStack(item, itemSlot.itemObject)) return true;
            }
        }

        return false;
    }

    public bool TransferItemStack(GameObject itemFromObject, GameObject itemToObject)
    {
        Item itemFromItem = itemFromObject.GetComponent<Item>();
        Item itemToItem = itemToObject.GetComponent<Item>();

        // Calculate highest transfer amount (as stack sizes aren't always 1)
        int stackTransfer = Math.Min(itemToItem.stackCountMax - itemToItem.stackCount, itemFromItem.stackCount);
        itemToItem.stackCount += stackTransfer;
        itemFromItem.stackCount -= stackTransfer;

        // Delete the from stack if it no longer has items
        if (itemFromItem.stackCount <= 0)
        {
            Destroy(itemFromObject);
        }

        return itemFromItem.stackCount <= 0;

    }

    private bool AttemptAddToInventory(List<ItemSlot> itemSlotsChecking, bool isActiveSlot, GameObject item)
    {
        // Check if a slot is empty
        for (int i = 0; i < itemSlotsChecking.Count; i++)
        {
            if (itemSlotsChecking[i].itemObject == null)
            {
                AddItemIntoSlot(item, i, isActiveSlot, STORAGE_MAIN);
                return true;
            }

        }

        return false;
    }

    private void AddItemIntoSlot(GameObject itemAddedObject, int index, bool inActiveSlot, int storageStored)
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
            if (storageStored == 0)
            {
                itemSlot = itemSlotsInactive[index].GetComponent<ItemSlot>();
            }
            else
            {
                itemSlot = itemSlotsExternal[index].GetComponent<ItemSlot>();
            }
        }

        // Store basic bookkeeping
        itemSlot.PickupItem(itemAddedObject ? itemAddedObject : null);
        itemSlot.storedIndex = index;
        itemSlot.isArm = inActiveSlot;
        if (itemAddedItem) { itemAddedItem.isActive = inActiveSlot; }

        // Store position and rotation of the item for future dropping feature
        if (itemAddedObject)
        {
            itemAddedObject.transform.localPosition = this.holdPosition;
            itemAddedObject.transform.localRotation = Quaternion.Euler(this.holdRotation.x, this.holdRotation.y, this.holdRotation.z);
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
        AddItemIntoSlot(itemStorageObject, armIndex, itemArm.isArm, itemArm.storedStorage);
        AddItemIntoSlot(itemArmObject, storageIndex, itemStorage.isArm, itemStorage.storedStorage);

        DeselectAllSlots();
    }

    public void DeselectAllSlots()
    {
        // Deselect all item slots
        foreach (ItemSlot slot in itemSlotsActive.Concat(itemSlotsInactive).Concat(itemSlotsExternal))
        {
            if (slot.selectedSlot) { slot.selectedSlot.SetActive(false); }
            slot.thisItemSelected = false;
        }

        // Remove name and description text
        itemDescriptionImage.enabled = false;
        itemDescriptionName.text = "";
        itemDescriptionText.text = "";

        ItemSlot.itemIsSelected = false;
        itemControlButtons.SetActive(false);
    }

    public void SetDescription(Item item, Image itemImage)
    {
        itemDescriptionName.text = item.itemName;
        itemDescriptionText.text = item.description;
        if (itemImage) { itemDescriptionImage.sprite = itemImage.sprite; }
        itemDescriptionImage.enabled = true;

        // Loop through all the option buttons
        itemControlButtons.SetActive(true);
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

    private void DropItem(GameObject dropItemObject)
        {
            Item dropItemItem = dropItemObject.GetComponent<Item>();

            // Place item on the ground at the player's feet
            dropItemObject.transform.SetParent(null);
            Vector3 parentPosition = this.transform.parent.transform.position;
            Vector3 parentRotation = this.transform.parent.transform.rotation.eulerAngles;
            dropItemObject.transform.localPosition = new Vector3(
                    parentPosition.x,
                    parentPosition.y,
                    parentPosition.z
            );
            dropItemObject.transform.localRotation = Quaternion.Euler(
                    this.dropRotation.x,
                    parentRotation.y + this.dropRotation.y,
                    this.dropRotation.z
            );

            dropItemItem.isActive = false;
        }

    public void DropSelectedItemOne()
    {
        ItemSlot itemSelected = ItemSlot.itemSelected;
        GameObject dropItem = itemSelected.itemObject;
        Item dropItemItem = dropItem.GetComponent<Item>();
        
        // Clone the dropped object and place a single item
        GameObject dropItemClone = Instantiate(dropItem);
        dropItemClone.GetComponent<Item>().stackCount = 1;
        DropItem(dropItemClone);

        // Delete the item if it was the last in the stack
        dropItemItem.stackCount--;
        if (dropItemItem.stackCount <= 0)
        {
            DeleteSelectedItem();
        }
    }

    public void DropSelectedItemAll()
    {
        ItemSlot itemSelected = ItemSlot.itemSelected;
        GameObject dropItemObject = itemSelected.itemObject;

        DropItem(dropItemObject);

        // Internally set it so the item is no longer in the inventory
        AddItemIntoSlot(null, itemSelected.storedIndex, itemSelected.isArm, itemSelected.storedStorage);
        ItemSlot.dropSelected = true;

        DeselectAllSlots();
    }

    public void SwitchStorageLeft()
    {
        storageOpen = (storageOpen + itemStorages.Count - 1) % itemStorages.Count; // Never goes below 0
    }

    public void SwitchStorageRight()
    {
        storageOpen = (storageOpen + 1) % itemStorages.Count; // Never goes above total number of storages
    }
}

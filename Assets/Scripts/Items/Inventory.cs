using System.Collections.Generic;
using System.Linq;
using System;
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

    // Dropping: how far in front of the player, the gap kept from walls, how far down to look for ground
    private const float dropDistance = 1f;
    private const float dropWallGap = 0.3f;
    private const float dropGroundSearch = 10f;

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

    public static bool menuActivated = false;
    internal Image itemDescriptionImage;
    internal TMP_Text itemDescriptionName;
    internal TMP_Text itemDescriptionText;
    private GameObject itemOptions; 

    // Keybinds
    [Header("Keybinds:")]
    [SerializeField]
    private KeyCode openInventory;
    [SerializeField]
    private KeyCode openInventoryAlternative;
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
        this.itemOptions = GameObject.Find("ItemOptions");

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
        // Static values survive between play sessions, so always start closed
        menuActivated = false;
        this.InventoryUI.SetActive(menuActivated);
    }

    private void Update()
    {
        // Pick up item
        // Esc also closes it, and it never opens on top of the pause menu
        bool togglePressed = Input.GetKeyDown(openInventory) || Input.GetKeyDown(openInventoryAlternative) || (menuActivated && Input.GetKeyDown(KeyCode.Escape));
        if (togglePressed && !PauseMenu.isPaused)
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

        // Set displayability of item options
        this.itemOptions.SetActive(ItemSlot.itemIsSelected);
    }

    public void PickupItem(GameObject pickedUpItem)
    {
        bool addedToInventory;

        // Put item into next available stack (if one exists)
        addedToInventory = ExistingStackExists(pickedUpItem);

        // Put item into next active slot
        if (!addedToInventory) addedToInventory = CheckItemSlots(itemSlotsActive, true, pickedUpItem);

        // Put item into storage if no active slots are available
        if (!addedToInventory) addedToInventory = CheckItemSlots(itemSlotsInactive, false, pickedUpItem);
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

    private bool CheckItemSlots(List<ItemSlot> itemSlotsChecking, bool isActiveSlot, GameObject item)
    {
        // Check if a slot is empty
        for (int i = 0; i < itemSlotsChecking.Count; i++)
        {
            if (itemSlotsChecking[i].itemObject == null)
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
            itemAddedObject.transform.localPosition = itemAddedItem.HoldLocalPosition();
            itemAddedObject.transform.localRotation = itemAddedItem.HoldLocalRotation();

            // Held items must not block the player, bullets or raycasts
            SetItemCollidersEnabled(itemAddedObject, false);
        }
    }

    private void SetItemCollidersEnabled(GameObject itemObject, bool enabled)
    {
        foreach (Collider itemCollider in itemObject.GetComponentsInChildren<Collider>(true))
        {
            itemCollider.enabled = enabled;
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
        itemDescriptionImage.preserveAspect = true;
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

    private void DropItem(GameObject dropItemObject)
    {
        Item dropItemItem = dropItemObject.GetComponent<Item>();

        // Place item on the ground in front of the player
        dropItemObject.transform.SetParent(null);
        Transform view = this.transform.parent;
        Vector3 parentRotation = view.rotation.eulerAngles;
        dropItemObject.transform.localRotation = Quaternion.Euler(
                dropItemItem.spawnRotation.x,
                parentRotation.y + dropItemItem.spawnRotation.y,
                dropItemItem.spawnRotation.z
        );

        bool foundGround = FindDropGround(view, out Vector3 dropPoint);
        dropItemObject.transform.localPosition = dropPoint;

        // Rest the model on the ground instead of floating or sinking into it
        if (foundGround)
        {
            Renderer[] renderers = dropItemObject.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                float lowest = renderers.Min(itemRenderer => itemRenderer.bounds.min.y);
                dropItemObject.transform.position += Vector3.up * (dropPoint.y - lowest);
            }
        }

        dropItemItem.isActive = false;

        // Turned off while in the inventory, needed again to be picked up
        SetItemCollidersEnabled(dropItemObject, true);

        // Items on the ground can still be picked up, but never block the player
        foreach (Collider itemCollider in dropItemObject.GetComponentsInChildren<Collider>(true))
        {
            itemCollider.isTrigger = true;
        }
    }

    // Ground in front of the view, kept on this side of any wall
    private bool FindDropGround(Transform view, out Vector3 groundPoint)
    {
        Vector3 forward = Vector3.ProjectOnPlane(view.forward, Vector3.up).normalized;
        if (forward == Vector3.zero) forward = view.root.forward;

        float distance = dropDistance;
        if (Physics.Raycast(view.position, forward, out RaycastHit wallHit, dropDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            distance = Mathf.Max(0f, wallHit.distance - dropWallGap);
        }

        Vector3 above = view.position + forward * distance;
        if (Physics.Raycast(above, Vector3.down, out RaycastHit groundHit, dropGroundSearch, ~0, QueryTriggerInteraction.Ignore))
        {
            groundPoint = groundHit.point;
            return true;
        }

        groundPoint = above;
        return false;
    }

    public void DropSelectedItemAll()
    {
        ItemSlot itemSelected = ItemSlot.itemSelected;
        GameObject dropItemObject = itemSelected.itemObject;

        DropItem(dropItemObject);

        // Internally set it so the item is no longer in the inventory
        AddItemIntoSlot(null, itemSelected.storedIndex, itemSelected.isArm);
        ItemSlot.dropSelected = true;

        DeselectAllSlots();
    }
}

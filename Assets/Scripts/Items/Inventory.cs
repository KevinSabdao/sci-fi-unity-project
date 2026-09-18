using System.Collections.Generic;
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

    private List<GameObject> itemSlots;
    private List<GameObject> activeItemSlots;
    [SerializeField]
    private int maxStorageSlots; // Maximum the UI can handle is 15 :/

    // Inventory UI
    private GameObject InventoryUI;
    [SerializeField]
    private GameObject ItemSlotPrefab;
    private GameObject ArmSlots;
    private List<GameObject> activeArmSlots;
    private GameObject StorageSlots;
    private List<GameObject> inactiveSlots;
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
        this.itemSlots = new List<GameObject>();
        this.activeItemSlots = new List<GameObject>();
        this.activeArmSlots = new List<GameObject>();
        this.inactiveSlots = new List<GameObject>();
        this.itemDescriptionImage = GameObject.Find("ItemImage").GetComponent<Image>();
        this.itemDescriptionImage.enabled = false;
        this.itemDescriptionName = GameObject.Find("ItemDescriptionNameText").GetComponent<TMP_Text>();
        this.itemDescriptionText = GameObject.Find("ItemDescriptionText").GetComponent<TMP_Text>();

        // Set item slots
        foreach(Transform itemSlot in GameObject.Find("InventoryStorage").transform)
        {
            itemSlots.Add(itemSlot.gameObject);
        }

        // Set active item slots
        for (int i = 0; i < this.transform.GetChild(0).childCount - 1; i++)
        {
            activeItemSlots.Add(itemSlots[i]);
            (Instantiate(ItemSlotPrefab) as GameObject).transform.SetParent(ArmSlots.transform);
            ArmSlots.transform.GetChild(i).GetComponent<ItemSlot>().isArm = true;
            ArmSlots.transform.GetChild(i).GetComponent<ItemSlot>().keyPress = (i == 0) ? useLeftArm : useRightArm;
            activeArmSlots.Add(ArmSlots.transform.GetChild(i).gameObject);
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
                Time.timeScale = 0;
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                Time.timeScale = 1;
                Cursor.lockState = CursorLockMode.Locked;
                DeselectAllSlots();
            }
        }
    }

    public void PickupItem(GameObject pickedUpItem)
    {
        if (inactiveSlots.Count > maxStorageSlots) { return; } // If no more items can go into the inventory

        // Put item into next active slot
        for (int i = 0; i < activeItemSlots.Count; i++)
        {
            if (activeItemSlots[i].transform.childCount == 0)
            {
                AddItemIntoSlot(pickedUpItem, activeItemSlots[i], i, true);
                return;
            }
        }

        // Put item into storage if no active slots are available
        (Instantiate(ItemSlotPrefab) as GameObject).transform.SetParent(StorageSlots.transform, false);
        inactiveSlots.Add(StorageSlots.transform.GetChild(inactiveSlots.Count).gameObject);
        AddItemIntoSlot(pickedUpItem, itemSlots[itemSlots.Count - 1], inactiveSlots.Count - 1, false);
    }

    private void AddItemIntoSlot(GameObject itemAdded, GameObject slot, int index, bool inActiveSlot)
    {
        // Add physical item corresponding inventory location
        itemAdded.transform.SetParent(slot.transform, false);

        // Add UI item to corresponding inventory location
        ItemSlot itemSlot;
        if (inActiveSlot)
        {
            itemSlot = activeArmSlots[index].GetComponent<ItemSlot>();
            itemSlot.PickupItem(itemAdded);
            itemSlot.storedIndex = index;
            itemSlot.isArm = inActiveSlot;
        }
        else
        {
            itemSlot = inactiveSlots[index].GetComponent<ItemSlot>();
            itemSlot.PickupItem(itemAdded);
            itemSlot.storedIndex = index;
            itemSlot.isArm = inActiveSlot;
        }

        // Set active state of item
        Item item = itemAdded.GetComponent<Item>();
        item.isActive = inActiveSlot;

        // Store position and rotation of the item for future dropping feature
        itemAdded.transform.localPosition = new Vector3(item.spawnPosition.x, item.spawnPosition.y, item.spawnPosition.z);
        itemAdded.transform.localRotation = Quaternion.Euler(item.spawnRotation.x, item.spawnRotation.y, item.spawnRotation.z);
    }

    public void SwapItems(ItemSlot itemArm, ItemSlot itemStorage)
    {
        // Create separate objects to avoid broken reference
        GameObject itemStorageItem = itemStorage.item;
        GameObject itemArmItem = itemArm.item;
        int storageIndex = itemStorage.storedIndex;
        int armIndex = itemArm.storedIndex;

        // Add item from storage to arm
        AddItemIntoSlot(itemStorageItem, activeItemSlots[armIndex], armIndex, true);

        // Add item to either arm or storage
        if (itemStorage.isArm)
        {
            AddItemIntoSlot(itemArmItem, activeItemSlots[storageIndex], storageIndex, true);
        }
        else // Put item into storage if no active slots are available
        {
            (Instantiate(ItemSlotPrefab) as GameObject).transform.SetParent(StorageSlots.transform, false);
            inactiveSlots.Add(StorageSlots.transform.GetChild(inactiveSlots.Count).gameObject);
            AddItemIntoSlot(itemArmItem, itemSlots[itemSlots.Count - 1], inactiveSlots.Count - 1, false);

            Destroy(inactiveSlots[storageIndex].gameObject);
            inactiveSlots.RemoveAt(storageIndex);
        }

        // Fix stored indexes
        for (int i = 0; i < inactiveSlots.Count; i++)
        {
            inactiveSlots[i].GetComponent<ItemSlot>().storedIndex = i;
        }

        DeselectAllSlots();
    }

    public void DeselectAllSlots()
    {
        ItemSlot itemSlot;

        // Deselect all arm slots
        foreach (GameObject slot in activeArmSlots)
        {
            itemSlot = slot.GetComponent<ItemSlot>();
            if (itemSlot.selectedSlot) { itemSlot.selectedSlot.SetActive(false); }
            itemSlot.thisItemSelected = false;
        }

        // Deselect all storage slots
        foreach (GameObject slot in inactiveSlots)
        {
            itemSlot = slot.GetComponent<ItemSlot>();
            if (itemSlot.selectedSlot) { itemSlot.selectedSlot.SetActive(false); }
            itemSlot.thisItemSelected = false;
        }

        // Remove name and description text
        itemDescriptionImage.enabled = false;
        itemDescriptionName.text = "";
        itemDescriptionText.text = "";

        ItemSlot.itemIsSelected = false;
    }

    public void SetDescription(GameObject itemObject, Image itemImage)
    {
        Item itemItem = itemObject.GetComponent<Item>();
        itemDescriptionName.text = itemItem.itemName;
        itemDescriptionText.text = itemItem.description;
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
    //     if (activeItemSlots.transform.childCount > 0)
    //     {
    //         var itemToDrop = activeItemSlots.transform.GetChild(0).gameObject;

    //         itemToDrop.GetComponent<Item>().isActive = false;

    //         itemToDrop.transform.SetParent(pickedUpItem.transform.parent);
    //         itemToDrop.transform.localPosition = pickedUpItem.transform.localPosition;
    //         itemToDrop.transform.localRotation = pickedUpItem.transform.localRotation;
    //     }
    // }
}

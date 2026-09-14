using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance { get; set; }

    // Inventory slots
    public List<GameObject> startingItems;
    public List<GameObject> itemSlots;
    public List<GameObject> activeItemSlots;
    public int maxItemSlots; // Maximum the UI can handle is 15 :/

    // Inventory UI
    public GameObject InventoryUI;
    public GameObject ItemSlotPrefab;
    public GameObject ArmSlots;
    public List<GameObject> activeArmSlots;
    public GameObject StorageSlots;
    public List<GameObject> inactiveSlots;
    private bool menuActivated = false;
    
    private void Awake()
    {
        ArmSlots = GameObject.Find("ArmSlots");
        StorageSlots = GameObject.Find("StorageSlots");

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
        // Set active item slots
        for (int i = 0; i < this.transform.GetChild(0).childCount - 1; i++)
        {
            activeItemSlots.Add(itemSlots[i]);
            (Instantiate(ItemSlotPrefab) as GameObject).transform.SetParent(ArmSlots.transform);
            ArmSlots.transform.GetChild(i).GetComponent<ItemSlot>().isArm = true;
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
        if (Input.GetKeyDown(KeyCode.E) && inactiveSlots.Count < maxItemSlots)
        {
            menuActivated = !menuActivated;
            InventoryUI.SetActive(menuActivated);
            Time.timeScale = menuActivated ? 0 : 1;
            Cursor.lockState = menuActivated ? CursorLockMode.None : CursorLockMode.Locked;
        }
    }

    private enum ScrollDirection
    {
        Left,
        Right
    }

    public void PickupItem(GameObject pickedUpItem)
    {
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
        itemAdded.transform.SetParent(slot.transform, false);
        if (inActiveSlot)
        {
            activeArmSlots[index].GetComponent<ItemSlot>().PickupItem(itemAdded);
            activeArmSlots[index].GetComponent<ItemSlot>().storedIndex = index;
            activeArmSlots[index].GetComponent<ItemSlot>().isArm = inActiveSlot;
        }
        else
        {
            inactiveSlots[index].GetComponent<ItemSlot>().PickupItem(itemAdded);
            inactiveSlots[index].GetComponent<ItemSlot>().storedIndex = index;
            inactiveSlots[index].GetComponent<ItemSlot>().isArm = inActiveSlot;
        }

        Item item = itemAdded.GetComponent<Item>();
        item.isActive = inActiveSlot;

        itemAdded.transform.localPosition = new Vector3(item.spawnPosition.x, item.spawnPosition.y, item.spawnPosition.z);
        itemAdded.transform.localRotation = Quaternion.Euler(item.spawnRotation.x, item.spawnRotation.y, item.spawnRotation.z);
    }

    public void SwapItems(ItemSlot itemArm, ItemSlot itemStorage)
    {
        GameObject itemArmItem = itemArm.item;
        GameObject itemStorageItem = itemStorage.item;
        int storageIndex = itemStorage.storedIndex;
        int armIndex = itemArm.storedIndex;
        AddItemIntoSlot(itemStorageItem, activeItemSlots[armIndex], armIndex, true);

        // Put item into storage if no active slots are available
        if (itemStorage.isArm)
        {
            AddItemIntoSlot(itemArmItem, activeItemSlots[storageIndex], storageIndex, true);
        }
        else
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

        // Deselect all inventory slots
        foreach (GameObject slot in inactiveSlots)
        {
            itemSlot = slot.GetComponent<ItemSlot>();
            if (itemSlot.selectedSlot) { itemSlot.selectedSlot.SetActive(false); }
            itemSlot.thisItemSelected = false;
        }

        // Remove name and description text
        ItemSlot.ItemDescriptionImage.enabled = false;
        ItemSlot.ItemDescriptionName.text = "";
        ItemSlot.ItemDescriptionText.text = "";

        ItemSlot.itemIsSelected = false;
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

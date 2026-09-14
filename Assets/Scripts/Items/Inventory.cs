using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance { get; set; }

    // Inventory slots
    public List<GameObject> startingItems;
    public List<GameObject> itemSlots;
    public List<GameObject> activeItemSlots;

    // Inventory UI
    public GameObject InventoryUI;
    public GameObject ItemSlot;
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
            (Instantiate(ItemSlot) as GameObject).transform.SetParent(ArmSlots.transform);
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
        // Temporary before a proper system is implemented
        // if (Input.GetKeyDown(KeyCode.R))
        // {
        //     CycleSlots(ScrollDirection.Right);
        // }
        // else if (Input.GetKeyDown(KeyCode.Q))
        // {
        //     CycleSlots(ScrollDirection.Left);
        // }
        // else if (Input.GetKeyDown(KeyCode.E))
        if (Input.GetKeyDown(KeyCode.E))
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
        (Instantiate(ItemSlot) as GameObject).transform.SetParent(StorageSlots.transform, false);
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

    public void DeselectAllSlots()
    {
        foreach (GameObject slot in activeArmSlots)
        {
            slot.GetComponent<ItemSlot>().selectedSlot.SetActive(false);
            slot.GetComponent<ItemSlot>().thisItemSelected = false;
        }

        foreach (GameObject slot in inactiveSlots)
        {
            slot.GetComponent<ItemSlot>().selectedSlot.SetActive(false);
            slot.GetComponent<ItemSlot>().thisItemSelected = false;
        }
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
            (Instantiate(ItemSlot) as GameObject).transform.SetParent(StorageSlots.transform, false);
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
    }

    // private void CycleSlots(ScrollDirection scrollDirection)
    // {
    //     int lastIndexWithItem = itemSlots.Count;

    //     int scrollFactor = (scrollDirection == ScrollDirection.Right) ? itemSlots[itemSlots.Count - 1].transform.childCount + activeItemSlots.Count - 1 : 1;

    //     for (int _ = 0; _ < scrollFactor; _++) // scroll items - 1 times if scrolling right
    //     {
    //         for (int i = itemSlots.Count - 1; i >= 0; i--)
    //         {
    //             if (itemSlots[i].transform.childCount > 0)
    //             {
    //                 AddItemIntoSlot(
    //                     itemSlots[i].transform.GetChild(0).gameObject,
    //                     itemSlots[(i + lastIndexWithItem - 1) % lastIndexWithItem],
    //                     (i + lastIndexWithItem - 1) % lastIndexWithItem, // Cycle through item slots
    //                     ((i + lastIndexWithItem - 1) % lastIndexWithItem) < activeItemSlots.Count
    //                 );
    //             }
    //             else
    //             {
    //                 lastIndexWithItem = i;
    //             }
    //         }
    //     }

        // for (int i = 0; i < ; i++)
        // {
        //     AddItemIntoSlot(
        //         inactiveSlots[i].gameObject,
        //         itemSlots[itemSlots.Count - 1],
        //         activeArmSlots.Count + i,
        //         false
        //     );
        // }
    // }

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

using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance { get; set; }

    public List<GameObject> startingItems;
    public List<GameObject> itemSlots;
    public List<GameObject> activeItemSlots;

    private void Awake()
    {
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
        for (int i = 0; i < this.transform.childCount - 1; i++)
        {
            activeItemSlots.Add(itemSlots[i]);
        }

        // Fill up inventory with pre-selected items
        foreach (GameObject item in startingItems)
        {
            PickupItem(item);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            CycleSlots(ScrollDirection.Right);
        }
        else if (Input.GetKeyDown(KeyCode.Q))
        {
            CycleSlots(ScrollDirection.Left);
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
                AddItemIntoSlot(pickedUpItem, activeItemSlots[i], true);
                return;
            }
        }

        // Put item into storage if no active slots are available
        AddItemIntoSlot(pickedUpItem, itemSlots[itemSlots.Count - 1], false);
    }

    private void AddItemIntoSlot(GameObject itemAdded, GameObject slot, bool inActiveSlot)
    {
        itemAdded.transform.SetParent(slot.transform, false);

        Item item = itemAdded.GetComponent<Item>();
        item.isActive = inActiveSlot;

        itemAdded.transform.localPosition = new Vector3(item.spawnPosition.x, item.spawnPosition.y, item.spawnPosition.z);
        itemAdded.transform.localRotation = Quaternion.Euler(item.spawnRotation.x, item.spawnRotation.y, item.spawnRotation.z);
    }

    private void CycleSlots(ScrollDirection scrollDirection)
    {
        int lastIndexWithItem = itemSlots.Count;

        int scrollFactor = (scrollDirection == ScrollDirection.Right) ? itemSlots[itemSlots.Count - 1].transform.childCount + activeItemSlots.Count - 1 : 1;

        for (int _ = 0; _ < scrollFactor; _++) // scroll items - 1 times if scrolling right
        {
            for (int i = itemSlots.Count - 1; i >= 0; i--)
            {
                if (itemSlots[i].transform.childCount > 0)
                {
                    AddItemIntoSlot(
                        itemSlots[i].transform.GetChild(0).gameObject,
                        itemSlots[(i + lastIndexWithItem - 1) % lastIndexWithItem], // Cycle through item slots
                        ((i + lastIndexWithItem - 1) % lastIndexWithItem) < activeItemSlots.Count
                    );
                }
                else
                {
                    lastIndexWithItem = i;
                }
            }
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

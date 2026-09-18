using UnityEngine;

public class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance { get; set; }

    public int interactDistance;

    // Nothing should ever access the hovered item
    // The manager accesses other areas USING the hovered item
    private Item hoveredItemCurrent = null;
    private Item hoveredItemPrev = null;
    private const KeyCode pickupKey = KeyCode.F;

    private void Awake()
    {
        // Create singleton of InteractionManager
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Update()
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        // If nothing was hit or if the interacted object was too far away
        if (!Physics.Raycast(ray, out hit) || hit.distance > interactDistance)
        {
            if (hoveredItemCurrent) { hoveredItemCurrent.GetComponent<Outline>().enabled = false; }
            return;
        } 

        GameObject objectHitByRaycast = hit.transform.gameObject;

        // Select item for interaction
        if (objectHitByRaycast.GetComponent<Item>() && !objectHitByRaycast.GetComponent<Item>().isActive)
        {
            // Highlight item
            hoveredItemPrev = hoveredItemCurrent;
            hoveredItemCurrent = objectHitByRaycast.gameObject.GetComponent<Item>();
            hoveredItemCurrent.GetComponent<Outline>().enabled = true;

            // Unhighlight previous item if immediately switching from looking at one to another
            if (hoveredItemPrev && hoveredItemPrev != hoveredItemCurrent)
            {
                hoveredItemPrev.GetComponent<Outline>().enabled = false;
            }

            // Interact with object (can be changed for other objects if needed)
            if (Input.GetKeyDown(pickupKey))
            {
                Inventory.Instance.PickupItem(objectHitByRaycast.gameObject);
            }
        }
        else if (hoveredItemCurrent) // Remove highlight if looking at nothing
        {
            hoveredItemCurrent.GetComponent<Outline>().enabled = false;
        }
    }
}

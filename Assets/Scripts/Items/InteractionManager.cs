using UnityEngine;

public class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance { get; set; }

    public Item hoveredItem = null;

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

    private void Update()
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            GameObject objectHitByRaycast = hit.transform.gameObject;

            if (objectHitByRaycast.GetComponent<Item>() && !objectHitByRaycast.GetComponent<Item>().isActive)
            {
                hoveredItem = objectHitByRaycast.gameObject.GetComponent<Item>();
                hoveredItem.GetComponent<Outline>().enabled = true;

                if (Input.GetKeyDown(KeyCode.F))
                {
                    Inventory.Instance.PickupItem(objectHitByRaycast.gameObject);
                }
            }
            else
            {
                if (hoveredItem)
                {
                    hoveredItem.GetComponent<Outline>().enabled = false;
                }
            }
        }
    }
}

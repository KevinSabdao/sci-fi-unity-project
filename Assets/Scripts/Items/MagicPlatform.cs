using System.Collections;
using UnityEngine;

public class EnergyPlatform : Item
{
    [SerializeField]
    private Vector3 platformScale;
    [SerializeField]
    private int platformStayTime;
    [SerializeField]
    private GameObject platformPrefab;

    private void Update()
    {
        if (!isActive || Inventory.menuActivated) return;

        if (Input.GetKeyDown(keyPressRequired) && this.stackCount > 0)
        {
            // Create new magic platform
            GameObject platform = Instantiate(platformPrefab);
            this.stackCount--;

            // Place the magic platform under the player
            platform.transform.SetParent(null);
            platform.transform.localPosition = GameObject.Find("PlayerTest").transform.position;

            // Destroy the platform and the platform creator
            Destroy(platform, platformStayTime);
            if (this.stackCount <= 0) Destroy(this.gameObject);
        }
    }

}

using System.Collections;
using UnityEngine;

public class Collectible : Item
{
    public Camera playerCamera;

    private void Awake()
    {
    }

    void Update()
    {
        if (!isActive) return;

        // Implement item pickup
        base.checkRequiredKeyPress();
    }

}

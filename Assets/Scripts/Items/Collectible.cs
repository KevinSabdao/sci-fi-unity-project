using System.Collections;
using UnityEngine;

public class Collectible : Item
{
    private void Awake()
    {
    }

    private void Update()
    {
        if (!isActive) return;

        // Implement item pickup
        base.CheckRequiredKeyPress();
    }

}

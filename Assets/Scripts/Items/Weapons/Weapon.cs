using System.Collections;
using UnityEngine;

public abstract class Weapon : Item
{
    public Camera playerCamera;

    // Durability
    public int durabilityMax;
    public int durability;

    // Attacking
    internal bool isAttacking, readyToAttack;
    internal bool allowReset = true;
    public float attackingDelay = 1f;

    // Damage
    public float damage = 25f;

    protected void Awake()
    {
        durability = durabilityMax;
        readyToAttack = true;
        if (!playerCamera) playerCamera = Camera.main;
    }

    protected bool CanUseWeapon()
    {
        // Immediately back out if weapon definitely can't be used
        if (
                !isActive ||
                Inventory.menuActivated ||
                FirstPersonLook.UiHasCursor
        ) return false; 

        // Destroy weapon upon no more durability (can't use weapon)
        if (durability <= 0)
        {
            Destroy(gameObject);
        }

        return true;
    }

    public abstract void UseWeapon();

    protected void ResetAttack()
    {
        readyToAttack = true;
        allowReset = true;
    }
}

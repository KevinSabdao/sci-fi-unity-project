using System.Collections;
using UnityEngine;

public abstract class Weapon : Item
{
    // Durability
    public int durabilityMax;
    public int durability;

    // Attacking
    internal bool isAttacking, readyToAttack;
    internal bool allowReset = true;
    public float attackingDelay = 2f;

    // Damage
    public float damage = 25f;

    protected void Awake()
    {
        durability = durabilityMax;
        readyToAttack = true;
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

    protected void UseWeapon()
    {
        readyToAttack = false;

        // Check if we are done shooting
        if (allowReset)
        {
            Invoke("ResetAttack", attackingDelay);
            allowReset = false;
        }
    }

    protected void ResetAttack()
    {
        readyToAttack = true;
        allowReset = true;
    }
}

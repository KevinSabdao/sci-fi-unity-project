using System.Collections;
using UnityEngine;

public class MeleeWeapon : Weapon
{
    // Damage variation
    public float damageVariation = 1f;
    [Range(0f, 1f)]
    public float critChance = 0f;
    public float critMultiplier = 1f;

    private void Update()
    {
        if (!CanUseWeapon()) return;

        isAttacking = Input.GetKeyDown(keyPressRequired);

        if (readyToAttack && isAttacking)
        {
            UseWeapon();
            durability--;
        }

    }

    new private void UseWeapon()
    {
        readyToAttack = false;

        // Check if we are done swinging
        if (allowReset)
        {
            Invoke("ResetShot", attackingDelay);
            allowReset = false;
        }
    }

    public float CalculateDamage()
    {
        return (damage + Random.Range(damageVariation, damageVariation)) * CalculateCrit();
    }

    public float CalculateCrit()
    {
        return (Random.Range(0f, 1f) <= critChance) ? critMultiplier : 1f;
    }

}

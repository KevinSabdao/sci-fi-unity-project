using System.Collections;
using UnityEngine;

public class MeleeWeapon : Item
{
    // Durability
    public int durabilityMax;
    public int durability;

    // Swinging
    private bool isSwinging, readyToSwing;
    private bool allowReset = true;
    public float swingingDelay = 2f;

    // Damage
    public float damage = 25f;
    [Range(0f, 1f)]
    public float critChance = 0f;
    public float critDamage = 0f;

    public enum WeaponModel
    {
        Pistol,
        MachineGun,
        Sniper
    }

    public WeaponModel thisWeaponModel;

    private void Awake()
    {
        durability = durabilityMax;
        readyToSwing = true;
    }

    private void Update()
    {
        if (!isActive) return; // Update with new fix from other branch

        // the arm keys are mouse buttons, so without this a click on an
        // inventory slot also pulls the trigger
        if (FirstPersonLook.UiHasCursor) return;

        isSwinging = Input.GetKeyDown(keyPressRequired);

        if (readyToSwing && isSwinging)
        {
            SwingWeapon();
            durability--;
        }

        if (durability <= 0)
        {
            Destroy(gameObject);
        }
    }

    private void SwingWeapon()
    {
        readyToSwing = false;

        // Check if we are done swinging
        if (allowReset)
        {
            Invoke("ResetShot", swingingDelay);
            allowReset = false;
        }
    }

    private void ResetSwing()
    {
        readyToSwing = true;
        allowReset = true;
    }

    public float CalculateDamage()
    {
        return 0f;
    }

    public float CalculateCrit()
    {
        return 0f;
    }

}

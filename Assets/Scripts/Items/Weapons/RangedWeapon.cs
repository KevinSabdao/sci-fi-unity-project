using System.Collections;
using UnityEngine;

public class RangedWeapon : Weapon
{
    public Camera playerCamera;

    // Burst
    public int bulletsPerBurst = 3;
    public int burstBulletsLeft;

    // Spread
    public float spreadIntensity;

    // Bullet
    public GameObject bulletPrefab;
    public Transform bulletSpawn;
    public float bulletVelocity = 30;
    public float bulletPrefabLifeTime = 3f;

    public enum ShootingMode
    {
        Single,
        Burst,
        Auto
    }

    public ShootingMode currentShootingMode;

    new private void Awake()
    {
        base.Awake();

        burstBulletsLeft = bulletsPerBurst;
        // the shot is aimed through this camera, so an unassigned reference
        // throws on the first trigger pull rather than at startup
        if (playerCamera == null) playerCamera = Camera.main;
    }

    private void Update()
    {
        if (!CanUseWeapon()) return;

        switch (currentShootingMode)
        {
            // Holding down left mouse button
            case ShootingMode.Auto:
                isAttacking = Input.GetKey(keyPressRequired);
                break;

            // Clicking left mouse button once
            case ShootingMode.Single:
            case ShootingMode.Burst:
                isAttacking = Input.GetKeyDown(keyPressRequired);
                break;
        }

        if (readyToAttack && isAttacking)
        {
            burstBulletsLeft = bulletsPerBurst;
            durability--;
            UseWeapon();
        }

    }

    new private void UseWeapon()
    {
        readyToAttack = false;

        Vector3 shootingDirection = CalculateDirectionAndSpread().normalized;
        
        // Instantiate the bullet
        GameObject bullet = Instantiate(bulletPrefab, bulletSpawn.position, Quaternion.identity);

        // Tell the round how hard this gun hits, without it the bullet is just
        // physics and an enemy never loses health
        if (bullet.TryGetComponent(out Bullet bulletScript)) bulletScript.damage = damage;

        // Point the bullet to face the shooting direction
        bullet.transform.forward = shootingDirection;

        // Shoot the bullet
        bullet.GetComponent<Rigidbody>().AddForce(bullet.transform.forward.normalized * bulletVelocity, ForceMode.Impulse);

        // Destroy the bullet after some time
        Destroy(bullet, bulletPrefabLifeTime);

        // Check if we are done shooting
        if (allowReset)
        {
            Invoke("ResetAttack", attackingDelay);
            allowReset = false;
        }

        // Burst Mode
        if (currentShootingMode == ShootingMode.Burst && burstBulletsLeft > 1) // we already shoot once before this check
        {
            burstBulletsLeft--;
            UseWeapon();
        }
    }

    private Vector3 CalculateDirectionAndSpread()
    {
        // Shooting from the middle of the screen to check where are we pointing at
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        Vector3 targetPoint;
        if (Physics.Raycast(ray, out hit))
        {
            // Hitting something
            targetPoint = hit.point;
        }
        else
        {
            // Shooting at the air
            targetPoint = ray.GetPoint(100);
        }

        Vector3 direction = targetPoint - bulletSpawn.position;

        float x = Random.Range(-spreadIntensity, spreadIntensity);
        float y = Random.Range(-spreadIntensity, spreadIntensity);

        // Returning the shooting direction and spread
        return direction + new Vector3(x, y, 0);
    }
}

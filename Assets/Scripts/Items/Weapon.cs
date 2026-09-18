using System.Collections;
using UnityEngine;

public class Weapon : Item
{
    public Camera playerCamera;

    // Durability
    public int durabilityMax;
    public int durability;

    // Shooting
    private bool isShooting, readyToShoot;
    private bool allowReset = true;
    public float shootingDelay = 2f;

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

    public enum WeaponModel
    {
        Pistol,
        MachineGun,
        Sniper
    }

    public WeaponModel thisWeaponModel;

    public enum ShootingMode
    {
        Single,
        Burst,
        Auto
    }

    public ShootingMode currentShootingMode;

    private void Awake()
    {
        durability = durabilityMax;
        readyToShoot = true;
        burstBulletsLeft = bulletsPerBurst;
    }

    private void Update()
    {
        if (!isActive) return;

        switch (currentShootingMode)
        {
            // Holding down left mouse button
            case ShootingMode.Auto:
                isShooting = Input.GetKey(keyPressRequired);
                break;

            // Clicking left mouse button once
            case ShootingMode.Single:
            case ShootingMode.Burst:
                isShooting = Input.GetKeyDown(keyPressRequired);
                break;
        }

        if (readyToShoot && isShooting)
        {
            burstBulletsLeft = bulletsPerBurst;
            FireWeapon();
            durability--;
        }

        if (durability <= 0)
        {
            Destroy(gameObject);
        }
    }

    private void FireWeapon()
    {
        readyToShoot = false;

        Vector3 shootingDirection = CalculateDirectionAndSpread().normalized;
        
        // Instantiate the bullet
        GameObject bullet = Instantiate(bulletPrefab, bulletSpawn.position, Quaternion.identity);

        // Point the bullet to face the shooting direction
        bullet.transform.forward = shootingDirection;

        // Shoot the bullet
        bullet.GetComponent<Rigidbody>().AddForce(bullet.transform.forward.normalized * bulletVelocity, ForceMode.Impulse);

        // Destroy the bullet after some time
        StartCoroutine(DestroyBulletAfterTime(bullet, bulletPrefabLifeTime));

        // Check if we are done shooting
        if (allowReset)
        {
            Invoke("ResetShot", shootingDelay);
            allowReset = false;
        }

        // Burst Mode
        if (currentShootingMode == ShootingMode.Burst && burstBulletsLeft > 1) // we already shoot once before this check
        {
            burstBulletsLeft--;
            FireWeapon();
        }
    }

    private void ResetShot()
    {
        readyToShoot = true;
        allowReset = true;
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

        float x = UnityEngine.Random.Range(-spreadIntensity, spreadIntensity);
        float y = UnityEngine.Random.Range(-spreadIntensity, spreadIntensity);

        // Returning the shooting direction and spread
        return direction + new Vector3(x, y, 0);
    }

    private IEnumerator DestroyBulletAfterTime(GameObject bullet, float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(bullet);
    }
}

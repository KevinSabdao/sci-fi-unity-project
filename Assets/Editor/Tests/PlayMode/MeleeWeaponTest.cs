using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class MeleeWeaponTest
{
    private GameObject playerTest;
    private MeleeWeapon meleeWeapon;
    private Camera playerCamera;
    private GameObject enemy;

    [SetUp]
    public void Setup()
    {
        // Create new weapon, camera, and enemy
        playerTest = new GameObject();
        meleeWeapon = GameObject.CreatePrimitive(PrimitiveType.Cube).AddComponent<MeleeWeapon>();
        playerCamera = new GameObject().AddComponent<Camera>();
        enemy = GameObject.CreatePrimitive(PrimitiveType.Cube);

        // Set testing values
        meleeWeapon.transform.SetParent(playerTest.transform);
        playerCamera.transform.SetParent(playerTest.transform);
        meleeWeapon.transform.localPosition = new Vector3(3, -1, 4);
        meleeWeapon.transform.localRotation = Quaternion.Euler(new Vector3(0, 180, 0));
        meleeWeapon.playerCamera = playerCamera;
        meleeWeapon.range = 10;
        meleeWeapon.transform.localScale = new Vector3(1, 3, 1);
    }

    [TearDown]
    public void Teardown()
    {
        GameObject.DestroyImmediate(meleeWeapon.gameObject);
        GameObject.DestroyImmediate(playerCamera.gameObject);
        GameObject.DestroyImmediate(enemy);
    }

    [UnityTest]
    public IEnumerator HitEnemySuccess()
    {
        // Set up the positions for attacking
        playerTest.transform.localPosition = new Vector3(0, 0, -8);
        EnemyHealth enemyHealth = enemy.AddComponent<EnemyHealth>();

        // Store the current health to test if it is lowered
        float enemyCurrentHealth = enemyHealth.Current;
        meleeWeapon.UseWeapon();

        yield return new WaitForSeconds(1);
        Assert.That(enemyCurrentHealth, Is.Not.EqualTo(enemyHealth.Current));
    }

    [UnityTest]
    public IEnumerator HitEnemyOutOfRange()
    {
        // Set up the positions for attacking
        playerTest.transform.localPosition = new Vector3(0, 0, -12);
        EnemyHealth enemyHealth = enemy.AddComponent<EnemyHealth>();

        // Store the current health to test if it is lowered
        float enemyCurrentHealth = enemyHealth.Current;
        meleeWeapon.UseWeapon();

        yield return new WaitForSeconds(1);
        Assert.That(enemyCurrentHealth, Is.EqualTo(enemyHealth.Current));
    }

    [UnityTest]
    public IEnumerator HitEnemyNotAnEnemy()
    {
        // Set up the positions for attacking
        playerTest.transform.localPosition = new Vector3(0, 0, -8);

        // Store the current health to test if it is lowered
        meleeWeapon.UseWeapon();

        yield return new WaitForSeconds(1);
        // If it doesn't error, it is successful
    }

    [UnityTest]
    public IEnumerator HitEnemyNoEnemy()
    {
        // Set up the positions for attacking
        playerTest.transform.localPosition = new Vector3(0, 0, -8);
        enemy.transform.localPosition = new Vector3(-4, 0, 0);

        // Store the current health to test if it is lowered
        meleeWeapon.UseWeapon();

        yield return new WaitForSeconds(1);
        // If it doesn't error, it is successful
    }
}

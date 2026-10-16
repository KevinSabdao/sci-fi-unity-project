using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class MeleeWeaponTest
{
    private MeleeWeapon meleeWeapon;

    [SetUp]
    public void Setup()
    {
        meleeWeapon = new GameObject().AddComponent<MeleeWeapon>();
    }

    [TearDown]
    public void Teardown()
    {
        GameObject.DestroyImmediate(meleeWeapon.gameObject);
    }
    
    [Test]
    [Repeat(1000)]
    public void CalculateCritNoChance()
    {
        // Setup damage values
        meleeWeapon.critChance = 0f;
        meleeWeapon.critMultiplier = 10f;

        // Expected output is 0
        Assert.That(meleeWeapon.CalculateCrit(), Is.EqualTo(1f));
    }

    [Test]
    [Repeat(1000)]
    public void CalculateCritFullChance()
    {
        // Setup damage values
        meleeWeapon.critChance = 1f;
        meleeWeapon.critMultiplier = 10f;

        // Test output
        Assert.That(meleeWeapon.CalculateCrit(), Is.EqualTo(10f));
    }

    [Test]
    public void CalculateCritHalfChance()
    {
        // Setup damage values
        meleeWeapon.critChance = 0.5f;
        meleeWeapon.critMultiplier = 10f;

        // Run the random test a bunch of times
        float critDamage;
        int timesWithoutCrit = 0;

        for (int i = 0; i < 1000; i++)
        {
            critDamage = meleeWeapon.CalculateCrit();
            if (critDamage == 1) timesWithoutCrit++;
            else Assert.That(critDamage, Is.EqualTo(meleeWeapon.critMultiplier));
        }

        // Outside of 474 < x < 525 is where the data is statistically significant
        Assert.That(timesWithoutCrit, Is.InRange(474, 525));
    }    

    [Test]
    [Repeat(1000)]
    public void CalculateDamageCritNoChance()
    {
        // Setup damage values
        meleeWeapon.damage = 30f;
        meleeWeapon.damageVariation = 1f;
        meleeWeapon.critChance = 0f;

        // damage +- variation
        Assert.That(meleeWeapon.CalculateDamage(), Is.InRange(29f, 31f));
    }

    [Test]
    [Repeat(1000)]
    public void CalculateDamageCritFullChance()
    {
        // Setup damage values
        meleeWeapon.damage = 30f;
        meleeWeapon.damageVariation = 1f;
        meleeWeapon.critChance = 1f;
        meleeWeapon.critMultiplier = 10f;

        // (damage +- variation) * multiplier
        Assert.That(meleeWeapon.CalculateDamage(), Is.InRange(290f, 310f));
    }

    [Test]
    public void CalculateDamageCritHalfChance()
    {
        // Setup damage values
        meleeWeapon.damage = 30f;
        meleeWeapon.damageVariation = 1f;
        meleeWeapon.critChance = 0.5f;
        meleeWeapon.critMultiplier = 10f;

        // Run the random test a bunch of times
        float damage;
        int timesWithoutCrit = 0;
        for (int i = 0; i < 1000; i++)
        {
            damage = meleeWeapon.CalculateDamage();
            if (damage < 100) timesWithoutCrit++;
            Assert.That(damage, Is.InRange(29f, 31f).Or.InRange(290f, 310f));
        }

        // Outside of 474 < x < 525 is where the data is statistically significant
        Assert.That(timesWithoutCrit, Is.InRange(474, 525));
    }
}


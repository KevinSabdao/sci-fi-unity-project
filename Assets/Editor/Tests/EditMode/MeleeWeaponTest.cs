using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class MeleeWeaponTest
{
    [Test]
    public void CalculateDamage()
    {
        MeleeWeapon meleeWeapon = new MeleeWeapon();
        meleeWeapon.damage = 30f;
        Random.InitState(42);
        Assert.AreEqual(30f, meleeWeapon.CalculateDamage());
    }

}

using System.Collections;
using UnityEngine;

public class MeleeWeapon : Weapon
{
    // Damage variation
    public float damageVariation = 1f;
    [Range(0f, 1f)]
    public float critChance = 0f;
    public float critMultiplier = 1f;
    public float range = 2;

    private readonly Vector3 attackRotation = new Vector3(-90, 180, 0);

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

    public override void UseWeapon()
    {
        readyToAttack = false;

        // I'm not sure how to make an effective attack
        // I know how to use raycasts though
        // I'm sorry, whoever looks at my code next :(
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        Physics.Raycast(ray, out hit);

        // Swing back to the pose the inventory gave the sword in the hand
        Quaternion rotateFrom = HoldLocalRotation();

        // Rotate the sword towards an enemy if targeting one, otherwise swing forwards
        float zRotation;
        if (hit.transform?.gameObject.GetComponentInParent<EnemyHealth>())
        {
            zRotation = 90 - (Mathf.Atan2(hit.distance, -transform.parent.transform.localPosition.x) * 180 / Mathf.PI);
        }
        else
        {
            zRotation = 0;
        }
        Quaternion rotateTo = Quaternion.Euler(attackRotation.x, attackRotation.y, zRotation);
        StartCoroutine(AnimateAttack(rotateTo, rotateFrom));

        // Hit the enemy if it is within range
        if (hit.transform && hit.distance <= range) StartCoroutine(HitEnemy(hit.collider));

        // Check if the attack has finished
        if (allowReset)
        {
            Invoke("ResetAttack", attackingDelay);
            allowReset = false;
        }
    }

    private IEnumerator HitEnemy(Collider hitCollider)
    {
        // Get the enemy that was hit
        EnemyHealth enemyHealth = hitCollider.GetComponentInParent<EnemyHealth>();
        if (!enemyHealth) yield break;

        // Hit when the sword collides
        yield return new WaitForSeconds((attackingDelay / 2));

        // The collider picks the damage zone, such as the boss eye
        // It can be gone after the wait, which counts as a normal hit
        enemyHealth.TakeDamage(CalculateDamage(), hitCollider ? hitCollider : null);
    }

    public float CalculateDamage()
    {
        return (damage + Random.Range(damageVariation, damageVariation)) * CalculateCrit();
    }

    public float CalculateCrit()
    {
        return (Random.Range(0f, 1f) <= critChance) ? critMultiplier : 1f;
    }

    private IEnumerator AnimateAttack(Quaternion rotateTo, Quaternion rotateFrom)
    {
        StartCoroutine(RotateOverTime(rotateTo)); // Swing down
        yield return new WaitForSeconds((attackingDelay /  2));
        StartCoroutine(RotateOverTime(rotateFrom)); // Swing up
    }

    // Thank you person at https://stackoverflow.com/a/61479886
    private IEnumerator RotateOverTime(Quaternion targetRotation)
    {
        // Relative to the arm, so the swing follows the camera while it plays
        Quaternion startRotation = transform.localRotation;

        // Swing for half the attacking delay duration
        float timePassed = 0f;
        while (timePassed < attackingDelay / 2)
        {
            float factor = timePassed / (attackingDelay / 2);
            transform.localRotation = Quaternion.Slerp(startRotation, targetRotation, factor);
            timePassed += Time.deltaTime;

            yield return null;
        }

        // Fix the rotation in case it is wrong by a small amount :)
        transform.localRotation = targetRotation;
    }
}

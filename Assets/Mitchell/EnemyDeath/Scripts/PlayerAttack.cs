using COMP602;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{

    // Update is called once per frame
    void Update()

    {
        if (Raycasting.distanceFromTarget < 8 && Raycasting.target.layer == LayerMask.NameToLayer("Enemy") && Input.GetMouseButtonDown(0))
        {
            if (Raycasting.target.TryGetComponent<EnemyHealth>(out EnemyHealth enemyScript))
            {
                // 3. Successfully found the script! Run the public function
                enemyScript.TakeDamage(10);
            }
            else
            {
                Debug.LogWarning($"Hit {Raycasting.target.name} on Enemy layer, but it is missing the EnemyHealth script!");
            }
        }
    }
}

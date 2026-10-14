using UnityEngine;
using UnityEngine.UI;

public class WeaponDurability : MonoBehaviour
{

    [SerializeField] GameObject leftArm;
    [SerializeField] GameObject rightArm;
    [SerializeField] Image leftDurabilityBackground;
    [SerializeField] Image rightDurabilityBackground;

    void Update()
    {
        if (leftArm.transform.childCount > 0)
        {
            Weapon leftWeapon = leftArm.transform.GetChild(0).GetComponent<Weapon>();
            if (leftWeapon != null)
            {
                leftDurabilityBackground.gameObject.SetActive(true);
                Image leftDurabilityUi = leftDurabilityBackground.transform.GetChild(0).GetComponent<Image>();

                leftDurabilityUi.fillAmount = (float)leftWeapon.durability / leftWeapon.durabilityMax;
            }
            else
            {
                leftDurabilityBackground.gameObject.SetActive(false);


            }
        }
        else
        {
            leftDurabilityBackground.gameObject.SetActive(false);


        }
        if (rightArm.transform.childCount > 0)
        {
            Weapon rightWeapon = rightArm.transform.GetChild(0).GetComponent<Weapon>();
            if (rightWeapon != null)
            {
                rightDurabilityBackground.gameObject.SetActive(true);
                Image rightDurabilityUi = rightDurabilityBackground.transform.GetChild(0).GetComponent<Image>();

                rightDurabilityUi.fillAmount = (float)rightWeapon.durability / rightWeapon.durabilityMax;
            }
            else
            {
                rightDurabilityBackground.gameObject.SetActive(false);
            }
        }
        else
        {
            rightDurabilityBackground.gameObject.SetActive(false);
        }


    }
}

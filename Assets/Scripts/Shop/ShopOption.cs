using UnityEngine;
using UnityEngine.EventSystems;

public class ShopOption : MonoBehaviour, IPointerClickHandler
{


    [SerializeField]
    private ShopUpgradeType upgradeType;

    private ShopManager shop;

    private void Start()
    {
        shop = ShopManager.Instance;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        switch (upgradeType)
        {
            case ShopUpgradeType.Damage:
                shop.SelectDamageUpgrade();
                break;

            case ShopUpgradeType.Durability:
                shop.SelectDurabilityUpgrade();
                break;

            case ShopUpgradeType.Repair:
                shop.SelectRepairUpgrade();
                break;

            case ShopUpgradeType.FireRate:
                shop.SelectFireRateUpgrade();
                break;

            case ShopUpgradeType.AutoFire:
                shop.SelectAutoFireUpgrade();
                break;

            case ShopUpgradeType.Confirm:
                shop.ConfirmUpgrade();
                break;
            case ShopUpgradeType.Decline:
                shop.DeclineUpgrade();
                break;
        }
    }
}
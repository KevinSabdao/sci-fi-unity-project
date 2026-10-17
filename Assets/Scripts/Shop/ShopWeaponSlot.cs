using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShopWeaponSlot : MonoBehaviour, IPointerClickHandler
{
    private Weapon weapon;

    private GameObject selectedSlot;
    private Image image;

    private void Awake()
    {
        
    }

    public void Setup(Weapon newWeapon)
    {
        weapon = newWeapon;

        selectedSlot = transform.GetChild(0).gameObject;
        image = transform.GetChild(1).GetComponent<Image>();

        selectedSlot.SetActive(false);

        image.sprite = weapon.inventoryIcon;
        image.enabled = true;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        ShopManager.Instance.DeselectAllShopSlots();

        selectedSlot.SetActive(true);

        ShopManager.Instance.SelectWeapon(weapon);
    }

    public void Deselect()
    {
        selectedSlot.SetActive(false);
    }
}
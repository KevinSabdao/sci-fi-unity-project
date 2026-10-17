using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [Header("Shop Slots")]
    [SerializeField] private Transform shopSlotsParent;
    [SerializeField] private GameObject itemSlotPrefab;

    [Header("Description")]
    [SerializeField] private Image itemImage;
    [SerializeField] private TMP_Text itemName;
    [SerializeField] private TMP_Text itemDescription;
    public bool ShopOpen { get; private set; }
    private Weapon selectedWeapon;
    private ShopUpgradeType selectedUpgrade;
    private readonly List<ShopWeaponSlot> shopSlots = new();

    //buttons that are hidden depending on upgrade and weapon selected
    [Header("Upgrade Buttons")]
    [SerializeField] private GameObject fireRateButton;
    [SerializeField] private GameObject autoFireButton;
    [Header("Confirmation")]
    [SerializeField] private GameObject confirmButton;
    [SerializeField] private GameObject declineButton;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        ShopOpen = true;
        Instance = this;
        CloseShop();
    }

    public void ToggleShop()
    {
        if (ShopOpen)
        {
            CloseShop();
        }
        else
        {
            OpenShop();
        }
    }

    public void OpenShop()
    {
        if (ShopOpen)
            return;

        ShopOpen = true;

        PopulateShop();

        confirmButton.SetActive(false);
        declineButton.SetActive(false);

        gameObject.SetActive(true);

        FirstPersonLook.PushUiModal();
    }

    public void CloseShop()
    {
        if (!ShopOpen)
            return;

        ShopOpen = false;

        gameObject.SetActive(false);

        FirstPersonLook.PopUiModal();
    }

    private void PopulateShop()
    {
        foreach (Transform child in shopSlotsParent)
        {
            Destroy(child.gameObject);
        }

        shopSlots.Clear();

        foreach (ItemSlot inventorySlot in Inventory.Instance.AllItemSlots)
        {
            if (inventorySlot.itemObject == null)
                continue;

            Weapon weapon =
                inventorySlot.itemObject.GetComponent<Weapon>();

            if (weapon == null)
                continue;

            GameObject slotObject =
                Instantiate(itemSlotPrefab, shopSlotsParent);

            ShopWeaponSlot slot =
                slotObject.AddComponent<ShopWeaponSlot>();

            slot.Setup(weapon);

            shopSlots.Add(slot);
        }
    }

    public void SelectWeapon(Weapon weapon)
    {
        selectedWeapon = weapon;

        UpdateButtonVisibility();

        RefreshDisplay();
    }

    public void DeselectAllShopSlots()
    {
        foreach (ShopWeaponSlot slot in shopSlots)
        {
            slot.Deselect();
        }
    }

    private void RefreshDisplay()
    {
        if (selectedWeapon == null)
            return;

        itemImage.enabled = true;
        itemImage.sprite = selectedWeapon.inventoryIcon;

        itemName.text = selectedWeapon.itemName;

        string attributeText =
            selectedWeapon.hasLifeSteal
            ? $"Life Steal ({selectedWeapon.GetLifeStealPercentage() * 100f}%)"
            : "None";

        itemDescription.text =
            "===== STATS =====" +
            "\nDamage: " +
            selectedWeapon.damage +
            "\nDamage Level: " +
            selectedWeapon.damageLevel +

            "\nDurability: " +
            selectedWeapon.durability +
            "/" +
            selectedWeapon.durabilityMax +

            "\nDurability Level: " +
            selectedWeapon.durabilityLevel +

            "\nFire Rate Level: " +
            selectedWeapon.fireRateLevel +

            "\nAttribute: " +
            attributeText;
    }

    public Weapon GetSelectedWeapon()
    {
        return selectedWeapon;
    }
    public void SelectDamageUpgrade()
    {
        selectedUpgrade = ShopUpgradeType.Damage;

        ShowConfirmationButtons();

        itemDescription.text =
            "Damage Upgrade\n\n" +
            $"Current Damage: {selectedWeapon.damage}\n" +
            $"New Damage: {selectedWeapon.damage + 5}";
    }
    public void SelectDurabilityUpgrade()
    {
        selectedUpgrade = ShopUpgradeType.Durability;

        ShowConfirmationButtons();

        itemDescription.text =
            "Durability Upgrade\n\n" +
            $"Current Max Durability: {selectedWeapon.durabilityMax}\n" +
            $"New Max Durability: {selectedWeapon.durabilityMax + 25}";
    }
    public void SelectRepairUpgrade()
    {
        selectedUpgrade = ShopUpgradeType.Repair;

        ShowConfirmationButtons();

        itemDescription.text =
            "Repair Weapon\n\n" +
            $"Current Durability: {selectedWeapon.durability}/{selectedWeapon.durabilityMax}\n" +
            $"After Repair: {selectedWeapon.durabilityMax}/{selectedWeapon.durabilityMax}";
    }
    public void SelectFireRateUpgrade()
    {
        selectedUpgrade = ShopUpgradeType.FireRate;

        ShowConfirmationButtons();

        itemDescription.text =
            "Fire Rate Upgrade\n\n" +
            $"Current Delay: {selectedWeapon.shootingDelay}\n" +
            $"New Delay: {Mathf.Max(0.05f, selectedWeapon.shootingDelay - 0.05f)}";
    }
    public void SelectAutoFireUpgrade()
    {
        selectedUpgrade = ShopUpgradeType.AutoFire;

        ShowConfirmationButtons();

        itemDescription.text =
            "Auto Fire Upgrade\n\n" +
            "Converts this weapon to automatic fire.";
    }
    public void ConfirmUpgrade()
    {
        if (selectedWeapon == null)
            return;

        switch (selectedUpgrade)
        {
            case ShopUpgradeType.Damage:
                selectedWeapon.damageLevel++;
                selectedWeapon.ApplyUpgrades();
                break;

            case ShopUpgradeType.Durability:
                selectedWeapon.durabilityLevel++;
                selectedWeapon.ApplyUpgrades();
                selectedWeapon.RepairWeapon();
                break;

            case ShopUpgradeType.Repair:
                selectedWeapon.RepairWeapon();
                break;

            case ShopUpgradeType.FireRate:
                selectedWeapon.fireRateLevel++;
                selectedWeapon.ApplyUpgrades();
                break;

            case ShopUpgradeType.AutoFire:
                selectedWeapon.UnlockAutomaticFire();
                break;
        }

        UpdateButtonVisibility();

        selectedUpgrade = ShopUpgradeType.None;

        confirmButton.SetActive(false);
        declineButton.SetActive(false);

        RefreshDisplay();
    }
    public void DeclineUpgrade()
    {
        selectedUpgrade = ShopUpgradeType.None;

        confirmButton.SetActive(false);
        declineButton.SetActive(false);

        RefreshDisplay();
    }
    private void UpdateButtonVisibility()
    {
        if (selectedWeapon == null)
            return;

        fireRateButton.SetActive(
            selectedWeapon.currentShootingMode ==
            Weapon.ShootingMode.Auto
        );

        autoFireButton.SetActive(
            selectedWeapon.currentShootingMode ==
            Weapon.ShootingMode.Single
        );
    }
    private void ShowConfirmationButtons()
    {
        confirmButton.SetActive(true);
        declineButton.SetActive(true);
    }
}
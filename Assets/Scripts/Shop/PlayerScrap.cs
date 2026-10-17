using TMPro;
using UnityEngine;

public class PlayerScrap : MonoBehaviour
{
    public static PlayerScrap Instance { get; private set; }

    [SerializeField] private int scrap;
    [SerializeField] private TMP_Text scrapText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        UpdateDisplay();
    }

    public bool SpendScrap(int amount)
    {
        if (scrap < amount)
            return false;

        scrap -= amount;

        UpdateDisplay();

        return true;
    }

    public void AddScrap(int amount)
    {
        scrap += amount;

        UpdateDisplay();
    }

    public int GetScrap()
    {
        return scrap;
    }

    private void UpdateDisplay()
    {
        if (scrapText != null)
        {
            scrapText.text = $"Scrap: {scrap}";
        }
    }
}
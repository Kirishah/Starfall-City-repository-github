using TMPro;
using UnityEngine;

public class HudTweaker : MonoBehaviour
{
    public InventoryUI iTweaker;
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private string currencySymbol = "$";

    void OnEnable()
    {
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnMoneyChanged += UpdateMoneyDisplay;
            UpdateMoneyDisplay();
        }
    }

    void OnDisable()
    {
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnMoneyChanged -= UpdateMoneyDisplay;
        }
    }

    void UpdateMoneyDisplay()
    {
        if (CurrencyManager.Instance != null)
        {
            moneyText.text = currencySymbol + CurrencyManager.Instance.CurrentMoney.ToString("N0");
        }
        else
        {
            moneyText.text = currencySymbol + "0";
        }
    }

    private void InventoryOpen()
    {
        iTweaker.ToggleInventory();
    }
}

using UnityEngine;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }
    public int CurrentMoney { get; private set; }

    public event System.Action OnMoneyChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Optional: Set starting money
        CurrentMoney = 1000;
    }

    public void AddMoney(int amount)
    {
        CurrentMoney += amount;
        Debug.Log($"CurrencyManager: Added {amount} money. Total: {CurrentMoney}");
        OnMoneyChanged?.Invoke();
    }

    public bool SpendMoney(int amount)
    {
        if (CurrentMoney >= amount)
        {
            CurrentMoney -= amount;
            Debug.Log($"CurrencyManager: Spent {amount} money. Total: {CurrentMoney}");
            OnMoneyChanged?.Invoke();
            return true;
        }
        Debug.LogWarning($"CurrencyManager: Cannot spend {amount} money. Current: {CurrentMoney}");
        return false;
    }
}

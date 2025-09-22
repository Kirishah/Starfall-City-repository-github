using UnityEngine;

public class XPManager : MonoBehaviour
{
    public static XPManager Instance { get; private set; }

    private int _experience = 0;

    public int Experience => _experience;
    public event System.Action OnXPChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void AddExperience(int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning("Cannot add negative experience.");
            return;
        }
        _experience += amount;
        Debug.Log($"Added {amount} XP. Total: {_experience}");
        // TODO: Implement level-up logic if needed
    }
}

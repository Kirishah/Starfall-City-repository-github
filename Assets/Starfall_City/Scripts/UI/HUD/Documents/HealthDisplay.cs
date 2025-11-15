using UnityEngine;
using UnityEngine.UIElements;

public class HealthDisplay : MonoBehaviour
{
    [SerializeField] private UIDocument hudDocument;  

    private Label healthLabel;
    private VisualElement root;

    private void Awake()
    {
        // Auto-find UIDocument if not assigned
        if (hudDocument == null)
        {
            hudDocument = GetComponent<UIDocument>();
            if (hudDocument == null)
            {
                Debug.LogError("HealthDisplay: No UIDocument found on this GameObject! Assign manually.");
                return;
            }
        }

        root = hudDocument.rootVisualElement;
        if (root == null)
        {
            Debug.LogError("HealthDisplay: HUD rootVisualElement is null!");
            return;
        }
    }

    private void Start()
    {
        healthLabel = root.Q<Label>("Health-Value");
        if (healthLabel == null)
        {
            Debug.LogError("HealthDisplay: 'Health-Value' Label not found in HUD.uxml!");
            return;
        }

        SubscribeToEvents();
        UpdateHealthDisplay();  // Initial value
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void SubscribeToEvents()
    {
        if (CharacteristicsManager.Instance != null)
        {
            CharacteristicsManager.Instance.OnCharacteristicChanged += OnHealthChanged;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (CharacteristicsManager.Instance != null)
        {
            CharacteristicsManager.Instance.OnCharacteristicChanged -= OnHealthChanged;
        }
    }

    private void OnHealthChanged(CharacteristicType type, int newValue)
    {
        if (type == CharacteristicType.Health)
        {
            UpdateHealthDisplay(newValue);
        }
    }

    private void UpdateHealthDisplay(int? value = null)
    {
        if (healthLabel == null || CharacteristicsManager.Instance == null) return;

        int currentValue = value ?? CharacteristicsManager.Instance.GetCharacteristicValue(CharacteristicType.Health);
        healthLabel.text = currentValue.ToString();

        Debug.Log($"Health updated to: {currentValue}");
    }

    [ContextMenu("Test Health Increase")]
    public void TestHealthIncrease()
    {
        if (CharacteristicsManager.Instance != null)
        {
            CharacteristicsManager.Instance.ModifyCharacteristic(CharacteristicType.Health, 1);
        }
    }

    [ContextMenu("Test Health Decrease")]
    public void TestHealthDecrease()
    {
        if (CharacteristicsManager.Instance != null)
        {
            CharacteristicsManager.Instance.ModifyCharacteristic(CharacteristicType.Health, -1);
        }
    }

    [ContextMenu("Reset Health to Zero")]
    public void ResetHealthToZero()
    {
        if (CharacteristicsManager.Instance != null)
        {
            CharacteristicsManager.Instance.SetCharacteristic(CharacteristicType.Health, 0);
        }
    }
}

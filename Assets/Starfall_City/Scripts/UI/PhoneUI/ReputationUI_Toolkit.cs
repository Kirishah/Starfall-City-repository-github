using System;
using UnityEngine;
using UnityEngine.UIElements;

public class ReputationUI_Toolkit : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    private VisualElement root;
    private Label nameLabel;
    private Label valueLabel;
    private VisualElement positiveFill;
    private VisualElement negativeFill;

    void Start()
    {
        if (uiDocument == null) return;

        root = uiDocument.rootVisualElement;
        InitializeElements();
        SubscribeToEvents();
        RefreshUI();  // Initial update
    }

    void OnEnable()
    {
        if (root != null)
        {
            RefreshUI();
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void InitializeElements()
    {
        var container = root.Q<VisualElement>(classes: new[] { "reputation" });
        if (container != null)
        {
            nameLabel = container.Q<Label>("NameLabel");
            valueLabel = container.Q<Label>("ValueLabel");
            positiveFill = container.Q<VisualElement>("PositiveFill");
            negativeFill = container.Q<VisualElement>("NegativeFill");
        }
        else
        {
            Debug.LogError("ReputationUI_Toolkit: .reputation container not found in UXML!");
        }
    }

    private void SubscribeToEvents()
    {
        if (CharacteristicsManager.Instance != null)
        {
            CharacteristicsManager.Instance.OnCharacteristicChanged += OnReputationChanged;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (CharacteristicsManager.Instance != null)
        {
            CharacteristicsManager.Instance.OnCharacteristicChanged -= OnReputationChanged;
        }
    }

    private void OnReputationChanged(CharacteristicType type, int newValue)
    {
        if (type == CharacteristicType.Reputation)
        {
            UpdateBar(newValue);
        }
    }

    public void RefreshUI()
    {
        if (CharacteristicsManager.Instance == null) return;

        int value = CharacteristicsManager.Instance.GetCharacteristicValue(CharacteristicType.Reputation);
        UpdateBar(value);
    }

    private void UpdateBar(int value)
    {
        if (valueLabel == null) return;

        valueLabel.text = value.ToString();

        float absValue = Mathf.Abs(value);
        float fillPercent = (absValue / 5f) * 50f; // Half the bar is 50% width (matches original logic)

        if (value >= 0)
        {
            if (positiveFill != null)
            {
                positiveFill.style.display = DisplayStyle.Flex;
                positiveFill.style.width = new StyleLength(Length.Percent(fillPercent));
            }
            if (negativeFill != null) negativeFill.style.display = DisplayStyle.None;
        }
        else
        {
            if (positiveFill != null) positiveFill.style.display = DisplayStyle.None;
            if (negativeFill != null)
            {
                negativeFill.style.display = DisplayStyle.Flex;
                negativeFill.style.width = new StyleLength(Length.Percent(fillPercent));
            }
        }

        var characteristic = CharacteristicsManager.Instance?.GetCharacteristic(CharacteristicType.Reputation);
        if (characteristic != null && nameLabel != null)
        {
            nameLabel.text = characteristic.DisplayName;
        }
    }

    [ContextMenu("Test Positive Value")]
    public void TestPositiveValue()
    {
        if (CharacteristicsManager.Instance != null)
        {
            CharacteristicsManager.Instance.SetCharacteristic(CharacteristicType.Reputation, 4);
        }
    }

    [ContextMenu("Test Negative Value")]
    public void TestNegativeValue()
    {
        if (CharacteristicsManager.Instance != null)
        {
            CharacteristicsManager.Instance.SetCharacteristic(CharacteristicType.Reputation, -3);
        }
    }

    [ContextMenu("Reset to Zero")]
    public void ResetToZero()
    {
        if (CharacteristicsManager.Instance != null)
        {
            CharacteristicsManager.Instance.SetCharacteristic(CharacteristicType.Reputation, 0);
        }
    }
}

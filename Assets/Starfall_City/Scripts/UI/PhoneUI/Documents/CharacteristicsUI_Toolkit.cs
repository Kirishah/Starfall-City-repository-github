using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CharacteristicsUI_Toolkit : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    private VisualElement root;
    private Dictionary<CharacteristicType, BarElements> barsDictionary = new();

    private struct BarElements
    {
        public Label nameLabel;
        public Label valueLabel;
        public VisualElement positiveFill;
        public VisualElement negativeFill;
    }

    void Start()
    {
        if (uiDocument == null) return;

        root = uiDocument.rootVisualElement;
        InitializeBars();
        SubscribeToEvents();
        UpdateAllBars();
    }

    void OnEnable()
    {
        if (root != null)
        {
            UpdateAllBars();
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void InitializeBars()
    {
        // Map each type to its elements (containers queried by type-specific class like .reputation)
        foreach (CharacteristicType type in Enum.GetValues(typeof(CharacteristicType)))
        {
            string typeClass = type.ToString().ToLower();
            VisualElement container = root.Q<VisualElement>(classes: new[] { typeClass });
            if (container != null)
            {
                barsDictionary.Add(type, new BarElements
                {
                    nameLabel = container.Q<Label>("NameLabel"),
                    valueLabel = container.Q<Label>("ValueLabel"),
                    positiveFill = container.Q<VisualElement>("PositiveFill"),
                    negativeFill = container.Q<VisualElement>("NegativeFill")
                });
            }
        }
    }

    private void SubscribeToEvents()
    {
        if (CharacteristicsManager.Instance != null)
        {
            CharacteristicsManager.Instance.OnCharacteristicChanged += OnCharacteristicChanged;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (CharacteristicsManager.Instance != null)
        {
            CharacteristicsManager.Instance.OnCharacteristicChanged -= OnCharacteristicChanged;
        }
    }

    private void OnCharacteristicChanged(CharacteristicType type, int newValue)
    {
        UpdateBar(type, newValue);
    }

    public void UpdateAllBars()
    {
        if (CharacteristicsManager.Instance == null) return;

        foreach (CharacteristicType type in Enum.GetValues(typeof(CharacteristicType)))
        {
            int value = CharacteristicsManager.Instance.GetCharacteristicValue(type);
            UpdateBar(type, value);
        }
    }

    private void UpdateBar(CharacteristicType type, int value)
    {
        if (barsDictionary.TryGetValue(type, out BarElements elements))
        {
            elements.valueLabel.text = value.ToString();

            float absValue = Mathf.Abs(value);
            float fillPercent = (absValue / 5f) * 50f; // Half the bar is 50% width

            if (value >= 0)
            {
                elements.positiveFill.style.display = DisplayStyle.Flex;
                elements.positiveFill.style.width = new StyleLength(Length.Percent(fillPercent));
                elements.negativeFill.style.display = DisplayStyle.None;
            }
            else
            {
                elements.positiveFill.style.display = DisplayStyle.None;
                elements.negativeFill.style.display = DisplayStyle.Flex;
                elements.negativeFill.style.width = new StyleLength(Length.Percent(fillPercent));
            }

            var characteristic = CharacteristicsManager.Instance?.GetCharacteristic(type);
            if (characteristic != null)
            {
                elements.nameLabel.text = characteristic.DisplayName;
            }
        }
    }

    [ContextMenu("Test Positive Values")]
    public void TestPositiveValues()
    {
        if (CharacteristicsManager.Instance != null)
        {
            CharacteristicsManager.Instance.SetCharacteristic(CharacteristicType.Reputation, 4);
            CharacteristicsManager.Instance.SetCharacteristic(CharacteristicType.Blockhead, 2);
            CharacteristicsManager.Instance.SetCharacteristic(CharacteristicType.Aura, 5);
        }
    }

    [ContextMenu("Test Negative Values")]
    public void TestNegativeValues()
    {
        if (CharacteristicsManager.Instance != null)
        {
            CharacteristicsManager.Instance.SetCharacteristic(CharacteristicType.Reputation, -4);
            CharacteristicsManager.Instance.SetCharacteristic(CharacteristicType.Blockhead, -1);
            CharacteristicsManager.Instance.SetCharacteristic(CharacteristicType.Aura, -3);
        }
    }

    [ContextMenu("Test Mixed Values")]
    public void TestMixedValues()
    {
        if (CharacteristicsManager.Instance != null)
        {
            CharacteristicsManager.Instance.SetCharacteristic(CharacteristicType.Reputation, -3);
            CharacteristicsManager.Instance.SetCharacteristic(CharacteristicType.Blockhead, 4);
            CharacteristicsManager.Instance.SetCharacteristic(CharacteristicType.Aura, -1);
        }
    }

    [ContextMenu("Reset All to Zero")]
    public void ResetAllToZero()
    {
        if (CharacteristicsManager.Instance != null)
        {
            CharacteristicsManager.Instance.SetCharacteristic(CharacteristicType.Reputation, 0);
            CharacteristicsManager.Instance.SetCharacteristic(CharacteristicType.Blockhead, 0);
            CharacteristicsManager.Instance.SetCharacteristic(CharacteristicType.Aura, 0);
        }
    }
}

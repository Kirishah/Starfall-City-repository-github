using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacteristicsUI : MonoBehaviour
{
    [Header("Characteristic Bars")]
    [SerializeField] private List<CharacteristicBar> characteristicBars = new List<CharacteristicBar>();

    [Header("Colors")]
    [SerializeField] private Color healthPositiveColor = new Color(0.447f, 0.824f, 0.447f); // Green
    [SerializeField] private Color healthNegativeColor = new Color(0.824f, 0.447f, 0.447f);
    [SerializeField] private Color reputationPositiveColor = new Color(0.447f, 0.667f, 0.824f); // Blue
    [SerializeField] private Color reputationNegativeColor = new Color(0.824f, 0.447f, 0.667f);
    [SerializeField] private Color blockheadPositiveColor = new Color(0.824f, 0.667f, 0.447f); // Orange
    [SerializeField] private Color blockheadNegativeColor = new Color(0.667f, 0.447f, 0.824f);
    [SerializeField] private Color auraPositiveColor = new Color(0.824f, 0.447f, 0.824f); // Purple
    [SerializeField] private Color auraNegativeColor = new Color(0.447f, 0.824f, 0.824f);

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Image backgroundPanel;

    private Dictionary<CharacteristicType, CharacteristicBar> barsDictionary = new Dictionary<CharacteristicType, CharacteristicBar>();

    void Start()
    {
        InitializeUI();
        SubscribeToEvents();
        UpdateAllBars();
        ApplyStyling();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void InitializeUI()
    {
        // Create dictionary for easy access
        foreach (var bar in characteristicBars)
        {
            if (!barsDictionary.ContainsKey(bar.Type))
            {
                barsDictionary.Add(bar.Type, bar);

                // Configure slider for -5 to 5 range
                bar.slider.minValue = -5;
                bar.slider.maxValue = 5;
            }
        }
    }

    private void ApplyStyling()
    {
        // Set dark theme background
        if (backgroundPanel != null)
        {
            backgroundPanel.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        }

        // Style each bar with its specific color
        foreach (var bar in characteristicBars)
        {
            Color positiveColor = GetPositiveColorForType(bar.Type);
            Color negativeColor = GetNegativeColorForType(bar.Type);

            bar.positiveFillImage.color = positiveColor;
            bar.negativeFillImage.color = negativeColor;

            // Set background to dark gray
            if (bar.backgroundImage != null)
            {
                bar.backgroundImage.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);
            }

            // Style text
            bar.nameText.color = Color.white;
            bar.valueText.color = Color.white;
            bar.valueText.fontStyle = FontStyles.Bold;
        }

        // Style title
        if (titleText != null)
        {
            titleText.color = Color.white;
            titleText.fontStyle = FontStyles.Bold;
        }
    }

    private Color GetPositiveColorForType(CharacteristicType type)
    {
        return type switch
        {
            CharacteristicType.Health => healthPositiveColor,
            CharacteristicType.Reputation => reputationPositiveColor,
            CharacteristicType.Blockhead => blockheadPositiveColor,
            CharacteristicType.Aura => auraPositiveColor,
            _ => Color.green
        };
    }

    private Color GetNegativeColorForType(CharacteristicType type)
    {
        return type switch
        {
            CharacteristicType.Health => healthNegativeColor,
            CharacteristicType.Reputation => reputationNegativeColor,
            CharacteristicType.Blockhead => blockheadNegativeColor,
            CharacteristicType.Aura => auraNegativeColor,
            _ => Color.red
        };
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

    private void UpdateAllBars()
    {
        if (CharacteristicsManager.Instance == null) return;

        foreach (var bar in characteristicBars)
        {
            int value = CharacteristicsManager.Instance.GetCharacteristicValue(bar.Type);
            UpdateBar(bar.Type, value);
        }
    }

    private void UpdateBar(CharacteristicType type, int value)
    {
        if (barsDictionary.TryGetValue(type, out CharacteristicBar bar))
        {
            bar.slider.value = value;
            bar.valueText.text = value.ToString();

            // Show/hide fill images based on value
            if (value >= 0)
            {
                bar.positiveFillImage.gameObject.SetActive(true);
                bar.negativeFillImage.gameObject.SetActive(false);
            }
            else
            {
                bar.positiveFillImage.gameObject.SetActive(false);
                bar.negativeFillImage.gameObject.SetActive(true);
            }

            // Update name text based on type
            var characteristic = CharacteristicsManager.Instance?.GetCharacteristic(type);
            if (characteristic != null)
            {
                bar.nameText.text = characteristic.DisplayName;
            }
        }
    }

    // Method to manually update a specific bar (useful for testing)
    public void UpdateCharacteristicBar(CharacteristicType type)
    {
        if (CharacteristicsManager.Instance != null)
        {
            int value = CharacteristicsManager.Instance.GetCharacteristicValue(type);
            UpdateBar(type, value);
        }
    }

    // Method to simulate characteristic changes (for testing in inspector)
    [ContextMenu("Test Health Increase")]
    public void TestHealthIncrease()
    {
        CharacteristicsManager.Instance?.ModifyCharacteristic(CharacteristicType.Health, 1);
    }

    [ContextMenu("Test Health Decrease")]
    public void TestHealthDecrease()
    {
        CharacteristicsManager.Instance?.ModifyCharacteristic(CharacteristicType.Health, -1);
    }
}

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace CharacteristicsSystem
{
    public class ReputationUI_Toolkit : MonoBehaviour
    {
        [SerializeField] private UIDocument _uiDocument;

        private VisualElement _root;
        private Label _nameLabel;
        private Label _valueLabel;
        private VisualElement _positiveFill;
        private VisualElement _negativeFill;

        void Start()
        {
            if (_uiDocument == null) return;

            _root = _uiDocument.rootVisualElement;
            InitializeElements();
            SubscribeToEvents();
            RefreshUI();  // Initial update
        }

        void OnEnable()
        {
            if (_root != null)
            {
                RefreshUI();
            }
        }

        private void OnDestroy() => UnsubscribeFromEvents();

        private void InitializeElements()
        {
            var container = _root.Q<VisualElement>(classes: new[] { "reputation" });
            if (container != null)
            {
                _nameLabel = container.Q<Label>("NameLabel");
                _valueLabel = container.Q<Label>("ValueLabel");
                _positiveFill = container.Q<VisualElement>("PositiveFill");
                _negativeFill = container.Q<VisualElement>("NegativeFill");
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
            if (_valueLabel == null) return;

            _valueLabel.text = value.ToString();

            float absValue = Mathf.Abs(value);
            float fillPercent = (absValue / 5f) * 50f; // Half the bar is 50% width (matches original logic)

            if (value >= 0)
            {
                if (_positiveFill != null)
                {
                    _positiveFill.style.display = DisplayStyle.Flex;
                    _positiveFill.style.width = new StyleLength(Length.Percent(fillPercent));
                }
                if (_negativeFill != null) _negativeFill.style.display = DisplayStyle.None;
            }
            else
            {
                if (_positiveFill != null) _positiveFill.style.display = DisplayStyle.None;
                if (_negativeFill != null)
                {
                    _negativeFill.style.display = DisplayStyle.Flex;
                    _negativeFill.style.width = new StyleLength(Length.Percent(fillPercent));
                }
            }

            var characteristic = CharacteristicsManager.Instance.GetCharacteristic(CharacteristicType.Reputation);
            if (characteristic != null && _nameLabel != null)
            {
                _nameLabel.text = characteristic.DisplayName;
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
}

using System;
using UnityEngine;

    [System.Serializable]
    public class Characteristics
    {
        public CharacteristicType Type;
        public int CurrentValue;
        public int MinValue;
        public int MaxValue;
        public string DisplayName;

        public event Action<int> OnValueChanged;

        public void Modify(int amount)
        {
            int newValue = Mathf.Clamp(CurrentValue + amount, MinValue, MaxValue);
            if (newValue != CurrentValue)
            {
                CurrentValue = newValue;
                OnValueChanged?.Invoke(CurrentValue);
            }
        }

        public void Set(int value)
        {
            int clampedValue = Mathf.Clamp(value, MinValue, MaxValue);
            if (clampedValue != CurrentValue)
            {
                CurrentValue = clampedValue;
                OnValueChanged?.Invoke(CurrentValue);
            }
        }

        public bool MeetsRequirement(int requiredValue)
        {
            return CurrentValue >= requiredValue;
        }
    }
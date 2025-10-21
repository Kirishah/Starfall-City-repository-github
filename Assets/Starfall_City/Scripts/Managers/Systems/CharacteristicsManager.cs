using System;
using System.Collections.Generic;
using UnityEngine;


public class CharacteristicsManager : MonoBehaviour
{
    public static CharacteristicsManager Instance { get; private set; }

    [SerializeField] private List<Characteristics> _characteristics = new List<Characteristics>();

    // Events for characteristic changes
    public event Action<CharacteristicType, int> OnCharacteristicChanged;

    #region Initialization
    private void Awake()
    {
        InitializeSingleton();
        InitializeCharacteristics();
    }

    private void InitializeSingleton()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void InitializeCharacteristics()
    {
        // Initialize default characteristics if none exist
        if (_characteristics.Count == 0)
        {
            _characteristics = new List<Characteristics>
        {
            new Characteristics { Type = CharacteristicType.Health, CurrentValue = 0, MinValue = -5, MaxValue = 5, DisplayName = "Health" },
            new Characteristics { Type = CharacteristicType.Reputation, CurrentValue = 0, MinValue = -5, MaxValue = 5, DisplayName = "Reputation" },
            new Characteristics { Type = CharacteristicType.Blockhead, CurrentValue = 0, MinValue = -5, MaxValue = 5, DisplayName = "Blockhead" },
            new Characteristics { Type = CharacteristicType.Aura, CurrentValue = 0, MinValue = -5, MaxValue = 5, DisplayName = "Aura" }
        };
        }

        // Subscribe to change events
        foreach (var charac in _characteristics)
        {
            charac.OnValueChanged += (value) => OnCharacteristicChanged?.Invoke(charac.Type, value);
        }
    }
    #endregion

    #region Public Methods
    public void ModifyCharacteristic(CharacteristicType type, int amount)
    {
        Characteristics charac = GetCharacteristic(type);
        if (charac != null)
        {
            charac.Modify(amount);
            Debug.Log($"{charac.DisplayName} changed by {amount}. Current: {charac.CurrentValue}");
        }
    }

    public void SetCharacteristic(CharacteristicType type, int value)
    {
        Characteristics charac = GetCharacteristic(type);
        if (charac != null)
        {
            charac.Set(value);
        }
    }

    public int GetCharacteristicValue(CharacteristicType type)
    {
        Characteristics charac = GetCharacteristic(type);
        return charac?.CurrentValue ?? 0;
    }

    public bool CheckRequirement(CharacteristicType type, int requiredValue)
    {
        Characteristics charac = GetCharacteristic(type);
        return charac?.MeetsRequirement(requiredValue) ?? false;
    }

    public Characteristics GetCharacteristic(CharacteristicType type)
    {
        return _characteristics.Find(c => c.Type == type);
    }
    #endregion

    #region Item Effects
    public void ApplyItemEffect(string itemID)
    {
        // Define how different items affect characteristics
        switch (itemID.ToLower())
        {
            case "health_potion":
                ModifyCharacteristic(CharacteristicType.Health, 2);
                break;
            case "cigarettes":
                ModifyCharacteristic(CharacteristicType.Health, -1);
                ModifyCharacteristic(CharacteristicType.Blockhead, 1);
                break;
            case "alcohol":
                ModifyCharacteristic(CharacteristicType.Health, -1);
                ModifyCharacteristic(CharacteristicType.Blockhead, 2);
                ModifyCharacteristic(CharacteristicType.Aura, -1);
                break;
            case "nice_clothes":
                ModifyCharacteristic(CharacteristicType.Aura, 2);
                break;
                // Add more items as needed
        }
    }
    #endregion

    #region Save/Load
    [System.Serializable]
    private class CharacteristicsSaveData
    {
        public List<Characteristics> Characteristics;
    }

    public void SaveCharacteristics()
    {
        CharacteristicsSaveData data = new CharacteristicsSaveData { Characteristics = _characteristics };
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString("CharacteristicsData", json);
        Debug.Log("Characteristics saved");
    }

    public void LoadCharacteristics()
    {
        if (PlayerPrefs.HasKey("CharacteristicsData"))
        {
            string json = PlayerPrefs.GetString("CharacteristicsData");
            CharacteristicsSaveData data = JsonUtility.FromJson<CharacteristicsSaveData>(json);
            if (data != null && data.Characteristics != null)
            {
                _characteristics = data.Characteristics;

                // Update all UI and systems
                foreach (var charac in _characteristics)
                {
                    OnCharacteristicChanged?.Invoke(charac.Type, charac.CurrentValue);
                }

                Debug.Log("Characteristics loaded");
            }
        }
        else
        {
            Debug.Log("No characteristics save data found");
        }
    }
    #endregion
}

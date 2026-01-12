using System.Collections.Generic;
using UnityEngine;

namespace CharacteristicsSystem
{
    public class EffectsManager : MonoBehaviour
    {
        public static EffectsManager Instance { get; private set; }
        private readonly Dictionary<string, DialogueEffectsConfig> _effectsConfigs = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            LoadEffectsConfigs();
        }

        private void LoadEffectsConfigs()
        {
            var allConfigs = Resources.LoadAll<DialogueEffectsConfig>("DialogueEffects/");
            foreach (var config in allConfigs)
            {
                if (!_effectsConfigs.ContainsKey(config.configID))
                {
                    _effectsConfigs[config.configID] = config;
                }
            }
            Debug.Log($"Loaded {_effectsConfigs.Count} effects configs from Resources.");
        }

        /// <summary>
        /// Applies characteristic effects based on points and effectType (e.g., from dialogues, quests).
        /// Matches point ranges in the config and modifies via CharacteristicsManager.
        /// </summary>
        public void ApplyPoints(string effectType, int points)
        {
            if (points == 0 || string.IsNullOrEmpty(effectType))
            {
                Debug.Log("Skipping effects: 0 points or empty effectType.");
                return;
            }

            if (CharacteristicsManager.Instance == null)
            {
                Debug.LogWarning("CharacteristicsManager missing—skipping effects.");
                return;
            }

            if (_effectsConfigs.TryGetValue(effectType, out var config))
            {
                config.ApplyEffects(points, CharacteristicsManager.Instance);
            }
            else
            {
                Debug.LogWarning($"No config found for effectType '{effectType}'—skipping effects.");
                // Optional: Add fallback hardcoded logic here, e.g., ApplyDefaultKiryaStarPoints(points);
            }
        }

        /// <summary>
        /// Adds or updates a config at runtime (e.g., from JSON or dynamic events).
        /// </summary>
        public void AddOrUpdateConfig(string effectType, DialogueEffectsConfig config)
        {
            _effectsConfigs[effectType] = config;
            Debug.Log($"Added/updated config for '{effectType}' at runtime.");
        }

        /// <summary>
        /// Optional fallback for undefined types (e.g., original KiryaStar logic).
        /// Call this in ApplyPoints if you want a default.
        /// </summary>
        private void ApplyDefaultKiryaStarPoints(int points)
        {
            if (points >= 3)
            {
                CharacteristicsManager.Instance.ModifyCharacteristic(CharacteristicType.Aura, 1);
            }
            else if (points == 2)
            {
                CharacteristicsManager.Instance.ModifyCharacteristic(CharacteristicType.Blockhead, 1);
                CharacteristicsManager.Instance.ModifyCharacteristic(CharacteristicType.Reputation, -1);
            }
            else if (points <= 0)
            {
                CharacteristicsManager.Instance.ModifyCharacteristic(CharacteristicType.Reputation, -1);
            }
            // points == 1: No-op
        }

        [ContextMenu("Test KiryaStar Effects")]
        public void TestEffects() => EffectsManager.Instance.ApplyPoints("kiryaStar", 3);
    }
}

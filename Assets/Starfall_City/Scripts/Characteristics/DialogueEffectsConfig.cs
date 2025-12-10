using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DialogueEffectsConfig", menuName = "Dialogue/Effects Config", order = 1)]
public class DialogueEffectsConfig : ScriptableObject
{
    [System.Serializable]
    public class PointEffect
    {
        public int minPoints;  // Inclusive lower bound (e.g., <=0 uses this)
        public int maxPoints;  // Inclusive upper bound (e.g., >=3 uses this)
        public CharacteristicType characteristic;
        public int deltaValue;  // +1 or -1, etc.
    }

    public string configID;  // e.g., "kiryaStar" (matches Dialogue.effectType)
    public List<PointEffect> effects = new List<PointEffect>();

    // Apply effects for given points
    public void ApplyEffects(int points, CharacteristicsManager manager)
    {
        if (manager == null) return;

        foreach (var effect in effects)
        {
            if (points >= effect.minPoints && points <= effect.maxPoints)
            {
                manager.ModifyCharacteristic(effect.characteristic, effect.deltaValue);
                Debug.Log($"Applied {effect.deltaValue} to {effect.characteristic} for points {points} (config: {configID})");
            }
        }
    }
}

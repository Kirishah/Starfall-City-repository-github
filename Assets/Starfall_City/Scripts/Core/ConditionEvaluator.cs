using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace core
{
    public static class ConditionEvaluator
    {
        public static bool Evaluate(string condition, Dialogue currentDialogue = null)
        {
            if (string.IsNullOrEmpty(condition))
                return true;
            Debug.Log($"Evaluating condition for dialogue {currentDialogue?.id}: {condition}");
            // Split on logical operators
            var conditionParts = Regex.Split(condition, @"\s*(&&|\|\|)\s*")
                .Select(part => part.Trim())
                .Where(part => !string.IsNullOrWhiteSpace(part) && part != "&&" && part != "||")
                .ToArray();
            var operators = Regex.Matches(condition, @"\s*(&&|\|\|)\s*")
                .Cast<System.Text.RegularExpressions.Match>()
                .Select(match => match.Groups[1].Value)
                .ToList();
            if (conditionParts.Length == 0)
            {
                Debug.LogError($"No valid conditions found in: {condition}");
                return false;
            }
            bool result = EvaluateSingle(conditionParts[0], condition, currentDialogue);
            for (int i = 0; i < operators.Count && i + 1 < conditionParts.Length; i++)
            {
                bool nextCondition = EvaluateSingle(conditionParts[i + 1], condition, currentDialogue);
                if (operators[i] == "&&")
                    result = result && nextCondition;
                else if (operators[i] == "||")
                    result = result || nextCondition;
            }
            return result;
        }

        private static bool EvaluateSingle(string condition, string fullCondition, Dialogue currentDialogue)
        {
            if (string.IsNullOrWhiteSpace(condition))
            {
                Debug.LogError($"Empty condition part in dialogue {currentDialogue?.id}: {fullCondition}");
                return false;
            }
            var parts = condition.Split(':');
            if (parts.Length < 2)
            {
                Debug.LogError($"Invalid condition format in dialogue {currentDialogue?.id}, condition '{fullCondition}': {condition}");
                return false;
            }
            string conditionType = parts[0];
            switch (conditionType)
            {
                case "Char":
                    return EvaluateCharacteristic(parts, fullCondition, currentDialogue);
                case "QuestCompleted":
                    if (parts.Length != 2) return false;
                    var questSO = Resources.Load<QuestSO>("Quests/" + parts[1]);
                    if (questSO == null)
                    {
                        Debug.LogError($"QuestSO not found for QuestCompleted in dialogue {currentDialogue?.id}: {parts[1]}");
                        return false;
                    }
                    return QuestMemory.Instance.IsQuestCompleted(questSO);
                case "HasItem":
                    if (parts.Length != 3) return false;
                    Item item = ItemDataBase.Instance.GetItemByID(parts[1]);
                    if (item == null) return false;
                    if (!int.TryParse(parts[2], out int requiredAmount))
                    {
                        Debug.LogError($"Invalid amount in HasItem in dialogue {currentDialogue?.id}: {parts[2]}");
                        return false;
                    }
                    return InventoryManager.Instance.HasItem(item, requiredAmount);
                case "IsQuestObjectiveActive":
                    if (parts.Length != 3) return false;
                    return IsQuestObjectiveActive(parts[1], parts[2]);
                case "GameEventTriggered":
                    if (parts.Length != 2) return false;
                    return GameEventManager.Instance.IsEventTriggered(parts[1]);
                default:
                    Debug.LogError($"Unknown condition type in dialogue {currentDialogue?.id}: {conditionType}");
                    return false;
            }
        }

        private static bool EvaluateCharacteristic(string[] parts, string fullCondition, Dialogue currentDialogue)
        {
            if (parts.Length != 3)
            {
                Debug.LogError($"Invalid characteristic condition format: {fullCondition}");
                return false;
            }
            if (!Enum.TryParse<CharacteristicType>(parts[1], out CharacteristicType charType))
            {
                Debug.LogError($"Unknown characteristic type: {parts[1]}");
                return false;
            }
            if (!int.TryParse(parts[2], out int requiredValue))
            {
                Debug.LogError($"Invalid required value: {parts[2]}");
                return false;
            }
            return CharacteristicsManager.Instance.CheckRequirement(charType, requiredValue);
        }

        private static bool IsQuestObjectiveActive(string npcID, string itemID)
        {
            var activeQuests = QuestManager.Instance.GetActiveQuests();
            foreach (var quest in activeQuests)
            {
                var currentObj = quest.GetCurrentObjective();
                if (currentObj?.Type == ObjectiveType.GiveItem && currentObj is GiveItemObjective giveItemObj &&
                    giveItemObj.TargetNPCID == npcID && giveItemObj.TargetItemID == itemID && !currentObj.IsCompleted)
                {
                    return true;
                }
            }
            return false;
        }
    } 
}

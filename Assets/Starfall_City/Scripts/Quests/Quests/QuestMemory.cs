using System.Collections.Generic;
using UnityEngine;

namespace QuestSystem
{
    public class QuestMemory : MonoBehaviour
    {
        public static QuestMemory Instance { get; private set; }
        private readonly HashSet<string> _completedQuests = new();
        private readonly HashSet<string> _completedObjectives = new();

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("!!! PLAYERPREFS WIPED !!!");

            LoadCompletedQuests();
            LoadCompletedObjectives();
        }

        public void MarkQuestCompleted(QuestSO quest)
        {
            if (quest != null && !string.IsNullOrEmpty(quest.QuestID) && !_completedQuests.Contains(quest.QuestID))
            {
                _completedQuests.Add(quest.QuestID);
                SaveCompletedQuests();
                Debug.Log($"Marked quest {quest.QuestID} as completed.");
            }
        }

        public bool IsQuestCompleted(QuestSO quest) => quest != null && !string.IsNullOrEmpty(quest.QuestID) && _completedQuests.Contains(quest.QuestID);

        public void MarkObjectiveCompleted(string objectiveID)
        {
            if (!string.IsNullOrEmpty(objectiveID) && !_completedObjectives.Contains(objectiveID))
            {
                _completedObjectives.Add(objectiveID);
                SaveCompletedObjectives();
                Debug.Log($"Marked objective {objectiveID} as completed.");
            }
        }

        public bool IsObjectiveCompleted(string objectiveID) => !string.IsNullOrEmpty(objectiveID) && _completedObjectives.Contains(objectiveID);

        public HashSet<string> GetCompletedQuestNames() => new(_completedQuests);

        public HashSet<string> GetCompletedObjectiveIDs() => new(_completedObjectives);

        private void SaveCompletedQuests()
        {
            var json = string.Join(",", _completedQuests);
            PlayerPrefs.SetString("CompletedQuests", json);
        }

        private void LoadCompletedQuests()
        {
            var json = PlayerPrefs.GetString("CompletedQuests", "");
            if (!string.IsNullOrEmpty(json))
            {
                var quests = json.Split(',');
                _completedQuests.Clear();
                foreach (var questName in quests)
                {
                    if (!string.IsNullOrEmpty(questName))
                    {
                        _completedQuests.Add(questName);
                    }
                }
                Debug.Log($"Loaded completed quests: {string.Join(", ", _completedQuests)}");
            }
        }

        private void SaveCompletedObjectives()
        {
            var json = string.Join(",", _completedObjectives);
            PlayerPrefs.SetString("CompletedObjectives", json);
        }

        private void LoadCompletedObjectives()
        {
            var json = PlayerPrefs.GetString("CompletedObjectives", "");
            if (!string.IsNullOrEmpty(json))
            {
                var objectives = json.Split(',');
                _completedObjectives.Clear();
                foreach (var objectiveID in objectives)
                {
                    if (!string.IsNullOrEmpty(objectiveID))
                    {
                        _completedObjectives.Add(objectiveID);
                    }
                }
                Debug.Log($"Loaded completed objectives: {string.Join(", ", _completedObjectives)}");
            }
        }
    }
}

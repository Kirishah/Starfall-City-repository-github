using System.Collections.Generic;
using UnityEngine;

public class QuestMemory : MonoBehaviour
{
    public static QuestMemory Instance { get; private set; }
    private HashSet<string> _completedQuests = new HashSet<string>();
    private HashSet<string> _completedObjectives = new HashSet<string>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        LoadCompletedQuests();
        LoadCompletedObjectives();
    }

    public void MarkQuestCompleted(QuestSO quest)
    {
        if (quest != null && !_completedQuests.Contains(quest.name))
        {
            _completedQuests.Add(quest.name);
            SaveCompletedQuests();
            Debug.Log($"Marked quest {quest.name} as completed.");
        }
    }

    public bool IsQuestCompleted(QuestSO quest)
    {
        return quest != null && _completedQuests.Contains(quest.name);
    }

    public void MarkObjectiveCompleted(string objectiveID)
    {
        if (!string.IsNullOrEmpty(objectiveID) && !_completedObjectives.Contains(objectiveID))
        {
            _completedObjectives.Add(objectiveID);
            SaveCompletedObjectives();
            Debug.Log($"Marked objective {objectiveID} as completed.");
        }
    }

    public bool IsObjectiveCompleted(string objectiveID)
    {
        return !string.IsNullOrEmpty(objectiveID) && _completedObjectives.Contains(objectiveID);
    }

    public HashSet<string> GetCompletedQuestNames()
    {
        return new HashSet<string>(_completedQuests);
    }

    public HashSet<string> GetCompletedObjectiveIDs()
    {
        return new HashSet<string>(_completedObjectives);
    }

    private void SaveCompletedQuests()
    {
        string json = string.Join(",", _completedQuests);
        PlayerPrefs.SetString("CompletedQuests", json);
    }

    private void LoadCompletedQuests()
    {
        string json = PlayerPrefs.GetString("CompletedQuests", "");
        if (!string.IsNullOrEmpty(json))
        {
            string[] quests = json.Split(',');
            _completedQuests.Clear();
            foreach (string questName in quests)
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
        string json = string.Join(",", _completedObjectives);
        PlayerPrefs.SetString("CompletedObjectives", json);
    }

    private void LoadCompletedObjectives()
    {
        string json = PlayerPrefs.GetString("CompletedObjectives", "");
        if (!string.IsNullOrEmpty(json))
        {
            string[] objectives = json.Split(',');
            _completedObjectives.Clear();
            foreach (string objectiveID in objectives)
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

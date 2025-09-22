using System.Collections.Generic;
using UnityEngine;

public class QuestMemory : MonoBehaviour
{
    public static QuestMemory Instance { get; private set; }
    private HashSet<string> _completedQuests = new HashSet<string>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        LoadCompletedQuests();
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

    private void SaveCompletedQuests()
    {
        string json = string.Join(",", _completedQuests);
        PlayerPrefs.SetString("CompletedQuests", json);
        PlayerPrefs.Save();
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
}

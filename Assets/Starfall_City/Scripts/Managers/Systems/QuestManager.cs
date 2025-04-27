using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Quest;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }
    public static event Action<QuestSO> OnQuestStarted;
    public static event Action<QuestSO> OnQuestCompleted;
    public static event Action<ObjectiveSO, int, int> OnObjectiveProgressed;

    private List<Quest> _activeQuests = new List<Quest>();
    private HashSet<QuestSO> _activeQuestSet = new HashSet<QuestSO>();
    private Queue<Quest> _questPoolQueue = new Queue<Quest>();
    private Dictionary<string, (int current, int required)> _objectiveProgress = new Dictionary<string, (int, int)>(); // Tracks progress by ObjectiveID

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePool(10); // Initial pool size
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializePool(int capacity)
    {
        for (int i = 0; i < capacity; i++)
        {
            Quest quest = new Quest();
            _questPoolQueue.Enqueue(quest);
        }
    }

    public List<QuestSO> GetActiveQuests()
    {
        List<QuestSO> activeQuestSOs = new List<QuestSO>();
        foreach (Quest quest in _activeQuests)
        {
            activeQuestSOs.Add(quest.Data);
        }
        return activeQuestSOs;
    }

    public bool IsQuestActive(QuestSO questSO)
    {
        return _activeQuestSet.Contains(questSO);
    }

    public void StartQuest(QuestSO questSO)
    {
        Debug.Log($"Starting quest: {questSO.name}");
        if (IsQuestActive(questSO))
        {
            Debug.LogWarning($"Quest {questSO.name} is already active.");
            return;
        }
        if (_questPoolQueue.Count == 0) InitializePool(3);

        Quest quest = _questPoolQueue.Dequeue();
        quest.Initialize(questSO);
        _activeQuests.Add(quest);
        _activeQuestSet.Add(questSO);
        quest.StartQuest();

        // Trigger event
        OnQuestStarted?.Invoke(questSO);
    }

    public void CompleteQuest(Quest quest)
    {
        var questSO = quest.Data; 
        quest.Cleanup();
        _activeQuests.Remove(quest);
        _activeQuestSet.Remove(questSO);
        _questPoolQueue.Enqueue(quest);

        // Remove progress tracking for this quest's objectives
        foreach (var objective in questSO.Objectives)
        {
            _objectiveProgress.Remove(objective.ObjectiveID);
        }
        // Trigger event
        OnQuestCompleted?.Invoke(questSO);
    }

    // Called from other systems via events
    public void HandleObjectiveUpdate(ObjectiveType type, string identifier)
    {
        Debug.Log($"QuestManager HandleObjectiveUpdate: type={type}, identifier={identifier}");
        foreach (Quest quest in _activeQuests.ToList())
        {
            quest.ProcessObjectiveEvent(type, identifier);
        }
    }

    public void ReportObjectiveProgress(ObjectiveSO objective, int current, int required)
    {
        // Store the progress
        _objectiveProgress[objective.ObjectiveID] = (current, required);

        OnObjectiveProgressed?.Invoke(objective, current, required);
    }
    public (int current, int required) GetObjectiveProgress(string objectiveID)
    {
        if (_objectiveProgress.TryGetValue(objectiveID, out var progress))
        {
            return progress;
        }
        return (0, 1); // Default if no progress recorded
    }
    public Quest FindQuestByObjective(ObjectiveSO objective)
    {
        return _activeQuests.Find(quest => quest.Data.Objectives.Contains(objective));
    }
}

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
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        InitializePool(10);
    }

    private void InitializePool(int capacity)
    {
        for (int i = 0; i < capacity; i++)
        {
            Quest quest = new Quest();
            _questPoolQueue.Enqueue(quest);
        }
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

        OnQuestStarted?.Invoke(questSO);
    }

    public void CompleteQuest(Quest quest)
    {
        Debug.Log($"QuestManager: Attempting to complete quest {quest?.Data?.Title ?? "null"}");
        if (quest == null)
        {
            Debug.LogWarning("QuestManager: Attempted to complete a null quest.");
            return;
        }
        if (!_activeQuests.Contains(quest))
        {
            Debug.LogWarning($"QuestManager: Quest {quest.Data.Title} is not in active quests.");
            return;
        }

        var questSO = quest.Data;

        _activeQuests.Remove(quest);
        _activeQuestSet.Remove(questSO);

        // ”даление отслеживани€ прогресса дл€ целей этого квеста
        foreach (var objective in questSO.Objectives)
        {
            _objectiveProgress.Remove(objective.ObjectiveID);
        }
        // ѕометить как завершенное и задействовать UI
        QuestMemory.Instance.MarkQuestCompleted(quest.Data);
        OnQuestCompleted?.Invoke(questSO);
        Debug.Log($"QuestManager: Fired OnQuestCompleted for {questSO.Title}");

        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AddMoney(quest.Data.MoneyReward);
            Debug.Log($"Awarded {quest.Data.MoneyReward} Money for completing {quest.Data.Title}");
        }
        else
        {
            Debug.LogError("Instance is null. Cannot award rewards.");
        }

        // „истка и возвращение в пул
        quest.Cleanup();
        _questPoolQueue.Enqueue(quest);
        Debug.Log($"QuestManager: Quest {questSO.Title} cleaned up and returned to pool");
    }

    // ƒругие системы зовут этот метод
    public void HandleObjectiveUpdate(ObjectiveType type, string identifier, string itemID = null)
    {
        Debug.Log($"QuestManager HandleObjectiveUpdate: type={type}, identifier={identifier}," +
        $" itemID={itemID}, active quests: {string.Join(", ", _activeQuests.Select(q => q.Data.Title))}");
        var questsToProcess = new List<Quest>(_activeQuests);
        foreach (var quest in questsToProcess)
        {
            if (quest.IsCompleted) continue;
            quest.ProcessObjectiveEvent(type, identifier, itemID);
        }
    }

    public void ReportObjectiveProgress(ObjectiveSO objective, int current, int required)
    {
        if (objective == null || string.IsNullOrEmpty(objective.ObjectiveID))
        {
            Debug.LogError("Cannot report progress: Objective or ObjectiveID is null.");
            return;
        }
        _objectiveProgress[objective.ObjectiveID] = (current, required);
        OnObjectiveProgressed?.Invoke(objective, current, required);
        Debug.Log($"QuestManager: Reported progress for {objective.ObjectiveID}: {current}/{required}");
    }

    public (int current, int required) GetObjectiveProgress(string objectiveID)
    {
        if (_objectiveProgress.TryGetValue(objectiveID, out var progress))
        {
            return progress;
        }
        return (0, 1); // Default 
    }

    public Quest FindQuestByObjective(ObjectiveSO objective)
    {
        return _activeQuests.Find(quest => quest.Data.Objectives.Contains(objective));
    }

    public List<Quest> GetActiveQuests() => _activeQuests;
}

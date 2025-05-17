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
            InitializePool(10); 
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

    public List<Quest> GetActiveQuests()
    {
        return _activeQuests;
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
        var questSO = quest.Data; 
        quest.Cleanup();
        _activeQuests.Remove(quest);
        _activeQuestSet.Remove(questSO);
        _questPoolQueue.Enqueue(quest);

        // Удаление отслеживания прогресса для целей этого квеста
        foreach (var objective in questSO.Objectives)
        {
            _objectiveProgress.Remove(objective.ObjectiveID);
        }
        // Trigger event
        OnQuestCompleted?.Invoke(questSO);
        QuestMemory.Instance.MarkQuestCompleted(quest.Data);
    }

    // Другие системы зовут этот метод
    public void HandleObjectiveUpdate(ObjectiveType type, string identifier, string itemID = null)
    {
        Debug.Log($"QuestManager HandleObjectiveUpdate: type={type}, identifier={identifier}");
        foreach (Quest quest in _activeQuests.ToList())
        {
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
}

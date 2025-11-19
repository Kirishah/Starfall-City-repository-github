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

    // Коллить это в Awake() или после загрузки сохранения
    public void RestoreCompletedObjectivesFromMemory()
    {
        foreach (var quest in _activeQuests)
        {
            foreach (var objective in quest.Data.Objectives)
            {
                if (QuestMemory.Instance.IsObjectiveCompleted(objective.ObjectiveID))
                {
                    // Находим рантаймовый объект цели
                    var runtimeObjective = quest._objectives.Find(o => o.ObjectiveID == objective.ObjectiveID);
                    if (runtimeObjective != null)
                    {
                        runtimeObjective.Complete();
                    }
                }
            }
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

        // Mark any unfulfilled objectives as completed (safety net)
        foreach (var objective in quest.Data.Objectives)
        {
            if (objective != null && !QuestMemory.Instance.IsObjectiveCompleted(objective.ObjectiveID))
            {
                QuestMemory.Instance.MarkObjectiveCompleted(objective.ObjectiveID);
            }
        }

        // Удаление отслеживания прогресса для целей этого квеста
        foreach (var objective in questSO.Objectives)
        {
            _objectiveProgress.Remove(objective.ObjectiveID);
        }
        // Пометить как завершенное и задействовать UI
        QuestMemory.Instance.MarkQuestCompleted(quest.Data);
        OnQuestCompleted?.Invoke(questSO);
        Debug.Log($"QuestManager: Fired OnQuestCompleted for {questSO.Title}");

        CheckFollowUps(questSO);

        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AddMoney(quest.Data.MoneyReward);
            Debug.Log($"Awarded {quest.Data.MoneyReward} Money for completing {quest.Data.Title}");
        }
        else
        {
            Debug.LogError("Instance is null. Cannot award rewards.");
        }

        // Чистка и возвращение в пул
        quest.Cleanup();
        _questPoolQueue.Enqueue(quest);
        Debug.Log($"QuestManager: Quest {questSO.Title} cleaned up and returned to pool");
    }

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

        if (current >= required)
        {
            QuestMemory.Instance.MarkObjectiveCompleted(objective.ObjectiveID);
        }
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

    private void CheckFollowUps(QuestSO completedQuest)
    {
        if (completedQuest.FollowUpQuests == null || completedQuest.FollowUpQuests.Count == 0)
        {
            Debug.Log($"No follow-up quests configured for {completedQuest.Title}");
            return;
        }

        foreach (var followUp in completedQuest.FollowUpQuests)
        {
            if (followUp.Quest == null)
            {
                Debug.LogWarning($"Follow-up quest is null in {completedQuest.Title}");
                continue;
            }

            // Evaluate all unlock conditions (empty list evaluates to true)
            bool allConditionsMet = followUp.UnlockConditions == null || followUp.UnlockConditions.All(condition => condition.Evaluate());

            if (allConditionsMet &&
                !IsQuestActive(followUp.Quest) &&
                !QuestMemory.Instance.IsQuestCompleted(followUp.Quest))
            {
                StartQuest(followUp.Quest);
                Debug.Log($"Auto-started follow-up quest: {followUp.Quest.Title} after {completedQuest.Title}");

                // If a DialogueStartID is specified, trigger it after starting the quest
                // (This assumes the dialogue will handle any further quest progression if needed)
                if (!string.IsNullOrEmpty(followUp.DialogueStartID))
                {
                    // You may need to specify an NPC ID here; adjust based on your setup (e.g., a default NPC or from quest data)
                    string npcID = followUp.Quest.StartingDialogueID != null ? "default_npc" : ""; // Placeholder; customize as needed
                    if (DialogueManager_UIToolkit.Instance != null && !string.IsNullOrEmpty(npcID))
                    {
                        DialogueManager_UIToolkit.Instance.StartDialogue(followUp.DialogueStartID, npcID);
                        Debug.Log($"Triggered follow-up dialogue: {followUp.DialogueStartID} for quest {followUp.Quest.Title}");
                    }
                }
            }
            else
            {
                Debug.Log($"Follow-up quest {followUp.Quest.Title} blocked: conditions met={allConditionsMet}, active={IsQuestActive(followUp.Quest)}, completed={QuestMemory.Instance.IsQuestCompleted(followUp.Quest)}");
            }
        }
    }

    public List<Quest> GetActiveQuests() => _activeQuests;
}

using System.Linq;
using UnityEngine;
using core;

public class QuestStarter : MonoBehaviour
{
    [SerializeField] private QuestSO _initialQuest; // Начальный квест в цепочке
    [SerializeField] private string _npcID;
    [SerializeField] private string _defaultDialogueStartID; // Диалог по умолчанию, если квест недоступен
    [SerializeField] private string _postQuestDialogueStartID; // Диалог после завершения квеста

    private QuestSO _currentQuest; // Текущий квест, который предлагается 
    private string _currentDialogueStartID; // Диалог, связанный с текущим квестом

    public void StartDialogue()
    {
        DetermineCurrentQuestAndDialogue();

        if (DialogueManager.Instance == null)
        {
            Debug.LogError("DialogueManager.Instance is null!");
            return;
        }

        if (string.IsNullOrEmpty(_currentDialogueStartID))
        {
            Debug.LogError($"No dialogue ID resolved for QuestStarter on {gameObject.name}. Check assignments.");
            return;
        }

        Debug.Log($"[QuestStarter] Starting dialogue '{_currentDialogueStartID}' (NPC: {_npcID})");
        DialogueManager.Instance.StartDialogue(_currentDialogueStartID, _npcID);
    }

    private void DetermineCurrentQuestAndDialogue()
    {
        // Reset
        _currentQuest = null;
        _currentDialogueStartID = _defaultDialogueStartID;

        if (QuestMemory.Instance == null)
        {
            Debug.LogError("QuestMemory.Instance is missing in the scene!");
            return;
        }

        if (_initialQuest == null)
        {
            Debug.LogError($"_initialQuest not assigned on {name}");
            return;
        }

        // -------------------------------------------------
        // 1. Is the very first quest still available?
        // -------------------------------------------------
        if (!QuestMemory.Instance.IsQuestCompleted(_initialQuest))
        {
            _currentQuest = _initialQuest;
            _currentDialogueStartID = GetStartingDialogueOrFallback(_initialQuest);
            Debug.Log($"[QuestStarter] Offering initial quest: {_currentQuest.Title}");
            return;
        }

        // -------------------------------------------------
        // 2. Walk the follow-up chain until we find an available quest
        // -------------------------------------------------
        QuestSO questToCheck = _initialQuest;

        while (questToCheck != null)
        {
            // Look for the first follow-up whose unlock conditions are satisfied
            var validFollowUp = questToCheck.FollowUpQuests
                .FirstOrDefault(fu =>
                    fu.Quest != null &&
                    !QuestMemory.Instance.IsQuestCompleted(fu.Quest) &&
                    (fu.UnlockConditions == null ||
                     fu.UnlockConditions.Count == 0 ||
                     ConditionEvaluator.EvaluateUnlockConditions(fu.UnlockConditions))); // <-- NEW OR-SUPPORT!

            if (validFollowUp != null)
            {
                _currentQuest = validFollowUp.Quest;
                _currentDialogueStartID = !string.IsNullOrEmpty(validFollowUp.DialogueStartID)
                    ? validFollowUp.DialogueStartID
                    : GetStartingDialogueOrFallback(_currentQuest);

                Debug.Log($"[QuestStarter] Offering follow-up quest: {_currentQuest.Title} " +
                          $"(triggered by {questToCheck.Title})");
                return;
            }

            // No valid follow-up → move to the next quest in chain that would have been started
            // (we have to find which one *was* started as the actual next step)
            questToCheck = questToCheck.FollowUpQuests
                .Select(fu => fu.Quest)
                .FirstOrDefault(q => q != null && QuestMemory.Instance.IsQuestCompleted(q));
        }

        // -------------------------------------------------
        // 3. Whole chain completed → post-quest dialogue
        // -------------------------------------------------
        _currentDialogueStartID = !string.IsNullOrEmpty(_postQuestDialogueStartID)
            ? _postQuestDialogueStartID
            : _defaultDialogueStartID;

        Debug.Log("[QuestStarter] All quests in chain completed → using post-quest dialogue.");
    }

    private string GetStartingDialogueOrFallback(QuestSO quest)
    {
        if (!string.IsNullOrEmpty(quest.StartingDialogueID))
            return quest.StartingDialogueID;

        Debug.LogWarning($"Quest '{quest.Title}' has no StartingDialogueID → falling back to default.");
        return _defaultDialogueStartID;
    }
}

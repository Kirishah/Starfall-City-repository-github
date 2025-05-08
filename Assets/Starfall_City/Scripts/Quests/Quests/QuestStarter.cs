using System.Linq;
using UnityEngine;

public class QuestStarter : MonoBehaviour
{
    [SerializeField] private QuestSO _initialQuest; // The starting quest in the chain
    [SerializeField] private string _targetDialogueID; // The dialogue line ID that triggers the quest
    [SerializeField] private string _npcID;
    [SerializeField] private string _defaultDialogueStartID; // Default dialogue if no quest is available

    private bool _hasStartedQuest;
    private QuestSO _currentQuest; // The quest currently being offered
    private string _currentDialogueStartID; // The dialogue associated with the current quest

    void OnEnable()
    {
        DialogueManager.OnDialogueLineDisplayed += HandleDialogueLineDisplayed;
    }

    void OnDisable()
    {
        DialogueManager.OnDialogueLineDisplayed -= HandleDialogueLineDisplayed;
    }

    public void StartDialogue()
    {
        DetermineCurrentQuestAndDialogue();
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartDialogue(_currentDialogueStartID, _npcID);
        }
        else
        {
            Debug.LogError("DialogueManager.Instance is null when trying to start dialogue.");
        }
    }

    private void DetermineCurrentQuestAndDialogue()
    {
        // Reset current quest and dialogue
        _currentQuest = null;
        _currentDialogueStartID = _defaultDialogueStartID;

        // Start with the initial quest
        QuestSO questToCheck = _initialQuest;
        bool initialQuestCompleted = QuestMemory.Instance.IsQuestCompleted(_initialQuest);

        if (!initialQuestCompleted)
        {
            _currentQuest = _initialQuest;
            _currentDialogueStartID = _initialQuest.FollowUpQuests != null && _initialQuest.FollowUpQuests.Any()
                ? _initialQuest.FollowUpQuests[0].DialogueStartID // Use the dialogue from the first follow-up
                : _defaultDialogueStartID;
            Debug.Log($"Offering initial quest: {_currentQuest.Title}");
            return;
        }

        // Traverse the quest chain to find the next available quest
        while (questToCheck != null)
        {
            if (!QuestMemory.Instance.IsQuestCompleted(questToCheck))
            {
                _currentQuest = questToCheck;
                _currentDialogueStartID = questToCheck.FollowUpQuests != null && questToCheck.FollowUpQuests.Any()
                    ? questToCheck.FollowUpQuests[0].DialogueStartID
                    : _defaultDialogueStartID;
                Debug.Log($"Offering quest: {_currentQuest.Title}");
                return;
            }

            // Check follow-up quests
            QuestSO nextQuest = null;
            foreach (var followUp in questToCheck.FollowUpQuests)
            {
                if (followUp.UnlockConditions.All(condition => condition.Evaluate()))
                {
                    nextQuest = followUp.Quest;
                    _currentDialogueStartID = followUp.DialogueStartID;
                    break;
                }
            }

            questToCheck = nextQuest;
        }

        Debug.Log("No new quests available in the chain.");
    }

    private void HandleDialogueLineDisplayed(string dialogueID, string npcID)
    {
        if (_hasStartedQuest) return; // Prevent starting the quest multiple times

        // Check if this is the target dialogue line and (optionally) the correct NPC
        bool isTargetDialogue = dialogueID == _targetDialogueID;
        bool isTargetNPC = string.IsNullOrEmpty(_npcID) || npcID == _npcID;

        if (isTargetDialogue && isTargetNPC)
        {
            StartQuest();
        }
    }

    private void StartQuest()
    {
        if (_currentQuest != null && QuestManager.Instance != null)
        {
            QuestManager.Instance.StartQuest(_currentQuest);
            _hasStartedQuest = true;
            Debug.Log($"Quest {_currentQuest.Title} started after dialogue line {_targetDialogueID}");
        }
        else
        {
            Debug.LogWarning("No quest to start or QuestManager is not assigned.");
        }
    }
}

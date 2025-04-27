using UnityEngine;

public class QuestStarter : MonoBehaviour
{
    [SerializeField] private QuestSO _questToStart;
    [SerializeField] private string _targetDialogueID; // The dialogue line ID that triggers the quest
    [SerializeField] private string _npcID;
    [SerializeField] private string _dialogueStartID;

    private bool _hasStartedQuest;

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
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartDialogue(_dialogueStartID, _npcID);
        }
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
        if (_questToStart != null && QuestManager.Instance != null)
        {
            QuestManager.Instance.StartQuest(_questToStart);
            _hasStartedQuest = true;
            Debug.Log($"Quest {_questToStart.Title} started after dialogue line {_targetDialogueID}");
        }
        else
        {
            Debug.LogWarning("QuestToStart or QuestManager is not assigned.");
        }
    }
}

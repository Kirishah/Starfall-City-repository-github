using System.Linq;
using UnityEngine;

public class QuestStarter : MonoBehaviour
{
    [SerializeField] private QuestSO _initialQuest; // Начальный квест в цепочке
    [SerializeField] private string _npcID;
    [SerializeField] private string _defaultDialogueStartID; // Диалог по умолчанию, если квест недоступен
    [SerializeField] private string _postQuestDialogueStartID; // Диалог после завершения квеста

    private bool _hasStartedQuest;
    private QuestSO _currentQuest; // Текущий квест, который предлагается 
    private string _currentDialogueStartID; // Диалог, связанный с текущим квестом

    public void StartDialogue()
    {
        DetermineCurrentQuestAndDialogue();
        if (DialogueManager.Instance != null)
        {
            if (string.IsNullOrEmpty(_currentDialogueStartID))
            {
                Debug.LogError($"Cannot start dialogue: _currentDialogueStartID is empty on GameObject {gameObject.name}. Check QuestSO or DefaultDialogueStartID.");
                return;
            }
            Debug.Log($"Attempting to start dialogue with _currentDialogueStartID={_currentDialogueStartID}, _npcID={_npcID}");
            DialogueManager.Instance.StartDialogue(_currentDialogueStartID, _npcID);
        }
        else
        {
            Debug.LogError("DialogueManager.Instance is null when trying to start dialogue.");
        }
    }

    private void DetermineCurrentQuestAndDialogue()
    {
        // Сброс текущего квеста и диалога
        _currentQuest = null;
        _currentDialogueStartID = _defaultDialogueStartID;

        if (QuestMemory.Instance == null)
        {
            Debug.LogError("QuestMemory.Instance is null. Ensure a QuestMemory GameObject exists.");
            return;
        }
        if (_initialQuest == null)
        {
            Debug.LogError($"_initialQuest is not assigned in QuestStarter on GameObject {gameObject.name}. Please assign a QuestSO in the Inspector.");
            return;
        }

        // Start with the initial quest
        QuestSO questToCheck = _initialQuest;
        bool initialQuestCompleted = QuestMemory.Instance.IsQuestCompleted(_initialQuest);

        if (!initialQuestCompleted)
        {
            _currentQuest = _initialQuest;
            _currentDialogueStartID = _initialQuest.StartingDialogueID;
            if (string.IsNullOrEmpty(_currentDialogueStartID))
            {
                Debug.LogWarning($"StartingDialogueID is empty for quest {_initialQuest.Title}. Using DefaultDialogueStartID: {_defaultDialogueStartID}");
                _currentDialogueStartID = _defaultDialogueStartID;
            }
            Debug.Log($"Offering initial quest: {_currentQuest.Title} with dialogue: {_currentDialogueStartID}");
            return;
        }

        // Пробежка по цепочке квестов, чтобы найти следующий доступный квест
        while (questToCheck != null)
        {
            if (!QuestMemory.Instance.IsQuestCompleted(questToCheck))
            {
                _currentQuest = questToCheck;
                _currentDialogueStartID = questToCheck.StartingDialogueID;
                if (string.IsNullOrEmpty(_currentDialogueStartID))
                {
                    Debug.LogWarning($"StartingDialogueID is empty for quest {_currentQuest.Title}. Using DefaultDialogueStartID: {_defaultDialogueStartID}");
                    _currentDialogueStartID = _defaultDialogueStartID;
                }
                Debug.Log($"Offering quest: {_currentQuest.Title} with dialogue: {_currentDialogueStartID}");
                return;
            }

            // Чек последующих квестов
            QuestSO nextQuest = null;
            foreach (var followUp in questToCheck.FollowUpQuests)
            {
                if (followUp.UnlockConditions.All(condition => condition.Evaluate()))
                {
                    nextQuest = followUp.Quest;
                    _currentDialogueStartID = followUp.DialogueStartID;
                    Debug.Log($"Selected follow-up quest: {nextQuest.Title}, DialogueStartID: {_currentDialogueStartID}");
                    break;
                }
            }

            questToCheck = nextQuest;
        }

        // Если все квесты завершены, используем PostQuestDialogueStartID
        _currentDialogueStartID = _postQuestDialogueStartID;
        if (string.IsNullOrEmpty(_currentDialogueStartID))
        {
            Debug.LogWarning($"PostQuestDialogueStartID is empty on GameObject {gameObject.name}. Falling back to DefaultDialogueStartID: {_defaultDialogueStartID}");
            _currentDialogueStartID = _defaultDialogueStartID;
        }
        Debug.Log($"No new quests available in the chain. Using PostQuestDialogueStartID: {_currentDialogueStartID}");
    }
}

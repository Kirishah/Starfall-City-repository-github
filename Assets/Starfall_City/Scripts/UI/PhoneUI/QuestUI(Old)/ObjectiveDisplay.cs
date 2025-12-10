using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ObjectiveDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _objectiveText;
    [SerializeField] private Image _statusIcon;
    [SerializeField] private Sprite _completeIcon;
    [SerializeField] private Sprite _incompleteIcon;

    private ObjectiveSO _objectiveData;
    private Objective _objectiveInstance;
    private int _currentProgress;
    private int _requiredProgress;

    public void Initialize(ObjectiveSO objectiveData, Objective objectiveInstance)
    {
        _objectiveData = objectiveData;
        _objectiveInstance = objectiveInstance;
        _requiredProgress = GetRequiredProgress(objectiveData);

        // Синхронизация с текущим прогрессом из QuestManager
        var progress = QuestManager.Instance.GetObjectiveProgress(_objectiveData.ObjectiveID);
        _currentProgress = progress.current;
        _requiredProgress = progress.required;

        UpdateDisplay();
        QuestManager.OnObjectiveProgressed += HandleObjectiveProgress;
    }

    private int GetRequiredProgress(ObjectiveSO objective)
    {
        // Добавление требований к прогрессу для конкретного типа
        if (objective is QTEObjectiveSO qteObjective)
            return qteObjective.RequiredSuccessCount;

        if (objective is DialogueSO dialogueObjective)
            return 1; 
        if (objective is InteractionSO interactionObjective)
            return interactionObjective.RequiredInteractions;
        if (objective is CollectItemSO collectionObjective)
            return collectionObjective.RequiredAmount;
        if (objective is ExplorationSO)
            return 1;
        if (objective is PerformanceSO)
            return 1;
        return 1; // Default
    }

    private void HandleObjectiveProgress(ObjectiveSO objective, int current, int required)
    {
        Debug.Log($"ObjectiveDisplay HandleObjectiveProgress: objective={objective.ObjectiveID}, current={current}, required={required}");
        if (objective.ObjectiveID == _objectiveData.ObjectiveID)
        {
            _currentProgress = current;
            _requiredProgress = required;
            UpdateDisplay();
            Debug.Log($"ObjectiveDisplay updated: {_objectiveText.text}");
        }
    }

    private void UpdateDisplay()
    {
        bool isComplete = _objectiveInstance != null ? _objectiveInstance.IsCompleted : _currentProgress >= _requiredProgress;

        _statusIcon.sprite = isComplete ? _completeIcon : _incompleteIcon;
        _objectiveText.text = $"{_objectiveData.Description} ({_currentProgress}/{_requiredProgress})";
        _objectiveText.fontStyle = isComplete ? FontStyles.Strikethrough : FontStyles.Normal;
    }

    void OnDestroy()
    {
        QuestManager.OnObjectiveProgressed -= HandleObjectiveProgress;
    }
}

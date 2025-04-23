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
    private int _currentProgress;
    private int _requiredProgress;

    public void Initialize(ObjectiveSO objectiveData)
    {
        _objectiveData = objectiveData;
        _currentProgress = 0;
        _requiredProgress = GetRequiredProgress(objectiveData);

        UpdateDisplay();
        QuestManager.OnObjectiveProgressed += HandleObjectiveProgress;
    }

    private int GetRequiredProgress(ObjectiveSO objective)
    {
        // Add type-specific progress requirements
        if (objective is QTEObjectiveSO qteObjective)
            return qteObjective.RequiredSuccessCount;

        if (objective is DialogueSO dialogueObjective)
            return 1; // Dialogue objectives typically require 1 completion

        return 1; // Default
    }

    private void HandleObjectiveProgress(ObjectiveSO objective, int current, int required)
    {
        if (objective.ObjectiveID == _objectiveData.ObjectiveID)
        {
            _currentProgress = current;
            _requiredProgress = required;
            UpdateDisplay();
        }
    }

    private void UpdateDisplay()
    {
        bool isComplete = _currentProgress >= _requiredProgress;

        _statusIcon.sprite = isComplete ? _completeIcon : _incompleteIcon;
        _objectiveText.text = $"{_objectiveData.Description} ({_currentProgress}/{_requiredProgress})";
        _objectiveText.fontStyle = isComplete ? FontStyles.Strikethrough : FontStyles.Normal;
    }

    void OnDestroy()
    {
        QuestManager.OnObjectiveProgressed -= HandleObjectiveProgress;
    }
}

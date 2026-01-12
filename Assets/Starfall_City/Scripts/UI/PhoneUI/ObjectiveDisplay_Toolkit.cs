using UnityEngine;
using UnityEngine.UIElements;

namespace QuestSystem
{
    public class ObjectiveDisplay_Toolkit : MonoBehaviour
    {
        private VisualTreeAsset _objectiveUXML;

        private VisualElement _parentElement;  // The container we'll build into
        private Label _objectiveLabel;
        private Image _statusIcon;
        private Sprite _completeIcon;
        private Sprite _incompleteIcon;

        private ObjectiveSO _objectiveData;
        private Objective _objectiveInstance;
        private int _currentProgress;
        private int _requiredProgress;

        public void Initialize(VisualElement parent, ObjectiveSO objectiveDataParam,
            Objective objectiveInstanceParam, Sprite completeIconParam, Sprite incompleteIconParam, VisualTreeAsset objectiveUXML)
        {
            _parentElement = parent;
            _objectiveData = objectiveDataParam;
            _objectiveInstance = objectiveInstanceParam;
            _completeIcon = completeIconParam;
            _incompleteIcon = incompleteIconParam;
            _objectiveUXML = objectiveUXML;

            _requiredProgress = GetRequiredProgress(_objectiveData);

            // Sync with QuestManager
            var (current, required) = QuestManager.Instance.GetObjectiveProgress(_objectiveData.ObjectiveID);
            _currentProgress = current;
            _requiredProgress = required;

            BuildUI();
            UpdateDisplay();
            if (QuestManager.Instance != null)
            {
                QuestManager.OnObjectiveProgressed += HandleObjectiveProgress;
            }
        }

        private void BuildUI()
        {
            if (_parentElement == null || _objectiveUXML == null) return;

            // Instantiate directly into parent (no UIDocument or root.Clear needed)
            var objectiveElement = _objectiveUXML.Instantiate();
            _parentElement.Add(objectiveElement);

            _objectiveLabel = objectiveElement.Q<Label>("ObjectiveText");
            _statusIcon = objectiveElement.Q<Image>("StatusIcon");

            if (_objectiveLabel == null) Debug.LogWarning("ObjectiveDisplay: 'ObjectiveText' Label not found in UXML!");
            if (_statusIcon == null) Debug.LogWarning("ObjectiveDisplay: 'StatusIcon' Image not found in UXML!");
            if (objectiveElement.childCount == 0) Debug.LogWarning("ObjectiveDisplay: Instantiated element has no children—check UXML structure.");

            objectiveElement.RemoveFromClassList("completed");
        }

        private int GetRequiredProgress(ObjectiveSO objective)
        {
            if (objective is QTEObjectiveSO qteObjective)
                return qteObjective.requiredSuccessCount;
            if (objective is DialogueSO dialogueObjective)
                return 1;
            if (objective is InteractionSO interactionObjective)
                return interactionObjective.requiredInteractions;
            if (objective is CollectItemSO collectionObjective)
                return collectionObjective.requiredAmount;
            if (objective is ExplorationSO)
                return 1;
            if (objective is PerformanceSO)
                return 1;
            return 1; // Default
        }

        private void HandleObjectiveProgress(ObjectiveSO objective, int current, int required)
        {
            if (objective.ObjectiveID == this._objectiveData.ObjectiveID)
            {
                _currentProgress = current;
                _requiredProgress = required;
                UpdateDisplay();
            }
        }

        private void UpdateDisplay()
        {
            if (_statusIcon == null || _objectiveLabel == null || _objectiveData == null)
            {
                Debug.LogWarning("label, icon, or data null.");
                return;
            }

            bool isComplete = _objectiveInstance != null ? _objectiveInstance.IsCompleted : _currentProgress >= _requiredProgress;

            if (_statusIcon != null)
            {
                if (_completeIcon != null && _incompleteIcon != null)
                {
                    _statusIcon.style.backgroundImage = new StyleBackground(isComplete ? _completeIcon.texture : _incompleteIcon.texture);
                }
                _statusIcon.style.unityBackgroundImageTintColor = Color.white;  // Optional: Tint if needed
            }

            if (_objectiveLabel != null)
            {
                var desc = string.IsNullOrEmpty(_objectiveData.Description) ? "Unnamed Objective" : _objectiveData.Description;
                _objectiveLabel.text = $"{desc} ({_currentProgress}/{_requiredProgress})";

                // Strikethrough via USS class
                if (isComplete)
                {
                    _objectiveLabel.AddToClassList("completed");
                }
                else
                {
                    _objectiveLabel.RemoveFromClassList("completed");
                }
            }
        }

        public void CopyConfigurationFromOUI(ObjectiveDisplay_Toolkit prefabSource)
        {
            if (prefabSource == null)
            {
                Debug.LogError("ObjectiveDisplay_Toolkit: the method called with null source.");
                return;
            }

            _objectiveUXML = prefabSource._objectiveUXML;
        }

        private void OnDestroy()
        {
            if (QuestManager.Instance != null)
            {
                QuestManager.OnObjectiveProgressed -= HandleObjectiveProgress;
            }
        }
    }
}

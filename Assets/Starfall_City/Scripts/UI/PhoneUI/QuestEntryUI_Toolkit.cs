using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace QuestSystem
{
    public class QuestEntryUI_Toolkit : MonoBehaviour
    {
        private readonly VisualTreeAsset _questEntryUXML;
        private VisualTreeAsset _objectiveDisplayUXML;
        private Sprite _completeIcon;
        private Sprite _incompleteIcon;

        public QuestSO QuestData { get; private set; }

        private VisualElement _parentElement;  // The container we'll build into

        private Label _titleLabel;
        private Label _descriptionLabel;
        private Label _rewardsLabel;
        private VisualElement _objectivesContainer;

        private Quest _quest;
        private readonly List<ObjectiveDisplay_Toolkit> _objectiveDisplays = new();

        public void Initialize(VisualElement parent, QuestSO questData,
            VisualTreeAsset objectiveDisplayUXML, Sprite completeIcon, Sprite incompleteIcon)
        {
            _parentElement = parent;
            QuestData = questData;

            // Store the passed assets
            this._objectiveDisplayUXML = objectiveDisplayUXML;
            this._completeIcon = completeIcon;
            this._incompleteIcon = incompleteIcon;

            BuildUI();
            PopulateStaticContent();
            _quest = QuestManager.Instance.FindQuestByObjective(questData.Objectives[0]);
            if (_quest == null)
            {
                Debug.LogWarning($"Quest instance for {questData.Title} not found.");
                return;
            }
            RefreshObjectives();
        }

        private void BuildUI()
        {
            if (_parentElement == null)
            {
                Debug.LogError("QuestEntryUI: Missing parent element or UXML reference!");
                return;
            }

            _titleLabel = _parentElement.Q<Label>("QuestTitle");
            _descriptionLabel = _parentElement.Q<Label>("QuestDescription");
            _rewardsLabel = _parentElement.Q<Label>("RewardsText");
            _objectivesContainer = _parentElement.Q<VisualElement>("ObjectivesContainer");

            if (_titleLabel == null) Debug.LogError("QuestTitle label not found!");
            if (_descriptionLabel == null) Debug.LogError("QuestDescription label not found!");
            if (_objectivesContainer == null) Debug.LogError("ObjectivesContainer not found!");
        }

        private void PopulateStaticContent()
        {
            _titleLabel.text = QuestData.Title;
            _descriptionLabel.text = QuestData.Description;

            var rewards = "";
            if (QuestData.MoneyReward > 0)
                rewards += $"Money: {QuestData.MoneyReward}";
            _rewardsLabel.text = rewards.Length > 0 ? $"Rewards: {rewards}" : "No rewards";
        }

        private void RefreshObjectives()
        {
            // Clear old displays
            foreach (var display in _objectiveDisplays)
            {
                if (display != null) Destroy(display.gameObject);
            }
            _objectiveDisplays.Clear();
            if (_objectivesContainer == null) return;

            _objectivesContainer.Clear();

            // Completed objectives
            var completedObjectives = _quest.GetCompletedObjectives();
            foreach (var objective in completedObjectives)
            {
                var objectiveSO = _quest.GetObjectiveSO(objective);
                if (objectiveSO != null)
                {
                    // Create component GO (no UIDocument)
                    var displayGO = new GameObject("ObjectiveDisplay");
                    var display = displayGO.AddComponent<ObjectiveDisplay_Toolkit>();
                    display.Initialize(_objectivesContainer, objectiveSO, objective,
    _completeIcon, _incompleteIcon, _objectiveDisplayUXML);
                }
            }
            // Current objective
            var currentObjective = _quest.GetCurrentObjective();
            if (currentObjective != null)
            {
                var currentObjectiveSO = _quest.GetObjectiveSO(currentObjective);
                if (currentObjectiveSO != null)
                {
                    var displayGO = new GameObject("ObjectiveDisplay");
                    var display = displayGO.AddComponent<ObjectiveDisplay_Toolkit>();
                    display.Initialize(_objectivesContainer, currentObjectiveSO, currentObjective,
                        _completeIcon, _incompleteIcon, _objectiveDisplayUXML);
                }
            }
        }

        public void CopyConfigurationFromQEUI(QuestEntryUI_Toolkit prefabSource)
        {
            if (prefabSource == null)
            {
                Debug.LogError("QuestEntryUI_Toolkit: the method called with null source.");
                return;
            }

            _objectiveDisplayUXML = prefabSource._objectiveDisplayUXML;
            _completeIcon = prefabSource._completeIcon;
            _incompleteIcon = prefabSource._incompleteIcon;
        }

        private void OnDestroy()
        {
            foreach (var display in _objectiveDisplays)
            {
                if (display != null) Destroy(display.gameObject);
            }
            _objectiveDisplays.Clear();
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class QuestEntryUI_Toolkit : MonoBehaviour
{
    [SerializeField] public VisualTreeAsset questEntryUXML;  // Made public for assignment
    [SerializeField] public VisualTreeAsset objectiveDisplayUXML;  // Made public for assignment
    [SerializeField] public Sprite completeIcon;  // Made public for assignment
    [SerializeField] public Sprite incompleteIcon;

    public QuestSO QuestData { get; private set; }

    public VisualElement parentElement;  // The container we'll build into

    private Label titleLabel;
    private Label descriptionLabel;
    private Label rewardsLabel;
    private VisualElement objectivesContainer;

    private Quest quest;
    private List<ObjectiveDisplay_Toolkit> objectiveDisplays = new List<ObjectiveDisplay_Toolkit>();

    public void Initialize(VisualElement parent, QuestSO questData)
    {
        parentElement = parent;
        QuestData = questData;
        BuildUI();
        PopulateStaticContent();
        quest = QuestManager.Instance.FindQuestByObjective(questData.Objectives[0]);
        if (quest == null)
        {
            Debug.LogWarning($"Quest instance for {questData.Title} not found.");
            return;
        }
        RefreshObjectives();
    }

    private void BuildUI()
    {
        if (parentElement == null)
        {
            Debug.LogError("QuestEntryUI: Missing parent element or UXML reference!");
            return;
        }

        titleLabel = parentElement.Q<Label>("QuestTitle");
        descriptionLabel = parentElement.Q<Label>("QuestDescription");
        rewardsLabel = parentElement.Q<Label>("RewardsText");
        objectivesContainer = parentElement.Q<VisualElement>("ObjectivesContainer");

        if (titleLabel == null) Debug.LogError("QuestTitle label not found!");
        if (descriptionLabel == null) Debug.LogError("QuestDescription label not found!");
        if (objectivesContainer == null) Debug.LogError("ObjectivesContainer not found!");
    }

    private void PopulateStaticContent()
    {
        titleLabel.text = QuestData.Title;
        descriptionLabel.text = QuestData.Description;

        string rewards = "";
        if (QuestData.MoneyReward > 0)
            rewards += $"Money: {QuestData.MoneyReward}";
        rewardsLabel.text = rewards.Length > 0 ? $"Rewards: {rewards}" : "No rewards";
    }

    private void RefreshObjectives()
    {
        // Clear old displays
        foreach (var display in objectiveDisplays)
        {
            if (display != null) Destroy(display.gameObject);
        }
        objectiveDisplays.Clear();
        if (objectivesContainer == null) return;

        objectivesContainer.Clear();

        // Completed objectives
        var completedObjectives = quest.GetCompletedObjectives();
        foreach (var objective in completedObjectives)
        {
            var objectiveSO = quest.GetObjectiveSO(objective);
            if (objectiveSO != null)
            {
                // Create component GO (no UIDocument)
                var displayGO = new GameObject("ObjectiveDisplay");
                var display = displayGO.AddComponent<ObjectiveDisplay_Toolkit>();
                display.objectiveUXML = objectiveDisplayUXML;
                // Pass objectivesContainer directly
                display.Initialize(objectivesContainer, objectiveSO, objective, completeIcon, incompleteIcon);
                objectiveDisplays.Add(display);
            }
        }

        // Current objective
        var currentObjective = quest.GetCurrentObjective();
        if (currentObjective != null)
        {
            var currentObjectiveSO = quest.GetObjectiveSO(currentObjective);
            if (currentObjectiveSO != null)
            {
                var displayGO = new GameObject("ObjectiveDisplay");
                var display = displayGO.AddComponent<ObjectiveDisplay_Toolkit>();
                display.objectiveUXML = objectiveDisplayUXML;
                display.Initialize(objectivesContainer, currentObjectiveSO, currentObjective, completeIcon, incompleteIcon);
                objectiveDisplays.Add(display);
            }
        }
    }

    private void OnDestroy()
    {
        foreach (var display in objectiveDisplays)
        {
            if (display != null) Destroy(display.gameObject);
        }
        objectiveDisplays.Clear();
    }
}

using UnityEngine;
using UnityEngine.UIElements;

public class ObjectiveDisplay_Toolkit : MonoBehaviour
{
    [SerializeField] public VisualTreeAsset objectiveUXML;

    private VisualElement parentElement;  // The container we'll build into
    private Label objectiveLabel;
    private Image statusIcon;
    private Sprite completeIcon;
    private Sprite incompleteIcon;

    private ObjectiveSO objectiveData;
    private Objective objectiveInstance;
    private int currentProgress;
    private int requiredProgress;

    public void Initialize(VisualElement parent, ObjectiveSO objectiveDataParam, Objective objectiveInstanceParam, Sprite completeIconParam, Sprite incompleteIconParam)
    {
        parentElement = parent;
        objectiveData = objectiveDataParam;
        objectiveInstance = objectiveInstanceParam;
        completeIcon = completeIconParam;
        incompleteIcon = incompleteIconParam;

        requiredProgress = GetRequiredProgress(objectiveData);

        // Sync with QuestManager
        var progress = QuestManager.Instance.GetObjectiveProgress(objectiveData.ObjectiveID);
        currentProgress = progress.current;
        requiredProgress = progress.required;

        BuildUI();
        UpdateDisplay();
        if (QuestManager.Instance != null)
        {
            QuestManager.OnObjectiveProgressed += HandleObjectiveProgress;
        }
    }

    private void BuildUI()
    {
        if (parentElement == null || objectiveUXML == null) return;

        // Instantiate directly into parent (no UIDocument or root.Clear needed)
        var objectiveElement = objectiveUXML.Instantiate();
        parentElement.Add(objectiveElement);

        objectiveLabel = objectiveElement.Q<Label>("ObjectiveText");
        statusIcon = objectiveElement.Q<Image>("StatusIcon");

        if (objectiveLabel == null) Debug.LogWarning($"ObjectiveDisplay: 'ObjectiveText' Label not found in UXML!");
        if (statusIcon == null) Debug.LogWarning($"ObjectiveDisplay: 'StatusIcon' Image not found in UXML!");
        if (objectiveElement.childCount == 0) Debug.LogWarning($"ObjectiveDisplay: Instantiated element has no children—check UXML structure.");

        objectiveElement.RemoveFromClassList("completed");
    }

    private int GetRequiredProgress(ObjectiveSO objective)
    {
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
        if (objective.ObjectiveID == this.objectiveData.ObjectiveID)
        {
            currentProgress = current;
            requiredProgress = required;
            UpdateDisplay();
        }
    }

    private void UpdateDisplay()
    {
        if (statusIcon == null || objectiveLabel == null || objectiveData == null)
        {
            Debug.LogWarning($"label, icon, or data null.");
            return;
        }

        bool isComplete = objectiveInstance != null ? objectiveInstance.IsCompleted : currentProgress >= requiredProgress;

        if (statusIcon != null)
        {
            if (completeIcon != null && incompleteIcon != null)
            {
                statusIcon.style.backgroundImage = new StyleBackground(isComplete ? completeIcon.texture : incompleteIcon.texture);
            }
            statusIcon.style.unityBackgroundImageTintColor = Color.white;  // Optional: Tint if needed
        }

        if (objectiveLabel != null)
        {
            string desc = string.IsNullOrEmpty(objectiveData?.Description) ? "Unnamed Objective" : objectiveData.Description;
            objectiveLabel.text = $"{desc} ({currentProgress}/{requiredProgress})";

            // Strikethrough via USS class
            if (isComplete)
            {
                objectiveLabel.AddToClassList("completed");
            }
            else
            {
                objectiveLabel.RemoveFromClassList("completed");
            }
        }
    }

    private void OnDestroy()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.OnObjectiveProgressed -= HandleObjectiveProgress;
        }
    }
}

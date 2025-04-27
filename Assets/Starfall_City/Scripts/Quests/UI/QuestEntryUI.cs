using NUnit.Framework.Interfaces;
using System.Linq;
using TMPro;
using UnityEngine;

public class QuestEntryUI : MonoBehaviour
{
    public QuestSO QuestData { get; private set; }
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _descriptionText;
    [SerializeField] private Transform _objectivesContainer;
    [SerializeField] private ObjectiveDisplay _objectivePrefab;

    private Quest _quest;

    public void Initialize(QuestSO questData)
    {
        QuestData = questData;
        _titleText.text = questData.Title;
        _descriptionText.text = questData.Description;

        // Find the Quest instance
        _quest = QuestManager.Instance.FindQuestByObjective(questData.Objectives[0]);
        if (_quest == null)
        {
            Debug.LogWarning($"Quest instance for {questData.Title} not found.");
            return;
        }

        // Clear existing objectives
        foreach (Transform child in _objectivesContainer)
        {
            Destroy(child.gameObject);
        }

        // Display completed objectives
        var completedObjectives = _quest.GetCompletedObjectives();
        foreach (var objective in completedObjectives)
        {
            var objectiveSO = _quest.GetObjectiveSO(objective);
            if (objectiveSO != null)
            {
                var display = Instantiate(_objectivePrefab, _objectivesContainer);
                display.Initialize(objectiveSO);
            }
        }

        // Display the current active objective, if any
        var currentObjective = _quest.GetCurrentObjective();
        if (currentObjective != null)
        {
            var currentObjectiveSO = _quest.GetObjectiveSO(currentObjective);
            if (currentObjectiveSO != null)
            {
                var display = Instantiate(_objectivePrefab, _objectivesContainer);
                display.Initialize(currentObjectiveSO);
            }
        }
    }
}

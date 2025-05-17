using NUnit.Framework.Interfaces;
using System.Collections.Generic;
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
    private List<ObjectiveDisplay> _objectiveDisplays = new List<ObjectiveDisplay>();

    public void Initialize(QuestSO questData)
    {
        QuestData = questData;
        _titleText.text = questData.Title;
        _descriptionText.text = questData.Description;

        
        _quest = QuestManager.Instance.FindQuestByObjective(questData.Objectives[0]);
        if (_quest == null)
        {
            Debug.LogWarning($"Quest instance for {questData.Title} not found.");
            return;
        }

        RefreshObjectives();
    }

    private void OnEnable()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.OnObjectiveProgressed += HandleObjectiveProgressed;
            QuestManager.OnQuestCompleted += HandleQuestCompleted;
        }
    }

    private void OnDisable()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.OnObjectiveProgressed -= HandleObjectiveProgressed;
            QuestManager.OnQuestCompleted -= HandleQuestCompleted;
        }
    }

    private void HandleObjectiveProgressed(ObjectiveSO objective, int current, int required)
    {
        // Чек относится ли эта цель к текущему квесту
        if (_quest.Data.Objectives.Contains(objective))
        {
            RefreshObjectives();
        }
    }

    private void HandleQuestCompleted(QuestSO completedQuest)
    {
        if (completedQuest == QuestData)
        {
            RefreshObjectives();
        }
    }

    private void RefreshObjectives()
    {
        // Убираем старые отображения целей
        foreach (var display in _objectiveDisplays)
        {
            Destroy(display.gameObject);
        }
        _objectiveDisplays.Clear();

        // Показываем выполненные цели
        var completedObjectives = _quest.GetCompletedObjectives();
        foreach (var objective in completedObjectives)
        {
            var objectiveSO = _quest.GetObjectiveSO(objective);
            if (objectiveSO != null)
            {
                var display = Instantiate(_objectivePrefab, _objectivesContainer);
                display.Initialize(objectiveSO, objective);
                _objectiveDisplays.Add(display);
            }
        }

        // Показываем следующую цель, если такая есть
        var currentObjective = _quest.GetCurrentObjective();
        if (currentObjective != null)
        {
            var currentObjectiveSO = _quest.GetObjectiveSO(currentObjective);
            if (currentObjectiveSO != null)
            {
                var display = Instantiate(_objectivePrefab, _objectivesContainer);
                display.Initialize(currentObjectiveSO, currentObjective);
                _objectiveDisplays.Add(display);
            }
        }
    }
}

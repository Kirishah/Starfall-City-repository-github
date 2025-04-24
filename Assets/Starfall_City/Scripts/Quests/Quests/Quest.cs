using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

public class Quest
{
    public QuestSO Data { get; private set; }

    // property to fix potential missing reference
    public bool IsCompleted { get; private set; }
    private List<Objective> _objectives = new List<Objective>();

    public void Initialize(QuestSO questSO)
    {
        Data = questSO;
        _objectives.Clear(); // Ensure the list starts empty
        foreach (ObjectiveSO objectiveSO in questSO.Objectives)
        {
            var objective = objectiveSO.CreateObjective();
            objective.OnProgressChanged += HandleObjectiveProgress;
            _objectives.Add(objective);
            Debug.Log($"Initialized objective: {objectiveSO.GetType().Name} for quest {questSO.Title}");
        }
    }

    public void StartQuest()
    {
        foreach (Objective objective in _objectives)
        {
            objective.Initialize();
            objective.OnCompleted += CheckAllObjectivesCompleted;
        }
    }

    public void ProcessObjectiveEvent(ObjectiveType type, string identifier)
    {
        foreach (Objective objective in _objectives)
        {
            objective.CheckProgress(type, identifier);
        }
        CheckAllObjectivesCompleted();
    }

    private void CheckAllObjectivesCompleted()
    {
        foreach (var objective in _objectives)
        {
            if (!objective.IsCompleted) return;
        }
        IsCompleted = true;
        QuestManager.Instance.CompleteQuest(this);
    }

    public void Cleanup()
    {
        foreach (Objective objective in _objectives)
        {
            objective.OnProgressChanged -= HandleObjectiveProgress; // Unsubscribe from progress events
            objective.OnCompleted -= CheckAllObjectivesCompleted;  // Unsubscribe from completion events
            objective.Cleanup();                                   // Clean up objective internals
        }
        _objectives.Clear();                                      // Reset the list
    }
    private void HandleObjectiveProgress(ObjectiveSO objective, int current, int required)
    {
        QuestManager.Instance.ReportObjectiveProgress(objective, current, required);
    }
}

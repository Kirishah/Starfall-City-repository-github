using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

public class Quest
{
    private QuestSO _data;
    private bool _isCompleted;
    private List<Objective> _objectives = new List<Objective>();
    private int _activeObjectiveIndex;

    public QuestSO Data => _data;
    public bool IsCompleted => _isCompleted;
    public int ActiveObjectiveIndex => _activeObjectiveIndex;

    public void Initialize(QuestSO questSO)
    {
        _data = questSO;
        _objectives.Clear(); // Ensure the list starts empty
        _activeObjectiveIndex = 0;
        foreach (ObjectiveSO objectiveSO in questSO.Objectives)
        {
            var objective = objectiveSO.CreateObjective();
            objective.OnProgressChanged += HandleObjectiveProgress;
            objective.OnCompleted += HandleObjectiveCompleted;
            _objectives.Add(objective);
            Debug.Log($"Initialized objective: {objectiveSO.GetType().Name} for quest {questSO.Title}");
        }
    }

    public void StartQuest()
    {
        foreach (Objective objective in _objectives)
        {
            objective.Initialize();
        }
    }

    public void ProcessObjectiveEvent(ObjectiveType type, string identifier, string itemID)
    {
        Debug.Log($"Quest.ProcessObjectiveEvent called: type={type}, identifier={identifier}, objectives count={_objectives.Count}");

        // Find the first incomplete objective
        int firstIncompleteIndex = -1;
        for (int i = 0; i < _objectives.Count; i++)
        {
            if (!_objectives[i].IsCompleted)
            {
                firstIncompleteIndex = i;
                break;
            }
        }

        // If all objectives are complete, do nothing (CheckAllObjectivesCompleted will handle quest completion)
        if (firstIncompleteIndex == -1)
        {
            Debug.Log("All objectives completed, skipping ProcessObjectiveEvent.");
            return;
        }

        // Update the active objective index
        _activeObjectiveIndex = firstIncompleteIndex;
        Objective activeObjective = _objectives[_activeObjectiveIndex];
        Debug.Log($"Calling CheckProgress on active objective: {activeObjective.GetType().Name} (Index: {_activeObjectiveIndex})");
        activeObjective.CheckProgress(type, identifier, itemID);

        CheckAllObjectivesCompleted();
    }

    private void HandleObjectiveProgress(ObjectiveSO objective, int current, int required)
    {
        QuestManager.Instance.ReportObjectiveProgress(objective, current, required);
    }

    private void HandleObjectiveCompleted()
    {
        CheckAllObjectivesCompleted();
    }

    private void CheckAllObjectivesCompleted()
    {
        foreach (var objective in _objectives)
        {
            if (!objective.IsCompleted) return;
        }
        _isCompleted = true;
        Debug.Log($"Quest {Data.Title} completed!");
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

    // Get all completed objectives
    public List<Objective> GetCompletedObjectives()
    {
        List<Objective> completed = new List<Objective>();
        for (int i = 0; i < _objectives.Count; i++)
        {
            if (_objectives[i].IsCompleted)
            {
                completed.Add(_objectives[i]);
            }
            else
            {
                break; // Stop at the first incomplete objective
            }
        }
        return completed;
    }

    // Get the current active objective, or null if all are completed
    public Objective GetCurrentObjective()
    {
        int firstIncompleteIndex = -1;
        for (int i = 0; i < _objectives.Count; i++)
        {
            if (!_objectives[i].IsCompleted)
            {
                firstIncompleteIndex = i;
                break;
            }
        }
        return firstIncompleteIndex >= 0 ? _objectives[firstIncompleteIndex] : null;
    }

    // Map an Objective to its corresponding ObjectiveSO
    public ObjectiveSO GetObjectiveSO(Objective objective)
    {
        int index = _objectives.IndexOf(objective);
        if (index >= 0 && index < Data.Objectives.Length)
        {
            return Data.Objectives[index];
        }
        return null;
    }
}

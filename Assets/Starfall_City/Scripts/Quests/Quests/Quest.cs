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
        _objectives.Clear(); // список пустой
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
        CheckActiveObjective();
    }

    public void ProcessObjectiveEvent(ObjectiveType type, string identifier, string itemID)
    {
        Debug.Log($"Quest.ProcessObjectiveEvent called: type={type}, identifier={identifier}, objectives count={_objectives.Count}");

        // Ќаходишь первую невыполненную цель
        int firstIncompleteIndex = -1;
        for (int i = 0; i < _objectives.Count; i++)
        {
            if (!_objectives[i].IsCompleted)
            {
                firstIncompleteIndex = i;
                break;
            }
        }

        // ≈сли все цели выполнены, ничего не делай (CheckAllObjectivesCompleted обработает завершение квеста).
        if (firstIncompleteIndex == -1)
        {
            Debug.Log("All objectives completed, skipping ProcessObjectiveEvent.");
            return;
        }

        // ќбновление индекса активной цели
        _activeObjectiveIndex = firstIncompleteIndex;
        Objective activeObjective = _objectives[_activeObjectiveIndex];
        Debug.Log($"Calling CheckProgress on active objective: {activeObjective.GetType().Name} (Index: {_activeObjectiveIndex})");
        // ѕроверь автозавершение, когда цель становитс€ активной
        CheckActiveObjective();

        activeObjective.CheckProgress(type, identifier, itemID);

        // ѕовторна€ проверка после обработки событи€ 
        firstIncompleteIndex = -1;
        for (int i = 0; i < _objectives.Count; i++)
        {
            if (!_objectives[i].IsCompleted)
            {
                firstIncompleteIndex = i;
                break;
            }
        }
        if (firstIncompleteIndex != -1 && firstIncompleteIndex != _activeObjectiveIndex)
        {
            _activeObjectiveIndex = firstIncompleteIndex;
            Debug.Log($"Objective completed, updated active index to {firstIncompleteIndex}");
            CheckActiveObjective();
        }
        CheckAllObjectivesCompleted();
    }

    private void HandleObjectiveProgress(ObjectiveSO objective, int current, int required)
    {
        QuestManager.Instance.ReportObjectiveProgress(objective, current, required);
    }

    private void HandleObjectiveCompleted()
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
        if (firstIncompleteIndex != -1 && firstIncompleteIndex != _activeObjectiveIndex)
        {
            _activeObjectiveIndex = firstIncompleteIndex;
            Debug.Log($"Objective completed, updated active index to {firstIncompleteIndex} from HandleObjectiveCompleted");
            CheckActiveObjective();
        }
        CheckAllObjectivesCompleted();
    }

    private void CheckActiveObjective()
    {
        if (_activeObjectiveIndex < 0 || _activeObjectiveIndex >= _objectives.Count)
        {
            Debug.LogWarning($"Invalid activeObjectiveIndex: {_activeObjectiveIndex}");
            return;
        }

        Objective activeObjective = _objectives[_activeObjectiveIndex];
        Debug.Log($"Checking active objective: {activeObjective.GetType().Name}");

        if (activeObjective is CollectItemObjective collectionObj)
        {
            Debug.Log($"CollectionObjective detected: TargetItemID={collectionObj.TargetItemID}, RequiredAmount={((CollectItemSO)collectionObj._data).RequiredAmount}");
            Item item = ItemDataBase.Instance.GetItemByID(collectionObj.TargetItemID);
            if (item != null)
            {
                Debug.Log($"Item found in database: {item.name}, ID={item.ItemID}");
                int requiredAmount = ((CollectItemSO)collectionObj._data).RequiredAmount;
                bool hasItem = InventoryManager.Instance.HasItem(item, requiredAmount);
                Debug.Log($"Inventory has item? {hasItem}");
                if (hasItem)
                {
                    Debug.Log("Auto-completing CollectionObjective.");
                    QuestManager.Instance.ReportObjectiveProgress((CollectItemSO)collectionObj._data, requiredAmount, requiredAmount);
                    collectionObj.Complete();
                }
            }
            else
            {
                Debug.LogError($"Item not found in ItemDataBase for ID: {collectionObj.TargetItemID}");
            }
        }
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
        // ќтписываемс€ от событий
        foreach (Objective objective in _objectives)
        {
            objective.OnProgressChanged -= HandleObjectiveProgress;
            objective.OnCompleted -= CheckAllObjectivesCompleted;  
            objective.Cleanup();                                   
        }
        _objectives.Clear();                                      
    }

    
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
                break; // ќстановливаемс€ у первой незавершенной цели
            }
        }
        return completed;
    }

    
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

    // —опоставл€ем цель с соответствующим ей SO
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

using UnityEngine;
using UnityEngine.EventSystems;
using System;

// Base class for all objectives in the quest system
public abstract class Objective
{
    public ObjectiveSO _data { get; protected set; }
    public string ObjectiveID { get; private set; }
    public string Description { get; private set; }
    public ObjectiveType Type { get; protected set; }
    public bool IsCompleted { get; protected set; }
    public Objective(ObjectiveSO data)
    {
        _data = data;
        ObjectiveID = data.ObjectiveID;
        Description = data.Description;
        Type = GetObjectiveType();
        IsCompleted = false;
    }


    public delegate void ProgressHandler(ObjectiveSO objective, int current, int required);
    public event ProgressHandler OnProgressChanged;
    public event Action OnCompleted;
    

    public virtual void Initialize() => IsCompleted = false;
    public virtual void Cleanup() { }
    public abstract void CheckProgress(ObjectiveType type, string identifier, string itemID);

    public void Complete()
    {
        if (!IsCompleted)
        {
            IsCompleted = true;
            OnCompleted?.Invoke();
            Debug.Log($"Objective {_data.ObjectiveID} completed.");
        }
    }
    protected void UpdateProgress(int current, int required)
    {
        OnProgressChanged?.Invoke(_data, current, required);
        if (current >= required) Complete();
    }
    protected abstract ObjectiveType GetObjectiveType();
}

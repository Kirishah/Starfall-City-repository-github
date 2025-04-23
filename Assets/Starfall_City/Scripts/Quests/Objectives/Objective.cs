using UnityEngine;
using UnityEngine.EventSystems;
using System;

// Base class for all objectives in the quest system
public abstract class Objective
{
    public event Action<ObjectiveSO, int, int> OnProgressChanged;
    public ObjectiveSO _data { get; protected set; }

    public event Action OnCompleted;
    public bool IsCompleted { get; protected set; }

    public virtual void Initialize() => IsCompleted = false;
    public virtual void Cleanup() { }
    public virtual void CheckProgress(ObjectiveType type, string identifier) { }

    protected void Complete()
    {
        IsCompleted = true;
        OnCompleted?.Invoke();
    }
    protected void UpdateProgress(int current, int required)
    {
        OnProgressChanged?.Invoke(_data, current, required);
    }
}

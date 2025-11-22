using System;
using System.Collections.Generic;
using UnityEngine;

// Base class for all objectives in the quest system
public abstract class Objective
{
    public ObjectiveSO _data { get; protected set; }
    public string ObjectiveID { get; private set; }
    public string Description { get; private set; }
    public ObjectiveType Type { get; protected set; }
    public bool IsCompleted { get; protected set; }
    public int CurrentProgress { get; protected set; }
    public int RequiredProgress { get; protected set; }


    public Objective(ObjectiveSO data)
    {
        _data = data;
        ObjectiveID = data.ObjectiveID;
        Description = data.Description;
        Type = GetObjectiveType();
        IsCompleted = false;
        CurrentProgress = 0;
        RequiredProgress = 1; // Default to 1, can be overridden in derived classes
    }


    public delegate void ProgressHandler(ObjectiveSO objective, int current, int required);
    public event ProgressHandler OnProgressChanged;
    public event Action OnCompleted;
    

    public virtual void Initialize() => IsCompleted = false;
    public virtual void Cleanup() { }
    public abstract void CheckProgress(ObjectiveType type, string identifier, string itemID);

    public virtual void Complete()
    {
        if (!IsCompleted)
        {
            IsCompleted = true;

            if (!string.IsNullOrEmpty(_data.eventOnComplete))
            {
                var paramsDict = BuildParamsDictionary(_data);
                if (paramsDict != null && paramsDict.Count > 0)
                    EventBus.Instance.Publish(_data.eventOnComplete, paramsDict);
                else
                    EventBus.Instance.Publish(_data.eventOnComplete);
            }

            OnCompleted?.Invoke();
            Debug.Log($"Objective {_data.ObjectiveID} completed.");
        }
    }

    private Dictionary<string, object> BuildParamsDictionary(ObjectiveSO data)
    {
        if (data.eventParameters == null || data.eventParameters.Count == 0) return null;

        var dict = new Dictionary<string, object>();
        foreach (var p in data.eventParameters)
        {
            switch (p.type)
            {
                case ParameterType.String:
                    dict[p.key] = p.stringValue;
                    break;
                case ParameterType.GameObject:
                    dict[p.key] = p.objectValue;
                    break;
                case ParameterType.Vector3:
                    dict[p.key] = p.vectorValue;
                    break;
                    // add more types as needed
            }
        }
        return dict;
    }

    protected void UpdateProgress(int current, int required)
    {
        CurrentProgress = current;
        RequiredProgress = required;
        OnProgressChanged?.Invoke(_data, current, required);

        if (!string.IsNullOrEmpty(_data.eventOnProgress))
        {
            var progressParams = BuildParamsDictionary(_data); // reuse same param logic
            if (progressParams != null && progressParams.Count > 0)
                EventBus.Instance.Publish(_data.eventOnProgress, progressParams);
            else
                EventBus.Instance.Publish(_data.eventOnProgress);
        }

        if (current >= required) Complete();
    }
    protected abstract ObjectiveType GetObjectiveType();
}

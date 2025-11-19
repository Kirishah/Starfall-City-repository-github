using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Quests/Objectives/Objective")]
public abstract class ObjectiveSO : ScriptableObject
{
    public string ObjectiveID;
    public string Description;

    [Tooltip("EventBus event type to publish when this objective is completed. Leave empty to do nothing.")]
    public string eventOnComplete = "";
    [Tooltip("Optional parameters to send with the event. Key = param name, Value = what to send.")]
    public List<EventParameter> eventParameters = new List<EventParameter>();

    public abstract Objective CreateObjective();
    public virtual int GetDefaultRequiredProgress() => 1;
}

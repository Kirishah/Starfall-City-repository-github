using System.Collections.Generic;
using UnityEngine;
using core;

[CreateAssetMenu(menuName = "Quests/Objectives/Objective")]
public abstract class ObjectiveSO : ScriptableObject
{
    public string ObjectiveID;
    public string Description;

    [Tooltip("EventBus event type to publish when this objective is completed. Leave empty to do nothing.")]
    public string eventOnComplete = "";
    [Tooltip("Optional parameters to send with the event. Key = param name, Value = what to send.")]
    public List<EventParameter> eventParameters = new List<EventParameter>();
    [Tooltip("Event to publish every time progress is made (e.g., collected 1/5 items).")]
    public string eventOnProgress = "";

    public abstract Objective CreateObjective();
    public virtual int GetDefaultRequiredProgress() => 1;
}

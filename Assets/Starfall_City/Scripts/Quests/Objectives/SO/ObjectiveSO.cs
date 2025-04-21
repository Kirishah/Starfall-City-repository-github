using UnityEngine;

[CreateAssetMenu(menuName = "Quests/Objectives/Objective")]
public abstract class ObjectiveSO : ScriptableObject
{
    public string ObjectiveID;
    public string Description;
    public abstract Objective CreateObjective();
}

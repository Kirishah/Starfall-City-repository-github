using UnityEngine;

[CreateAssetMenu(menuName = "Quests/Objectives/Exploration")]
public class ExplorationSO : ObjectiveSO
{
    public string ZoneID;

    public override Objective CreateObjective() => new ExplorationObjective(this);
}

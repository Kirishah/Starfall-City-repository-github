using UnityEngine;

[CreateAssetMenu(menuName = "Quests/Objectives/Exploration")]
public class ExplorationSO : ObjectiveSO
{
    public string LocationID;

    public override Objective CreateObjective() => new ExplorationObjective(this);
}

using UnityEngine;

[CreateAssetMenu(menuName = "Quests/Objectives/Performance")]
public class PerformanceSO : ObjectiveSO
{
    public string ChallengeID; 
    // Additional data like time limits or conditions could be added here

    public override Objective CreateObjective() => new PerformanceObjective(this);
}

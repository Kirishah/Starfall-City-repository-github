using UnityEngine;

[CreateAssetMenu(menuName = "Quests/Objectives/Performance")]
public class PerformanceSO : ObjectiveSO
{
    public PerformanceType PerformanceType;
    public float RequiredScore;

    public override Objective CreateObjective() => new PerformanceObjective(this);
}

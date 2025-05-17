using UnityEngine;

[CreateAssetMenu(menuName = "Quests/Objectives/Performance")]
public class PerformanceSO : ObjectiveSO
{
    public string ChallengeID;
    // Сюда можно добавить дополнительные данные, например, ограничения по времени или условия

    public override Objective CreateObjective() => new PerformanceObjective(this);
}

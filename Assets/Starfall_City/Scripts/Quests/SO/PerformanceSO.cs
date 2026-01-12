using UnityEngine;

namespace QuestSystem
{
    [CreateAssetMenu(menuName = "Quests/Objectives/Performance")]
    public class PerformanceSO : ObjectiveSO
    {
        public string challengeID;

        public override Objective CreateObjective() => new PerformanceObjective(this);
    }
}

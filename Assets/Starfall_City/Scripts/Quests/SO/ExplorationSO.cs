using UnityEngine;

namespace QuestSystem
{
    [CreateAssetMenu(menuName = "Quests/Objectives/Exploration")]
    public class ExplorationSO : ObjectiveSO
    {
        public string LocationID { get; }

        public override Objective CreateObjective() => new ExplorationObjective(this);
    }
}

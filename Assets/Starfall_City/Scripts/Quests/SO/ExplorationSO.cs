using UnityEngine;

namespace QuestSystem
{
    [CreateAssetMenu(menuName = "Quests/Objectives/Exploration")]
    public class ExplorationSO : ObjectiveSO
    {
        public string locationID;

        public override Objective CreateObjective() => new ExplorationObjective(this);
    }
}

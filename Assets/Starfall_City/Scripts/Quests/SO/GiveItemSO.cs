using UnityEngine;

namespace QuestSystem
{
    [CreateAssetMenu(fileName = "New GiveItemObjective", menuName = "Quests/Objectives/GiveItem")]
    public class GiveItemSO : ObjectiveSO
    {
        public string TargetNPCID { get; }
        public string TargetItemID { get; }
        public int RequiredAmount { get; }

        public override Objective CreateObjective() => new GiveItemObjective(this);
    }
}

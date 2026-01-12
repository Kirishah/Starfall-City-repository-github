using UnityEngine;

namespace QuestSystem
{
    [CreateAssetMenu(fileName = "New GiveItemObjective", menuName = "Quests/Objectives/GiveItem")]
    public class GiveItemSO : ObjectiveSO
    {
        public string targetNPCID;
        public string targetItemID;
        public int requiredAmount;

        public override Objective CreateObjective() => new GiveItemObjective(this);
    }
}

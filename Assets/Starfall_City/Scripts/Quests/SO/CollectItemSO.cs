using UnityEngine;

namespace QuestSystem
{
    public class CollectItemSO : ObjectiveSO
    {
        public string TargetItemID { get; }
        public int RequiredAmount { get; }
        public override Objective CreateObjective() => new CollectItemObjective(this);
    }
}

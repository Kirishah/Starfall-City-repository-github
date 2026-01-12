using UnityEngine;

namespace QuestSystem
{
    public class CollectItemSO : ObjectiveSO
    {
        public string targetItemID;
        public int requiredAmount;
        public override Objective CreateObjective() => new CollectItemObjective(this);
    }
}

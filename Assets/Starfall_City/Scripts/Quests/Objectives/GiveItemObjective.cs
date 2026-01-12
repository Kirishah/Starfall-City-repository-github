using UnityEngine;

namespace QuestSystem
{
    public class GiveItemObjective : Objective
    {
        private readonly int _requiredAmount;
        private int _currentAmount;

        public string TargetNPCID { get; }
        public string TargetItemID { get; }

        public GiveItemObjective(GiveItemSO data) : base(data)
        {
            TargetNPCID = data.targetNPCID;
            TargetItemID = data.targetItemID;
            _requiredAmount = data.requiredAmount;
            _currentAmount = 0;
        }

        public override void CheckProgress(ObjectiveType type, string identifier, string itemID = null)
        {
            if (type == ObjectiveType.GiveItem && identifier == TargetNPCID && itemID == TargetItemID)
            {
                _currentAmount++;
                UpdateProgress(_currentAmount, _requiredAmount);
                if (_currentAmount >= _requiredAmount) Complete();
            }
        }

        protected override ObjectiveType GetObjectiveType() => ObjectiveType.GiveItem;
    }
}

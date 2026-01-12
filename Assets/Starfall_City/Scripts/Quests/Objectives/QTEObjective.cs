using UnityEngine;

namespace QuestSystem
{
    public class QTEObjective : Objective
    {
        private readonly string _qteID;
        private int _successCount;
        private readonly int _requiredCount;

        public QTEObjective(QTEObjectiveSO data) : base(data)
        {
            _data = data;
            _qteID = data.qteID;
            _requiredCount = data.requiredSuccessCount;
            _successCount = 0;
        }

        public override void CheckProgress(ObjectiveType type, string identifier, string itemID = null)
        {
            if (type == ObjectiveType.QTE && identifier == _qteID)
            {
                _successCount++;
                UpdateProgress(_successCount, _requiredCount);
                if (_successCount >= _requiredCount)
                {
                    Complete();
                }
            }
        }

        protected override ObjectiveType GetObjectiveType() => ObjectiveType.QTE;
    }
}

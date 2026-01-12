using UnityEngine;

namespace QuestSystem
{
    public class PerformanceObjective : Objective
    {
        private readonly string _challengeID;

        public PerformanceObjective(PerformanceSO data) : base(data)
        {
            _data = data;
            _challengeID = data.ChallengeID;
        }

        public override void CheckProgress(ObjectiveType type, string identifier, string itemID = null)
        {
            if (type == ObjectiveType.Performance && identifier == _challengeID)
            {
                // Предположим, что идентификатор подтверждает успех (например, «Challenge123:Success»)
                UpdateProgress(1, 1);
                Complete();
            }
        }

        protected override ObjectiveType GetObjectiveType() => ObjectiveType.Performance;
    }
}

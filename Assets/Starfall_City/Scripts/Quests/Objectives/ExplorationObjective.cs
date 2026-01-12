using UnityEngine;

namespace QuestSystem
{
    public class ExplorationObjective : Objective
    {
        private readonly string _locationID;

        public ExplorationObjective(ExplorationSO data) : base(data)
        {
            _data = data;
            _locationID = data.LocationID;
        }

        public override void CheckProgress(ObjectiveType type, string identifier, string itemID = null)
        {
            if (type == ObjectiveType.Exploration && identifier == _locationID)
            {
                UpdateProgress(1, 1); // Exploration typically requires 1 visit
                Complete();
            }
        }

        protected override ObjectiveType GetObjectiveType() => ObjectiveType.Exploration;
    }
}

using UnityEngine;

public class ExplorationObjective : Objective
{
    private readonly string _locationID;

    public ExplorationObjective(ExplorationSO data)
    {
        _data = data;
        _locationID = data.LocationID;
    }

    public override void CheckProgress(ObjectiveType type, string identifier)
    {
        if (type == ObjectiveType.Exploration && identifier == _locationID)
        {
            UpdateProgress(1, 1); // Exploration typically requires 1 visit
            Complete();
        }
    }
}

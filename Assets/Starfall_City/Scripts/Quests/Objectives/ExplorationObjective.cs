using UnityEngine;

public class ExplorationObjective : Objective
{
    private readonly ExplorationSO _data;

    public ExplorationObjective(ExplorationSO data) => _data = data;

    public override void CheckProgress(ObjectiveType type, string identifier)
    {
        if (type == ObjectiveType.Exploration && identifier == _data.ZoneID)
        {
            Complete();
        }
    }
}

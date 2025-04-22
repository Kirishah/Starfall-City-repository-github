using UnityEngine;

public class PerformanceObjective : Objective
{
    private readonly PerformanceSO _data;

    public PerformanceObjective(PerformanceSO data) => _data = data;

    public override void CheckProgress(ObjectiveType type, string identifier)
    {
        if (type == ObjectiveType.Performance && identifier == _data.PerformanceType.ToString())
        {
            // Assuming identifier includes score in format "PerformanceType:Score"
            if (float.TryParse(identifier.Split(':')[1], out float score) && score >= _data.RequiredScore)
            {
                Complete();
            }
        }
    }
}

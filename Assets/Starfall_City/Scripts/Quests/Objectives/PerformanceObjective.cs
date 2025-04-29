using UnityEngine;

public class PerformanceObjective : Objective
{
    private readonly string _challengeID;

    public PerformanceObjective(PerformanceSO data) : base(data)
    {
        _data = data;
        _challengeID = data.ChallengeID;
    }

    public override void CheckProgress(ObjectiveType type, string identifier, string itemID)
    {
        if (type == ObjectiveType.Performance && identifier == _challengeID)
        {
            // Assume identifier confirms success (e.g., "Challenge123:Success")
            UpdateProgress(1, 1);
            Complete();
        }
    }
}

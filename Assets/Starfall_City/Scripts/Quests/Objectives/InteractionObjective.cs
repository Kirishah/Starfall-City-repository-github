using UnityEngine;

public class InteractionObjective : Objective
{
    private readonly InteractionSO _data;

    public InteractionObjective(InteractionSO data) => _data = data;

    public override void CheckProgress(ObjectiveType type, string identifier)
    {
        if (type == ObjectiveType.Interaction && identifier == _data.ObjectID)
        {
            Complete();
        }
    }
}

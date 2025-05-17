using UnityEngine;

public class InteractionObjective : Objective
{
    private int _interactionCount;
    private readonly string _objectID;
    private readonly int _requiredCount;

    public InteractionObjective(InteractionSO data) : base(data) 
    {
        _data = data;
        _objectID = data.ObjectID;
        _requiredCount = data.RequiredInteractions;
        _interactionCount = 0;
    }

    public override void CheckProgress(ObjectiveType type, string identifier, string itemID)
    {
        if (type == ObjectiveType.Interaction && identifier == _objectID)
        {
            Debug.Log($"InteractionObjective CheckProgress: type={type}, identifier={identifier}, target={_objectID}");
            _interactionCount++;
            UpdateProgress(_interactionCount, _requiredCount);
            if (_interactionCount >= _requiredCount)
            {
                Complete();
            }
        }
    }

    protected override ObjectiveType GetObjectiveType()
    {
        return ObjectiveType.Interaction;
    }
}

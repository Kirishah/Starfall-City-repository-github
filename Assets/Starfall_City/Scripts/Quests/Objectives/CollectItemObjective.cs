using UnityEngine;

public class CollectItemObjective : Objective
{
    private int _currentCount;
    public readonly string _targetItemID;
    public readonly int _requiredCount;

    public string TargetItemID => _targetItemID;

    public CollectItemObjective(CollectItemSO data) : base(data) 
    {
        _data = data;
        _targetItemID = data.TargetItemID;
        _requiredCount = data.RequiredAmount;
        _currentCount = 0;
        Debug.Log($"CollectionObjective initialized: TargetItemID={_targetItemID}, RequiredAmount={_requiredCount}");
    }

    public override void CheckProgress(ObjectiveType type, string identifier, string itemID = null)
    {
        
        if (type == ObjectiveType.Collection && identifier == _targetItemID)
        {
            Debug.Log($"CollectionObjective CheckProgress: type={type}, identifier={identifier}, target={_targetItemID}");
            _currentCount++;
            UpdateProgress(_currentCount, _requiredCount);
        }
    }

    protected override ObjectiveType GetObjectiveType()
    {
        return ObjectiveType.Collection;
    }
}

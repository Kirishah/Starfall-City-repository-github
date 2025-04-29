using UnityEngine;

public class CollectItemObjective : Objective
{
    private int _currentCount;
    private readonly string _targetItemID;
    private readonly int _requiredCount;

    public CollectItemObjective(CollectItemSO data) : base(data) // Call the base constructor with data
    {
        _data = data;
        _targetItemID = data.TargetItemID;
        _requiredCount = data.RequiredAmount;
        _currentCount = 0;
    }

    public override void CheckProgress(ObjectiveType type, string identifier, string itemID)
    {
        
        if (type == ObjectiveType.Collection && identifier == _targetItemID)
        {
            Debug.Log($"CollectionObjective CheckProgress: type={type}, identifier={identifier}, target={_targetItemID}");
            _currentCount++;
            UpdateProgress(_currentCount, _requiredCount);
            if (_currentCount >= _requiredCount)
            {
                Complete();
            }
        }
    }
}

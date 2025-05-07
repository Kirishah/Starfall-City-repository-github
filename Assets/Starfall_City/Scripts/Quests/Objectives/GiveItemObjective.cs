using UnityEngine;

public class GiveItemObjective : Objective
{
    private string _targetNPCID;
    private string _targetItemID;
    private int _requiredAmount;
    private int _currentAmount;

    public string TargetNPCID => _targetNPCID;
    public string TargetItemID => _targetItemID;

    public GiveItemObjective(GiveItemSO data) : base(data)
    {
        _targetNPCID = data.TargetNPCID;
        _targetItemID = data.TargetItemID;
        _requiredAmount = data.RequiredAmount;
        _currentAmount = 0;
    }

    public override void CheckProgress(ObjectiveType type, string identifier, string itemID = null)
    {
        if (type == ObjectiveType.GiveItem && identifier == _targetNPCID && itemID == _targetItemID)
        {
            _currentAmount++;
            UpdateProgress(_currentAmount, _requiredAmount);
            if (_currentAmount >= _requiredAmount) Complete();
        }
    }
}

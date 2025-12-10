using UnityEngine;

public class CollectItemSO : ObjectiveSO
{
    public string TargetItemID; 
    public int RequiredAmount;
    public override Objective CreateObjective() => new CollectItemObjective(this);
}

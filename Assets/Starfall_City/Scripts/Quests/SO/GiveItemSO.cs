using UnityEngine;

[CreateAssetMenu(fileName = "New GiveItemObjective", menuName = "Quests/Objectives/GiveItem")]
public class GiveItemSO : ObjectiveSO
{
    public string TargetNPCID;
    public string TargetItemID;
    public int RequiredAmount;

    public override Objective CreateObjective() => new GiveItemObjective(this);
}

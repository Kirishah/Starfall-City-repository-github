using UnityEngine;

[CreateAssetMenu(menuName = "Quests/Objectives/Interaction")]
public class InteractionSO : ObjectiveSO
{
    public string ObjectID;

    public override Objective CreateObjective() => new InteractionObjective(this);
}

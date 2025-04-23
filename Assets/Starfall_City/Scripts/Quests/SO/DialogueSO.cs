using UnityEngine;

[CreateAssetMenu(menuName = "Quests/Objectives/Dialogue")]
public class DialogueSO : ObjectiveSO
{
    public string TargetNPCID;

    public override Objective CreateObjective() => new DialogueObjective(this);
}

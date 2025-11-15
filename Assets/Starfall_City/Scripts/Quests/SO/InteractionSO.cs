using UnityEngine;

[CreateAssetMenu(menuName = "Quests/Objectives/Interaction")]
public class InteractionSO : ObjectiveSO
{
    public string ObjectID; 
    public int RequiredInteractions;

    [Header("Post-Interaction Trigger")]
    public string PostInteractionDialogueID; // Dialogue start ID to trigger on completion
    public string PostDialogueNPCID; // NPC ID for the triggered dialogue

    public override Objective CreateObjective() => new InteractionObjective(this);
}

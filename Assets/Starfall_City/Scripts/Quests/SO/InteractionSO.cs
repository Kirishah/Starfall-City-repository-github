using UnityEngine;

namespace QuestSystem
{
    [CreateAssetMenu(menuName = "Quests/Objectives/Interaction")]
    public class InteractionSO : ObjectiveSO
    {
        public string objectID;
        public int requiredInteractions;

        [Header("Post-Interaction Trigger")]
        public string postInteractionDialogueID; // Dialogue start ID to trigger on completion
        public string postDialogueNPCID; // NPC ID for the triggered dialogue

        public override Objective CreateObjective() => new InteractionObjective(this);
    }
}

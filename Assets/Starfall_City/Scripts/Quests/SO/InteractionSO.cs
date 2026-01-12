using UnityEngine;

namespace QuestSystem
{
    [CreateAssetMenu(menuName = "Quests/Objectives/Interaction")]
    public class InteractionSO : ObjectiveSO
    {
        public string ObjectID { get; }
        public int RequiredInteractions { get; }

        [Header("Post-Interaction Trigger")]
        public string PostInteractionDialogueID { get; } // Dialogue start ID to trigger on completion
        public string PostDialogueNPCID { get; } // NPC ID for the triggered dialogue

        public override Objective CreateObjective() => new InteractionObjective(this);
    }
}

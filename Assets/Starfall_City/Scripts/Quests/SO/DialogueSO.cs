using UnityEngine;

namespace QuestSystem
{
    [CreateAssetMenu(menuName = "Quests/Objectives/Dialogue")]
    public class DialogueSO : ObjectiveSO
    {
        public string targetNPCID;

        public override Objective CreateObjective() => new DialogueObjective(this);
    }
}

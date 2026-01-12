using UnityEngine;

namespace QuestSystem
{
    public class DialogueObjective : Objective
    {
        public DialogueObjective(DialogueSO data) : base(data)
        {
            _data = data;
        }

        public override void CheckProgress(ObjectiveType type, string identifier, string itemID = null)
        {

            if (type == ObjectiveType.Dialogue && identifier == ((DialogueSO)_data).targetNPCID)
            {
                Debug.Log($"DialogueObjective CheckProgress: type={type}, identifier={identifier}, target={((DialogueSO)_data).targetNPCID}");
                // Report completion progress
                UpdateProgress(1, 1);  // 1/1 required
                Complete();
            }
        }

        protected override ObjectiveType GetObjectiveType() => ObjectiveType.Dialogue;
    }
}

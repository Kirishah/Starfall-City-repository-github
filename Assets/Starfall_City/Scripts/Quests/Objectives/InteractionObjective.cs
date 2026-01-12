using DialogueSystem;
using UnityEngine;

namespace QuestSystem
{
    public class InteractionObjective : Objective
    {
        private int _interactionCount;
        private readonly string _objectID;
        private readonly int _requiredCount;
        private readonly string _postInteractionDialogueID;
        private readonly string _postDialogueNPCID;

        public InteractionObjective(InteractionSO data) : base(data)
        {
            _data = data;
            _objectID = data.ObjectID;
            _requiredCount = data.RequiredInteractions;
            _postInteractionDialogueID = data.PostInteractionDialogueID;
            _postDialogueNPCID = data.PostDialogueNPCID;
            _interactionCount = 0;
        }

        public override void CheckProgress(ObjectiveType type, string identifier, string itemID)
        {
            if (type == ObjectiveType.Interaction && identifier == _objectID)
            {
                Debug.Log($"InteractionObjective CheckProgress: type={type}, identifier={identifier}, target={_objectID}");
                _interactionCount++;
                UpdateProgress(_interactionCount, _requiredCount);
                if (_interactionCount >= _requiredCount)
                {
                    Complete();
                }
            }
        }

        public override void Complete()
        {
            base.Complete();
            // Trigger post-interaction dialogue if configured
            if (!string.IsNullOrEmpty(_postInteractionDialogueID) && !string.IsNullOrEmpty(_postDialogueNPCID))
            {
                if (DialogueManager_UIToolkit.Instance != null)
                {
                    DialogueManager_UIToolkit.Instance.StartDialogue(_postInteractionDialogueID, _postDialogueNPCID);
                    Debug.Log($"Triggered post-interaction dialogue: {_postInteractionDialogueID} with NPC {_postDialogueNPCID}");
                }
                else
                {
                    Debug.LogError("DialogueManager_UIToolkit.Instance is null. Cannot trigger post-interaction dialogue.");
                }
            }
        }

        protected override ObjectiveType GetObjectiveType() => ObjectiveType.Interaction;
    }
}

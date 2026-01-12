using QuestSystem;
using DialogueSystem;
using UnityEngine;

namespace Interaction
{
    public class NPCInteractionPresenter : InteractionPresenter
    {
        [SerializeField] private QuestStarter _questStarter;
        [SerializeField] private string _startDialogueID;
        [SerializeField] private string _npcID;

        protected override void PerformInteraction()
        {
            if (_questStarter != null)
                _questStarter.StartDialogue();
            else
                DialogueManager_UIToolkit.Instance.StartDialogue(_startDialogueID, _npcID);
        }

        public override string GetIdentifier() => _npcID;
    }
}

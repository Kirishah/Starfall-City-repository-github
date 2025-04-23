using UnityEngine;

public class QuestStarter : MonoBehaviour
{
    [SerializeField] private QuestSO _questToStart;
    [SerializeField] private string _dialogueStartID;
    [SerializeField] private string _npcID;

    public void StartDialogueAndQuest()
    {
        if (!QuestManager.Instance.IsQuestActive(_questToStart))
        {
            // Start quest first
            QuestManager.Instance.StartQuest(_questToStart);
            // Start associated dialogue
            DialogueManager.Instance.StartDialogue(_dialogueStartID, _npcID);
        }
    }
}

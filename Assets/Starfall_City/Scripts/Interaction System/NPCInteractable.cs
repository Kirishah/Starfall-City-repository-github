using UnityEngine;
using TMPro;

public class NPCInteractable : Interactable
{
    [Header("Quest NPC?")]
    [SerializeField] private QuestStarter questStarter;

    [Header("Dialogue Settings")]
    [SerializeField] private string startDialogueID;
    [SerializeField] private string npcID;

    public override void Interact()
    {
        // Respect gating conditions (base handles in virtual Interact)
        if (questStarter != null)
        {
            questStarter.StartDialogue();
        }
        else
        {
            Debug.LogWarning("No QuestStarter assigned to NPC: " + npcID);
            DialogueManager_UIToolkit.Instance.StartDialogue(startDialogueID, npcID);
        }
        onInteract.Invoke();
    }

    public override string GetIdentifier()
    {
        return npcID; 
    }
}

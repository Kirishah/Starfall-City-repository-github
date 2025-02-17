using UnityEngine;

public class NPCInteractable : Interactable
{
    public Dialogue dialogue; // Reference to the dialogue data

    public override void Interact()
    {
        DialogueManager dialogueManager = FindFirstObjectByType<DialogueManager>();
        Debug.Log("Interacting with NPC");
        if (dialogueManager != null)
        {
            dialogueManager.StartDialogue(1); // Start the dialogue
        }
    }
}

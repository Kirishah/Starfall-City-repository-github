using UnityEngine;

public class NPCInteractable : Interactable
{
    public Dialogue dialogue; // Reference to the dialogue data

    public override void Interact()
    {
        DialogueManager dialogueManager = FindFirstObjectByType<DialogueManager>();
        if (dialogueManager != null)
        {
            dialogueManager.StartDialogue(dialogue); // Start the dialogue
        }
    }
}

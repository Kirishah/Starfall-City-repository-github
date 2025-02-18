using UnityEngine;

public class NPCInteractable : Interactable
{
    [Header("Dialogue Settings")]
    public int startDialogueID;

    public override void Interact()
    {
        DialogueManager.Instance.StartDialogue(startDialogueID);
        onInteract.Invoke(); 
    }
}

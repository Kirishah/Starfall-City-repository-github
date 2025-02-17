using UnityEngine;
using UnityEngine.Events;

public abstract class Interactable : MonoBehaviour
{
    [Header("Base Settings")]
    public string interactionText = "Press E to interact";
    public abstract void Interact();
}

/* Примеры объектов для взаимодействия:
 
    public class DialogueInteractable : Interactable
{
    public string[] dialogueLines;

    public override void Interact()
    {
        DialogueManager.Instance.StartDialogue(dialogueLines);
    }
}


    public class DoorInteractable : Interactable
{
    public bool isOpen;

    public override void Interact()
    {
        isOpen = !isOpen;
        // Logic to open/close the door
    }
}
*/

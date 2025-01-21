using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
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

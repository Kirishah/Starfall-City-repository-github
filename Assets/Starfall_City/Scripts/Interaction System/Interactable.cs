using UnityEngine;
using UnityEngine.Events;

public abstract class Interactable : MonoBehaviour
{
    [SerializeField] public string interactionText;
    public UnityEvent onInteract;
    public GameObject promptPrefab; // Assign the UI prompt prefab
    [SerializeField] public Vector3 promptOffset; // Adjust height

    public abstract void ShowPrompt(); 

    public abstract void HidePrompt();

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

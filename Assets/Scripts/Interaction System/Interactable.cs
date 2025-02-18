using TMPro;
using UnityEngine;
using UnityEngine.Events;

public abstract class Interactable : MonoBehaviour
{
    public string interactionText = "Press E to interact";
    public UnityEvent onInteract;
    public GameObject promptPrefab; // Assign the UI prompt prefab
    private Vector3 promptOffset = new Vector3(1f, 1.8f, 0); // Adjust height

    private GameObject currentPrompt;

    public virtual void ShowPrompt() {
        if (currentPrompt == null && promptPrefab != null)
        {
            currentPrompt = Instantiate(promptPrefab, WorldCanvasManager.Instance.transform);
            currentPrompt.GetComponent<TMP_Text>().text = interactionText;
        }
        if (currentPrompt != null)
        {
            // Convert NPC's position to viewport space (0-1 range)
            Vector3 viewportPos = Camera.main.WorldToViewportPoint(transform.position + promptOffset);

            // Check if the NPC is visible on screen
            if (viewportPos.z > 0 && viewportPos.x >= 0 && viewportPos.x <= 1 && viewportPos.y >= 0 && viewportPos.y <= 1)
            {
                // Convert viewport to screen space
                Vector3 screenPos = new Vector3(
                    viewportPos.x * Screen.width,
                    viewportPos.y * Screen.height,
                    0
                );
                currentPrompt.transform.position = screenPos;
                currentPrompt.SetActive(true);
            }
            else
            {
                currentPrompt.SetActive(false);
            }
        }
    }

    public virtual void HidePrompt()
    {
        if (currentPrompt != null)
        {
            currentPrompt.SetActive(false);
            // Optional: Destroy or pool the prompt if needed
        }
    }

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

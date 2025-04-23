using UnityEngine;
using TMPro;

public class NPCInteractable : Interactable
{
    [Header("Dialogue Settings")]
    [SerializeField] private string startDialogueID;
    [SerializeField] private string npcID;


    [Header("Prompt")]
    private GameObject currentPrompt;

    public override void Interact()
    {
        DialogueManager.Instance.StartDialogue(startDialogueID, npcID);
        onInteract.Invoke();
    }

    public override void ShowPrompt()
    {
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
    public override void HidePrompt()
    {
        if (currentPrompt != null)
        {
            currentPrompt.SetActive(false);
            // Optional: Destroy or pool the prompt if needed
        }
    }
}

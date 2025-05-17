using UnityEngine;
using TMPro;

public class NPCInteractable : Interactable
{
    [Header("Quest NPC?")]
    [SerializeField] private QuestStarter questStarter;

    [Header("Dialogue Settings")]
    [SerializeField] private string startDialogueID;
    [SerializeField] private string npcID;


    [Header("Prompt")]
    private GameObject currentPrompt;

    public override void Interact()
    {
        if (questStarter != null)
        {
            questStarter.StartDialogue();
        }
        else
        {
            Debug.LogWarning("No QuestStarter assigned to NPC: " + npcID);
            DialogueManager.Instance.StartDialogue(startDialogueID, npcID);
        }
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
            // Конвертирование позиции NPC в viewport space (диапазон 0-1)
            Vector3 viewportPos = Camera.main.WorldToViewportPoint(transform.position + promptOffset);

            // Чек если NPC в пределах камеры
            if (viewportPos.z > 0 && viewportPos.x >= 0 && viewportPos.x <= 1 && viewportPos.y >= 0 && viewportPos.y <= 1)
            {
                // Конвертирование viewport to screen space
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
        }
    }

    public override string GetIdentifier()
    {
        return npcID; 
    }
}

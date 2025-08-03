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
            currentPrompt = Instantiate(promptPrefab, WorldCanvasManager.Instance.worldCanvas.transform);
            currentPrompt.GetComponent<TMP_Text>().text = interactionText;
            // Reset position and set proper anchoring
            RectTransform rt = currentPrompt.GetComponent<RectTransform>();
            rt.anchoredPosition = Vector2.zero;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
        }
        if (currentPrompt != null)
        {
            Camera mainCamera = Camera.main;  // Camera rendering the game world
            Camera uiCamera = WorldCanvasManager.Instance.worldCanvas.worldCamera;  // UI rendering 

            // Get world position with offset
            Vector3 worldPos = transform.position + promptOffset;
            Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);

            // Convert to canvas space
            RectTransform canvasRect = WorldCanvasManager.Instance.worldCanvas.GetComponent<RectTransform>();
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPos,
                uiCamera,
                out localPoint
            );

            // Set position
            currentPrompt.GetComponent<RectTransform>().anchoredPosition = localPoint;

            // Visibility check
            bool isVisible = (screenPos.z > 0 &&
                              screenPos.x >= 0 && screenPos.x <= Screen.width &&
                              screenPos.y >= 0 && screenPos.y <= Screen.height);

            currentPrompt.SetActive(isVisible);
        }
    }

    private void OnDisable()
    {
        DestroyPrompt();
    }

    private void OnDestroy()
    {
        DestroyPrompt();
    }

    private void DestroyPrompt()
    {
        if (currentPrompt != null)
        {
            Destroy(currentPrompt);
            currentPrompt = null;
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

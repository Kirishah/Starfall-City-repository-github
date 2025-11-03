using System.Collections;
using TMPro;
using UnityEngine;

public class ChairInteractable : Interactable
{
    [Header("Chair Settings")]
    [SerializeField] private Transform sitPosition; // Assign the chair's sit transform in Inspector
    [SerializeField] private bool useRootMotionForSit = false; // If sit anim has root motion (e.g., slide-in)
    [SerializeField] private int blackScreenFrames = 2; // "One frame or more"
    [SerializeField] private string itemID;

    [Header("Prompt")]
    private GameObject currentPrompt; // For the TMP prompt UI

    private PlayerMovement playerMovement; // For disabling movement
    private PlayerAnimation playerAnim; // For sitting state

    private void Start()
    {
        GameObject playerGO = GameObject.FindWithTag("Player");
        if (playerGO == null)
        {
            Debug.LogError("Player with tag 'Player' not found!");
            return;
        }

        playerMovement = playerGO.GetComponent<PlayerMovement>();
        playerAnim = playerGO.GetComponent<PlayerAnimation>();
        if (playerMovement == null) Debug.LogError("PlayerMovement not found on Player!");
        if (playerAnim == null) Debug.LogError("PlayerAnimation not found on Player!");
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

    // Implement abstract: Show the prompt (copied/adapted from ObjectInteractable)
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

    // Implement abstract: Hide the prompt
    public override void HidePrompt()
    {
        if (currentPrompt != null)
        {
            currentPrompt.SetActive(false);
        }
    }

    public override void Interact()
    {
        if (!_isInteractable) return;

        base.Interact(); // Handles quests, events, etc.

        // Start the reusable transition sequence
        StartCoroutine(TransitionSequence(() => PerformSitAction()));
    }

    private void PerformSitAction()
    {
        if (playerMovement == null || playerAnim == null)
        {
            Debug.LogError("Player components missing—cannot sit!");
            return;
        }

        // Reposition player to sit spot
        playerMovement.transform.position = sitPosition.position;
        playerMovement.transform.rotation = sitPosition.rotation;

        playerAnim.TriggerSit(useRootMotionForSit);

        // Disable movement to lock in place
        playerMovement.enabled = false;
    }

    // Reusable sequence: Black screen → Action → Hide
    private IEnumerator TransitionSequence(System.Action action)
    {
        // Optional: Pre-transition event (e.g., play sound)
        onInteract.Invoke(); // Or a custom event

        // Show black
        yield return ScreenFader.Instance.FadeToBlack(duration: 0f, frameWait: blackScreenFrames);

        // Perform action (e.g., sit)
        action?.Invoke();

        // Hide black (with optional fade-out)
        yield return ScreenFader.Instance.FadeFromBlack(duration: 0f);

        // Post-transition: Re-enable player, update quests, etc.
        // QuestManager.Instance.HandleObjectiveUpdate(...);
    }

    public override string GetIdentifier()
    {
        return itemID; // Or override for quest ID
    }
}

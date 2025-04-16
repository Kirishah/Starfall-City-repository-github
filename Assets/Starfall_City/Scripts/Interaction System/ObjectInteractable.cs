using TMPro;
using UnityEngine;

public class ObjectInteractable : Interactable
{
    [Header("Item Settings")]
    [SerializeField] private Item _item; // Assign the ScriptableObject in the Inspector
    [SerializeField] private int _quantity = 1;

    [Header("Prompt")]
    private GameObject currentPrompt;
    private bool _isPickedUp;

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

    public override void Interact()
    {
        if (_isPickedUp) return;

        // Add the item to the inventory
        bool success = InventoryManager.Instance.AddItem(_item, _quantity);
        if (success)
        {
            _isPickedUp = true;
            HidePrompt();
            gameObject.SetActive(false);
        }
        else
        {
            Debug.Log("Inventory full!");
        }
    }
}

using TMPro;
using UnityEngine;

public class ObjectInteractable : Interactable
{
    [Header("Item Settings")]
    [SerializeField] private Item _item; // SO в инспекторе
    [SerializeField] private int _quantity = 1;
    [SerializeField] private string itemID;

    [Header("Prompt")]
    private GameObject currentPrompt;
    private bool _isPickedUp;

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
    public override void HidePrompt()
    {
        if (currentPrompt != null)
        {
            currentPrompt.SetActive(false);
        }
    }

    public override void Interact()
    {
        if (_isPickedUp) return;

        // Добавление предмета в инвентарь
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
    public override string GetIdentifier()
    {
        return itemID;
    }
}

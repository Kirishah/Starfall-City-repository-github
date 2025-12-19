using QTE;
using UnityEngine;
using UnityEngine.UIElements;

public class TutorialBanner : MonoBehaviour
{
    [SerializeField] public UIDocument uiDocument;
    [SerializeField] private string closeButtonName = "CloseButton";

    private VisualElement root;
    private Button closeButton;

    private void Awake()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        gameObject.SetActive(false); // Hidden by default
    }

    public void Show()
    {
        root = uiDocument.rootVisualElement;
        closeButton = root.Q<Button>(closeButtonName);

        if (closeButton == null)
        {
            Debug.LogError("[TutorialBanner] Button with name='CloseButton' not found after tree build! Check UXML hierarchy.", this);
        }
        else
        {
            closeButton.clicked += OnCloseClicked;
            Debug.Log("[TutorialBanner] Close button hooked successfully in Show().");
        }

        gameObject.SetActive(true);
        Time.timeScale = 0f; // Freeze game until player closes banner
        if (QTEGameManager.Instance != null)
        {
            QTEGameManager.Instance.SetQTEPaused(true);
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        Time.timeScale = 1f;

        if (QTEGameManager.Instance != null)
        {
            QTEGameManager.Instance.SetQTEPaused(false);
        }

        // Cleanup event to avoid leaks
        if (closeButton != null)
        {
            closeButton.clicked -= OnCloseClicked;
            closeButton = null;
        }
    }

    private void OnCloseClicked()
    {
        Debug.Log("[TutorialBanner] Close clicked! Hiding banner.");
        Hide();
    }

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.clicked -= OnCloseClicked;
    }
}

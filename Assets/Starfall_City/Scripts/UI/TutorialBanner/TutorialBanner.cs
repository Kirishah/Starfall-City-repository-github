using QTE;
using UnityEngine;
using UnityEngine.UIElements;

public class TutorialBanner : MonoBehaviour
{
    [SerializeField] private UIDocument _uiDocument;
    [SerializeField] private string _closeButtonName = "CloseButton";

    private VisualElement _root;
    private Button _closeButton;

    private void Awake()
    {
        if (_uiDocument == null) _uiDocument = GetComponent<UIDocument>();
        gameObject.SetActive(false); // Hidden by default
    }

    public void Show()
    {
        _root = _uiDocument.rootVisualElement;
        _closeButton = _root.Q<Button>(_closeButtonName);

        if (_closeButton == null)
        {
            Debug.LogError("[TutorialBanner] Button with name='CloseButton' not found after tree build! Check UXML hierarchy.", this);
        }
        else
        {
            _closeButton.clicked += OnCloseClicked;
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
        if (_closeButton != null)
        {
            _closeButton.clicked -= OnCloseClicked;
            _closeButton = null;
        }
    }

    private void OnCloseClicked()
    {
        Debug.Log("[TutorialBanner] Close clicked! Hiding banner.");
        Hide();
    }

    public void SetVisualTreeAsset(VisualTreeAsset asset)
    {
        if (_uiDocument != null)
        {
            _uiDocument.visualTreeAsset = asset;
            Debug.Log($"[TutorialBanner] VisualTreeAsset set to: {(asset != null ? asset.name : "null")}");
        }
        else
        {
            Debug.LogError("[TutorialBanner] Cannot set VisualTreeAsset — _uiDocument is null!");
        }
    }

    private void OnDestroy()
    {
        if (_closeButton != null)
            _closeButton.clicked -= OnCloseClicked;
    }
}

using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class ScreenFader : MonoBehaviour
{
    [Header("UI Setup")]
    [SerializeField] private VisualTreeAsset blackScreenAsset; // Assign BlackScreen.uxml in Inspector
    [SerializeField] private PanelSettings panelSettings; // Assign BlackScreenPanelSettings (optional, but recommended for full-screen)

    private VisualElement blackPanel;
    private UIDocument blackScreenDoc;

    private static ScreenFader instance;
    public static ScreenFader Instance => instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        SetupUI();
    }

    private void SetupUI()
    {
        // Add UIDocument if not already present (matches your dialogue GO setup)
        blackScreenDoc = GetComponent<UIDocument>() ?? gameObject.AddComponent<UIDocument>();

        if (panelSettings != null)
        {
            blackScreenDoc.panelSettings = panelSettings; // Handles full-screen scaling/rendering
        }

        if (blackScreenAsset != null)
        {
            blackScreenDoc.visualTreeAsset = blackScreenAsset; // Auto-builds rootVisualElement
            blackScreenDoc.enabled = true;
            blackPanel = blackScreenDoc.rootVisualElement.Q<VisualElement>("BlackScreenPanel");
            if (blackPanel != null)
            {
                // Initial inactive state: Fully hidden (no rendering or picking), even with high sortingOrder
                blackPanel.style.display = DisplayStyle.None;
                blackPanel.style.opacity = 0f; // Redundant but safe for reset
                blackPanel.pickingMode = PickingMode.Ignore; // Redundant with None, but explicit

                // Force full-screen if not in USS
                blackPanel.style.position = Position.Absolute;
                blackPanel.style.left = 0;
                blackPanel.style.right = 0;
                blackPanel.style.top = 0;
                blackPanel.style.bottom = 0;
            }
            else
            {
                Debug.LogError("BlackScreenPanel not found in UXML! Check name in UI Builder.");
            }
        }
        else
        {
            Debug.LogError("Assign BlackScreen.uxml to blackScreenAsset in Inspector!");
        }
    }

    // Core method: Show black screen for frames, with optional fade
    public Coroutine FadeToBlack(float duration = 0f, int frameWait = 1)
    {
        if (blackPanel == null)
        {
            Debug.LogWarning("BlackPanel not ready—skipping fade.");
            return null;
        }
        return StartCoroutine(FadeRoutine(true, duration, frameWait));
    }

    public Coroutine FadeFromBlack(float duration = 0f)
    {
        if (blackPanel == null) return null;
        return StartCoroutine(FadeRoutine(false, duration, 0));
    }

    private IEnumerator FadeRoutine(bool toBlack, float duration, int frameWait)
    {
        float startAlpha = toBlack ? 0f : 1f;
        float endAlpha = toBlack ? 1f : 0f;

        if (toBlack)
        {
            // Activate: Show, enable picking, and fade in (blocks UI immediately)
            blackPanel.style.display = DisplayStyle.Flex;
            blackPanel.pickingMode = PickingMode.Position;
            blackPanel.style.opacity = startAlpha;
        }
        else
        {
            // Start fade out from current (1f)
            blackPanel.style.opacity = startAlpha;
        }

        if (duration > 0)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                blackPanel.style.opacity = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
                yield return null;
            }
            blackPanel.style.opacity = endAlpha;
        }
        else
        {
            blackPanel.style.opacity = endAlpha; // Instant
        }

        // Wait specified frames
        for (int i = 0; i < frameWait; i++)
        {
            yield return null;
        }

        if (!toBlack)
        {
            blackPanel.style.display = DisplayStyle.None;
            blackPanel.pickingMode = PickingMode.Ignore;
        }
    }
}

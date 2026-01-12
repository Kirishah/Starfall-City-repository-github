using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

public class ScreenFader : MonoBehaviour
{
    [Header("UI Setup")]
    [SerializeField] private VisualTreeAsset _blackScreenAsset; // Assign BlackScreen.uxml in Inspector
    [SerializeField] private PanelSettings _panelSettings; // Assign BlackScreenPanelSettings (optional, but recommended for full-screen)

    private VisualElement _blackPanel;
    private UIDocument _blackScreenDoc;

    private static ScreenFader _instance;
    public static ScreenFader Instance => _instance;

    private CancellationTokenSource _fadeCts;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        SetupUI();
    }

    private void SetupUI()
    {
        // Add UIDocument if not already present (matches your dialogue GO setup)
        _blackScreenDoc = GetComponent<UIDocument>();
        if (_blackScreenDoc == null)
        {
            _blackScreenDoc = gameObject.AddComponent<UIDocument>();
        }

        if (_panelSettings != null)
        {
            _blackScreenDoc.panelSettings = _panelSettings; // Handles full-screen scaling/rendering
        }

        if (_blackScreenAsset != null)
        {
            _blackScreenDoc.visualTreeAsset = _blackScreenAsset; // Auto-builds rootVisualElement
            _blackScreenDoc.enabled = true;
            _blackPanel = _blackScreenDoc.rootVisualElement.Q<VisualElement>("BlackScreenPanel");
            if (_blackPanel != null)
            {
                // Initial hidden state
                HidePanelInstantly();
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

    private void HidePanelInstantly()
    {
        _blackPanel.style.display = DisplayStyle.None;
        _blackPanel.style.opacity = 0f;
        _blackPanel.pickingMode = PickingMode.Ignore;
    }

    private void ShowPanelInstantly()
    {
        _blackPanel.style.display = DisplayStyle.Flex;
        _blackPanel.pickingMode = PickingMode.Position;
        _blackPanel.style.opacity = 1f;
    }

    // Public synchronous-style methods (fire-and-forget)
    public void FadeToBlack(float duration = 0.5f, int frameWait = 1) => FadeToBlackAsync(duration, frameWait).Forget();

    public void FadeFromBlack(float duration = 0.5f) => FadeFromBlackAsync(duration).Forget();

    // Public async methods (awaitable)
    public UniTask FadeToBlackAsync(float duration = 0.5f, int frameWait = 1)
    {
        CancelCurrentFade(); // Prevent overlap
        return FadeAsync(toBlack: true, duration, frameWait);
    }

    public UniTask FadeFromBlackAsync(float duration = 0.5f)
    {
        CancelCurrentFade();
        return FadeAsync(toBlack: false, duration, 0);
    }

    private async UniTask FadeAsync(bool toBlack, float duration, int frameWait)
    {
        if (_blackPanel == null)
        {
            Debug.LogWarning("BlackPanel not ready — skipping fade.");
            return;
        }

        _fadeCts = new CancellationTokenSource();

        try
        {
            float startAlpha = toBlack ? 0f : 1f;
            float targetAlpha = toBlack ? 1f : 0f;

            // Setup visibility
            if (toBlack)
            {
                _blackPanel.style.display = DisplayStyle.Flex;
                _blackPanel.pickingMode = PickingMode.Position;
                _blackPanel.style.opacity = startAlpha;
            }

            // Fade
            if (duration > 0f)
            {
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    _blackPanel.style.opacity = Mathf.Lerp(startAlpha, targetAlpha, t);
                    await UniTask.Yield(PlayerLoopTiming.Update, _fadeCts.Token);
                }
            }

            _blackPanel.style.opacity = targetAlpha;

            // Wait specified frames
            for (var i = 0; i < frameWait; i++)
            {
                await UniTask.DelayFrame(1, PlayerLoopTiming.Update, _fadeCts.Token);
            }

            // Cleanup if fading out
            if (!toBlack)
            {
                HidePanelInstantly();
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancelled — just exit cleanly
        }
        finally
        {
            _fadeCts?.Dispose();
            _fadeCts = null;
        }
    }

    private void CancelCurrentFade()
    {
        _fadeCts?.Cancel();
        _fadeCts?.Dispose();
        _fadeCts = null;
    }

    private void OnDestroy()
    {
        CancelCurrentFade();
        if (_instance == this)
            _instance = null;
    }
}

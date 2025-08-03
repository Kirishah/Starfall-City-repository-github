using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class XPHUD : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI xpText;
    [SerializeField] private Image xpIcon;
    [SerializeField] private Image panelBackground;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip gainSound;

    [Header("Display Settings")]
    [SerializeField] private string xpPrefix = "XP: ";

    private int previousXP = 0;
    private Color originalPanelColor;
    private Color originalIconColor;
    private Vector3 originalScale;

    void Awake()
    {
        // Cache original colors and scale for animation
        originalPanelColor = panelBackground.color;
        originalIconColor = xpIcon.color;
        originalScale = transform.localScale;
    }

    void Start()
    {
        UpdateXPDisplay();
    }

    void OnEnable()
    {
        if (XPManager.Instance != null)
        {
            XPManager.Instance.OnXPChanged += UpdateXPDisplay; // Requires OnXPChanged event
            previousXP = XPManager.Instance.Experience;
            UpdateXPDisplay();
            Debug.Log("XPHUD: Subscribed to OnXPChanged");
        }
        else
        {
            Debug.LogError("XPHUD: XPManager.Instance is null in OnEnable");
        }
    }

    void OnDisable()
    {
        if (XPManager.Instance != null)
        {
            XPManager.Instance.OnXPChanged -= UpdateXPDisplay;
            Debug.Log("XPHUD: Unsubscribed from OnXPChanged");
        }
    }

    void UpdateXPDisplay()
    {
        if (XPManager.Instance == null)
        {
            Debug.LogError("XPHUD: XPManager.Instance is null in UpdateXPDisplay");
            return;
        }

        int currentXP = XPManager.Instance.Experience;
        xpText.text = $"{xpPrefix}{currentXP:N0}";
        Debug.Log($"XPHUD: Updated XP display to {currentXP}");

        // Animate if XP increased (no loss case for XP)
        if (currentXP > previousXP)
        {
            StartCoroutine(AnimateChange(Color.cyan, gainSound));
        }

        previousXP = currentXP;
    }

    private IEnumerator AnimateChange(Color glowColor, AudioClip sound)
    {
        // Play sound
        if (audioSource != null && sound != null)
        {
            audioSource.PlayOneShot(sound);
        }

        float duration = 0.5f;
        float elapsed = 0f;

        // Glow effect
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Fade glow in and out
            float glowAmount = Mathf.Sin(t * Mathf.PI); // 0 -> 1 -> 0
            panelBackground.color = Color.Lerp(originalPanelColor, glowColor, glowAmount * 0.5f);
            xpIcon.color = Color.Lerp(originalIconColor, glowColor, glowAmount * 0.3f);

            // Slight scale animation
            float scaleAmount = 1 + (glowAmount * 0.1f); // Scale up to 10% larger
            transform.localScale = originalScale * scaleAmount;

            yield return null;
        }

        // Reset to original state
        panelBackground.color = originalPanelColor;
        xpIcon.color = originalIconColor;
        transform.localScale = originalScale;
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MoneyHUD : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private Image currencyIcon;
    [SerializeField] private Image panelBackground;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip gainSound;
    [SerializeField] private AudioClip lossSound;

    [Header("Display Settings")]
    [SerializeField] private string currencySymbol = "$";

    private int previousMoney = 0;
    private Color originalPanelColor;
    private Color originalIconColor;
    private Vector3 originalScale;

    void Awake()
    {
        // Cache original colors and scale for animations
        originalPanelColor = panelBackground.color;
        originalIconColor = currencyIcon.color;
        originalScale = transform.localScale;
    }
    private void Start()
    {
        UpdateMoneyDisplay();
    }
    void OnEnable()
    {
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnMoneyChanged += UpdateMoneyDisplay;
            previousMoney = CurrencyManager.Instance.CurrentMoney;
            UpdateMoneyDisplay();
        }
    }

    void OnDisable()
    {
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnMoneyChanged -= UpdateMoneyDisplay;
        }
    }

    void UpdateMoneyDisplay()
    {
        if (CurrencyManager.Instance == null) return;

        int currentMoney = CurrencyManager.Instance.CurrentMoney;
        moneyText.text = $"{currencySymbol}{currentMoney:N0}";

        // Animate based on money change
        if (currentMoney > previousMoney)
        {
            // Money gained: glow green
            StartCoroutine(AnimateChange(Color.green, gainSound));
        }
        else if (currentMoney < previousMoney)
        {
            // Money lost: glow red
            StartCoroutine(AnimateChange(Color.red, lossSound));
        }

        previousMoney = currentMoney;
    }

    private System.Collections.IEnumerator AnimateChange(Color glowColor, AudioClip sound)
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
            currencyIcon.color = Color.Lerp(originalIconColor, glowColor, glowAmount * 0.3f);

            // Slight scale animation
            float scaleAmount = 1 + (glowAmount * 0.1f); // Scale up to 10% larger
            transform.localScale = originalScale * scaleAmount;

            yield return null;
        }

        // Reset to original state
        panelBackground.color = originalPanelColor;
        currencyIcon.color = originalIconColor;
        transform.localScale = originalScale;
    }
}

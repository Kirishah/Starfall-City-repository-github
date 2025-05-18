using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

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
        if (moneyText == null) Debug.LogError("MoneyHUD: moneyText is not assigned in Inspector");
        if (currencyIcon == null) Debug.LogError("MoneyHUD: currencyIcon is not assigned in Inspector");
        if (panelBackground == null) Debug.LogError("MoneyHUD: panelBackground is not assigned in Inspector");
        if (audioSource == null) Debug.LogError("MoneyHUD: audioSource is not assigned in Inspector");

        // Кэширование исходных цветов и масштабов для анимации
        originalPanelColor = panelBackground != null ? panelBackground.color : Color.white;
        originalIconColor = currencyIcon != null ? currencyIcon.color : Color.white;
        originalScale = transform.localScale;
        DontDestroyOnLoad(gameObject); 
        Debug.Log($"MoneyHUD: Awake on {gameObject.name}, active: {gameObject.activeInHierarchy}");
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
            Debug.Log($"MoneyHUD: Subscribed to OnMoneyChanged, CurrentMoney: {previousMoney}");
        }
        else
        {
            Debug.LogError("MoneyHUD: CurrencyManager.Instance is null in OnEnable");
            StartCoroutine(WaitForCurrencyManager());
        }
    }

    void OnDisable()
    {
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnMoneyChanged -= UpdateMoneyDisplay;
            Debug.Log("MoneyHUD: Unsubscribed from OnMoneyChanged");
        }
    }

    private IEnumerator WaitForCurrencyManager()
    {
        while (CurrencyManager.Instance == null)
        {
            Debug.Log("MoneyHUD: Waiting for CurrencyManager.Instance...");
            yield return new WaitForSeconds(0.1f);
        }
        CurrencyManager.Instance.OnMoneyChanged += UpdateMoneyDisplay;
        previousMoney = CurrencyManager.Instance.CurrentMoney;
        UpdateMoneyDisplay();
        Debug.Log($"MoneyHUD: Subscribed to OnMoneyChanged after wait, CurrentMoney: {previousMoney}");
    }

    void UpdateMoneyDisplay()
    {
        if (CurrencyManager.Instance == null)
        {
            Debug.LogError("MoneyHUD: CurrencyManager.Instance is null in UpdateMoneyDisplay");
            return;
        }
        if (moneyText == null)
        {
            Debug.LogError("MoneyHUD: moneyText is null in UpdateMoneyDisplay");
            return;
        }

        int currentMoney = CurrencyManager.Instance.CurrentMoney;
        moneyText.text = $"{currencySymbol}{currentMoney:N0}";
        Debug.Log($"MoneyHUD: Updated money display to {currentMoney} on {moneyText.gameObject.name}, active: {moneyText.gameObject.activeInHierarchy}");

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

    private IEnumerator AnimateChange(Color glowColor, AudioClip sound)
    {
        // Play sound
        if (audioSource != null && sound != null)
        {
            audioSource.PlayOneShot(sound);
        }
        else
        {
            Debug.LogWarning($"MoneyHUD: AudioSource or sound clip missing for {sound?.name}");
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
        if (panelBackground != null)
            panelBackground.color = originalPanelColor;
        if (currencyIcon != null)
            currencyIcon.color = originalIconColor;
        transform.localScale = originalScale;
    }
}

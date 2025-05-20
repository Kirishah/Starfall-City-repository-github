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

    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem moneyChangeParticles;
    [SerializeField] private float particleDuration = 0.5f;
    [SerializeField] private float particleEmissionRate = 10f;

    [Header("Neon Glow Settings")]
    [SerializeField] private Color neonColor1 = new Color(1f, 0f, 1f); // Pink
    [SerializeField] private Color neonColor2 = new Color(0f, 1f, 1f); // Cyan
    [SerializeField] private float glowCycleDuration = 3f;

    [Header("Progression Settings")]
    [SerializeField] private Color glamorousPanelColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    [SerializeField] private Color glamorousIconColor = new Color(1f, 0.84f, 0f);

    private int previousMoney = 0;
    private Color originalPanelColor;
    private Color originalIconColor;
    private Vector3 originalScale;
    private ParticleSystem.EmissionModule particleEmission;
    private ParticleSystem.MainModule particleMain;
    private Canvas parentCanvas;

    void Awake()
    {
        if (moneyText == null) Debug.LogError("MoneyHUD: moneyText is not assigned in Inspector");
        if (currencyIcon == null) Debug.LogError("MoneyHUD: currencyIcon is not assigned in Inspector");
        if (panelBackground == null) Debug.LogError("MoneyHUD: panelBackground is not assigned in Inspector");
        if (audioSource == null) Debug.LogError("MoneyHUD: audioSource is not assigned in Inspector");
        if (moneyChangeParticles == null) Debug.LogError("MoneyHUD: moneyChangeParticles is not assigned in Inspector");
        

        // Кэширование исходных цветов и масштабов для анимации
        originalPanelColor = panelBackground != null ? panelBackground.color : Color.white;
        originalIconColor = currencyIcon != null ? currencyIcon.color : Color.white;
        originalScale = transform.localScale;

        // Проверка наличия канваса
        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null)
        {
            Debug.LogError("MoneyHUD: No Canvas found in parent hierarchy");
        }

        // Кэширование модулей ParticleSystem
        if (moneyChangeParticles != null)
        {
            particleEmission = moneyChangeParticles.emission;
            particleMain = moneyChangeParticles.main;
            particleMain.playOnAwake = false;
            particleMain.loop = false;
            particleMain.duration = particleDuration;
            particleMain.simulationSpace = ParticleSystemSimulationSpace.Local;
            particleEmission.enabled = true;

            // Set renderer to match canvas sorting
            var renderer = moneyChangeParticles.GetComponent<ParticleSystemRenderer>();
            string canvasSortingLayer = parentCanvas != null && parentCanvas.sortingLayerName != "Default" ? parentCanvas.sortingLayerName : "UI";
            renderer.sortingLayerName = canvasSortingLayer;
            renderer.sortingOrder = parentCanvas != null ? parentCanvas.sortingOrder + 1 : 1;
            if (renderer.material == null)
            {
                renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
                Debug.LogWarning("MoneyHUD: ParticleSystem material was null, assigned default Particles/Standard Unlit");
            }

            Debug.Log($"MoneyHUD: ParticleSystem initialized, duration: " +
                $"{particleMain.duration}, " +
                $"emission enabled: {particleEmission.enabled}, " +
                $"position: {moneyChangeParticles.transform.localPosition}, " +
                $"scale: {moneyChangeParticles.transform.localScale}, " +
                $"sortingLayer: {renderer.sortingLayerName}, " +
                $"sortingOrder: {renderer.sortingOrder}, " +
                $"canvas renderMode: {parentCanvas?.renderMode}");
        }
        DontDestroyOnLoad(gameObject);
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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) // Press Space to test
        {
            CurrencyManager.Instance.AddMoney(10);
        }

        if (Input.GetKeyDown(KeyCode.P)) // Press P to test particles
        {
            if (moneyChangeParticles != null)
            {
                moneyChangeParticles.Clear();
                moneyChangeParticles.Play();
                Debug.Log("MoneyHUD: Manual particle test triggered with KeyCode.P");
            }
        }

        // Neon glow effect
        float t = Mathf.PingPong(Time.time / glowCycleDuration, 1f);
        Color glowColor = Color.Lerp(neonColor1, neonColor2, t);
        panelBackground.color = Color.Lerp(originalPanelColor, glowColor, 0.2f);
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
        if (audioSource != null && sound != null)
        {
            audioSource.PlayOneShot(sound);
        }
        else
        {
            Debug.LogWarning($"MoneyHUD: AudioSource or sound clip missing for {sound?.name}");
        }

        if (moneyChangeParticles != null)
        {
            particleEmission.enabled = true;
            particleEmission.rateOverTime = particleEmissionRate;
            particleMain.startColor = glowColor;
            moneyChangeParticles.Clear();
            moneyChangeParticles.Play();
            Debug.Log($"MoneyHUD: Playing particles with rate {particleEmission.rateOverTime.constant}, color {particleMain.startColor.color}, duration {particleMain.duration}");
        }
        else
        {
            Debug.LogError("MoneyHUD: moneyChangeParticles is null in AnimateChange");
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

        if (moneyChangeParticles != null && moneyChangeParticles.isPlaying)
        {
            moneyChangeParticles.Stop();
            particleEmission.enabled = false; // Disable emission to prevent residual particles
            Debug.Log("MoneyHUD: Stopped particles after animation");
        }
    }

    public void UpgradeHUDAppearance()
    {
        originalPanelColor = glamorousPanelColor;
        originalIconColor = glamorousIconColor;
        if (panelBackground != null)
        {
            panelBackground.color = originalPanelColor;
        }
        if (currencyIcon != null)
        {
            currencyIcon.color = originalIconColor;
        }
        Debug.Log("MoneyHUD: Upgraded HUD appearance for story progression");
    }
}

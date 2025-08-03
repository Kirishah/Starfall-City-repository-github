using Cinemachine;
using TMPro;
using UnityEngine;

public class DanceGameManager : MonoBehaviour
{
    public static DanceGameManager Instance { get; private set; }

    [Header("References")]
    public Animator dancerAnimator;
    public AudioSource musicTrack;
    public AudioClip hitSFX, missSFX, comboBreakSFX;
    public TextMeshProUGUI scoreText, comboText;
    public CinemachineVirtualCamera wideCam, closeUpCam, dynamicCam;
    [SerializeField] private Camera qteDance_cam;

    [Header("Settings")]

    public float beatInterval = 0.5f;
    public int basePoints = 100;
    public float comboMultiplier = 0.1f;
    public float qteDuration = 30f;

    // Runtime Variables
    private int currentScore;
    private int currentCombo;
    private float timer;
    private bool isQTEActive;

    public delegate void QTECompleteHandler(bool success);
    public static event QTECompleteHandler OnQTEComplete;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (wideCam == null || closeUpCam == null || dynamicCam == null)
        {
            Debug.LogError("One or more Cinemachine cameras are not assigned!", this);
            return;
        }

        wideCam.Priority = 10;
        closeUpCam.Priority = 10;
        dynamicCam.Priority = 10;
        musicTrack.ignoreListenerPause = true;
    }

    public void StartQTE()
    {
        currentScore = 0;
        currentCombo = 0;
        timer = 0;
        isQTEActive = true;
        UpdateUI();

        if (musicTrack != null)
        {
            musicTrack.Play();
        }
        else
        {
            Debug.LogError("DanceGameManager: musicTrack is null, cannot play music!");
        }
        if (wideCam != null)
        {
            SwitchCamera(wideCam);
        }
        else
        {
            Debug.LogError("DanceGameManager: wideCam is null, cannot switch camera!");
        }
        ArrowSpawner spawner = GetComponent<ArrowSpawner>();
        Debug.Log($"DanceGameManager: ArrowSpawner={spawner}");
        if (spawner != null)
        {
            Debug.Log("DanceGameManager: Starting ArrowSpawner");
            spawner.StartSpawning();
        }
        else
        {
            Debug.LogError("DanceGameManager: ArrowSpawner component missing!");
        }
    }

    void Update()
    {
        if (!isQTEActive) return;

        timer += Time.unscaledDeltaTime;
        if (timer >= qteDuration)
        {
            isQTEActive = false;
            if (musicTrack != null) musicTrack.Stop();
            ArrowSpawner spawner = GetComponent<ArrowSpawner>();
            if (spawner != null) spawner.StopSpawning();
            if (qteDance_cam != null) qteDance_cam.tag = "Untagged";
            OnQTEComplete?.Invoke(currentScore > 1000);
        }
    }

    public void HandleArrowPress(string direction)
    {
        if (!isQTEActive) return;

        // Animation
        if (dancerAnimator != null)
        {
            Debug.Log($"DanceGameManager: Triggering animation dance_{direction}");
            dancerAnimator.SetTrigger($"dance_{direction}");
        }

        // Score & Combo
        currentCombo++;
        currentScore += Mathf.RoundToInt(basePoints * (1 + currentCombo * comboMultiplier));
        UpdateUI();

        // SFX
        if (hitSFX != null)
        {
            PlaySFX(hitSFX);
        }

        // Camera
        if (currentCombo % 10 == 0)
            SwitchCamera(closeUpCam); // Close-up on high combos
        else if (currentCombo % 5 == 0)
            SwitchCamera(dynamicCam); // Dynamic on every 5 combos
    }

    public void HandleMiss()
    {
        if (!isQTEActive) return;

        currentCombo = 0;
        if (missSFX != null)
        {
            PlaySFX(missSFX);
        }
        UpdateUI();
        if (wideCam != null)
        {
            SwitchCamera(wideCam);
        }
        if (GetComponent<CinemachineImpulseSource>())
            GetComponent<CinemachineImpulseSource>().GenerateImpulse();
    }

    void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = $"Score: {currentScore}";
        if (comboText != null)
            comboText.text = $"Combo: x{currentCombo}";
        if (comboText != null)
            comboText.color = Color.Lerp(Color.white, Color.yellow, currentCombo / 10f);
    }

    void PlaySFX(AudioClip clip)
    {
        AudioSource.PlayClipAtPoint(clip, qteDance_cam.transform.position);
    }

    void SwitchCamera(CinemachineVirtualCamera targetCam)
    {
        wideCam.Priority = 10;
        closeUpCam.Priority = 10;
        dynamicCam.Priority = 10;
        targetCam.Priority = 20;
    }

}

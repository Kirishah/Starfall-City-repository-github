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

    [Header("Settings")]
    public float beatInterval = 0.5f;
    public int basePoints = 100;
    public float comboMultiplier = 0.1f;

    // Runtime Variables
    private int currentScore;
    private int currentCombo;
    private float currentTimeWindow;

    void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
            Destroy(this);
        else
            Instance = this;
    }

    void Start()
    {
        currentScore = 0;
        currentCombo = 0;
        UpdateUI();
        musicTrack.Play();
    }

    public void HandleArrowPress(string direction)
    {
        // Animation
        dancerAnimator.SetTrigger($"dance_{direction}");

        // Score & Combo
        currentCombo++;
        currentScore += Mathf.RoundToInt(basePoints * (1 + currentCombo * comboMultiplier));
        UpdateUI();

        // SFX
        PlaySFX(hitSFX);

        // Camera
        if (currentCombo % 5 == 0)
            SwitchCamera(dynamicCam);
    }

    public void HandleMiss()
    {
        currentCombo = 0;
        PlaySFX(missSFX);
        UpdateUI();
        SwitchCamera(wideCam);
    }

    void UpdateUI()
    {
        scoreText.text = $"Score: {currentScore}";
        comboText.text = $"Combo: x{currentCombo}";
        comboText.color = Color.Lerp(Color.white, Color.yellow, currentCombo / 10f);
    }

    void PlaySFX(AudioClip clip)
    {
        AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position);
    }

    void SwitchCamera(CinemachineVirtualCamera targetCam)
    {
        wideCam.Priority = 10;
        closeUpCam.Priority = 10;
        dynamicCam.Priority = 10;
        targetCam.Priority = 20;
    }
}

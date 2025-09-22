using Cinemachine;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System;

namespace QTE
{
    public class DanceGameManager : MonoBehaviour
    {
        public static DanceGameManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private QTEConfig config;
        public Animator dancerAnimator;
        public AudioSource musicTrack;
        public AudioClip hitSFX, missSFX, comboBreakSFX;
        public TextMeshProUGUI scoreText, comboText;
        public CinemachineVirtualCamera wideCam, closeUpCam, dynamicCam;
        [SerializeField] private Camera qteDance_cam;
        [SerializeField] private DanceArrowPool pool;

        // Runtime Variables
        private int currentScore;
        private int currentCombo;
        private float timer;
        private bool isQTEActive;
        private List<AudioSource> audioSourcePool;
        private Queue<AudioSource> availableAudioSources;

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

            audioSourcePool = new List<AudioSource>();
            availableAudioSources = new Queue<AudioSource>();

            if (wideCam == null || closeUpCam == null || dynamicCam == null || config == null)
            {
                Debug.LogError("Missing Cinemachine cameras or QTEConfig!", this);
                return;
            }

            wideCam.Priority = 10;
            closeUpCam.Priority = 10;
            dynamicCam.Priority = 10;
            musicTrack.ignoreListenerPause = true;

            // Initialize AudioSource pool
            InitializeAudioSourcePool();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            // Clean up AudioSource pool
            if (audioSourcePool != null)
            {
                foreach (AudioSource audioSource in audioSourcePool)
                {
                    if (audioSource != null)
                        Destroy(audioSource.gameObject);
                }
                audioSourcePool.Clear();
            }

            if (availableAudioSources != null)
            {
                availableAudioSources.Clear();
            }
        }

        private void OnEnable()
        {
            DanceInput.OnArrowEvent += HandleArrowEvent;
        }

        private void OnDisable()
        {
            DanceInput.OnArrowEvent -= HandleArrowEvent;
        }

        private void InitializeAudioSourcePool()
        {
            for (int i = 0; i < config.audioSourcePoolSize; i++)
            {
                GameObject audioObj = new GameObject($"AudioSource_{i}");
                audioObj.transform.SetParent(transform);
                AudioSource audioSource = audioObj.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f; // 2D audio for UI
                audioSourcePool.Add(audioSource);
                availableAudioSources.Enqueue(audioSource);
            }
            Debug.Log($"Initialized AudioSource pool with {config.audioSourcePoolSize} sources", this);
        }

        public void StartQTE()
        {
            ResetQTE();

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
            if (!isQTEActive || QTEGameManager.IsQTEPaused) return;

            timer += Time.deltaTime;
            if (timer >= config.qteDuration)
            {
                isQTEActive = false;
                if (musicTrack != null) musicTrack.Stop();

                ResetQTE();
                
                if (qteDance_cam != null) qteDance_cam.tag = "Untagged";
                ResetAnimatorTriggers();
                if (dancerAnimator != null)
                {
                    dancerAnimator.SetTrigger("stop_dance");
                }

                OnQTEComplete?.Invoke(currentScore > 1000);
                CleanupAudioSources();
            }
        }

        public void ResetQTE()
        {
            ArrowSpawner spawner = GetComponent<ArrowSpawner>();
            if (spawner != null)
            {
                spawner.ResetSpawner();
            }
            if (pool != null)
            {
                pool.ResetAllArrows();
            }
            if (DanceInput.Instance != null)
            {
                DanceInput.Instance.ClearRegisteredArrows();
            }
            Debug.Log("DanceGameManager: Full QTE reset complete");
        }

        private void ResetAnimatorTriggers()
        {
            if (dancerAnimator == null) return;

            // List all known triggers used in the Animator
            string[] triggers = new[] { "dance_Up", "dance_Down", "dance_Left", "dance_Right"};
            foreach (string trigger in triggers)
            {
                dancerAnimator.ResetTrigger(trigger);
            }
            Debug.Log("Reset all Animator triggers");
        }

        public void HandleArrowEvent(ArrowDirection direction, DanceArrow.ArrowType type, bool success)
        {
            if (!isQTEActive || QTEGameManager.IsQTEPaused) return;
            if (!success)
            {
                HandleMiss();
                return;
            }
            // Animation
            if (dancerAnimator != null)
            {
                Debug.Log($"DanceGameManager: Triggering animation dance_{direction}");
                dancerAnimator.SetTrigger($"dance_{direction}");
            }

            // Score & Combo
            currentCombo++;
            currentScore += Mathf.RoundToInt(config.basePoints * (1 + currentCombo * config.comboMultiplier));
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
            if (!isQTEActive || QTEGameManager.IsQTEPaused) return;

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
            if (availableAudioSources.Count == 0)
            {
                Debug.LogWarning("No available AudioSources in pool! Consider increasing config.audioSourcePoolSize.", this);
                return;
            }
            AudioSource audioSource = availableAudioSources.Dequeue();
            audioSource.transform.position = qteDance_cam != null ? qteDance_cam.transform.position : Vector3.zero;
            audioSource.clip = clip;
            audioSource.Play();
            StartCoroutine(ReturnAudioSourceToPool(audioSource, clip.length));
        }

        private System.Collections.IEnumerator ReturnAudioSourceToPool(AudioSource audioSource, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (audioSource != null && audioSourcePool.Contains(audioSource))
            {
                audioSource.Stop();
                audioSource.clip = null;
                availableAudioSources.Enqueue(audioSource);
            }
        }

        private void CleanupAudioSources()
        {
            foreach (AudioSource audioSource in audioSourcePool)
            {
                if (audioSource != null)
                {
                    audioSource.Stop();
                    audioSource.clip = null;
                }
            }
            availableAudioSources.Clear();
            foreach (AudioSource audioSource in audioSourcePool)
            {
                if (audioSource != null)
                    availableAudioSources.Enqueue(audioSource);
            }
            Debug.Log("Cleaned up AudioSource pool", this);
        }

        void SwitchCamera(CinemachineVirtualCamera targetCam)
        {
            if (wideCam == null || closeUpCam == null || dynamicCam == null) return;
            wideCam.Priority = 10;
            closeUpCam.Priority = 10;
            dynamicCam.Priority = 10;
            targetCam.Priority = 20;
        }

    } 
}

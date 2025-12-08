using Cinemachine;
using Core;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using core;

namespace QTE
{
    public class DanceGameManager : MonoBehaviour
    {
        public static DanceGameManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private QTEConfig config;
        [SerializeField] private PersistentReference dancer;

        private GameObject _dancerGO;
        private Animator _dancerAnimator;

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

            InitializeAudioSourcePool();
        }

        private void Start()
        {
            ResolvePlayerDancer();
        }

        private bool ResolvePlayerDancer()
        {
            // Resolve Player Dancer
            if (dancer == null || !dancer.IsValid)
            {
                Debug.LogError("DanceGameManager: Player dancer PersistentReference is missing or invalid!");
                return false;
            }

            _dancerGO = dancer.Get<GameObject>();
            if (_dancerGO == null)
            {
                Debug.LogError("DanceGameManager: Failed to resolve dancer GameObject — check PersistentRegistry fix!");
                return false;
            }

            _dancerAnimator = _dancerGO.GetComponent<Animator>();
            if (_dancerAnimator == null)
            {
                Debug.LogError($"DanceGameManager: No Animator on dancer {_dancerGO.name}!");
                return false;
            }

            Debug.Log($"DanceGameManager: Player dancer resolved → {_dancerGO.name}");
            return true;
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
            ResetPlayerState();
            isQTEActive = true;
            timer = 0f;

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
                EndQTE();
            }
        }

        private void EndQTE()
        {
            isQTEActive = false;
            musicTrack?.Stop();

            // Let RivalDancer report its own score
            var rival = FindFirstObjectByType<RivalDancer>();
            bool playerWon = currentScore >= (rival?.FinalScore ?? 0);

            Debug.Log($"DANCE RESULT → Player: {currentScore} | Rival: {rival?.FinalScore ?? 0} → " +
                      (currentScore > (rival?.FinalScore ?? 0) ? "PLAYER WINS" :
                       currentScore < (rival?.FinalScore ?? 0) ? "RIVAL WINS" : "TIE"));

            _dancerAnimator?.SetTrigger("stop_dance");
            ResetPlayerState();
            OnQTEComplete?.Invoke(playerWon);
            CleanupAudioSources();
        }

        public void ResetPlayerState()
        {
            currentScore = 0;
            currentCombo = 0;
            UpdateUI();
            GetComponent<ArrowSpawner>()?.ResetSpawner();
            pool?.ResetAllArrows();
            DanceInput.Instance?.ClearRegisteredArrows();
        }

        private void ResetAnimatorTriggers()
        {
            if (_dancerAnimator != null)
            {
                string[] triggers = { "dance_Up", "dance_Down", "dance_Left", "dance_Right" };
                foreach (string t in triggers)
                {
                    _dancerAnimator?.ResetTrigger(t);
                }
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

            _dancerAnimator.SetTrigger($"dance_{direction}");

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

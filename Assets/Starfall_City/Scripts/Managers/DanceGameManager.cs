using Cinemachine;
using core;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

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

        public delegate void QTECompleteHandler(bool success);
        public static event QTECompleteHandler OnQTEComplete;

        public double MusicStartDSP { get; private set; }

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            if (wideCam == null || closeUpCam == null || dynamicCam == null || config == null)
            {
                Debug.LogError("Missing Cinemachine cameras or QTEConfig!", this);
                return;
            }

            wideCam.Priority = 10;
            closeUpCam.Priority = 10;
            dynamicCam.Priority = 10;
            musicTrack.ignoreListenerPause = true;
        }

        private void TryResolveDancer()
        {
            PersistentRegistry.OnReady -= TryResolveDancer; // Only once

            if (ResolvePlayerDancer())
            {
                Debug.Log("DanceGameManager: Dancer resolved successfully via PersistentRegistry.OnReady");
            }
            else
            {
                Debug.LogError("DanceGameManager: FAILED to resolve dancer even after registry ready!");
            }
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
        }

        private void OnEnable()
        {
            PersistentRegistry.OnReady += TryResolveDancer;
            DanceInput.OnArrowEvent += HandleArrowEvent;
        }

        private void OnDisable()
        {
            PersistentRegistry.OnReady -= TryResolveDancer;
            DanceInput.OnArrowEvent -= HandleArrowEvent;
        }


        public void StartQTE()
        {
            ResetPlayerState();
            isQTEActive = true;
            timer = 0f;

            if (musicTrack != null)
            {
                MusicStartDSP = AudioSettings.dspTime + 0.1; // маленькая задержка чтобы точно была синхронизация
                musicTrack.PlayScheduled(MusicStartDSP);
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
            var spawner = GetComponent<ArrowSpawner>();
            Debug.Log($"DanceGameManager: ArrowSpawner={spawner}");
            if (spawner != null)
            {
                spawner.StartSpawning(MusicStartDSP); 
                Debug.Log("DanceGameManager: Starting ArrowSpawner");
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

            if (currentCombo > 10 && comboBreakSFX != null)
            {
                PlaySFX(comboBreakSFX);
            }

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
            if (clip == null) return;

            musicTrack.PlayOneShot(clip);
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

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
        [SerializeField] private QTEConfig _config;
        [SerializeField] private PersistentReference _dancer;

        [SerializeField] private AudioSource _musicTrack;
        [SerializeField] private AudioClip _hitSFX, _missSFX, _comboBreakSFX;
        [SerializeField] private TextMeshProUGUI _scoreText, _comboText;

        [SerializeField] private CinemachineVirtualCamera _wideCam, _closeUpCam, _dynamicCam;
        [SerializeField] private Camera _qteDance_cam;
        [SerializeField] private DanceArrowPool _pool;

        // Runtime Variables
        private int _currentScore;
        private int _currentCombo;
        private float _timer;
        private bool _isQTEActive;
        private GameObject _dancerGO;
        private Animator _dancerAnimator;

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

            if (_wideCam == null || _closeUpCam == null || _dynamicCam == null || _config == null)
            {
                Debug.LogError("Missing Cinemachine cameras or QTEConfig!", this);
                return;
            }

            _wideCam.Priority = 10;
            _closeUpCam.Priority = 10;
            _dynamicCam.Priority = 10;
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
            if (_dancer == null || !_dancer.IsValid)
            {
                Debug.LogError("DanceGameManager: Player dancer PersistentReference is missing or invalid!");
                return false;
            }

            _dancerGO = _dancer.Get<GameObject>();
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
            _isQTEActive = true;
            _timer = 0f;

            if (_musicTrack != null)
            {
                MusicStartDSP = AudioSettings.dspTime + 0.1; // маленькая задержка чтобы точно была синхронизация
                _musicTrack.PlayScheduled(MusicStartDSP);
            }
            else
            {
                Debug.LogError("DanceGameManager: musicTrack is null, cannot play music!");
            }
            if (_wideCam != null)
            {
                SwitchCamera(_wideCam);
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
            if (!_isQTEActive || QTEGameManager.IsQTEPaused) return;

            _timer += Time.deltaTime;
            if (_timer >= _config.qteDuration)
            {
                EndQTE();
            }
        }

        private void EndQTE()
        {
            _isQTEActive = false;
            _musicTrack.Stop();

            // Let RivalDancer report its own score
            var rival = FindFirstObjectByType<RivalDancer>();
            bool playerWon = _currentScore >= (rival != null ? rival.FinalScore : 0);

            Debug.Log($"DANCE RESULT → Player: {_currentScore} | Rival: {rival.FinalScore} → " +
                      (_currentScore > (rival.FinalScore) ? "PLAYER WINS" :
                       _currentScore < (rival.FinalScore) ? "RIVAL WINS" : "TIE"));

            _dancerAnimator.SetTrigger("stop_dance");
            ResetPlayerState();
            OnQTEComplete.Invoke(playerWon);
        }

        public void ResetPlayerState()
        {
            _currentScore = 0;
            _currentCombo = 0;
            UpdateUI();
            GetComponent<ArrowSpawner>().ResetSpawner();
            _pool.ResetAllArrows();
            DanceInput.Instance.ClearRegisteredArrows();
        }

        private void ResetAnimatorTriggers()
        {
            if (_dancerAnimator != null)
            {
                string[] triggers = { "dance_Up", "dance_Down", "dance_Left", "dance_Right" };
                foreach (var t in triggers)
                {
                    _dancerAnimator.ResetTrigger(t);
                }
            }
            Debug.Log("Reset all Animator triggers");
        }

        public void HandleArrowEvent(ArrowDirection direction, DanceArrow.ArrowType type, bool success)
        {
            if (!_isQTEActive || QTEGameManager.IsQTEPaused) return;
            if (!success)
            {
                HandleMiss();
                return;
            }

            _dancerAnimator.SetTrigger($"dance_{direction}");

            _currentCombo++;
            _currentScore += Mathf.RoundToInt(_config.basePoints * (1 + (_currentCombo * _config.comboMultiplier)));
            UpdateUI();

            // SFX
            if (_hitSFX != null)
            {
                PlaySFX(_hitSFX);
            }

            // Camera
            if (_currentCombo % 10 == 0)
                SwitchCamera(_closeUpCam); // Close-up on high combos
            else if (_currentCombo % 5 == 0)
                SwitchCamera(_dynamicCam); // Dynamic on every 5 combos
        }

        public void HandleMiss()
        {
            if (!_isQTEActive || QTEGameManager.IsQTEPaused) return;

            if (_currentCombo > 10 && _comboBreakSFX != null)
            {
                PlaySFX(_comboBreakSFX);
            }

            _currentCombo = 0;

            if (_missSFX != null)
            {
                PlaySFX(_missSFX);
            }
            UpdateUI();
            if (_wideCam != null)
            {
                SwitchCamera(_wideCam);
            }
            if (GetComponent<CinemachineImpulseSource>())
                GetComponent<CinemachineImpulseSource>().GenerateImpulse();
        }

        void UpdateUI()
        {
            if (_scoreText != null)
                _scoreText.text = $"Score: {_currentScore}";
            if (_comboText != null)
                _comboText.text = $"Combo: x{_currentCombo}";
            if (_comboText != null)
                _comboText.color = Color.Lerp(Color.white, Color.yellow, _currentCombo / 10f);
        }

        void PlaySFX(AudioClip clip)
        {
            if (clip == null) return;

            _musicTrack.PlayOneShot(clip);
        }

        void SwitchCamera(CinemachineVirtualCamera targetCam)
        {
            if (_wideCam == null || _closeUpCam == null || _dynamicCam == null) return;
            _wideCam.Priority = 10;
            _closeUpCam.Priority = 10;
            _dynamicCam.Priority = 10;
            targetCam.Priority = 20;
        }

        public void SetMusicClip(AudioClip newClip)
        {
            if (_musicTrack != null && newClip != null)
            {
                _musicTrack.clip = newClip;
                Debug.Log($"DanceGameManager: Music clip overridden to {newClip.name}");
            }
            else if (newClip == null)
            {
                Debug.LogWarning("DanceGameManager: Attempted to set null music clip.");
            }
            else
            {
                Debug.LogError("DanceGameManager: musicTrack AudioSource is null — cannot set clip!");
            }
        }
    }
}

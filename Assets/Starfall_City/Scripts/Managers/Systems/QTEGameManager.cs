using QuestSystem;
using DialogueSystem;
using Core;
using Movement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace QTE
{
    public class QTEGameManager : MonoBehaviour
    {
        public static QTEGameManager Instance { get; private set; }
        public interface IRPGComponent { } // implement in RPG scripts like PlayerMovement.cs
        public static bool IsQTEActive { get; private set; } // Public state flag
        public static bool IsQTEPaused { get; private set; }
        public static event System.Action OnQTEStart;

        [Header("References")]
        [SerializeField] private GameObject _qteCanvas;
        [SerializeField] private DanceGameManager _danceGameManager;
        [SerializeField] private Camera _mainCamera; // Orthographic camera
        [SerializeField] private Camera _uiCamera;
        [SerializeField] private Camera _qteDance_cam;
        [SerializeField] private DanceInput _danceInput;

        [Header("QTE Configurations")]
        [SerializeField] private QTEConfig _defaultConfig; // fallback
        [SerializeField] private TutorialBanner _tutorialBanner; // optional reference

        private QTEConfig _activeConfig;

        private PlayerMovement _playerMovement;
        private int _playerOriginalLayer;
        private string _mainCameraOriginalTag;
        private string _currentQTEID;

        private AudioListener _audioListener;
        private AudioListener _audioListenerQTE;
        private bool _wasRPGPaused;
        private readonly List<MonoBehaviour> _rpgComponents = new(); // Cache list
        private bool _isInitialized = false; // Flag to prevent re-init spam

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start() => Initialize(); // Core init moved here

        private void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            // Validate static/serialized references (non-dynamic)
            if (!_qteCanvas || !_danceGameManager || !_mainCamera || !_uiCamera || !_qteDance_cam)
            {
                Debug.LogError("QTEGameManager: Missing static references!" +
                    $"qteCanvas={_qteCanvas}, danceGameManager={_danceGameManager}, " +
                    $"mainCamera={_mainCamera}, uiCamera={_uiCamera}, qteDance_cam={_qteDance_cam}");
                enabled = false; // Still disable if core scene objects are missing
                return;
            }

            // Resolve dynamic player references
            if (!InitializePlayerReferences())
            {
                // Player not ready yet -- retry once after a frame (common in async loads)
                StartCoroutine(RetryInitialization());
                return;
            }

            // Setup audio and UI (now safe)
            _audioListener = _mainCamera.GetComponent<AudioListener>();
            _audioListenerQTE = _qteDance_cam.GetComponent<AudioListener>();
            if (_audioListener == null || _audioListenerQTE == null)
            {
                Debug.LogError("AudioListener missing on mainCamera or qteDance_cam!", this);
                enabled = false;
                return;
            }
            _audioListenerQTE.enabled = false;
            _qteCanvas.SetActive(false);
            _qteDance_cam.enabled = false;

            // Cache RPG components (now includes Player if loaded)
            CacheRPGComponents();

            Debug.Log("QTEGameManager: Initialized successfully.");
        }

        private bool InitializePlayerReferences()
        {
            // Find Player dynamically (adjust tag if needed)
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null)
            {
                Debug.LogWarning("QTEGameManager: Player GameObject not found yet -- delaying init.");
                return false;
            }

            _playerMovement = playerObj.GetComponent<PlayerMovement>();
            if (_playerMovement == null)
            {
                Debug.LogError("QTEGameManager: PlayerMovement component missing on Player!");
                enabled = false;
                return false;
            }

            // If danceInput is on Player, resolve it too (or keep serialized if it's a prefab)
            if (_danceInput == null)
            {
                _danceInput = playerObj.GetComponent<DanceInput>();
                if (_danceInput == null)
                {
                    Debug.LogError("QTEGameManager: DanceInput missing on Player!");
                    enabled = false;
                    return false;
                }
            }

            return true;
        }

        private IEnumerator RetryInitialization()
        {
            yield return null; // Wait one frame
            Initialize(); // Re-run full init
        }

        private void CacheRPGComponents()
        {
            _rpgComponents.Clear();
            var allScripts = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var script in allScripts)
            {
                if (script is IRPGComponent)
                {
                    _rpgComponents.Add(script);
                }
            }
            Debug.Log($"QTEGameManager: Cached {_rpgComponents.Count} RPG components.");
        }

        private void OnEnable()
        {
            DialogueManager_UIToolkit.OnQTETrigger += StartQTE;
            DanceGameManager.OnQTEComplete += EndQTE;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            DialogueManager_UIToolkit.OnQTETrigger -= StartQTE;
            DanceGameManager.OnQTEComplete -= EndQTE;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        public void SetQTEPaused(bool paused)
        {
            IsQTEPaused = paused;
            // This freezes dspTime + pauses music perfectly
            AudioListener.pause = paused;

            // Optional: volume control for smoother feel (fades out/in)
            AudioListener.volume = paused ? 0f : 1f;
        }

        public void StartQTE(string qteId = "default")
        {
            var loadedConfig = Resources.LoadAll<QTEConfig>("QTE")
                    .FirstOrDefault(c => c.qteId == qteId);
            _activeConfig = loadedConfig != null ? loadedConfig : _defaultConfig;

            if (_activeConfig == null)
            {
                Debug.LogWarning($"No QTEConfig found with ID '{qteId}'! Using default.");
                _activeConfig = _defaultConfig;
            }

            ApplyConfig(_activeConfig);
            _currentQTEID = qteId;

            // Safety: Re-init player refs if somehow missing (e.g., scene reload)
            if (_playerMovement == null)
            {
                if (!InitializePlayerReferences())
                {
                    Debug.LogError("QTEGameManager: Cannot start QTE -- Player refs still missing!");
                    return;
                }
                CacheRPGComponents(); // Re-cache if needed
            }

            Debug.Log($"QTEGameManager: Starting QTE {qteId}");
            IsQTEActive = true;
            IsQTEPaused = false;
            _wasRPGPaused = PauseManager.IsPaused;

            if (OnQTEStart != null)
            {
                try
                {
                    OnQTEStart.Invoke();
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"QTEGameManager: Exception in OnQTEStart: {ex.Message}", this);
                }
            }
            else
            {
                Debug.LogWarning("QTEGameManager: OnQTEStart has no subscribers", this);
            }

            PauseRPG();

            _mainCameraOriginalTag = _mainCamera.tag;
            _mainCamera.enabled = false;
            _uiCamera.enabled = false;
            _audioListener.enabled = false;
            _qteDance_cam.enabled = true;
            _qteDance_cam.tag = _mainCameraOriginalTag;
            _audioListenerQTE.enabled = true;

            // Reset all components 
            DanceGameManager.Instance.ResetPlayerState(); // call to centralized reset

            _qteCanvas.SetActive(true);

            if (_danceGameManager != null)
            {
                _danceGameManager.StartQTE();
                _danceInput.EnableInput();
            }
            else
            {
                Debug.LogError("QTEGameManager: danceGameManager is null!");
            }

            _playerOriginalLayer = _playerMovement.gameObject.layer;
            _playerMovement.gameObject.layer = LayerMask.NameToLayer("QTE");
            if (_playerMovement.TryGetComponent<NavMeshAgent>(out var playerAgent))
            {
                playerAgent.EnterCinematicMode();
            }
            else
            {
                Debug.LogError("QTEGameManager: playerAgent is null!");
            }

            Time.timeScale = 1f; // Ensure normal time for QTE

            if (_activeConfig.showTutorialBanner)
            {
                ShowTutorialBanner();
            }
        }

        private void ApplyConfig(QTEConfig config)
        {
            var spawner = GetComponent<ArrowSpawner>();
            if (spawner) spawner.Config = config;

            if (_danceGameManager != null)
            {
                // Apply music override
                if (config.musicTrack != null)
                {
                    _danceGameManager.SetMusicClip(config.musicTrack);
                }

                // You can extend DanceGameManager to expose ApplyConfig() if you want more overrides
            }

            var rival = FindFirstObjectByType<RivalDancer>();
            if (rival != null && config.enableRival)
            {
                rival.SetAIParameters(
                    config.rivalAccuracy,
                    config.rivalReactionDelay,
                    config.rivalIntentionalMissChance,
                    config.rivalComboAggression
                );
            }
        }

        private void ShowTutorialBanner()
        {
            if (_tutorialBanner != null)
            {
                if (_activeConfig.tutorialBannerAsset != null)
                {
                    _tutorialBanner.SetVisualTreeAsset(_activeConfig.tutorialBannerAsset);
                }

                _tutorialBanner.Show();
            }
            else
            {
                Debug.LogWarning("Tutorial banner requested but QTETutorialBanner component missing!");
            }
        }

        public void EndQTE(bool success)
        {
            // Safety: Ensure player refs exist
            if (_playerMovement == null)
            {
                Debug.LogError("QTEGameManager: Cannot end QTE -- Player refs missing!");
                return;
            }

            Debug.Log("QTEGameManager: Ending QTE", this);
            IsQTEActive = false;
            IsQTEPaused = false;

            // Restore player
            _playerMovement.gameObject.layer = _playerOriginalLayer;
            FindObjectsByType<NavMeshAgent>(FindObjectsSortMode.None)
                .ToList()
                .ForEach(a => a.ExitCinematicMode());

            // Switch cameras
            _mainCamera.enabled = true;
            _mainCamera.tag = _mainCameraOriginalTag;
            _uiCamera.enabled = true;
            _audioListener.enabled = true;
            _audioListenerQTE.enabled = false;
            _qteDance_cam.enabled = false;
            _qteDance_cam.tag = "qteDance_cam";
            _qteCanvas.SetActive(false);

            _danceInput.DisableInput();
            DanceGameManager.Instance.ResetPlayerState();

            // Report QTE success to QuestManager if won
            if (success && QuestManager.Instance != null && !string.IsNullOrEmpty(_currentQTEID))
            {
                QuestManager.Instance.HandleObjectiveUpdate(ObjectiveType.QTE, _currentQTEID);
                Debug.Log($"QTE Objective progress reported for ID: {_currentQTEID}");
            }
            _currentQTEID = ""; // Reset

            ResumeRPG();
        }

        private void PauseRPG()
        {
            // Disable RPG-specific components 
            foreach (var script in _rpgComponents)
            {
                if (script.enabled) // Extra safety
                {
                    script.enabled = false;
                }
            }
        }

        private void ResumeRPG()
        {
            foreach (var script in _rpgComponents)
            {
                if (script != null)
                {
                    script.enabled = true;
                }
            }
            Time.timeScale = _wasRPGPaused ? 0f : 1f;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _isInitialized = false; // Reset flag to re-init on new scene
            CacheRPGComponents(); // Refresh cache
        }
    }
}

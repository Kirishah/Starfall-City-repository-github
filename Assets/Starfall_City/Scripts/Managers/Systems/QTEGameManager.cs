using Core;
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
        [SerializeField] private GameObject qteCanvas;
        [SerializeField] private DanceGameManager danceGameManager;
        [SerializeField] private Camera mainCamera; // Orthographic camera
        [SerializeField] private Camera uiCamera;
        [SerializeField] private Camera qteDance_cam;
        [SerializeField] private DanceInput danceInput;

        [Header("QTE Configurations")]
        [SerializeField] private QTEConfig defaultConfig;                    // fallback
        [SerializeField] private TutorialBanner tutorialBanner;           // optional reference

        private QTEConfig activeConfig;

        private PlayerMovement playerMovement;
        private int playerOriginalLayer;
        private string mainCameraOriginalTag;
        private string _currentQTEID;

        private AudioListener audioListener;
        private AudioListener audioListenerQTE;
        private bool wasRPGPaused;
        private List<MonoBehaviour> rpgComponents = new List<MonoBehaviour>(); // Cache list
        private bool isInitialized = false; // Flag to prevent re-init spam

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            Initialize(); // Core init moved here
        }

        private void Initialize()
        {
            if (isInitialized) return;
            isInitialized = true;

            // Validate static/serialized references (non-dynamic)
            if (!qteCanvas || !danceGameManager || !mainCamera || !uiCamera || !qteDance_cam)
            {
                Debug.LogError($"QTEGameManager: Missing static references! " +
                    $"qteCanvas={qteCanvas}, danceGameManager={danceGameManager}, " +
                    $"mainCamera={mainCamera}, uiCamera={uiCamera}, qteDance_cam={qteDance_cam}");
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
            audioListener = mainCamera.GetComponent<AudioListener>();
            audioListenerQTE = qteDance_cam.GetComponent<AudioListener>();
            if (audioListener == null || audioListenerQTE == null)
            {
                Debug.LogError("AudioListener missing on mainCamera or qteDance_cam!", this);
                enabled = false;
                return;
            }
            audioListenerQTE.enabled = false;
            qteCanvas.SetActive(false);
            qteDance_cam.enabled = false;

            // Cache RPG components (now includes Player if loaded)
            CacheRPGComponents();

            Debug.Log("QTEGameManager: Initialized successfully.");
        }

        private bool InitializePlayerReferences()
        {
            // Find Player dynamically (adjust tag if needed)
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null)
            {
                Debug.LogWarning("QTEGameManager: Player GameObject not found yet -- delaying init.");
                return false;
            }

            playerMovement = playerObj.GetComponent<PlayerMovement>();
            if (playerMovement == null)
            {
                Debug.LogError("QTEGameManager: PlayerMovement component missing on Player!");
                enabled = false;
                return false;
            }

            // If danceInput is on Player, resolve it too (or keep serialized if it's a prefab)
            if (danceInput == null)
            {
                danceInput = playerObj.GetComponent<DanceInput>();
                if (danceInput == null)
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
            rpgComponents.Clear();
            var allScripts = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var script in allScripts)
            {
                if (script is IRPGComponent)
                {
                    rpgComponents.Add(script);
                }
            }
            Debug.Log($"QTEGameManager: Cached {rpgComponents.Count} RPG components.");
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
            activeConfig = Resources.LoadAll<QTEConfig>("QTE")
                              .FirstOrDefault(c => c.qteId == qteId) ?? defaultConfig;

            if (activeConfig == null)
            {
                Debug.LogWarning($"No QTEConfig found with ID '{qteId}'! Using default.");
                activeConfig = defaultConfig;
            }

            ApplyConfig(activeConfig);
            _currentQTEID = qteId;

            // Safety: Re-init player refs if somehow missing (e.g., scene reload)
            if (playerMovement == null)
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
            wasRPGPaused = PauseManager.IsPaused;

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

            mainCameraOriginalTag = mainCamera.tag;
            mainCamera.enabled = false;
            uiCamera.enabled = false;
            audioListener.enabled = false;
            qteDance_cam.enabled = true;
            qteDance_cam.tag = mainCameraOriginalTag;
            audioListenerQTE.enabled = true;

            // Reset all components 
            DanceGameManager.Instance.ResetPlayerState(); // call to centralized reset

            qteCanvas.SetActive(true);

            if (danceGameManager != null)
            {
                danceGameManager.StartQTE();
                danceInput.EnableInput();
            }
            else
            {
                Debug.LogError("QTEGameManager: danceGameManager is null!");
            }

            playerOriginalLayer = playerMovement.gameObject.layer;
            playerMovement.gameObject.layer = LayerMask.NameToLayer("QTE");
            NavMeshAgent playerAgent = playerMovement.GetComponent<NavMeshAgent>();
            playerAgent?.EnterCinematicMode();

            Time.timeScale = 1f; // Ensure normal time for QTE

            if (activeConfig.showTutorialBanner)
            {
                ShowTutorialBanner();
            }
        }

        private void ApplyConfig(QTEConfig config)
        {
            var spawner = GetComponent<ArrowSpawner>();
            if (spawner) spawner.Config = config;

            if (danceGameManager != null)
            {
                // Apply music override
                if (config.musicTrack != null && danceGameManager.musicTrack != null)
                    danceGameManager.musicTrack.clip = config.musicTrack;

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
            if (tutorialBanner != null)
            {
                if (activeConfig.tutorialBannerAsset != null)
                    tutorialBanner.uiDocument.visualTreeAsset = activeConfig.tutorialBannerAsset;

                tutorialBanner.Show();
            }
            else
            {
                Debug.LogWarning("Tutorial banner requested but QTETutorialBanner component missing!");
            }
        }

        public void EndQTE(bool success)
        {
            // Safety: Ensure player refs exist
            if (playerMovement == null)
            {
                Debug.LogError("QTEGameManager: Cannot end QTE -- Player refs missing!");
                return;
            }

            Debug.Log("QTEGameManager: Ending QTE", this);
            IsQTEActive = false;
            IsQTEPaused = false;

            // Restore player
            playerMovement.gameObject.layer = playerOriginalLayer;
            FindObjectsByType<NavMeshAgent>(FindObjectsSortMode.None)
                .ToList()
                .ForEach(a => a.ExitCinematicMode());

            // Switch cameras
            mainCamera.enabled = true;
            mainCamera.tag = mainCameraOriginalTag;
            uiCamera.enabled = true;
            audioListener.enabled = true;
            audioListenerQTE.enabled = false;
            qteDance_cam.enabled = false;
            qteDance_cam.tag = "qteDance_cam";
            qteCanvas.SetActive(false);

            danceInput.DisableInput();
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
            foreach (var script in rpgComponents)
            {
                if (script != null && script.enabled) // Extra safety
                {
                    script.enabled = false;
                }
            }
        }

        private void ResumeRPG()
        {
            foreach (var script in rpgComponents)
            {
                if (script != null)
                {
                    script.enabled = true;
                }
            }
            Time.timeScale = wasRPGPaused ? 0f : 1f;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            isInitialized = false; // Reset flag to re-init on new scene
            CacheRPGComponents(); // Refresh cache
        }
    }
}

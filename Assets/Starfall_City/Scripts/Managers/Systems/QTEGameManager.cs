using Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

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

        private PlayerMovement playerMovement;
        private Vector3 playerOriginalPosition;
        private int playerOriginalLayer;
        private string mainCameraOriginalTag;

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


            if (!qteCanvas || !danceGameManager || !mainCamera || !uiCamera || !qteDance_cam || !playerMovement)
            {
                Debug.LogError($"QTEGameManager: Missing references! " +
                    $"qteCanvas={qteCanvas}, " +
                    $"danceGameManager={danceGameManager}, " +
                    $"mainCamera={mainCamera}, " +
                    $"uiCamera={uiCamera}, " +
                    $"qteDance_cam={qteDance_cam}, " +
                    $"playerMovement={playerMovement}");
                enabled = false;
                return;
            }
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

            var allScripts = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var script in allScripts)
            {
                if (script is IRPGComponent)
                {
                    rpgComponents.Add(script);
                }
            }
        }

        private void OnEnable()
        {
            DialogueManager.OnQTETrigger += StartQTE;
            DanceGameManager.OnQTEComplete += EndQTE;
        }

        private void OnDisable()
        {
            DialogueManager.OnQTETrigger -= StartQTE;
            DanceGameManager.OnQTEComplete -= EndQTE;
        }

        public void SetQTEPaused(bool paused)
        {
            IsQTEPaused = paused;
            if (danceGameManager != null && danceGameManager.musicTrack != null)
            {
                if (paused)
                    danceGameManager.musicTrack.Pause();
                else
                    danceGameManager.musicTrack.UnPause();
            }
        }

        public void StartQTE()
        {
            Debug.Log("QTEGameManager: Starting QTE");
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
            DanceGameManager.Instance.ResetQTE(); // call to centralized reset

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

            playerOriginalPosition = playerMovement.transform.position;
            playerOriginalLayer = playerMovement.gameObject.layer;
            playerMovement.gameObject.layer = LayerMask.NameToLayer("QTE");
            NavMeshAgent agent = playerMovement.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                // Stop agent instead of disabling component to avoid state issues
                agent.isStopped = true;
                agent.updatePosition = false;
                agent.updateRotation = false;
            }
            NavMeshHit hit;
            if (NavMesh.SamplePosition(playerOriginalPosition, out hit, 10f, NavMesh.AllAreas))
            {
                playerMovement.transform.position = hit.position + new Vector3(11.5f, 0, -5); // Align to NavMesh floor
            }
            else
            {
                playerMovement.transform.position = new Vector3(11.5f, 0, 1);
            }

            Time.timeScale = 1f; // Ensure normal time for QTE
        }

        public void EndQTE(bool success)
        {
            Debug.Log("QTEGameManager: Ending QTE", this);
            IsQTEActive = false;
            IsQTEPaused = false;

            // Restore player
            playerMovement.gameObject.layer = playerOriginalLayer;
            NavMeshAgent agent = playerMovement.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                // Re-sample original position and warp agent to valid NavMesh spot
                NavMeshHit hit;
                if (NavMesh.SamplePosition(playerOriginalPosition, out hit, 10f, NavMesh.AllAreas))
                {
                    agent.Warp(hit.position);
                    Debug.Log("QTEGameManager: Warped player to valid NavMesh position.");
                }
                else
                {
                    agent.Warp(playerOriginalPosition);
                    Debug.LogWarning("QTEGameManager: Failed to sample NavMesh for restore—using original position.");
                }
                // Resume agent instead of re-enabling component
                agent.updatePosition = true;
                agent.updateRotation = true;
                agent.isStopped = false;
            }
            else
            {
                playerMovement.transform.position = playerOriginalPosition;
            }

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
            DanceGameManager.Instance.ResetQTE();
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
    } 
}

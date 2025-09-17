using UnityEngine;
using System.Collections;
using UnityEngine.AI;
using Core;

namespace QTE
{
    public class QTEGameManager : MonoBehaviour
    {
        public static QTEGameManager Instance { get; private set; }
        public static bool IsQTEActive { get; private set; } // Public state flag
        public static bool IsQTEPaused { get; private set; }
        public static event System.Action OnQTEStart;

        [Header("References")]
        [SerializeField] private GameObject qteCanvas;
        [SerializeField] private DanceGameManager danceGameManager;
        [SerializeField] private Camera mainCamera; // Orthographic camera
        [SerializeField] private Camera uiCamera;
        [SerializeField] private Camera qteDance_cam;
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private DanceInput danceInput;


        private Vector3 playerOriginalPosition;
        private int playerOriginalLayer;
        private string mainCameraOriginalTag;

        private AudioListener audioListener;
        private AudioListener audioListenerQTE;
        private bool wasRPGPaused;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }


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
            DanceGameManager.Instance?.StartQTE();
            OnQTEStart?.Invoke();
            PauseRPG();

            mainCameraOriginalTag = mainCamera.tag;

            mainCamera.enabled = false;
            uiCamera.enabled = false;
            audioListener.enabled = false;

            qteDance_cam.enabled = true;
            qteDance_cam.tag = mainCameraOriginalTag;
            audioListenerQTE.enabled = true;

            qteCanvas.SetActive(true);

            playerOriginalPosition = playerMovement.transform.position;
            playerOriginalLayer = playerMovement.gameObject.layer;
            playerMovement.gameObject.layer = LayerMask.NameToLayer("QTE");
            NavMeshAgent agent = playerMovement.GetComponent<NavMeshAgent>();
            if (agent != null) agent.enabled = false;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(playerOriginalPosition, out hit, 10f, NavMesh.AllAreas))
            {
                playerMovement.transform.position = hit.position + new Vector3(11.5f, 0, 1); // Align to NavMesh floor
            }
            else
            {
                playerMovement.transform.position = new Vector3(11.5f, 0, 1);
            }

            Animator playerAnimator = playerMovement.GetComponent<Animator>();
            if (playerAnimator != null)
            {
                playerAnimator.updateMode = AnimatorUpdateMode.Normal;
            }

            Time.timeScale = 1f; // Ensure normal time for QTE
            if (danceGameManager != null)
            {
                danceGameManager.StartQTE();
                danceInput.EnableInput();
            }
            else
            {
                Debug.LogError("QTEGameManager: danceGameManager is null!");
            }

        }

        public void EndQTE(bool success)
        {
            Debug.Log("QTEGameManager: Ending QTE", this);
            IsQTEActive = false;
            IsQTEPaused = false;

            // Restore player
            playerMovement.gameObject.layer = playerOriginalLayer;
            playerMovement.transform.position = playerOriginalPosition;
            NavMeshAgent agent = playerMovement.GetComponent<NavMeshAgent>();
            if (agent != null) agent.enabled = true;

            Animator playerAnimator = playerMovement.GetComponent<Animator>();
            if (playerAnimator != null)
            {
                playerAnimator.updateMode = AnimatorUpdateMode.Normal;
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
            ResumeRPG();
        }

        private void PauseRPG()
        {
            // Disable RPG-specific components instead of setting timeScale
            if (playerMovement != null)
            {
                playerMovement.enabled = false;
            }
            // Add other RPG components to disable (e.g., enemy AI, scripts)
            foreach (var script in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (script != null && script != this &&
                    (script.GetType().Namespace?.Contains("QTE") != true) &&
                    (script.GetType().Namespace?.Contains("Core") != true) &&
                    (script.GetType().Namespace?.Contains("Cinemachine") != true) &&
                    (script.GetType().Namespace?.Contains("UnityEngine.Rendering.Universal") != true))
                {
                    script.enabled = false;
                }
            }
        }

        private void ResumeRPG()
        {
            if (playerMovement != null)
            {
                playerMovement.enabled = !wasRPGPaused; // Respect pause state
            }
            foreach (var script in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (script != null && script != this &&
                    (script.GetType().Namespace?.Contains("QTE") != true) &&
                    (script.GetType().Namespace?.Contains("Core") != true) &&
                    (script.GetType().Namespace?.Contains("Cinemachine") != true) &&
                    (script.GetType().Namespace?.Contains("UnityEngine.Rendering.Universal") != true))
                {
                    script.enabled = true;
                }
            }
            Time.timeScale = wasRPGPaused ? 0f : 1f;
        }
    } 
}

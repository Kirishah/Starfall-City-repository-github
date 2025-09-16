using UnityEngine;
using System.Collections;
using UnityEngine.AI;

public class QTEGameManager : MonoBehaviour
{
    public static QTEGameManager Instance { get; private set; }
    public static bool IsQTEActive { get; private set; } // Public state flag

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

    public void StartQTE()
    {
        Debug.Log("QTEGameManager: Starting QTE");
        IsQTEActive = true;
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
            playerMovement.transform.position = hit.position + new Vector3(0, 0.35f, 0); // Align to NavMesh floor
        }
        else
        {
            playerMovement.transform.position = new Vector3(0, 0.35f, 0);
        }

        Animator playerAnimator = playerMovement.GetComponent<Animator>();
        if (playerAnimator != null)
        {
            playerAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
        }

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
        Debug.Log("QTEGameManager: Ending QTE");
        IsQTEActive = false;

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
        Time.timeScale = 0;
    }

    private void ResumeRPG()
    {
        Time.timeScale = 1;
    }
}

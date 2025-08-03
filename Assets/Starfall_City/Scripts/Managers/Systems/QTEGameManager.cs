using UnityEngine;
using System.Collections;

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
        }
        audioListener = mainCamera.GetComponent<AudioListener>();
        audioListenerQTE = qteDance_cam.GetComponent<AudioListener>();
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
        // audioListener.enabled = false;
        Destroy(audioListener);

        qteDance_cam.enabled = true;
        // audioListenerQTE.enabled = true;
        audioListenerQTE = qteDance_cam.gameObject.AddComponent<AudioListener>();

        qteDance_cam.tag = mainCameraOriginalTag;

        qteCanvas.SetActive(true);

        playerOriginalPosition = playerMovement.transform.position;
        playerOriginalLayer = playerMovement.gameObject.layer;
        playerMovement.gameObject.layer = LayerMask.NameToLayer("QTE");
        playerMovement.transform.position = new Vector3(0, 0.35f, 0); // Adjust for Dance camera

        Animator playerAnimator = playerMovement.GetComponent<Animator>();
        if (playerAnimator != null)
        {
            playerAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
        }

        if (danceGameManager != null)
        {
            danceGameManager.StartQTE();
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

        Animator playerAnimator = playerMovement.GetComponent<Animator>();
        if (playerAnimator != null)
        {
            playerAnimator.updateMode = AnimatorUpdateMode.Normal;
        }
        // Switch cameras
        mainCamera.enabled = true;
        mainCamera.tag = mainCameraOriginalTag;
        audioListener = mainCamera.gameObject.AddComponent<AudioListener>();

        uiCamera.enabled = true;
        qteDance_cam.enabled = false;
        qteDance_cam.tag = "qteDance_cam";
        Destroy(audioListenerQTE);
        qteCanvas.SetActive(false);

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

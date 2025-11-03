using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using UnityEngine.SceneManagement;

public class IntroManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DialogueManager_UIToolkit dialogueManager;

    [Header("Timing")]
    [SerializeField] private float bossApproachDelay = 6f; // Seconds after scene load

    [Header("Positions")]
    [SerializeField] private float bossStoppingDistance = 1.5f; // Distance from deskPos to stop
    [SerializeField] private float minStartDistance = 3f; // Minimum distance between start and desk to ensure movement

    private GameObject player;
    private GameObject boss;
    private Vector3 bossInitialPos; 
    private Transform deskPos; // Куда подходит босс
    private PlayerMovement playerMovement;
    private Player3DMovement player3DMovement;
    private PlayerAnimation playerAnimation;
    private NavMeshAgent bossAgent;
    private Animator bossAnimator; // Optional: For boss walk/idle
    private NPCController bossController; // To use HasReachedDestination if available

    private bool sequenceActive = true;
    private bool isInitialized = false;

    void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (isInitialized) return;
        isInitialized = true;

        // Resolve dynamic refs
        if (!ResolveDynamicReferences())
        {
            // Retry after one frame if not ready
            StartCoroutine(RetryInitialization());
            return;
        }

        // Setup components
        playerMovement = player.GetComponent<PlayerMovement>();
        player3DMovement = player.GetComponent<Player3DMovement>();
        playerAnimation = player.GetComponent<PlayerAnimation>();
        bossAgent = boss.GetComponent<NavMeshAgent>();
        bossAnimator = boss.GetComponent<Animator>();
        bossController = boss.GetComponent<NPCController>();

        if (playerMovement == null && player3DMovement == null)
        {
            Debug.LogError("IntroManager: No valid PlayerMovement or Player3DMovement on Player!");
            enabled = false;
            return;
        }

        if (bossAgent == null)
        {
            Debug.LogError("IntroManager: NavMeshAgent missing on Boss NPC!");
            enabled = false;
            return;
        }

        bossAgent.stoppingDistance = bossStoppingDistance;

        // Initial state
        if (playerMovement != null) playerMovement.controlsEnabled = false;
        if (player3DMovement != null) player3DMovement.controlsEnabled = false;
        if (playerAnimation != null) playerAnimation.isSitting = true;

        Debug.Log($"IntroManager: Controls confirmed disabled - PM: {playerMovement?.controlsEnabled ?? true}, P3D: {player3DMovement?.controlsEnabled ?? true}");

        // Subscribe to events
        DialogueManager_UIToolkit.OnDialogueEnded += HandleDialogueEnd;

        // Start sequence
        StartCoroutine(RunIntroSequence());
        Debug.Log("IntroManager: Initialized successfully.");
    }

    private bool ResolveDynamicReferences()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("IntroManager: Player GameObject not found yet -- delaying init.");
            return false;
        }

        boss = GameObject.FindGameObjectWithTag("Boss");
        if (boss == null)
        {
            Debug.LogWarning("IntroManager: Boss NPC GameObject not found yet -- delaying init.");
            return false;
        }

        // Boss start pos is the boss itself
        bossInitialPos = boss.transform.position;

        // Find deskPos: closest GameObject tagged "Desk" to player
        GameObject[] potentialDesks = GameObject.FindGameObjectsWithTag("Desk");
        if (potentialDesks.Length == 0)
        {
            Debug.LogWarning("IntroManager: No GameObjects tagged 'Desk' found -- using fallback position in front of player.");
            deskPos = player.transform; // Fallback to player pos (adjust offset if needed)
            deskPos.position += player.transform.forward * 2f; // Example: 2 units in front
            return true; // Allow proceed with fallback
        }

        Transform closest = null;
        float minDist = float.MaxValue;
        foreach (var deskObj in potentialDesks)
        {
            float dist = Vector3.Distance(player.transform.position, deskObj.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = deskObj.transform;
            }
        }

        if (closest != null)
        {
            deskPos = closest;
            Debug.Log($"IntroManager: Selected closest desk at distance {minDist}.");
        }
        else
        {
            Debug.LogWarning("IntroManager: No valid desk found -- using fallback.");
            deskPos = player.transform;
            deskPos.position += player.transform.forward * 2f;
        }

        // Validate start and desk positions are sufficiently apart
        float distBetweenPositions = Vector3.Distance(bossInitialPos, deskPos.position);
        if (distBetweenPositions < minStartDistance)
        {
            Debug.LogWarning($"IntroManager: Start pos and desk pos too close ({distBetweenPositions:F2}m). Adjusting initial pos to distant location.");
            // Set a distant initial position (do not move boss yet; will move in sequence)
            bossInitialPos = player.transform.position - (player.transform.forward * 8f) + (player.transform.right * 2f); // Distant fallback: behind and to side
            distBetweenPositions = Vector3.Distance(bossInitialPos, deskPos.position);
            Debug.Log($"IntroManager: Adjusted initial pos to {bossInitialPos}. New distance to desk: {distBetweenPositions:F2}m.");
        }

        return true;
    }

    private IEnumerator RetryInitialization()
    {
        yield return null; // One frame
        Initialize();
    }

    private IEnumerator RunIntroSequence()
    {
        // Wait for scene settle
        yield return new WaitForSeconds(bossApproachDelay);

        // Boss approaches
        boss.transform.position = bossInitialPos;
        bossAgent.enabled = true;
        bossAgent.ResetPath(); // Clear any prior path
        bossAgent.SetDestination(deskPos.position);

        if (bossAnimator != null) bossAnimator.SetBool("isWalking", true); 

        // Wait for boss to reach with tolerance and path check
        float tolerance = 0.2f;
        while (bossAgent.enabled &&
               (bossAgent.pathPending ||
                bossAgent.remainingDistance > bossAgent.stoppingDistance + tolerance))
        {
            // Debug log every 2 seconds
            if (Time.frameCount % 120 == 0)
            {
                Debug.Log($"IntroManager: Boss remaining distance: {bossAgent.remainingDistance}, stopping: {bossAgent.stoppingDistance}, pathPending: {bossAgent.pathPending}");
            }
            yield return null;
        }

        Debug.Log("IntroManager: Boss has reached destination.");

        if (bossAnimator != null) bossAnimator.SetBool("isWalking", false); // Idle

        // Rotate boss to face player
        Vector3 lookDirection = (player.transform.position - boss.transform.position).normalized;
        lookDirection.y = 0; // Keep on ground plane
        if (lookDirection != Vector3.zero)
        {
            boss.transform.rotation = Quaternion.LookRotation(lookDirection);
        }

        // Start dialogue only after approach
        if (dialogueManager != null)
        {
            dialogueManager.StartDialogue("d_boss_intro", "Boss");
        }
        else
        {
            Debug.LogError("IntroManager: DialogueManager not assigned!");
        }
    }

    public void HandleDialogueEnd()
    {
        if (!sequenceActive) return;
        sequenceActive = false;

        // Trigger get-up
        if (playerAnimation != null) playerAnimation.TriggerGetUp();

        // Wait for get-up to finish, then enable controls
        StartCoroutine(EnableControlsAfterGetUp());
    }

    private IEnumerator EnableControlsAfterGetUp()
    {
        yield return new WaitForSeconds(1.5f); // Match GetUpSequence duration

        if (playerMovement != null) playerMovement.controlsEnabled = true;
        if (player3DMovement != null) player3DMovement.controlsEnabled = true;

        // Boss returns to initial position and stays there
        if (bossAgent != null)
        {
            float returnDistance = Vector3.Distance(boss.transform.position, bossInitialPos);
            Debug.Log($"IntroManager: Boss returning to initial pos {bossInitialPos}. Distance: {returnDistance}");

            if (returnDistance < 0.5f)
            {
                Debug.LogWarning("IntroManager: Boss already near initial pos (dist < 0.5m) -- skipping return movement.");
            }
            else
            {
                bossAgent.stoppingDistance = 0.1f; // Close stop for idle position
                bossAgent.ResetPath();
                bossAgent.SetDestination(bossInitialPos);
                if (bossAnimator != null) bossAnimator.SetBool("isWalking", true);

                // Wait for boss to reach back with tolerance
                float tolerance = 0.2f;
                while (bossAgent.enabled &&
                       (bossAgent.pathPending ||
                        (bossController != null ? !bossController.HasReachedDestination() : bossAgent.remainingDistance > bossAgent.stoppingDistance + tolerance)))
                {
                    // Debug log every 2 seconds during return
                    if (Time.frameCount % 120 == 0)
                    {
                        Debug.Log($"IntroManager: Boss returning - remaining distance: {bossAgent.remainingDistance}, stopping: {bossAgent.stoppingDistance}, pathPending: {bossAgent.pathPending}");
                    }
                    yield return null;
                }

                Debug.Log("IntroManager: Boss has returned to initial position and is now idle.");
            }

            // Ensure idle at position
            if (bossAnimator != null) bossAnimator.SetBool("isWalking", false);

            // Optional: Rotate boss to a default facing (e.g., forward or away from player)
            // boss.transform.rotation = Quaternion.LookRotation(Vector3.forward); // Example: face forward
        }

        // Deactivate the boss after sequence completion
        if (boss != null)
        {
            boss.SetActive(false);
            Debug.Log("IntroManager: Boss deactivated.");
        }

        // Clean up IntroManager
        gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        DialogueManager_UIToolkit.OnDialogueEnded -= HandleDialogueEnd;
        // Re-enable if destroyed early
    }

    // For scene reloads/async loads
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        isInitialized = false;
        // Re-resolve if needed (e.g., player/boss persist or reload)
    }
}

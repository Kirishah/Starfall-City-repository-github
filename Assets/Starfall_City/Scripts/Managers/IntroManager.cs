using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using UnityEngine.SceneManagement;

public class IntroManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DialogueManager_UIToolkit dialogueManager;

    [Header("Timing")]
    [SerializeField] private float bossApproachDelay = 1f; // Seconds after scene load

    private GameObject player;
    private GameObject boss;
    private Transform bossStartPos; 
    private Transform deskPos; // Куда подходит босс
    private PlayerMovement playerMovement;
    private Player3DMovement player3DMovement;
    private PlayerAnimation playerAnimation;
    private NavMeshAgent bossAgent;
    private Animator bossAnimator; // Optional: For boss walk/idle

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

        // Initial state
        if (playerMovement != null) playerMovement.controlsEnabled = false;
        if (player3DMovement != null) player3DMovement.controlsEnabled = false;
        if (playerAnimation != null) playerAnimation.isSitting = true;

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
        bossStartPos = boss.transform;

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
        boss.transform.position = bossStartPos.position;
        bossAgent.enabled = true;
        bossAgent.SetDestination(deskPos.position);
        if (bossAnimator != null) bossAnimator.SetBool("isWalking", true); // Optional

        // Wait for boss to reach
        while (bossAgent.enabled && bossAgent.remainingDistance > bossAgent.stoppingDistance)
        {
            yield return null;
        }

        if (bossAnimator != null) bossAnimator.SetBool("isWalking", false); // Idle

        // Start dialogue
        dialogueManager.StartDialogue("boss_intro", "Boss");
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

        // Optional: Boss leaves
        if (bossAgent != null)
        {
            bossAgent.SetDestination(bossStartPos.position);
            if (bossAnimator != null) bossAnimator.SetBool("isWalking", true);
        }

        Destroy(gameObject, 5f); // Clean up
    }

    void OnDestroy()
    {
        DialogueManager_UIToolkit.OnDialogueEnded -= HandleDialogueEnd;
        // Re-enable if destroyed early
        if (playerMovement != null) playerMovement.controlsEnabled = true;
        if (player3DMovement != null) player3DMovement.controlsEnabled = true;
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

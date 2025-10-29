using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class IntroManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject boss;
    [SerializeField] private Transform bossStartPos;
    [SerializeField] private Transform deskPos; // Where boss stops
    [SerializeField] private DialogueManager_UIToolkit dialogueManager;

    [Header("Timing")]
    [SerializeField] private float bossApproachDelay = 1f; // Seconds after scene load

    private PlayerMovement playerMovement;
    private Player3DMovement player3DMovement;
    private PlayerAnimation playerAnimation;
    private NavMeshAgent bossAgent;
    private Animator bossAnimator; // Optional: For boss walk/idle

    private bool sequenceActive = true;

    void Start()
    {
        if (player == null || boss == null || dialogueManager == null)
        {
            Debug.LogError("IntroSequenceManager: Missing references!");
            return;
        }

        DialogueManager_UIToolkit.OnDialogueEnded += HandleDialogueEnd;

        // Setup initial state
        playerMovement = player.GetComponent<PlayerMovement>();
        player3DMovement = player.GetComponent<Player3DMovement>();
        playerAnimation = player.GetComponent<PlayerAnimation>();
        bossAgent = boss.GetComponent<NavMeshAgent>();
        bossAnimator = boss.GetComponent<Animator>();

        if (playerMovement != null) playerMovement.controlsEnabled = false;
        if (player3DMovement != null) player3DMovement.controlsEnabled = false;
        playerAnimation.isSitting = true; // Starts sitting animation

        // Position player at desk if needed (assume already placed in scene)
        // player.transform.position = deskPos.position; // Uncomment if dynamic



        // Start sequence
        StartCoroutine(RunIntroSequence());
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

        // Listen for end (via event or direct call; add event to DialogueManager if needed)
        // For simplicity, assume you add public event DialogueManager.OnDialogueEnded += HandleDialogueEnd;
        // Here, we'll use a coroutine wait (hacky; improve with event)
        yield return new WaitForSeconds(3f); // Adjust based on voice/reading time; better: poll currentDialogue == null

        // Or directly: In DialogueManager.EndDialogue(), invoke a static event if (!sequenceActive) return; sequenceActive = false; FindObjectOfType<IntroSequenceManager>()?.HandleDialogueEnd();

        HandleDialogueEnd();
    }

    public void HandleDialogueEnd()
    {
        if (!sequenceActive) return;
        sequenceActive = false;

        // Trigger get-up
        playerAnimation.TriggerGetUp();

        // Wait for get-up to finish, then enable controls
        StartCoroutine(EnableControlsAfterGetUp());
    }

    private IEnumerator EnableControlsAfterGetUp()
    {
        yield return new WaitForSeconds(1.5f); // Match GetUpSequence duration

        if (playerMovement != null) playerMovement.controlsEnabled = true;
        if (player3DMovement != null) player3DMovement.controlsEnabled = true;

        // Optional: Fade out boss or move away
        bossAgent.SetDestination(bossStartPos.position); // Boss leaves
        if (bossAnimator != null) bossAnimator.SetBool("isWalking", true);

        Destroy(gameObject, 5f); // Clean up after boss leaves
    }

    void OnDestroy()
    {
        DialogueManager_UIToolkit.OnDialogueEnded -= HandleDialogueEnd;
        // Re-enable if destroyed early
        if (playerMovement != null) playerMovement.controlsEnabled = true;
        if (player3DMovement != null) player3DMovement.controlsEnabled = true;
    }
}

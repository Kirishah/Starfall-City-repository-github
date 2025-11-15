using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using QTE;

public class PlayerAnimation : MonoBehaviour
{
    private Animator animator;
    private PlayerMovement playerMovement;
    private Player3DMovement player3DMovement;
    private CharacterController controller;

    [Header("Pose State")]
    public bool isInPose = false; // Replaces isSitting
    public PoseConfig currentConfig; // Track full config for exit
    private string currentPoseID; // Track for exit (e.g., "Sit") - used for logging/events
    private string currentExitTrigger; // Track for generic exit 
    private Coroutine currentEnterCoroutine;
    private Coroutine currentExitCoroutine;

    private float speedThreshold = 0.1f; 
    private float smoothTime = 0.1f; 
    private float currentSpeed;

    // Turn animation variables
    private Vector3 previousDesired;
    private float turnThreshold = 100f; // Degrees per second to trigger turn
    private float turnCooldown = 1.0f; // Prevent rapid successive turns
    private float lastTurnTime;


    void Start()
    {
        animator = GetComponent<Animator>();
        playerMovement = GetComponent<PlayerMovement>();
        player3DMovement = GetComponent<Player3DMovement>();
        controller = GetComponent<CharacterController>();

        isInPose = false;
        currentPoseID = null;
        currentExitTrigger = null;
        currentConfig = null;

        // Initialize turn tracking
        previousDesired = transform.forward;
        lastTurnTime = -turnCooldown; // Allow immediate turn

        DialogueManager_UIToolkit.OnDialogueEnded += HandleDialogueEnd;
    }

    private void OnDestroy() 
    {
        if (DialogueManager_UIToolkit.Instance != null)
        {
            DialogueManager_UIToolkit.OnDialogueEnded -= HandleDialogueEnd;
        }
    }

    // Public setters for tracking (called from PosePresenter)
    public void SetCurrentPose(string poseID, PoseConfig config)
    {
        currentPoseID = poseID;
        currentConfig = config; // Store for exit
        Debug.Log($"Entered pose: {currentPoseID} (using config: {config?.name ?? "null"})");
    }

    public void SetCurrentExitTrigger(string exitTrigger)
    {
        currentExitTrigger = exitTrigger;
    }

    // uses tracked values with fallback
    private void HandleDialogueEnd()
    {
        if (!isInPose)
        {
            return; // Not posed, do nothing
        }

        PoseConfig configToUse = currentConfig ?? ScriptableObject.CreateInstance<PoseConfig>(); // Fallback instance if null (rare)
        configToUse.blackHoldDuration = 2f; // Set fallback value after creation
        Debug.Log($"Dialogue ended while in pose '{currentPoseID}'. Instant exiting with config: {configToUse.name}");
        InstantExitPose(configToUse);
    }

    public void InstantExitPose(PoseConfig config)
    {
        if (currentExitCoroutine != null) StopCoroutine(currentExitCoroutine);
        currentExitCoroutine = StartCoroutine(InstantExitSequence(config));
    }

    private IEnumerator InstantExitSequence(PoseConfig config)
    {
        Debug.Log($"Instant exit from pose '{currentPoseID}'.");

        // Black screen in
        yield return ScreenFader.Instance.FadeToBlack(duration: 0f, frameWait:0);

        // Immediately start transition out of pose (hidden under black)
        isInPose = false;
        animator.SetBool("isSitting", false); // Starts blend to standing now

        // Hold full black for config duration (buffer for transition to complete)
        yield return new WaitForSecondsRealtime(config.blackHoldDuration);

        // Re-enable movement
        if (playerMovement != null) playerMovement.controlsEnabled = true;
        if (player3DMovement != null) player3DMovement.controlsEnabled = true;
        if (player3DMovement != null) player3DMovement.SnapToSurface();

        // Clear tracking
        currentPoseID = null;
        currentExitTrigger = null;
        currentConfig = null;

        // Black screen out
        yield return ScreenFader.Instance.FadeFromBlack(duration: 0f);

        Debug.Log("Instant pose exit complete: Standing and movement re-enabled.");
    }

    private void OnAnimatorMove()
    {
        if (animator.applyRootMotion && controller != null && player3DMovement.IsInTransitionAnimation)
        {
            // Apply position delta from root motion to CharacterController
            controller.Move(animator.deltaPosition);

            // Optional: Apply rotation if your get-up anim includes root rotation
            // transform.rotation = animator.deltaRotation * transform.rotation;
        }
    }

    void Update()
    {
        if (QTEGameManager.IsQTEActive) return;
        CheckMovement();

        if (isInPose)
        {
            animator.SetBool("isSitting", true);
            animator.SetBool("is_Walking", false); // Override walking during sit
        }
        else
        {
            animator.SetBool("isSitting", false);
        }

        CheckTurnAnimation();
    }

    void CheckMovement()
    {
        // Check if the player is moving
        Vector3 velocity = playerMovement.GetVelocity(); 

        float targetSpeed = velocity.magnitude;
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, smoothTime);
        bool isMoving = currentSpeed > speedThreshold;


        if (isMoving)
        {
            // If the player is moving, set the walking animation
            animator.SetBool("is_Walking", true);
        }
        else
        {
            // If the player is not moving, set the standing animation
            animator.SetBool("is_Walking", false);
        }
    }

    void CheckTurnAnimation()
    {
        // Get desired direction based on movement mode
        Vector3 desired = Vector3.zero;
        if (playerMovement.player.enabled)
        {
            desired = playerMovement.GetDesiredDirection();
        }
        else
        {
            desired = player3DMovement.GetDesiredDirection();
        }

        // Only check for turns when moving and have a valid desired direction
        if (desired.magnitude > 0.01f && animator.GetBool("is_Walking"))
        {
            // Calculate turn angle between previous desired and current desired
            float turnAngle = Vector3.SignedAngle(previousDesired, desired, Vector3.up);
            float turnRate = Mathf.Abs(turnAngle) / Time.deltaTime; // Degrees per second

            // Check if we should trigger a 180 turn
            if (turnRate > turnThreshold && Mathf.Abs(turnAngle) > 90f &&
                Time.time - lastTurnTime > turnCooldown)
            {
                animator.SetTrigger("Turn180");
                lastTurnTime = Time.time;
            }

            // Update previous desired only if valid
            previousDesired = desired;
        }
    }

    // Generalized enter 
    public void TriggerEnterPose(string enterTrigger, bool enableRootMotion = false)
    {
        if (isInPose)
        {
            Debug.LogWarning($"Already in pose '{currentPoseID}'. Skipping enter.");
            return;
        }

        if (currentEnterCoroutine != null) StopCoroutine(currentEnterCoroutine);
        currentEnterCoroutine = StartCoroutine(EnterPoseSequence(enterTrigger, enableRootMotion));
    }

    private IEnumerator EnterPoseSequence(string enterTrigger, bool enableRootMotion)
    {
        isInPose = true;
        if (enableRootMotion)
        {
            animator.applyRootMotion = true;
            if (player3DMovement != null)
            {
                player3DMovement.IsInTransitionAnimation = true;
            }
        }

        animator.SetTrigger(enterTrigger);
        animator.Update(0f);  // Force immediate evaluation of transitions (0 deltaTime = next "frame")
        yield return null;    // One frame for state change to propagate

        // Brief wait for transition to start
        yield return new WaitForEndOfFrame();

        // Poll for state entry (generic; customize per anim if needed)
        float maxWaitTime = 1f;
        float elapsed = 0f;
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        bool stateEntered = false;


        while (elapsed < maxWaitTime)
        {
            // Check for any "enter" state; extend with specific names if multi-pose
            if (stateInfo.IsName("Sitting Idle") || stateInfo.IsName("Neutral Idle")) 
            {
                stateEntered = true;
                break;
            }
            yield return null;
            elapsed += Time.deltaTime;
            stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        }

        if (!stateEntered)
        {
            Debug.LogWarning($"EnterPoseSequence: Animator did not enter pose state within timeout!");
            if (enableRootMotion)
            {
                animator.applyRootMotion = false;
                player3DMovement.IsInTransitionAnimation = false;
            }
            yield break;
        }

        // Wait for anim length minus buffer
        float animLength = stateInfo.length;
        yield return new WaitForSeconds(animLength - 0.05f);

        if (enableRootMotion)
        {
            animator.applyRootMotion = false;
            player3DMovement.IsInTransitionAnimation = false;
        }

        if (player3DMovement != null)
        {
            player3DMovement.SnapToSurface();
        }

        Debug.Log("Pose enter complete.");
    }

    // Yieldable wait (generalized)
    public IEnumerator WaitForPoseComplete(bool isEnter)
    {
        yield return new WaitForSeconds(0.1f); // Buffer
        if (isEnter && currentEnterCoroutine != null)
        {
            yield return currentEnterCoroutine;
        }
        else if (!isEnter && currentExitCoroutine != null)
        {
            yield return currentExitCoroutine;
        }
        else if (!isEnter)
        {
            // For instant exit, brief wait to cover fades
            yield return new WaitForSeconds(0.3f);
        }
        else
        {
            yield return null;
        }
    }
}


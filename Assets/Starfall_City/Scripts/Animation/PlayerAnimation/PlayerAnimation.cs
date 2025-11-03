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

    [Header("Sitting Animations")]
    public bool isSitting = false;
    private Coroutine getUpCoroutine;

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

        isSitting = false; // Initial state

        // Initialize turn tracking
        previousDesired = transform.forward;
        lastTurnTime = -turnCooldown; // Allow immediate turn
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

        if (isSitting)
        {
            animator.SetBool("isSitting", true);
            animator.SetBool("is_Walking", false); // Override walking during sit
        }
        else
        {
            animator.SetBool("isSitting", false);
            // Existing walking logic...
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

    public void TriggerGetUp()
    {
        if (getUpCoroutine != null) StopCoroutine(getUpCoroutine);
        getUpCoroutine = StartCoroutine(GetUpSequence());
    }

    private IEnumerator GetUpSequence()
    {
        animator.applyRootMotion = true;
        animator.SetTrigger("GetUp");

        // Brief wait for transition to start (1 frame ensures state updates)
        yield return new WaitForEndOfFrame();

        player3DMovement.IsInTransitionAnimation = true;

        // Poll for the state entry (in case of blend/transition time >1 frame)
        float maxWaitTime = 0.5f; // Timeout after 0.5s if state doesn't enter
        float elapsed = 0f;
        AnimatorStateInfo stateInfo;
        bool stateEntered = false;
        stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        while (elapsed < maxWaitTime)
        {
            if (stateInfo.IsName("Sit to Stand"))
            {
                stateEntered = true;
                break;
            }
            yield return null; // Wait one more frame
            elapsed += Time.deltaTime;
        }

        if (!stateEntered)
        {
            Debug.LogWarning("GetUpSequence: Animator did not enter 'Sit to Stand' state within timeout!");
            // Fallback: Proceed anyway to avoid hanging, but disable root motion early
            animator.applyRootMotion = false;
            player3DMovement.IsInTransitionAnimation = false;
            isSitting = false;
            yield break;
        }

        // Now that we're in the state, get its length
        float animLength = stateInfo.length;
        // Wait for the full length, minus a tiny buffer to apply the last delta before disabling
        yield return new WaitForSeconds(animLength - 0.05f);

        animator.applyRootMotion = false;
        player3DMovement.IsInTransitionAnimation = false;
        isSitting = false; // Re-enable locomotion

        // Snap to surface immediately after anim ends for visual correction
        if (player3DMovement != null)
        {
            player3DMovement.SnapToSurface(); // Forces Y-alignment before locomotion resumes
        }
    }

    public void TriggerSit(bool enableRootMotion = false)
    {
        isSitting = true;
        if (enableRootMotion)
        {
            animator.applyRootMotion = true;
            if (player3DMovement != null)
            {
                player3DMovement.IsInTransitionAnimation = true;
            }
        }
        // Optional: Set a trigger if your Animator uses one for sit entry
        // animator.SetTrigger("Sit"); // Uncomment if added to controller
    }

}


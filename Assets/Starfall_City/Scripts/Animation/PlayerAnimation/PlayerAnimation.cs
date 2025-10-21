using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using QTE;

public class PlayerAnimation : MonoBehaviour
{
    private Animator animator;
    private PlayerMovement playerMovement;
    private Player3DMovement player3DMovement;

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

        // Initialize turn tracking
        previousDesired = transform.forward;
        lastTurnTime = -turnCooldown; // Allow immediate turn
    }

    void Update()
    {
        if (QTEGameManager.IsQTEActive) return;
        CheckMovement();
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
}


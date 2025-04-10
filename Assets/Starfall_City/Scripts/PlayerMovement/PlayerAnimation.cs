using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class PlayerAnimation : MonoBehaviour
{
    private Animator animator;
    private PlayerMovement playerMovement;
    private Player3DMovement player3DMovement;

    private float speedThreshold = 0.1f; 
    private float smoothTime = 0.1f; 
    private float currentSpeed;


    void Start()
    {
        // Get the Animator component and PlayerMovement component
        animator = GetComponent<Animator>();
        playerMovement = GetComponent<PlayerMovement>();
        player3DMovement = GetComponent<Player3DMovement>();
    }

    void Update()
    {
        CheckMovement();
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
            // If the player is moving, set the running animation
            animator.SetBool("is_Running", true);
            animator.SetBool("is_Standing", false);
        }
        else
        {
            // If the player is not moving, set the standing animation
            animator.SetBool("is_Running", false);
            animator.SetTrigger("Stopping");
        }
    }
}


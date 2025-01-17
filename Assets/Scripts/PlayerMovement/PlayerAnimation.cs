using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class PlayerAnimation : MonoBehaviour
{
    private Animator animator;
    private PlayerMovement playerMovement;
    private bool isStopping = false;


    void Start()
    {
        // Get the Animator component and PlayerMovement component
        animator = GetComponent<Animator>();
        playerMovement = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        CheckMovement();
    }

    void CheckMovement()
    {
        // Check if the player is moving
        Vector3 velocity = playerMovement.GetVelocity(); // Replace with your method to get velocity
        bool isMoving = velocity.magnitude > 0.1f;

        if (isMoving)
        {
            // If the player is moving, set the running animation
            animator.SetBool("is_Running", true);
            animator.SetBool("is_Standing", false);
            Debug.Log("Running");
        }
        else
        {
            // If the player is not moving, set the standing animation
            animator.SetBool("is_Running", false);
            animator.SetTrigger("Stopping");
            Debug.Log("Standing");
        }
    }
}


using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class PlayerAnimation : MonoBehaviour
{
    private Animator animator;
    private PlayerMovement playerMovement;
    private NavMeshAgent playerAgent;
    private bool isStopping = false;


    void Start()
    {
        // Get the Animator component and PlayerMovement component
        animator = GetComponent<Animator>();
        playerMovement = GetComponent<PlayerMovement>();
        playerAgent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        CheckMovement();
    }

    void CheckMovement()
    {
        // Check if the player is moving
        if (playerAgent.hasPath && !playerAgent.pathPending && playerAgent.remainingDistance > playerAgent.stoppingDistance)
        {
            animator.SetBool("is_Standing", false);
            animator.SetBool("is_Stopping", false);
        }
        else
        {
            // If the player is not moving, trigger stopping animation
            if (!isStopping)
            {
                isStopping = true; // Start stopping
                StartCoroutine(TransitionToStanding());
            }
        }
    }

    private IEnumerator TransitionToStanding()
    {
        animator.SetBool("is_Stopping", true);
        while (animator.GetCurrentAnimatorStateInfo(0).IsName("Stopping")) 
        {
            yield return null;
        }
        animator.SetBool("is_Standing", true);
        animator.SetBool("is_Stopping", false);
        isStopping = false;
    }
}


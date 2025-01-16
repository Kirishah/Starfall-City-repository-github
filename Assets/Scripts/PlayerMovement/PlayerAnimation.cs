using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class PlayerAnimation : MonoBehaviour
{
    private Animator animator;
    private PlayerMovement playerMovement;


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
        if (playerMovement.player.remainingDistance > playerMovement.player.stoppingDistance)
        {
            animator.SetBool("is_Standing", false);
            animator.SetBool("is_Stopping", false);
        }
        else
        {
            animator.SetBool("is_Stopping", true);
            StartCoroutine(TransitionToStanding());
        }
    }

    private IEnumerator TransitionToStanding()
    {
        while (animator.GetCurrentAnimatorStateInfo(0).IsName("Stopping")) 
        {
            yield return null;
        }
        animator.SetBool("is_Standing", true);
        animator.SetBool("is_Stopping", false);
    }
}


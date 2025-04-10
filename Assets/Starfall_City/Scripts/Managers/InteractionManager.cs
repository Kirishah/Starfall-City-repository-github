using TMPro;
using UnityEngine;

public class InteractionManager : MonoBehaviour
{
    [Header("Settings")]
    private float interactionRadius = 1f; 
    public LayerMask interactableLayer;

    private Interactable currentInteractable;

    void Update()
    {
        DetectInteractable();
    }

    void DetectInteractable()
    {
        // Detect all interactables in a sphere around the player
        Collider[] interactables = Physics.OverlapSphere(
            transform.position,
            interactionRadius,
            interactableLayer
        );

        Interactable closestInteractable = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider col in interactables)
        {
            Interactable interactable = col.GetComponent<Interactable>();
            if (interactable != null)
            {
                // Calculate distance to player
                float distance = Vector3.Distance(transform.position, col.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestInteractable = interactable;
                }
            }
        }

        // Update current interactable
        if (currentInteractable != null && currentInteractable != closestInteractable)
        {
            currentInteractable.HidePrompt();
        }
        currentInteractable = closestInteractable;

        if (currentInteractable != null)
        {
            // Show interaction prompt
            currentInteractable.ShowPrompt();

            // Handle interaction input
            if (Input.GetKeyDown(KeyCode.E))
            {
                currentInteractable.Interact();
            }
        }
    }
}

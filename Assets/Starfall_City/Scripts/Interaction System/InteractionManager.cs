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

    /* private float interactionRange = 1000f;
    private Interactable currentInteractable;

    void Update()
    {
        DetectInteractable();
        if ((Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(1)) && currentInteractable != null)
        {
            Debug.Log("Interaction called");
            currentInteractable.Interact();
        }
    }

    void DetectInteractable()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, interactionRange);
        currentInteractable = null;

        Debug.Log("Detected " + hitColliders.Length + " colliders.");

        foreach (var hitCollider in hitColliders)
        {
            Debug.Log("Detected collider: " + hitCollider.gameObject.name);
            Interactable interactable = hitCollider.GetComponent<Interactable>();
            if (interactable != null)
            {
                currentInteractable = interactable;
                Debug.Log("Interacting with " + currentInteractable);
                break; // только первый найденный объект
            }
        }
        if (currentInteractable != null)
        {
            Debug.Log("Current interactable set to: " + currentInteractable.gameObject.name);
        }
        else
        {
            Debug.Log("No interactable found.");
        }
    }
    */
}

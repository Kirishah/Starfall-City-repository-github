using UnityEngine;

public class InteractionManager : MonoBehaviour
{
    private float interactionRange = 1000f;
    private Interactable currentInteractable;

    void Update()
    {
        DetectInteractable();
        if ((Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0)) && currentInteractable != null)
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
}

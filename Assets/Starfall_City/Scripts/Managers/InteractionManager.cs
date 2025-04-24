using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class InteractionManager : MonoBehaviour
{
    [Header("Settings")]
    public float interactionRadius = 1f; 
    public LayerMask interactableLayer;

    private List<Interactable> _proximityInteractables = new();
    private Interactable _closestInteractable;
    private Interactable _hoveredInteractable;

    void Update()
    {
        DetectProximityInteractables();
        DetectHoverInteractable();
        HandleEKeyInteraction();
    }

    void DetectProximityInteractables()
    {
        // Clear previous proximity states
        foreach (var i in _proximityInteractables) i.SetProximity(false);

        // Find new proximity interactables
        var colliders = Physics.OverlapSphere(transform.position, interactionRadius, interactableLayer);
        _proximityInteractables = colliders
            .Select(c => c.GetComponent<Interactable>())
            .Where(i => i != null)
            .ToList();

        // Update proximity states and find closest
        _closestInteractable = null;
        var closestDistance = Mathf.Infinity;
        foreach (var interactable in _proximityInteractables)
        {
            interactable.SetProximity(true);

            var distance = Vector3.Distance(transform.position, interactable.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                _closestInteractable = interactable;
            }
        }
    }

    void DetectHoverInteractable()
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out var hit, Mathf.Infinity, interactableLayer))
        {
            if (_hoveredInteractable != null)
            {
                _hoveredInteractable.SetHovered(false);
                _hoveredInteractable = null;
            }
            return;
        }

        var newHover = hit.collider.GetComponent<Interactable>();
        if (newHover == _hoveredInteractable) return;

        if (_hoveredInteractable != null)
            _hoveredInteractable.SetHovered(false);

        _hoveredInteractable = newHover;
        _hoveredInteractable.SetHovered(true);
    }

    void HandleEKeyInteraction()
    {
        if (Input.GetKeyDown(KeyCode.E) && _closestInteractable != null)
        {
            _closestInteractable.Interact();

            string identifier = _closestInteractable.GetIdentifier(); 
            if (!string.IsNullOrEmpty(identifier))
            {
                QuestManager.Instance.HandleObjectiveUpdate(ObjectiveType.Interaction, identifier);
            }
        }
    }

}

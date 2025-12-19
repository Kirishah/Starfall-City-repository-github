using System.Collections.Generic;
using UnityEngine;
using QTE;

public class InteractionManager : MonoBehaviour, QTEGameManager.IRPGComponent
{
    [Header("Settings")]
    [SerializeField] private float interactionRadius = 1f;
    [SerializeField] private LayerMask interactableLayer;

    // Pre-allocated buffer – size based on reasonable max interactables in radius (adjust if needed)
    private readonly Collider[] _overlapBuffer = new Collider[20];
    private readonly List<Interactable> _proximityInteractables = new();


    private Interactable _closestInteractable;
    private Interactable _hoveredInteractable;


    void Update()
    {
        if (QTEGameManager.IsQTEActive || !GameSystems.IsReady) return;

        DetectProximityInteractables();
        DetectHoverInteractable();
        HandleEKeyInteraction();
    }

    private void DetectProximityInteractables()
    {
        // Clear previous proximity state
        foreach (var i in _proximityInteractables)
        {
            i.SetProximity(false);
        }
        _proximityInteractables.Clear();

        // NonAlloc overlap — zero garbage!
        int hitCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            interactionRadius,
            _overlapBuffer,
            interactableLayer
        );

        _closestInteractable = null;
        float closestDistance = Mathf.Infinity;

        for (int i = 0; i < hitCount; i++)
        {
            if (_overlapBuffer[i].TryGetComponent<Interactable>(out var interactable))
            {
                _proximityInteractables.Add(interactable);
                interactable.SetProximity(true);

                float distance = Vector3.Distance(transform.position, interactable.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    _closestInteractable = interactable;
                }
            }
        }
    }

    private void DetectHoverInteractable()
    {
        if (Camera.main == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        // Single raycast is usually safe (no array allocation), but we can make it fully predictable
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, interactableLayer))
        {
            if (hit.collider.TryGetComponent<Interactable>(out var newHover))
            {
                if (newHover == _hoveredInteractable) return;

                if (_hoveredInteractable != null)
                    _hoveredInteractable.SetHovered(false);

                _hoveredInteractable = newHover;
                _hoveredInteractable.SetHovered(true);
                return;
            }
        }

        // No hit or not interactable
        if (_hoveredInteractable != null)
        {
            _hoveredInteractable.SetHovered(false);
            _hoveredInteractable = null;
        }
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

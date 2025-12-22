using System.Collections.Generic;
using PlayerInputActions;
using QTE;
using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionManager : MonoBehaviour, QTEGameManager.IRPGComponent
{
    [Header("Settings")]
    [SerializeField] private float interactionRadius = 1f;
    [SerializeField] private LayerMask interactableLayer;

    // Pre-allocated buffer – size based on reasonable max interactables in radius (adjust if needed)
    private readonly Collider[] _overlapBuffer = new Collider[20];
    private readonly RaycastHit[] _raycastBuffer = new RaycastHit[1]; // Size 1 for single hit

    private readonly List<Interactable> _proximityInteractables = new();


    private Interactable _closestInteractable;
    private Interactable _hoveredInteractable;

    private PlayerControls _controls;
    private PlayerControls.InteractionActions _interactionActions;

    private void Awake()
    {
        _controls = new PlayerControls();
        _interactionActions = _controls.Interaction;
    }

    private void OnEnable()
    {
        _interactionActions.Enable();
        _interactionActions.Key.performed += OnInteractKeyPressed;
    }

    private void OnDisable()
    {
        _interactionActions.Key.performed -= OnInteractKeyPressed;
        _interactionActions.Disable();
    }

    void Update()
    {
        if (QTEGameManager.IsQTEActive || !GameSystems.IsReady) return;

        DetectProximityInteractables();
        DetectHoverInteractable();
    }

    private void OnInteractKeyPressed(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
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

        // NonAlloc overlap 
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

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        // NonAlloc single-hit raycast 
        int hitCount = Physics.RaycastNonAlloc(ray, _raycastBuffer, Mathf.Infinity, interactableLayer);

        if (hitCount > 0)
        {
            if (_raycastBuffer[0].collider.TryGetComponent<Interactable>(out var newHover))
            {
                if (newHover == _hoveredInteractable) return;

                _hoveredInteractable?.SetHovered(false);

                _hoveredInteractable = newHover;
                _hoveredInteractable?.SetHovered(true);
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
        if (_closestInteractable != null)
        {
            _closestInteractable.Interact();

            string identifier = _closestInteractable.GetIdentifier(); 
            if (!string.IsNullOrEmpty(identifier))
            {
                QuestManager.Instance.HandleObjectiveUpdate(ObjectiveType.Interaction, identifier);
            }
        }
    }

    private void OnDestroy()
    {
        _interactionActions.Key.performed -= OnInteractKeyPressed;
        _interactionActions.Disable();
    }
}

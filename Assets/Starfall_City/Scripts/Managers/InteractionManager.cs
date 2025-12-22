using System.Collections.Generic;
using PlayerInputActions;
using QTE;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Interaction
{
    public class InteractionManager : MonoBehaviour, QTEGameManager.IRPGComponent
    {
        [Header("Settings")]
        [SerializeField] private float _interactionRadius = 1f;
        [SerializeField] private LayerMask _interactableLayer;

        // Pre-allocated buffer – size based on reasonable max interactables in radius (adjust if needed)
        private readonly Collider[] _overlapBuffer = new Collider[20];
        private readonly RaycastHit[] _raycastBuffer = new RaycastHit[1]; // Size 1 for single hit

        private readonly List<InteractionPresenter> _proximityPresenters = new();

        private InteractionPresenter _closestPresenter;
        private InteractionPresenter _hoveredPresenter;

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

        private void OnInteractKeyPressed(UnityEngine.InputSystem.InputAction.CallbackContext context) => _closestPresenter?.Interact();

        private void DetectProximityInteractables()
        {
            // Clear previous proximity state
            foreach (var i in _proximityPresenters)
            {
                i.SetProximity(false);
            }
            _proximityPresenters.Clear();

            // NonAlloc overlap 
            int hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                _interactionRadius,
                _overlapBuffer,
                _interactableLayer
            );

            _closestPresenter = null;
            float closestDistance = Mathf.Infinity;

            for (int i = 0; i < hitCount; i++)
            {
                if (_overlapBuffer[i].TryGetComponent<InteractionPresenter>(out var presenter))
                {
                    _proximityPresenters.Add(presenter);
                    presenter.SetProximity(true);

                    float distance = Vector3.Distance(transform.position, presenter.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        _closestPresenter = presenter;
                    }
                }
            }
        }

        private void DetectHoverInteractable()
        {
            if (Camera.main == null) return;

            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

            // NonAlloc single-hit raycast 
            int hitCount = Physics.RaycastNonAlloc(ray, _raycastBuffer, Mathf.Infinity, _interactableLayer);

            if (hitCount > 0)
            {
                if (_raycastBuffer[0].collider.TryGetComponent<InteractionPresenter>(out var newHover))
                {
                    if (newHover == _hoveredPresenter) return;

                    _hoveredPresenter?.SetHovered(false);

                    _hoveredPresenter = newHover;
                    _hoveredPresenter?.SetHovered(true);
                    return;
                }
            }

            // No hit or not interactable
            if (_hoveredPresenter != null)
            {
                _hoveredPresenter.SetHovered(false);
                _hoveredPresenter = null;
            }
        }

        private void OnDestroy()
        {
            _interactionActions.Key.performed -= OnInteractKeyPressed;
            _interactionActions.Disable();
        }
    }
}

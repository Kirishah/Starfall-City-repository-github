using Interaction;
using PlayerInputActions;
using QTE;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour, QTEGameManager.IRPGComponent
{
    [Header("References")]
    public NavMeshAgent player;
    private InteractionPresenter _currentTargetInteractable;

    [Header("Movement Settings")]
    private Vector3 _lastPosition;
    private Vector3 _velocity;
    private const float _defaultStoppingDistance = 0.1f;

    [Header("Destination Indicator")]
    public GameObject destinationIndicatorPrefab;
    private GameObject _destinationIndicator;

    [Header("Interaction")]
    [SerializeField] private float _interactionRange = 1f;

    [Header("Controls")]
    public bool controlsEnabled = true;

    private readonly RaycastHit[] _clickRaycastBuffer = new RaycastHit[1];

    private PlayerControls _controls;
    private PlayerControls.PlayerMovementActions _playerMovementActions;

    private void Awake()
    {
        _controls = new PlayerControls();
        _playerMovementActions = _controls.PlayerMovement;
    }

    private void OnEnable()
    {
        _controls.Enable();
        _playerMovementActions.Click.performed += OnPointAndClick;
    }

    private void OnDisable()
    {
        _playerMovementActions.Click.performed -= OnPointAndClick;
        _controls.Disable();
    }

    private void Start()
    {
       _lastPosition = transform.position;
       player.stoppingDistance = _defaultStoppingDistance;
       player.angularSpeed = 360f; // Increased for smoother, faster turns
       player.acceleration = 20f; // Increased for quicker speed changes

       DialogueManager_UIToolkit.OnDialogueStarted += PauseControls;
       DialogueManager_UIToolkit.OnDialogueEnded += ResumeControls;
    }

    private void OnDestroy()
    {
        DialogueManager_UIToolkit.OnDialogueStarted -= PauseControls;
        DialogueManager_UIToolkit.OnDialogueEnded -= ResumeControls;

        _playerMovementActions.Click.performed -= OnPointAndClick;
        _controls.Disable();
    }

    public void PauseControls() => controlsEnabled = false;
    public void ResumeControls() => controlsEnabled = true;

    void Update()
    {
        if (QTEGameManager.IsQTEActive || !controlsEnabled)
        {
            CalculateVelocity(); // Still update velocity for animations
            return;
        }

        if (player.enabled)
        {
            CheckIfReachedDestination();
        }
        CalculateVelocity();
    }

    private void OnPointAndClick(InputAction.CallbackContext context)
    {
        // Ignore click if:
        // - NavMeshAgent is disabled (means we're in direct WASD mode via Player3DMovement)
        // - Controls are paused
        if (!player.enabled || !controlsEnabled)
            return;

        HandlePointAndClickInput();
    }

    void HandlePointAndClickInput()
    {
        _currentTargetInteractable = null;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);

        // NonAlloc 
        int hitCount = Physics.RaycastNonAlloc(ray, _clickRaycastBuffer);

        if (hitCount > 0)
        {
            RaycastHit hit = _clickRaycastBuffer[0];

            // Sample NavMesh to ensure destination is valid
            if (NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, 2f, NavMesh.AllAreas))
            {
                Vector3 validDestination = navHit.position;

                if (hit.collider.TryGetComponent<InteractionPresenter>(out var interactable) &&
                    interactable.gameObject.activeInHierarchy)
                {
                    SetInteractableTarget(interactable, validDestination);
                }
                else
                {
                    SetRegularMovement(validDestination);
                }

                UpdateDestinationIndicator(validDestination);
            }
        }
    }

    void SetInteractableTarget(InteractionPresenter interactable, Vector3 targetPosition)
    {
        _currentTargetInteractable = interactable;
        player.stoppingDistance = _interactionRange;
        player.SetDestination(targetPosition);
    }

    void SetRegularMovement(Vector3 targetPosition)
    {
        _currentTargetInteractable = null;
        player.stoppingDistance = _defaultStoppingDistance;
        player.SetDestination(targetPosition);
    }

    void UpdateDestinationIndicator(Vector3 position)
    {
        if (_destinationIndicator == null || !_destinationIndicator) // Recreate if missing/destroyed
        {
            _destinationIndicator = Instantiate(destinationIndicatorPrefab, position, Quaternion.identity);
        }
        else
        {
            _destinationIndicator.SetActive(true);
            _destinationIndicator.transform.position = position;
        }
    }

    void CheckIfReachedDestination()
    {
        if (!player.enabled) return;

        if (player.hasPath && !player.pathPending &&
            player.remainingDistance <= player.stoppingDistance + 0.05f) // small tolerance
        {
            ClearDestinationIndicator();
            TryInteractWithTarget();
        }

        // Clear target if it becomes invalid
        if (_currentTargetInteractable?.gameObject.activeInHierarchy == false)
        {
            _currentTargetInteractable = null;
            player.ResetPath();
            ClearDestinationIndicator();
        }
    }

    void TryInteractWithTarget()
    {
        if (_currentTargetInteractable == null) return;

        if (_currentTargetInteractable.gameObject.activeInHierarchy)
        {
            _currentTargetInteractable.Interact();
        }
        _currentTargetInteractable = null;
    }

    public void ClearDestinationIndicator()
    {
        if (_destinationIndicator != null)
        {
            // If somehow destroyed, clear reference
            if (!_destinationIndicator)
            {
                _destinationIndicator = null;
            }
            else
            {
                _destinationIndicator.SetActive(false);
            }
        }
    }

    protected void CalculateVelocity()
    {
        // Calculate the velocity based on the change in position over time
        if (player.enabled)
        {
            _velocity = (player.velocity); // Use NavMeshAgent's velocity
        }
        else
        {
            _velocity = (transform.position - _lastPosition) / Time.deltaTime;
        }
        // Update last position for the next frame
        _lastPosition = transform.position;
    }

    public Vector3 GetVelocity() => _velocity;

    public Vector3 GetDesiredDirection() => player.enabled && player.desiredVelocity.magnitude > 0.01f ? player.desiredVelocity.normalized : Vector3.zero;
}

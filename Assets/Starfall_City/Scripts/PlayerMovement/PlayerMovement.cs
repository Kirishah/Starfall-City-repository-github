using UnityEngine;
using UnityEngine.AI;
using QTE;

public class PlayerMovement : MonoBehaviour, QTEGameManager.IRPGComponent
{
    [Header("References")]
    public NavMeshAgent player;
    private Interactable _currentTargetInteractable;

    [Header("Movement Settings")]
    private Vector3 lastPosition;
    private Vector3 velocity;
    private const float DefaultStoppingDistance = 0.1f;

    [Header("Destination Indicator")]
    public GameObject destinationIndicatorPrefab; 
    private GameObject destinationIndicator;

    [Header("Interaction")]
    [SerializeField] private float interactionRange = 1.5f;

    [Header("Controls")]
    public bool controlsEnabled = true;

    private void Start()
    {
       lastPosition = transform.position;
       player.stoppingDistance = DefaultStoppingDistance;
       player.angularSpeed = 360f; // Increased for smoother, faster turns
       player.acceleration = 20f; // Increased for quicker speed changes

       DialogueManager_UIToolkit.OnDialogueStarted += PauseControls;
       DialogueManager_UIToolkit.OnDialogueEnded += ResumeControls;
    }

    private void OnDestroy() 
    {
        DialogueManager_UIToolkit.OnDialogueStarted -= PauseControls;
        DialogueManager_UIToolkit.OnDialogueEnded -= ResumeControls;
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
            if (Input.GetMouseButtonDown(1))
            {
                HandleMovementInput();
            }
            CheckIfReachedDestination();
        }
        CalculateVelocity();
    }

    void HandleMovementInput()
    {
        // Early exit if NavMeshAgent isn't active
        if (!player.enabled) return;
        // Clear previous interaction target immediately
        _currentTargetInteractable = null;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit)) return;

        // Check if we clicked an interactable
        Interactable interactable = hit.collider.GetComponent<Interactable>();
        if (interactable != null)
        {
            SetInteractableTarget(interactable, hit.point);
        }
        else
        {
            SetRegularMovement(hit.point);
        }

        UpdateDestinationIndicator(hit.point);
    }

    void SetInteractableTarget(Interactable interactable, Vector3 targetPosition)
    {
        _currentTargetInteractable = interactable;
        player.stoppingDistance = interactionRange;
        player.SetDestination(targetPosition);
    }

    void SetRegularMovement(Vector3 targetPosition)
    {
        _currentTargetInteractable = null;
        player.stoppingDistance = DefaultStoppingDistance;
        player.SetDestination(targetPosition);
    }

    void UpdateDestinationIndicator(Vector3 position)
    {
        if (destinationIndicator == null)
        {
            destinationIndicator = Instantiate(destinationIndicatorPrefab, position, Quaternion.identity);
        }
        else
        {
            destinationIndicator.SetActive(true);
            destinationIndicator.transform.position = position;
        }
    }

    void CheckIfReachedDestination()
    {
        if (!player.enabled) return;

        if (player.hasPath && !player.pathPending &&
            player.remainingDistance <= player.stoppingDistance)
        {
            ClearDestinationIndicator();
            TryInteractWithTarget();
        }

        // Clear target if it becomes invalid
        if (_currentTargetInteractable != null &&
            !_currentTargetInteractable.gameObject.activeInHierarchy)
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
        if (destinationIndicator != null)
        {
            destinationIndicator.SetActive(false);
        }
    }

    protected void CalculateVelocity()
    {
        // Calculate the velocity based on the change in position over time
        if (player.enabled)
        {
            velocity = (player.velocity); // Use NavMeshAgent's velocity
        }
        else 
        {
            velocity = (transform.position - lastPosition) / Time.deltaTime;
        }
        // Update last position for the next frame
        lastPosition = transform.position;
    }

    public Vector3 GetVelocity() => velocity;

    public Vector3 GetDesiredDirection()
    {
        return player.enabled && player.desiredVelocity.magnitude > 0.01f ? player.desiredVelocity.normalized : Vector3.zero;
    }
}

    


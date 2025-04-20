using UnityEngine;
using System.Collections;
using UnityEngine.AI;
using Invector.vCharacterController;
using UnityEngine.EventSystems;

public class PlayerMovement : MonoBehaviour
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

    private void Start()
    {
       lastPosition = transform.position;
       player.stoppingDistance = DefaultStoppingDistance; 
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(1)) 
        {
            HandleMovementInput();
        }
        CheckIfReachedDestination();
        CalculateVelocity();
    }

    void HandleMovementInput()
    {
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
            destinationIndicator.transform.position = position;
        }
    }

    void CheckIfReachedDestination()
    {

        if (!player.pathPending &&
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

        // Verify target is still valid and in range
        if (_currentTargetInteractable.gameObject.activeInHierarchy &&
            Vector3.Distance(transform.position, _currentTargetInteractable.transform.position) <= interactionRange)
        {
            _currentTargetInteractable.Interact();
        }
        _currentTargetInteractable = null;
    }

    void ClearDestinationIndicator()
    {
        if (destinationIndicator != null)
        {
            Destroy(destinationIndicator);
            destinationIndicator = null;
        }
    }

    protected void CalculateVelocity()
    {
        // Calculate the velocity based on the change in position over time
        velocity = (transform.position - lastPosition) / Time.deltaTime;

        // Update last position for the next frame
        lastPosition = transform.position;
    }

    public Vector3 GetVelocity() => velocity;
}

    


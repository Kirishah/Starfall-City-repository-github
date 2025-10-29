using UnityEngine;
using UnityEngine.AI;

public class NPCController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NavMeshAgent agent; // Drag your NavMeshAgent here or auto-assign in Start

    [Header("Movement Settings")]
    [SerializeField] private float stoppingDistance = 1f; // Default stopping distance
    [SerializeField] private float angularSpeed = 360f; // Rotation speed
    [SerializeField] private float acceleration = 20f; // Acceleration for smoother starts/stops

    [Header("Animation")]
    [SerializeField] private Animator animator; // Drag your Animator here
    [SerializeField] private string walkingParam = "isWalking"; // Animator bool parameter name for walking

    private Vector3 lastPosition;
    private Vector3 velocity;

    void Start()
    {
        // Auto-assign if not set
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponent<Animator>();

        if (agent == null)
        {
            Debug.LogError($"NavMeshAgent missing on {gameObject.name}!");
            return;
        }

        // Setup agent
        agent.stoppingDistance = stoppingDistance;
        agent.angularSpeed = angularSpeed;
        agent.acceleration = acceleration;

        lastPosition = transform.position;
    }

    void Update()
    {
        CalculateVelocity();
        UpdateAnimation();
    }

    // Sets the NPC to move to a destination point.
    public void SetDestination(Vector3 destination)
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.SetDestination(destination);
            Debug.Log($"NPC {gameObject.name} set to move to {destination}");
        }
        else
        {
            Debug.LogWarning($"NPC {gameObject.name} cannot set destination: Not on NavMesh!");
        }
    }

    // Resets the path and stops movement.
    public void StopMovement()
    {
        if (agent != null)
        {
            agent.ResetPath();
        }
    }

    // Checks if the NPC has reached its destination.
    public bool HasReachedDestination()
    {
        return agent != null && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance;
    }

    // Gets the current velocity for external use (e.g., animation blending).
    public Vector3 GetVelocity() => velocity;

    private void CalculateVelocity()
    {
        if (agent != null)
        {
            velocity = agent.velocity;
        }
        else
        {
            velocity = (transform.position - lastPosition) / Time.deltaTime;
        }
        lastPosition = transform.position;
    }

    private void UpdateAnimation()
    {
        if (animator == null) return;

        bool isMoving = velocity.magnitude > 0.1f;
        animator.SetBool(walkingParam, isMoving);
    }
}

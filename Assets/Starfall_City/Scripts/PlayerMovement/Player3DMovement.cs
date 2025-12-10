using UnityEngine;
using UnityEngine.AI;
using QTE;

public class Player3DMovement : MonoBehaviour, QTEGameManager.IRPGComponent
{
    public bool IsInTransitionAnimation { get; set; } = false;

    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float turnSpeed = 20f;

    [Header("NavMesh Validation")]
    [SerializeField] private float navMeshSampleDistance = 0.5f; // adjust based on your CharacterController.height / 2 + buffer

    [Header("Surface Snapping")]
    [SerializeField] private bool handleGravity = true; // Toggle off if floors are perfectly flat/no jumps
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private LayerMask groundLayerMask = 6; // Set to your floor/ground layers (default: all)
    [SerializeField] private float surfaceSnapTolerance = 0.01f; // Max Y drift before snapping (prevents jitter)
    [SerializeField] private float navMeshSnapDistance = 2f; // Max distance for NavMesh sample in snapping (larger than validation)
    [SerializeField] private bool preferNavMeshForSnap = true; // Prioritize NavMesh over raycast for consistency
    [SerializeField] private bool forceSnapWhenGrounded = true; // Always snap if isGrounded (eliminates drift)

    [Header("References")]
    private CharacterController controller;
    private PlayerMovement agent;
    private NavMeshAgent navAgent;
    private Animator animator;

    [Header("Controls")]
    public bool controlsEnabled = true;

    private Vector3 moveDirection;
    private Vector3 desiredDirection;
    private Vector3 verticalVelocity; // For gravity

    // Cached offsets
    private float bottomOffset;
    private float topOffset;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        agent = GetComponent<PlayerMovement>();
        navAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (controller == null)
        {
            Debug.LogError("CharacterController missing on " + gameObject.name);
            return;
        }

        // Calculate offsets once
        bottomOffset = controller.center.y - (controller.height / 2f); // Now -1.0f with center.y=0
        topOffset = controller.center.y + (controller.height / 2f);   // Now +1.0f
        Debug.Log($"Capsule offsets - Bottom: {bottomOffset}, Top: {topOffset}. Expected pivot Y on surface=0: {-bottomOffset}"); // Logs ~1.0

        // Sync NavMeshAgent to use capsule center as pivot
        navAgent.baseOffset = -bottomOffset; // 1.0f - agent will set position.y = surface + 1.0
        Debug.Log($"Set NavMeshAgent.baseOffset to {-bottomOffset} for center alignment");

        if (navAgent.baseOffset != 0)
        {
            navAgent.baseOffset = 0f;
            Debug.Log("Reset NavMeshAgent.baseOffset to 0 for feet alignment.");
        }

        // Initial snap on Start
        SnapToSurface();

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
        // Modified: Skip snap if root motion is active during transition
        if (IsInTransitionAnimation && animator != null && animator.applyRootMotion)
        {
            // Let root motion handle positioning - no snap here
        }
        else if (IsInTransitionAnimation)
        {
            SnapToSurface(); // Only snap every frame for non-root-motion transitions
        }

        if (QTEGameManager.IsQTEActive || !controlsEnabled) return;

        GatherInput();
        if (moveDirection.magnitude >= 0.1f)
        {
            // Disable NavMeshAgent when using WASD
            if (navAgent.enabled)
            {
                Debug.Log("Switching to WASD movement");
                float preSwitchY = transform.position.y;
                navAgent.ResetPath();
                navAgent.enabled = false;
                agent.ClearDestinationIndicator();
                SnapToSurface();
                Debug.Log($"Post-switch to CC - Y: {transform.position.y} (was {preSwitchY}, target bottom Y: {transform.position.y + bottomOffset})");
            }
            Look();
            Move();
        }
        else if (!navAgent.enabled)
        {
            // Re-enable NavMeshAgent when stopping WASD
            Debug.Log("Switching to Point-and-Click movement");
            float preSwitchY = transform.position.y;
            SnapToSurface(); // Align before re-enabling (though agent will project)
            navAgent.enabled = true;
            verticalVelocity.y = 0f; // Reset vertical on switch
        }
    }

    

    private void GatherInput()
    {
        moveDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;
    }
    private void Look()
    {
        Vector3 isoDirection = moveDirection.ToIso();
        Quaternion targetRotation = Quaternion.LookRotation(isoDirection.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            turnSpeed * Time.deltaTime
        );
    }

    private void Move()
    {
        Vector3 horizontalMove = transform.forward * moveSpeed * Time.deltaTime;
        horizontalMove.y = 0f; // Ensure no accidental Y from forward

        // Vertical (gravity) if enabled
        Vector3 verticalMove = Vector3.zero;
        if (handleGravity && !IsInTransitionAnimation)
        {
            if (controller.isGrounded && verticalVelocity.y < 0)
            {
                verticalVelocity.y = -1f; // Small downward nudge to maintain contact (prevents hover)
            }
            verticalVelocity.y += gravity * Time.deltaTime;
            verticalMove.y = verticalVelocity.y * Time.deltaTime;
        }

        // Combined move
        Vector3 totalMove = horizontalMove + verticalMove;

        // Сохранение начальной позиции для проверки
        Vector3 initialPosition = transform.position;
        Vector3 proposedPosition = initialPosition + horizontalMove;

        // Project to capsule bottom for validation
        Vector3 bottomProposed = proposedPosition + new Vector3(0, bottomOffset, 0);

        // Чек действительна ли позиция цели на NavMesh
        if (IsPositionValid(bottomProposed))
        {
            controller.Move(totalMove);
        }
        else
        {
            Debug.Log("Blocked movement beyond NavMesh boundaries at proposed bottom: " + bottomProposed);
            // Still apply vertical (allow falling/sliding down edges)
            controller.Move(verticalMove);
        }

        // Snap Y after move
        SnapToSurface();
    }

    private bool IsPositionValid(Vector3 bottomTargetPosition)
    {
        NavMeshHit hit;
        bool isValid = NavMesh.SamplePosition(bottomTargetPosition, out hit, navMeshSampleDistance, NavMesh.AllAreas);

        // Temporary debug (remove after testing)
        if (!isValid && Time.frameCount % 60 == 0)
        {
            float distToSurface = Vector3.Distance(bottomTargetPosition, hit.position);
            Debug.Log($"Validation failed at bottom {bottomTargetPosition}. Nearest: {hit.position}, Dist: {distToSurface}, MaxAllowed: {navMeshSampleDistance}");
        }

        return isValid;
    }

    public void SnapToSurface()
    {
        if (controller == null) return;

        float currentBottomY = transform.position.y + bottomOffset; // Now same as position.y
        float targetSurfaceY = currentBottomY; // Default: no change
        bool snapped = false;

        // Prefer NavMesh sample for consistency with agent
        if (preferNavMeshForSnap)
        {
            NavMeshHit navHit;
            Vector3 samplePos = transform.position + new Vector3(0, bottomOffset, 0); // Sample at current bottom (position.y)
            if (NavMesh.SamplePosition(samplePos, out navHit, navMeshSnapDistance, NavMesh.AllAreas))
            {
                targetSurfaceY = navHit.position.y;
                snapped = true;
            }
        }

        // Fallback to raycast if no NavMesh hit or disabled
        if (!snapped)
        {
            Vector3 rayStart = transform.position + new Vector3(0, topOffset, 0);
            Vector3 rayDirection = Vector3.down;
            float rayDistance = controller.height + surfaceSnapTolerance + Mathf.Abs(bottomOffset); // ~2.1f now

            if (Physics.Raycast(rayStart, rayDirection, out RaycastHit groundHit, rayDistance, groundLayerMask))
            {
                targetSurfaceY = groundHit.point.y + controller.skinWidth;
                snapped = true;
            }
            else
            {
                Debug.LogWarning("No ground hit for snap - applying extra gravity");
                if (handleGravity) verticalVelocity.y += gravity * Time.deltaTime * 1.5f;
                return;
            }
        }

        // If snapped, calculate and apply (pivot Y = surface Y)
        if (snapped)
        {
            float desiredPivotY = targetSurfaceY - bottomOffset; // = targetSurfaceY (0.035)

            // Check drift
            float yDrift = Mathf.Abs(transform.position.y - desiredPivotY);

            // Force snap if grounded (ignores tolerance for zero-drift reliability)
            bool shouldSnap = forceSnapWhenGrounded && controller.isGrounded || yDrift > surfaceSnapTolerance;

            if (IsInTransitionAnimation)
            {
                shouldSnap = true; // Force snap every frame during transitions, ignoring tolerance
            }

            if (shouldSnap)
            {
                float oldY = transform.position.y;
                transform.position = new Vector3(transform.position.x, desiredPivotY, transform.position.z);
                if (handleGravity) verticalVelocity.y = 0f;
            }
        }
    }

    public Vector3 GetDesiredDirection() => desiredDirection;
}

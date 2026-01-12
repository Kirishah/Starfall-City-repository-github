using DialogueSystem;
using PlayerInputActions;
using QTE;
using UnityEngine;
using UnityEngine.AI;

namespace Movement
{
    public class Player3DMovement : MonoBehaviour, QTEGameManager.IRPGComponent
    {
        public bool IsInTransitionAnimation { get; set; } = false;

        [SerializeField] private float _moveSpeed = 3f;
        [SerializeField] private float _turnSpeed = 20f;

        [Header("NavMesh Validation")]
        [SerializeField] private float _navMeshSampleDistance = 0.5f; // adjust based on your CharacterController.height / 2 + buffer

        [Header("Surface Snapping")]
        [SerializeField] private bool _handleGravity = true; // Toggle off if floors are perfectly flat/no jumps
        [SerializeField] private float _gravity = -9.81f;
        [SerializeField] private LayerMask _groundLayerMask = 6; // Set to your floor/ground layers (default: all)
        [SerializeField] private float _surfaceSnapTolerance = 0.01f; // Max Y drift before snapping (prevents jitter)
        [SerializeField] private float _navMeshSnapDistance = 2f; // Max distance for NavMesh sample in snapping (larger than validation)
        [SerializeField] private bool _preferNavMeshForSnap = true; // Prioritize NavMesh over raycast for consistency
        [SerializeField] private bool _forceSnapWhenGrounded = true; // Always snap if isGrounded (eliminates drift)

        [Header("References")]
        private CharacterController _controller;
        private PlayerMovement _agent;
        private NavMeshAgent _navAgent;
        private Animator _animator;

        [Header("Controls")]
        public bool controlsEnabled = true;
        private PlayerControls _controls;
        private PlayerControls.PlayerMovementActions _movementActions;

        private Vector3 _moveInput;
        private Vector3 _moveDirection;
        private Vector3 _verticalVelocity; // For gravity

        // Cached offsets
        private float _bottomOffset;
        private float _topOffset;

        private readonly RaycastHit[] _groundRaycastBuffer = new RaycastHit[1];

        private void Awake()
        {
            _controls = new PlayerControls();
            _movementActions = _controls.PlayerMovement;
        }

        private void OnEnable()
        {
            _controls.Enable();
            _movementActions.Movement.performed += OnMovementPerformed;
            _movementActions.Movement.canceled += OnMovementCanceled;
        }

        private void OnDisable()
        {
            _movementActions.Movement.performed -= OnMovementPerformed;
            _movementActions.Movement.canceled -= OnMovementCanceled;
            _controls.Disable();
        }

        private void OnMovementPerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx) => _moveInput = ctx.ReadValue<Vector2>();

        private void OnMovementCanceled(UnityEngine.InputSystem.InputAction.CallbackContext ctx) => _moveInput = Vector2.zero;

        void Start()
        {
            _controller = GetComponent<CharacterController>();
            _agent = GetComponent<PlayerMovement>();
            _navAgent = GetComponent<NavMeshAgent>();
            _animator = GetComponent<Animator>();

            if (_controller == null)
            {
                Debug.LogError("CharacterController missing on " + gameObject.name);
                return;
            }

            // Calculate offsets once
            _bottomOffset = _controller.center.y - (_controller.height / 2f); // Now -1.0f with center.y=0
            _topOffset = _controller.center.y + (_controller.height / 2f);   // Now +1.0f
            Debug.Log($"Capsule offsets - Bottom: {_bottomOffset}, Top: {_topOffset}. Expected pivot Y on surface=0: {-_bottomOffset}"); // Logs ~1.0

            // Sync NavMeshAgent to use capsule center as pivot
            _navAgent.baseOffset = -_bottomOffset; // 1.0f - agent will set position.y = surface + 1.0
            Debug.Log($"Set NavMeshAgent.baseOffset to {-_bottomOffset} for center alignment");

            if (_navAgent.baseOffset != 0)
            {
                _navAgent.baseOffset = 0f;
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

            _movementActions.Movement.performed -= OnMovementPerformed;
            _movementActions.Movement.canceled -= OnMovementCanceled;
            _controls.Disable();
        }

        public void PauseControls() => controlsEnabled = false;
        public void ResumeControls() => controlsEnabled = true;

        void Update()
        {
            // Skip snapping during root-motion transitions
            if (!(IsInTransitionAnimation && _animator.applyRootMotion))
            {
                if (IsInTransitionAnimation || !_controller.isGrounded)
                    SnapToSurface();
            }

            if (QTEGameManager.IsQTEActive || !controlsEnabled) return;

            // Convert input (WASD) to direction
            var inputDir = new Vector3(_moveInput.x, 0f, _moveInput.y);

            if (inputDir.sqrMagnitude >= 0.01f)
            {
                _moveDirection = inputDir.normalized;
                // Disable NavMeshAgent when using WASD
                if (_navAgent.enabled)
                {
                    Debug.Log("Switching to WASD movement");
                    float preSwitchY = transform.position.y;
                    _navAgent.ResetPath();
                    _navAgent.enabled = false;
                    _agent.ClearDestinationIndicator();
                    SnapToSurface();
                    Debug.Log($"Post-switch to CC - Y: {transform.position.y} (was {preSwitchY}, target bottom Y: {transform.position.y + _bottomOffset})");
                }

                Look();
                Move();
            }
            else if (!_navAgent.enabled)
            {
                // Re-enable NavMeshAgent when stopping WASD
                Debug.Log("Switching to Point-and-Click movement");
                SnapToSurface(); // Align before re-enabling (though agent will project)
                _navAgent.enabled = true;
                _verticalVelocity.y = 0f; // Reset vertical on switch
            }
        }

        private void Look()
        {
            Vector3 isoDirection = _moveDirection.ToIso();
            if (isoDirection.sqrMagnitude < 0.01f) return;

            var targetRotation = Quaternion.LookRotation(isoDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                _turnSpeed * Time.deltaTime
            );
        }

        private void Move()
        {
            Vector3 horizontalMove = _moveSpeed * Time.deltaTime * transform.forward;
            horizontalMove.y = 0f; // Ensure no accidental Y from forward

            // Vertical (gravity) if enabled
            var verticalMove = Vector3.zero;
            if (_handleGravity && !IsInTransitionAnimation)
            {
                if (_controller.isGrounded && _verticalVelocity.y < 0)
                {
                    _verticalVelocity.y = -1f; // Small downward nudge to maintain contact (prevents hover)
                }
                _verticalVelocity.y += _gravity * Time.deltaTime;
                verticalMove.y = _verticalVelocity.y * Time.deltaTime;
            }

            // Combined move
            Vector3 totalMove = horizontalMove + verticalMove;

            // Сохранение начальной позиции для проверки
            Vector3 initialPosition = transform.position;
            Vector3 proposedPosition = initialPosition + horizontalMove;

            // Project to capsule bottom for validation
            var bottomProposed = proposedPosition + new Vector3(0, _bottomOffset, 0);

            // Чек действительна ли позиция цели на NavMesh
            if (IsPositionValid(bottomProposed))
            {
                _controller.Move(totalMove);
            }
            else
            {
                Debug.Log("Blocked movement beyond NavMesh boundaries at proposed bottom: " + bottomProposed);
                // Still apply vertical (allow falling/sliding down edges)
                _controller.Move(verticalMove);
            }

            // Snap Y after move
            SnapToSurface();
        }

        private bool IsPositionValid(Vector3 bottomTargetPosition)
        {
            bool isValid = NavMesh.SamplePosition(bottomTargetPosition, out NavMeshHit hit, _navMeshSampleDistance, NavMesh.AllAreas);

            // Temporary debug (remove after testing)
            if (!isValid && Time.frameCount % 60 == 0)
            {
                float distToSurface = Vector3.Distance(bottomTargetPosition, hit.position);
                Debug.Log($"Validation failed at bottom {bottomTargetPosition}. Nearest: {hit.position}, Dist: {distToSurface}, MaxAllowed: {_navMeshSampleDistance}");
            }

            return isValid;
        }

        public void SnapToSurface()
        {
            if (_controller == null) return;

            float currentBottomY = transform.position.y + _bottomOffset; // Now same as position.y
            float targetSurfaceY = currentBottomY; // Default: no change
            bool snapped = false;

            // Prefer NavMesh sample for consistency with agent
            if (_preferNavMeshForSnap)
            {
                var samplePos = transform.position + new Vector3(0, _bottomOffset, 0); // Sample at current bottom (position.y)
                if (NavMesh.SamplePosition(samplePos, out NavMeshHit navHit, _navMeshSnapDistance, NavMesh.AllAreas))
                {
                    targetSurfaceY = navHit.position.y;
                    snapped = true;
                }
            }

            // Fallback to raycast if no NavMesh hit or disabled
            if (!snapped)
            {
                var rayStart = transform.position + new Vector3(0, _topOffset, 0);
                var rayDirection = Vector3.down;
                float rayDistance = _controller.height + _surfaceSnapTolerance + Mathf.Abs(_bottomOffset);

                // NonAlloc raycast 
                int hitCount = Physics.RaycastNonAlloc(
                    rayStart,
                    rayDirection,
                    _groundRaycastBuffer,
                    rayDistance,
                    _groundLayerMask
                );

                if (hitCount > 0)
                {
                    RaycastHit groundHit = _groundRaycastBuffer[0];
                    targetSurfaceY = groundHit.point.y + _controller.skinWidth;
                    snapped = true;
                }
                else
                {
                    Debug.LogWarning("No ground hit for snap - applying extra gravity");
                    if (_handleGravity) _verticalVelocity.y += _gravity * Time.deltaTime * 1.5f;
                    return;
                }
            }

            // If snapped, calculate and apply (pivot Y = surface Y)
            if (snapped)
            {
                float desiredPivotY = targetSurfaceY - _bottomOffset; // = targetSurfaceY (0.035)

                // Check drift
                float yDrift = Mathf.Abs(transform.position.y - desiredPivotY);

                // Force snap if grounded (ignores tolerance for zero-drift reliability)
                bool shouldSnap = (_forceSnapWhenGrounded && _controller.isGrounded) || yDrift > _surfaceSnapTolerance;

                if (IsInTransitionAnimation)
                {
                    shouldSnap = true; // Force snap every frame during transitions, ignoring tolerance
                }

                if (shouldSnap)
                {
                    transform.position = new Vector3(transform.position.x, desiredPivotY, transform.position.z);
                    if (_handleGravity) _verticalVelocity.y = 0f;
                }
            }
        }

        public Vector3 GetDesiredDirection() => _moveDirection;
    }
}

using UnityEngine;
using System.Collections;
using QTE;

public class CameraMovement : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform _target;                   // Drag player here or auto-find

    [Header("Isometric Follow")]
    [SerializeField] private Vector3 _offset = new(-6.4f, 0f, -6.2f);  // Your perfect isometric offset
    [SerializeField] private float _smoothTime = 0.1f;                     // Slight smoothing feels great

    [Header("Free Movement (Edge Scroll)")]
    [SerializeField] private float _edgeMoveSpeed = 5f;
    [SerializeField] private float _borderThickness = 50f;

    [Header("Wall Transparency")]
    [SerializeField] private Material _transparentMaterial;  // Assign your semi-transparent wall material here
    [SerializeField] private float _rayDistance = 50f;       // Max ray length (adjust for your scene size)
    [SerializeField] private float _unobstructedThreshold = 0.1f;  // Time (seconds) of clear sight before restoring wall (anti-flicker buffer)
    [SerializeField] private float _rayOffsetHeight = 1f;    // Height offset for multi-ray (half player height, e.g., 1m for 2m player)

    private Vector3 _velocity = Vector3.zero;
    private bool _isFollowing = true;
    private Vector3 _initialOffset;          // Stores the original fixed offset
    private float _fixedY;                   // Locked Y position (isometric must stay level)

    // Wall transparency tracking
    private MeshRenderer _currentWall;  // Tracks the obstructing wall
    private Collider _currentWallCollider;  // Tracks the obstructing wall's collider
    private Material _originalWallMaterial;  // Stores the original shared material for the current wall
    private bool _isObstructed = false;
    private float _unobstructedTime = 0f;

    private readonly RaycastHit[] _raycastBuffer = new RaycastHit[1];


    private void Awake()
    {
        _initialOffset = _offset;
        _fixedY = transform.position.y;  // Lock height from the start
    }

    void Start()
    {
        if (_target == null)
            StartCoroutine(FindPlayer());
    }

    void Update()
    {
        if (QTEGameManager.IsQTEActive) return;
        // Переключение режима камеры с помощью клавиши F
        if (Input.GetKeyDown(KeyCode.F))
        {
            _isFollowing = !_isFollowing;
        }

        // Перемещение камеры мышкой по краям экрана
        if (!_isFollowing)
        {
            HandleEdgeMovement();
        }
    }

    void LateUpdate()
    {
        if (_target == null) return;

        if (_isFollowing)
        {
            FollowPlayerIsometric();
        }

        HandleWallTransparency();
    }

    private void FollowPlayerIsometric()
    {
        var targetPosition = _target.position + _initialOffset;
        targetPosition.y = _fixedY; // Enforce fixed height — essential for isometric

        // Optional: force exact rotation every frame (prevents any drift)
        transform.SetPositionAndRotation(Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref _velocity,
            _smoothTime
        ), Quaternion.Euler(45f, 45f, 0f));
    }

    void HandleWallTransparency()
    {
        // Temporarily enable current wall collider for accurate obstruction check (if it exists)
        var wasDisabled = false;
        if (_currentWallCollider != null)
        {
            wasDisabled = !_currentWallCollider.enabled;
            _currentWallCollider.enabled = true;
        }

        var currentlyObstructed = false;
        MeshRenderer closestWallRenderer = null;
        Collider closestWallCollider = null;
        float closestWallDist = float.MaxValue;

        Vector3[] heightOffsets = { Vector3.zero, Vector3.up * _rayOffsetHeight, Vector3.down * _rayOffsetHeight };

        foreach (var heightOffset in heightOffsets)
        {
            var targetPoint = _target.position + heightOffset;
            var direction = (targetPoint - transform.position).normalized;
            float maxDistance = Vector3.Distance(transform.position, targetPoint);

            // NonAlloc single-hit raycast
            int hitCount = Physics.RaycastNonAlloc(
                transform.position,
                direction,
                _raycastBuffer,
                maxDistance
            );

            if (hitCount > 0)
            {
                var hit = _raycastBuffer[0];
                if (hit.collider.CompareTag("Wall") && hit.collider.gameObject != _target.gameObject)
                {
                    currentlyObstructed = true;

                    if (hit.distance < closestWallDist)
                    {
                        closestWallDist = hit.distance;
                        closestWallRenderer = hit.collider.GetComponent<MeshRenderer>();
                        closestWallCollider = hit.collider;
                    }
                }
            }
        }

        if (currentlyObstructed)
        {
            if (!_isObstructed || closestWallRenderer != _currentWall)
            {
                if (_currentWall != null) ResetWall();

                if (closestWallRenderer != null)
                {
                    _currentWall = closestWallRenderer;
                    _currentWallCollider = closestWallCollider;
                    _originalWallMaterial = _currentWall.sharedMaterial;
                    _currentWall.sharedMaterial = _transparentMaterial;
                    if (_currentWallCollider != null) _currentWallCollider.enabled = false;
                }
            }
            _isObstructed = true;
            _unobstructedTime = 0f;
        }
        else
        {
            if (_isObstructed)
            {
                _unobstructedTime += Time.deltaTime;
                if (_unobstructedTime >= _unobstructedThreshold)
                {
                    ResetWall();
                    _currentWall = null;
                    _currentWallCollider = null;
                    _originalWallMaterial = null;
                    _isObstructed = false;
                }
            }
        }

        // Restore if needed
        if (wasDisabled && _isObstructed && _currentWallCollider != null)
        {
            _currentWallCollider.enabled = false;
        }
    }

    void ResetWall()
    {
        if (_currentWall != null && _originalWallMaterial != null)
        {
            // Swap back to original opaque shared material
            _currentWall.sharedMaterial = _originalWallMaterial;
        }
        if (_currentWallCollider != null)
        {
            // Re-enable collider
            _currentWallCollider.enabled = true;
        }
        Debug.Log("Reverting back to the shared material and activating collider");
    }

    private void HandleEdgeMovement()
    {
        var move = Vector3.zero;
        Vector2 mouse = Input.mousePosition;

        // Use camera's own forward/right projected on XZ plane (isometric-friendly)
        var forward = transform.forward;
        forward.y = 0; forward.Normalize();

        var right = transform.right;
        right.y = 0; right.Normalize();

        if (mouse.y >= Screen.height - _borderThickness) move += forward;
        if (mouse.y <= _borderThickness) move -= forward;
        if (mouse.x >= Screen.width - _borderThickness) move += right;
        if (mouse.x <= _borderThickness) move -= right;

        if (move != Vector3.zero)
        {
            var delta = _edgeMoveSpeed * Time.deltaTime * move.normalized;
            delta.y = 0;
            transform.position += delta;
        }
    }

    private IEnumerator FindPlayer()
    {
        yield return new WaitForSeconds(0.1f);
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            _target = player.transform;
        }
        else
        {
            Debug.LogWarning("Player not found, retrying...");
            yield return new WaitForSeconds(0.5f);
            StartCoroutine(FindPlayer());
        }
    }
}

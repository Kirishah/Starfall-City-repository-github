using UnityEngine;
using System.Collections;
using QTE;

public class CameraMovement : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;                   // Drag player here or auto-find

    [Header("Isometric Follow")]
    [SerializeField] private Vector3 offset = new Vector3(-3f, 5f, -3f);  // Your perfect isometric offset
    [SerializeField] private float smoothTime = 0.12f;                     // Slight smoothing feels great

    [Header("Free Movement (Edge Scroll)")]
    [SerializeField] private float edgeMoveSpeed = 18f;
    [SerializeField] private float borderThickness = 40f;

    [Header("Wall Transparency")]
    [SerializeField] private Material transparentMaterial;  // Assign your semi-transparent wall material here
    [SerializeField] private float rayDistance = 50f;       // Max ray length (adjust for your scene size)
    [SerializeField] private float unobstructedThreshold = 0.1f;  // Time (seconds) of clear sight before restoring wall (anti-flicker buffer)
    [SerializeField] private float rayOffsetHeight = 1f;    // Height offset for multi-ray (half player height, e.g., 1m for 2m player)

    private Vector3 velocity = Vector3.zero;
    private bool isFollowing = true;
    private Vector3 initialOffset;          // Stores the original fixed offset
    private float fixedY;                   // Locked Y position (isometric must stay level)

    // Wall transparency tracking
    private MeshRenderer currentWall;  // Tracks the obstructing wall
    private Collider currentWallCollider;  // Tracks the obstructing wall's collider
    private Material originalWallMaterial;  // Stores the original shared material for the current wall
    private bool isObstructed = false;
    private float unobstructedTime = 0f;


    private void Awake()
    {
        initialOffset = offset;
        fixedY = transform.position.y;  // Lock height from the start
    }

    void Start()
    {
        if (target == null)
            StartCoroutine(FindPlayer());
    }

    void Update()
    {
        if (QTEGameManager.IsQTEActive) return;
        // ѕереключение режима камеры с помощью клавиши F
        if (Input.GetKeyDown(KeyCode.F))
        {
            isFollowing = !isFollowing; 
        }

        // ѕеремещение камеры мышкой по кра€м экрана
        if (!isFollowing)
        {
            HandleEdgeMovement();
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        if (isFollowing)
        {
            FollowPlayerIsometric();
        }

        HandleWallTransparency();
    }

    private void FollowPlayerIsometric()
    {
        Vector3 targetPosition = target.position + initialOffset;
        targetPosition.y = fixedY; // Enforce fixed height Ч essential for isometric

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref velocity,
            smoothTime
        );

        // Optional: force exact rotation every frame (prevents any drift)
        transform.rotation = Quaternion.Euler(45f, 45f, 0f);
    }

    void HandleWallTransparency()
    {
        // Temporarily enable current wall collider for accurate obstruction check (if it exists)
        bool wasDisabled = false;
        if (currentWallCollider != null)
        {
            wasDisabled = !currentWallCollider.enabled;
            currentWallCollider.enabled = true;
        }

        bool currentlyObstructed = false;
        Vector3[] heightOffsets = { Vector3.zero, Vector3.up * rayOffsetHeight, Vector3.down * rayOffsetHeight };
        float minDistToPlayer = float.MaxValue;
        MeshRenderer potentialWall = null;
        Collider potentialCollider = null;
        float closestWallDist = float.MaxValue;

        foreach (Vector3 heightOffset in heightOffsets)
        {
            Vector3 targetPoint = target.position + heightOffset;
            float distToPoint = Vector3.Distance(transform.position, targetPoint);
            minDistToPlayer = Mathf.Min(minDistToPlayer, distToPoint);
            Vector3 directionToPoint = (targetPoint - transform.position).normalized;

            RaycastHit hit;
            if (Physics.Raycast(transform.position, directionToPoint, out hit, distToPoint))
            {
                // Check if hit is a wall closer than the target point
                if (hit.distance < distToPoint && hit.collider.CompareTag("Wall") && hit.collider.gameObject != target.gameObject)
                {
                    MeshRenderer wallRenderer = hit.collider.GetComponent<MeshRenderer>();
                    if (wallRenderer != null && hit.distance < closestWallDist)
                    {
                        closestWallDist = hit.distance;
                        potentialWall = wallRenderer;
                        potentialCollider = hit.collider;
                    }
                    currentlyObstructed = true;  // Any wall hit = obstructed
                }
            }

            // Debug rays (uncomment for visualization in Scene view)
            // Debug.DrawRay(transform.position, directionToPoint * distToPoint, Color.green, 0.1f);
        }

        // Use the closest wall for fading (avoids switching between nearby walls)
        if (currentlyObstructed)
        {
            if (!isObstructed || potentialWall != currentWall)
            {
                // Fade the (new/closest) wall
                if (currentWall != null) ResetWall();
                if (potentialWall != null)
                {
                    currentWall = potentialWall;
                    currentWallCollider = potentialCollider;
                    originalWallMaterial = currentWall.sharedMaterial;
                    currentWall.sharedMaterial = transparentMaterial;
                    currentWallCollider.enabled = false;
                }
            }
            isObstructed = true;
            unobstructedTime = 0f;
        }
        else
        {
            if (isObstructed)
            {
                unobstructedTime += Time.deltaTime;
                if (unobstructedTime >= unobstructedThreshold)
                {
                    ResetWall();
                    currentWall = null;
                    currentWallCollider = null;
                    originalWallMaterial = null;
                    isObstructed = false;
                    unobstructedTime = 0f;
                }
                else
                {
                    // During buffer period (clear check but not expired), keep collider enabled
                    if (currentWallCollider != null) currentWallCollider.enabled = true;
                }
            }
            // If not obstructed, no action needed (collider stays enabled)
        }

        // Restore disable state only if it was disabled and we're still obstructing
        if (wasDisabled && isObstructed && currentWallCollider != null)
        {
            currentWallCollider.enabled = false;
        }
    }

    void ResetWall()
    {
        if (currentWall != null && originalWallMaterial != null)
        {
            // Swap back to original opaque shared material
            currentWall.sharedMaterial = originalWallMaterial;
        }
        if (currentWallCollider != null)
        {
            // Re-enable collider
            currentWallCollider.enabled = true;
        }
        Debug.Log("Reverting back to the shared material and activating collider");
    }

    private void HandleEdgeMovement()
    {
        Vector3 move = Vector3.zero;
        Vector2 mouse = Input.mousePosition;

        // Use camera's own forward/right projected on XZ plane (isometric-friendly)
        Vector3 forward = transform.forward;
        forward.y = 0; forward.Normalize();

        Vector3 right = transform.right;
        right.y = 0; right.Normalize();

        if (mouse.y >= Screen.height - borderThickness) move += forward;
        if (mouse.y <= borderThickness) move -= forward;
        if (mouse.x >= Screen.width - borderThickness) move += right;
        if (mouse.x <= borderThickness) move -= right;

        if (move != Vector3.zero)
        {
            Vector3 delta = move.normalized * edgeMoveSpeed * Time.deltaTime;
            delta.y = 0;
            transform.position += delta;
        }
    }

    private IEnumerator FindPlayer()
    {
        yield return new WaitForSeconds(0.1f);
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            target = player.transform;
        }
        else
        {
            Debug.LogWarning("Player not found, retrying...");
            yield return new WaitForSeconds(0.5f);
            StartCoroutine(FindPlayer());
        }
    }
}

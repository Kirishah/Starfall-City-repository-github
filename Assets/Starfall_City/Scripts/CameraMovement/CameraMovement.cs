using UnityEngine;
using System.Collections;
using QTE;

public class CameraMovement : MonoBehaviour
{
    private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(-3, 5, -3);
    [SerializeField] private float smoothTime = 0.1f;
    [SerializeField] private float edgeMoveSpeed = 5f;
    [SerializeField] private float borderThickness = 50f;

    [Header("Wall Transparency")]
    [SerializeField] private Material transparentMaterial;  // Assign your semi-transparent wall material here
    [SerializeField] private float rayDistance = 50f;       // Max ray length (adjust for your scene size)
    [SerializeField] private float unobstructedThreshold = 0.1f;  // Time (seconds) of clear sight before restoring wall (anti-flicker buffer)
    [SerializeField] private float rayOffsetHeight = 1f;    // Height offset for multi-ray (half player height, e.g., 1m for 2m player)

    private Vector3 velocity = Vector3.zero;
    private bool isFollowing = true;
    private Vector3 originalOffset;
    private float fixedYPosition;  // начальное положение по оси Y
    private MeshRenderer currentWall;  // Tracks the obstructing wall
    private Collider currentWallCollider;  // Tracks the obstructing wall's collider
    private Material originalWallMaterial;  // Stores the original shared material for the current wall
    private bool isObstructed = false;
    private float unobstructedTime = 0f;

    void Start()
    {
        originalOffset = offset;
        fixedYPosition = transform.position.y;  
        StartCoroutine(FindPlayer());
    }

    void Update()
    {
        if (QTEGameManager.IsQTEActive) return;
        // Переключение режима камеры с помощью клавиши F
        if (Input.GetKeyDown(KeyCode.F))
        {
            isFollowing = !isFollowing;
            if (isFollowing) offset = originalOffset;  
        }

        // Перемещение камеры мышкой по краям экрана
        if (!isFollowing)
        {
            HandleEdgeMovement();
        }
    }

    void LateUpdate()
    {
        if (isFollowing && target != null)
        {
            // Плавное следование с исходным смещением
            Vector3 targetPosition = target.position + offset;
            targetPosition.y = fixedYPosition;  
            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref velocity,
                smoothTime
            );
        }

        // Always handle wall transparency if player exists (works in both modes)
        if (target != null)
        {
            HandleWallTransparency();
        }
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

    void HandleEdgeMovement()
    {
        Vector3 mousePos = Input.mousePosition;
        Vector3 movement = Vector3.zero;

        // Получение относительных направлений камеры (только в плоскости XZ)
        Vector3 forward = transform.forward;
        forward.y = 0;
        forward.Normalize();

        Vector3 right = transform.right;
        right.y = 0;
        right.Normalize();

        // Чек краев экрана
        if (mousePos.y >= Screen.height - borderThickness)
            movement += forward;
        if (mousePos.y <= borderThickness)
            movement -= forward;
        if (mousePos.x >= Screen.width - borderThickness)
            movement += right;
        if (mousePos.x <= borderThickness)
            movement -= right;

        // Движение без изменения вектора Y
        Vector3 newPosition = transform.position + movement.normalized * edgeMoveSpeed * Time.deltaTime;
        newPosition.y = fixedYPosition;  // Keep original height
        transform.position = newPosition;
    }

    IEnumerator FindPlayer()
    {
        yield return new WaitForSeconds(0.1f);

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            target = player.transform;
            offset = new Vector3(
                transform.position.x - target.position.x,
                0,  // Ignore Y difference
                transform.position.z - target.position.z
            );
            originalOffset = offset;
            fixedYPosition = transform.position.y;  // Set initial height
        }
        else
        {
            Debug.LogError("Player not found! Retrying...");
            yield return new WaitForSeconds(0.5f);
            StartCoroutine(FindPlayer()); // Retry if player not found
        }
    }
}

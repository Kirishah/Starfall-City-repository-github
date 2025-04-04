using UnityEngine;
using System.Collections;

public class CameraMovementTest : MonoBehaviour
{
    private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(-3, 5, -3);
    [SerializeField] private float smoothTime = 0.1f;
    [SerializeField] private float edgeMoveSpeed = 5f;
    [SerializeField] private float borderThickness = 50f;

    private Vector3 velocity = Vector3.zero;
    private bool isFollowing = true;
    private Vector3 originalOffset;
    private float fixedYPosition;  // Store initial Y position

    void Start()
    {
        originalOffset = offset;
        fixedYPosition = transform.position.y;  // Lock initial height
        StartCoroutine(FindPlayer());
    }

    void Update()
    {
        // Toggle follow mode with F key
        if (Input.GetKeyDown(KeyCode.F))
        {
            isFollowing = !isFollowing;
            if (isFollowing) offset = originalOffset;  // Reset to original offset
        }

        // Edge-based movement when not following
        if (!isFollowing)
        {
            HandleEdgeMovement();
        }
    }

    void LateUpdate()
    {
        if (isFollowing && target != null)
        {
            // Smooth follow with original offset
            Vector3 targetPosition = target.position + offset;
            targetPosition.y = fixedYPosition;  // Maintain camera height
            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref velocity,
                smoothTime
            );
        }
    }

    void HandleEdgeMovement()
    {
        Vector3 mousePos = Input.mousePosition;
        Vector3 movement = Vector3.zero;

        // Get camera-relative directions (XZ plane only)
        Vector3 forward = transform.forward;
        forward.y = 0;
        forward.Normalize();

        Vector3 right = transform.right;
        right.y = 0;
        right.Normalize();

        // Screen edge checks
        if (mousePos.y >= Screen.height - borderThickness)
            movement += forward;
        if (mousePos.y <= borderThickness)
            movement -= forward;
        if (mousePos.x >= Screen.width - borderThickness)
            movement += right;
        if (mousePos.x <= borderThickness)
            movement -= right;

        // Apply movement while maintaining Y position
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
            offset = transform.position - target.position;
            originalOffset = offset;
            fixedYPosition = transform.position.y;  // Set initial height
        }
        else
        {
            Debug.LogError("Player not found!");
            yield return new WaitForSeconds(0.1f);

            while (true)
            {
                yield return new WaitForSeconds(0.1f);
                GameObject potentialPlayer = GameObject.FindGameObjectWithTag("Player");
                if (potentialPlayer != null)
                {
                    target = potentialPlayer.transform;
                    offset = transform.position - target.position;
                    originalOffset = offset;
                    fixedYPosition = transform.position.y;
                    break;
                }
            }
        }
    }
}

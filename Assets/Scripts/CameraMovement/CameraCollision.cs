using UnityEngine;

public class CameraCollision : MonoBehaviour
{
    public Transform player; // Reference to the player character
    public float distanceFromPlayer = 5f; // Default distance from the player
    public float minDistanceFromPlayer = 2f; // Minimum distance to maintain
    public LayerMask collisionMask; // Layer mask for collision detection
    public float cameraHeight = 3f; // Height of the camera above the player
    public float smoothSpeed = 0.125f; // Smoothing speed for camera movement

    private Vector3 desiredPosition;

    void LateUpdate()
    {
        // Calculate the desired position based on the player's position
        desiredPosition = player.position - player.forward * distanceFromPlayer;
        desiredPosition.y = player.position.y + cameraHeight;

        // Check for collisions
        RaycastHit hit;
        if (Physics.Raycast(player.position, -player.forward, out hit, distanceFromPlayer, collisionMask))
        {
            // If a collision is detected, adjust the camera position
            float distanceToWall = hit.distance;
            float adjustedDistance = Mathf.Clamp(distanceToWall, minDistanceFromPlayer, distanceFromPlayer);
            desiredPosition = player.position - player.forward * adjustedDistance;
            desiredPosition.y = player.position.y + cameraHeight;
        }

        // Smoothly interpolate to the desired position
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

        // Optionally, make the camera look at the player
        transform.LookAt(player.position + Vector3.up * cameraHeight);
    }
}

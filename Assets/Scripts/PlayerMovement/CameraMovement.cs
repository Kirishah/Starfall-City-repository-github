using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    private float moveSpeed = 5.0f; // Speed of camera movement
    private Vector3 minBound = new Vector3(-20f, 9.5f, -5f); // Minimum clamp position
    private Vector3 maxBound = new Vector3(5f, 9.5f, 5f);   // Maximum clamp position

    void Update()
    {
        MoveCamera();
    }

    void MoveCamera()
    {
        // Get input for camera movement
        float moveInputX = Input.GetAxis("Vertical"); // Use arrow keys or A/D keys
        float moveInputZ = Input.GetAxis("Horizontal");

        Vector3 moveX = new Vector3(moveInputX * moveSpeed * Time.deltaTime, 0, 0);
        Vector3 moveZ = new Vector3(0, 0, moveInputZ * moveSpeed * Time.deltaTime);

        // Move the camera
        transform.position += moveX + moveZ;

        // Clamp the camera's position to the defined boundaries
        transform.position = new Vector3(
            Mathf.Clamp(transform.position.x, minBound.x, maxBound.x),
            transform.position.y,
            Mathf.Clamp(transform.position.z, minBound.z, maxBound.z)
        );
    }
}

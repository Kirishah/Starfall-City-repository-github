using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    private float moveSpeed = 5.0f; // Speed of camera movement
    private Vector3 minBound = new Vector3(-3.6f, 9.5f, -8.6f); // Minimum clamp position
    private Vector3 maxBound = new Vector3(20f, 9.5f, 8.5f);   // Maximum clamp position
    private Camera m_Camera;

    private void Awake()
    {
        m_Camera = GetComponent<Camera>();
    }

    void Update()
    {
        MoveCamera();
    }

    void MoveCamera()
    {
        // Get input for camera movement
        float moveInputX = Input.GetAxis("Horizontal"); // Use arrow keys or A/D keys
        float moveInputZ = Input.GetAxis("Vertical");

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

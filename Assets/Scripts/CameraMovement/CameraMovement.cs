using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public Transform player; // Reference to the player
    public Vector3 offset; // Offset from the player

    void Start()
    {
        // Set the initial offset
        offset = transform.position - player.position;
    }

    void LateUpdate()
    {
        // Follow the player
        transform.position = player.position + offset;
    }
}

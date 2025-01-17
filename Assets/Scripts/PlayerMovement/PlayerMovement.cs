using UnityEngine;
using System.Collections;
using UnityEngine.AI;
using Invector.vCharacterController;
using UnityEngine.EventSystems;

public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    private CharacterController characterController;
    public Camera cam;
    public NavMeshAgent player;

    [Header("Movement Settings")]
    private Vector3 moveDirection;
    private Vector3 lastPosition;
    private Vector3 velocity;
    [SerializeField] private float speed = 5f;
    private const bool V = false;

    private void Start()
    {
        player.updateRotation = V;
        characterController = GetComponent<CharacterController>();

        lastPosition = transform.position;
    }

    void Update()
    {
        if (Input.GetMouseButton(1)) // Right mouse button
        {
            MovePlayer();
        }
        CalculateVelocity();
    }

    void MovePlayer()
    {
        // Get the mouse position in the world
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hitPoint;

        if (Physics.Raycast(ray, out hitPoint))
        {
            // Set the destination for the NavMeshAgent
            player.SetDestination(hitPoint.point);

            // Calculate the movement direction
            Vector3 targetPosition = hitPoint.point;
            targetPosition.y = transform.position.y; // Keep the y position the same to avoid vertical movement

            // Calculate the movement direction
            moveDirection = (targetPosition - transform.position).normalized * speed;
        }
        else
        {
            Debug.Log("Raycast did not hit any collider."); // Log if nothing was hit
        }

 
    }

    void CalculateVelocity()
    {
        // Calculate the velocity based on the change in position over time
        velocity = (transform.position - lastPosition) / Time.deltaTime;

        // Update last position for the next frame
        lastPosition = transform.position;
    }

    public Vector3 GetVelocity()
    {
        
        return velocity;
    }
}

    


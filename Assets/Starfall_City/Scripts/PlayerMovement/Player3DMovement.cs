using UnityEngine;

public class Player3DMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float turnSpeed = 9f;

    [Header("References")]
    private CharacterController controller;
    private PlayerMovement agent;

    private Vector3 moveDirection;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        agent = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        GatherInput();
        Look();
        Move();
    }

    private void GatherInput()
    {
        moveDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;
    }
    private void Look()
    {
        if (moveDirection.magnitude >= 0.1f)
        {
            // Calculate target rotation based on camera-relative input
            Vector3 isoDirection = moveDirection.ToIso(); // Ensure this converts input to world space
            Quaternion targetRotation = Quaternion.LookRotation(isoDirection.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                turnSpeed * Time.deltaTime
            );
        }
    }

    private void Move()
    {
        if (moveDirection.magnitude >= 0.1f)
        {
            // Move in the direction the player is facing
            Vector3 move = transform.forward * moveDirection.magnitude * moveSpeed * Time.deltaTime;
            controller.Move(move);
            agent.player.SetDestination(transform.position);
        }
    }
}

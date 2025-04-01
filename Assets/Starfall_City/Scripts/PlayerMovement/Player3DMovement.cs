using UnityEngine;

public class Player3DMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float turnSpeed = 9;

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
    }

    private void FixedUpdate()
    {
        Move();
    }

    private void GatherInput()
    {
        moveDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;
    }
    private void Look()
    { 
        // player rotation
         if (moveDirection.magnitude >= 0.1f)
        {
            var relative = (transform.position + moveDirection.ToIso()) - transform.position;
            var rot = Quaternion.LookRotation(relative, Vector3.up);

            transform.rotation = Quaternion.RotateTowards(transform.rotation, rot, turnSpeed);
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

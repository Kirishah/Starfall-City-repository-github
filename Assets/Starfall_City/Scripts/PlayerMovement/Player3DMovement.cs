using UnityEngine;
using UnityEngine.AI;

public class Player3DMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float turnSpeed = 9f;

    [Header("References")]
    private CharacterController controller;
    private PlayerMovement agent;
    private NavMeshAgent navAgent;

    private Vector3 moveDirection;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        agent = GetComponent<PlayerMovement>();
        navAgent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (QTEGameManager.IsQTEActive) return;

        GatherInput();
        if (moveDirection.magnitude >= 0.1f)
        {
            // Disable NavMeshAgent when using WASD
            if (navAgent.enabled)
            {
                Debug.Log("Switching to WASD movement");
                navAgent.ResetPath();
                navAgent.enabled = false;
                agent.ClearDestinationIndicator();
            }
            Look();
            Move();
        }
        else if (!navAgent.enabled)
        {
            // Re-enable NavMeshAgent when stopping WASD
            Debug.Log("Switching to Point-and-Click movement");
            navAgent.enabled = true;
        }
    }

    private void GatherInput()
    {
        moveDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;
    }
    private void Look()
    {
        Vector3 isoDirection = moveDirection.ToIso();
        Quaternion targetRotation = Quaternion.LookRotation(isoDirection.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            turnSpeed * Time.deltaTime
        );
    }

    private void Move()
    {
        Vector3 move = transform.forward * moveSpeed * Time.deltaTime;
        // —охранение начальной позиции дл€ проверки
        Vector3 initialPosition = transform.position;
        Vector3 proposedPosition = initialPosition + move;

        // „ек действительна ли позици€ цели на NavMesh
        if (IsPositionValid(proposedPosition))
        {
            controller.Move(move);
        }
        else
        {
            Debug.Log("Blocked movement beyond NavMesh boundaries");
            // ћожно добавить звук или иконку, что невозможно достичь местоположени€
        }
    }

    private bool IsPositionValid(Vector3 targetPosition)
    {
        NavMeshHit hit;
        return NavMesh.SamplePosition(targetPosition, out hit, 0.1f, NavMesh.AllAreas);
    }
}

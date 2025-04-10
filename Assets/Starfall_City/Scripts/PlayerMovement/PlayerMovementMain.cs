using UnityEngine;
using UnityEngine.AI;

public class PlayerMovementMain : MonoBehaviour
{
    private const bool V = false;

    public Camera cam;
    public NavMeshAgent player;


    private void Start()
    {
        player.updateRotation = V;
    }

    void Update()
    {
        if (Input.GetMouseButton(1)) // Right mouse button
        {
            MovePlayer();
        }
    }

    void MovePlayer()
    {
        // Get the mouse position in the world
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hitPoint;

        if (Physics.Raycast(ray, out hitPoint))
        {
            player.SetDestination(hitPoint.point);
        }
        else
        {
            Debug.Log("Raycast did not hit any collider."); // Log if nothing was hit
        }


    }
}

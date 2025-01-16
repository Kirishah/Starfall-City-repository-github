using UnityEngine;
using System.Collections;
using UnityEngine.AI;
using Invector.vCharacterController;
using SojaExiles;

public class PlayerMovement : MonoBehaviour
{
    public Camera cam;
    public NavMeshAgent player;

    public vThirdPersonController character;

    private void Start()
    {
        player.updateRotation = false;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(1)) // Right mouse button
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

        if (player.remainingDistance > player.stoppingDistance)
        {
            character.MoveCharacter(player.desiredVelocity);
        } else
        {
            character.MoveCharacter(Vector3.zero);
        }
    }
    



}

    


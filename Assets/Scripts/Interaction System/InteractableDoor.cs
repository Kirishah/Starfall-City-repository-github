using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class InteractableDoor : Interactable
{
    public Animator openandclose;
    public bool isOpen;
    public Transform Player;
    private NavMeshObstacle navMeshObstacle;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        isOpen = false;
        navMeshObstacle = GetComponent<NavMeshObstacle>();
    }

    

    public override void Interact()
    {
        Debug.Log("Door has been interacted with!");
        float dist = Vector3.Distance(Player.position, transform.position);
        if (dist < 3)
        {
            if (isOpen)
            {
                StartCoroutine(CloseDoor());
            }
            else
            {
                StartCoroutine(OpenDoor());
            }
        }
        else
        {
            Debug.Log("Too faraway");
        }
    }
    private IEnumerator OpenDoor()
    {
        print("you are opening the door");
        openandclose.Play("Opening");
        isOpen = true;
        yield return new WaitForSeconds(.5f);
    }

    private IEnumerator CloseDoor()
    {
        print("you are closing the door");
        openandclose.Play("Closing");
        isOpen = false;
        yield return new WaitForSeconds(.5f);
    }
}

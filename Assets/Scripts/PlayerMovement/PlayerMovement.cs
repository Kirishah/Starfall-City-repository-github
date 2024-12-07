using UnityEngine;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;

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
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            // Check if the hit object is on the correct layer (optional)
             if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Floor 2"))
             {
            Vector3 targetPosition = new Vector3(hit.point.x, transform.position.y, hit.point.z);

            // Move the player towards the target position
            StartCoroutine(MoveToPosition(targetPosition));
             }
        }
    }
    private IEnumerator MoveToPosition(Vector3 targetPosition)
    {
        while (Vector3.Distance(transform.position, targetPosition) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            yield return null; // Wait for the next frame
        }
        transform.position = targetPosition;
    }
}

    


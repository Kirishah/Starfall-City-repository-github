using UnityEngine;
using System.Collections;

public class CameraMovement : MonoBehaviour
{
    private Transform target; // Reference to the player
    [SerializeField] private Vector3 offset = new Vector3(-3, 5, -3); // Offset from the player
    private Vector3 velocity = Vector3.zero;

    void Start()
    {
        StartCoroutine(FindPlayer());
        
    }

    IEnumerator FindPlayer()
    {
        yield return new WaitForSeconds(0.1f); // Wait for 0.1 seconds

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            target = player.transform;
            Debug.Log("Player found!");
        }
        else
        {
            Debug.LogError("Player not found!");
            yield return new WaitForSeconds(0.1f); // Wait again and try again

            while (true)
            {
                yield return new WaitForSeconds(0.1f);
                GameObject potentialPlayer = GameObject.FindGameObjectWithTag("Player");
                if (potentialPlayer != null)
                {
                    target = potentialPlayer.transform;
                    // Set the initial offset
                    offset = transform.position - target.position;
                    break;
                }
            }
        }
    }

    void LateUpdate()
    {
        // Follow the player
        if (target != null) // Add a null check for target
        {
            transform.position = target.position + offset;
        }
    }
}

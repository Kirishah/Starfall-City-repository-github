using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    public Dialogue dialogue; // Reference to the Dialogue script

    private void Update()
    {
        // Check for right mouse button click
        if (Input.GetMouseButtonDown(1)) // 1 is the right mouse button
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit))
            {
                // Check if the clicked object is this interactable object
                if (hit.transform == transform)
                {
                    // Check if the dialogue component is assigned
                    if (dialogue != null)
                    {
                        // Check if the dialogue is already active
                        if (!dialogue.gameObject.activeSelf)
                        {
                            dialogue.gameObject.SetActive(true); // Show the dialogue UI
                            dialogue.StartDialogue(); // Start the dialogue
                        }
                    }
                }
            }
        }
    }
}

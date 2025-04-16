using UnityEngine;
using UnityEngine.Events;

public abstract class Interactable : MonoBehaviour
{
    [SerializeField] public string interactionText;
    public UnityEvent onInteract;
    public GameObject promptPrefab; // Assign the UI prompt prefab
    [SerializeField] public Vector3 promptOffset; // Adjust height

    public abstract void ShowPrompt(); 

    public abstract void HidePrompt();

    public abstract void Interact();
}

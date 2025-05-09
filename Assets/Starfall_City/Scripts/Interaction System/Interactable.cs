using UnityEngine;
using UnityEngine.Events;

public abstract class Interactable : MonoBehaviour
{
    [SerializeField] public string interactionText;
    public UnityEvent onInteract;
    protected string _objectID; 
    public GameObject promptPrefab; // Assign the UI prompt prefab
    [SerializeField] public Vector3 promptOffset; // Adjust height

    // state tracking fields
    protected bool _isInProximity;
    protected bool _isHovered;

    public void SetProximity(bool state) => _isInProximity = state;
    public void SetHovered(bool state) => _isHovered = state;

    protected virtual void Update()
    {
        // Update prompt visibility based on states
        if (_isInProximity || _isHovered) ShowPrompt();
        else HidePrompt();
    }

    public abstract void ShowPrompt(); 

    public abstract void HidePrompt();

    public virtual void Interact()
    {
        onInteract?.Invoke();
        QuestManager.Instance.HandleObjectiveUpdate(ObjectiveType.Interaction, _objectID);
        Debug.Log($"Interactable interacted: ID={_objectID}");
    }

    public virtual string GetIdentifier()
    {
        return gameObject.name; // Default to GameObject name, override in subclasses
    }
}

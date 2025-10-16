using UnityEngine;
using UnityEngine.Events;
using QTE;

public abstract class Interactable : MonoBehaviour, QTEGameManager.IRPGComponent
{
    [SerializeField] public string interactionText;
    public UnityEvent onInteract;
    protected string _objectID; 
    public GameObject promptPrefab; // the UI prompt prefab
    [SerializeField] public Vector3 promptOffset; // Высота промпта над объектом

    // state tracking fields
    protected bool _isInProximity;
    protected bool _isHovered;

    public void SetProximity(bool state) => _isInProximity = state;
    public void SetHovered(bool state) => _isHovered = state;

    protected virtual void Update()
    {
        if (QTEGameManager.IsQTEActive) return;

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
        return gameObject.name; // По умолчанию имя GameObject, переопределяется в подклассах
    }
}

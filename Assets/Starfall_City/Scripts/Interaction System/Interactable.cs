using QTE;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using static QuestSO;
using static QuestSO.UnlockCondition;

public abstract class Interactable : MonoBehaviour, QTEGameManager.IRPGComponent
{
    [SerializeField] public string interactionText;
    public UnityEvent onInteract;
    protected string _objectID; 
    public GameObject promptPrefab; // the UI prompt prefab
    [SerializeField] public Vector3 promptOffset; // Позиция промпта над объектом

    // Gating Conditions (for future-proofing)
    [Header("Interaction Conditions (All Must Be True)")]
    [SerializeField] private List<UnlockCondition> unlockConditions = new List<UnlockCondition>();

    // state tracking fields
    protected bool _isInProximity;
    protected bool _isHovered;
    protected bool _isInteractable = true;  // Runtime flag: true if conditions met
    private bool _wasInteractableLastFrame = true;

    public void SetProximity(bool state) => _isInProximity = state;
    public void SetHovered(bool state) => _isHovered = state;

    protected virtual void Update()
    {
        if (QTEGameManager.IsQTEActive) return;

        // Re-evaluate conditions every frame (or optimize with events later)
        _isInteractable = EvaluateConditions();

        if (_isInteractable && !_wasInteractableLastFrame)
        {
            Debug.Log($"Interaction unlocked for {gameObject.name}: Conditions now met.");
        }

        if ((_isInProximity || _isHovered) && _isInteractable)
        {
            ShowPrompt();
        }
        else
        {
            HidePrompt();
        }
    }

    // Evaluate all conditions (mirrors QuestSO logic)
    protected bool EvaluateConditions()
    {
        if (unlockConditions == null || unlockConditions.Count == 0)
        {
            _wasInteractableLastFrame = true;
            return true;  // No conditions = always allowed
        }

        bool allMet = unlockConditions.All(condition => condition.Evaluate());

        // Log only on transition to gated (not every frame)
        if (!allMet && _wasInteractableLastFrame)
        {
            Debug.Log($"Interaction gated for {gameObject.name}: Conditions not met.");
        }
        _wasInteractableLastFrame = allMet;

        return allMet;
    }

    public abstract void ShowPrompt(); 

    public abstract void HidePrompt();

    public virtual void Interact()
    {
        // Double-check conditions (in case called externally)
        if (!_isInteractable)
        {
            Debug.LogWarning($"Cannot interact with {gameObject.name}: Conditions not met. Complete prerequisites first.");
            return;
        }

        onInteract?.Invoke();
        QuestManager.Instance.HandleObjectiveUpdate(ObjectiveType.Interaction, _objectID);
        Debug.Log($"Interactable interacted: ID={_objectID}");
    }

    public virtual string GetIdentifier()
    {
        return gameObject.name; // По умолчанию имя GameObject, переопределяется в подклассах
    }
}

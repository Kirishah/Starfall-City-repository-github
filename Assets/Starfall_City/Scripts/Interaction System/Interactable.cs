using Core;
using QTE;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using static QuestSO;
using static QuestSO.UnlockCondition;

public abstract class Interactable : MonoBehaviour, QTEGameManager.IRPGComponent
{
    [SerializeField] public string interactionText;
    [SerializeField] public string _objectID;
    public UnityEvent onInteract;
    
    public GameObject promptPrefab; // the UI prompt prefab
    [SerializeField] public Vector3 promptOffset; // Позиция промпта над объектом

    // Gating Conditions (for future-proofing)
    [Header("Interaction Conditions (All Must Be True)")]
    [SerializeField] private List<UnlockCondition> unlockConditions = new List<UnlockCondition>();

    [Header("Debug Settings")]
    [SerializeField] private bool enableConditionLogging = false;

    // state tracking fields
    protected bool _isInProximity;
    protected bool _isHovered;
    protected bool _isInteractable = true;  // Runtime flag: true if conditions met
    private bool _conditionsEverEvaluated = false;

    // Shared prompt field
    protected GameObject currentPrompt;

    public void SetProximity(bool state) => _isInProximity = state;
    public void SetHovered(bool state) => _isHovered = state;

    protected virtual void Start()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.OnQuestCompleted += HandleQuestCompleted;
            QuestManager.OnObjectiveProgressed += HandleObjectiveProgressed;
        }

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryUpdated += HandleInventoryUpdated;
        }
    }

    protected virtual void OnDestroy()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.OnQuestCompleted -= HandleQuestCompleted;
            QuestManager.OnObjectiveProgressed -= HandleObjectiveProgressed;
        }

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryUpdated -= HandleInventoryUpdated;
        }

        DestroyPrompt();
    }

    private void HandleQuestCompleted(QuestSO _) => ForceReevaluation();
    private void HandleObjectiveProgressed(ObjectiveSO _, int __, int ___) => ForceReevaluation();
    private void HandleInventoryUpdated() => ForceReevaluation();

    // force re-evaluation only when something relevant changes
    private void ForceReevaluation()
    {
        var tempLogging = enableConditionLogging;
        enableConditionLogging = true; // <--- THIS IS THE KEY

        Debug.Log($"[DEBUG] ForceReevaluation triggered for {gameObject.name} (from event). Current _isInteractable: {_isInteractable}");
        bool old = _isInteractable;
        _isInteractable = EvaluateConditions();
        _conditionsEverEvaluated = true;

        enableConditionLogging = tempLogging;

        if (_isInteractable && !old)
            Debug.Log($"[Interactable] Unlocked: {gameObject.name}");
        else if (!_isInteractable && old)
            Debug.Log($"[Interactable] Locked: {gameObject.name}");
    }

    protected virtual void Update()
    {
        if (QTEGameManager.IsQTEActive) return;

        // Initial evaluation the first time player gets near/hovers
        if (!_conditionsEverEvaluated && (_isInProximity || _isHovered))
            ForceReevaluation();

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
            if (enableConditionLogging)
                Debug.Log($"{name}: No conditions → interactable");
            return true;
        }

        bool allMet = core.ConditionEvaluator.EvaluateUnlockConditions(unlockConditions);

        if (enableConditionLogging || Debug.isDebugBuild)
        {
            Debug.Log($"[Conditions] {name}: Final evaluation result = {allMet}");
            for (int i = 0; i < unlockConditions.Count; i++)
            {
                var cond = unlockConditions[i];
                bool met = false;
                // Re-evaluate individually just for logging
                switch (cond.Type)
                {
                    case ConditionType.QuestCompleted:
                        var q = Resources.Load<QuestSO>("Quests/" + cond.TargetID);
                        met = q != null && QuestMemory.Instance.IsQuestCompleted(q);
                        break;
                    case ConditionType.ObjectiveCompleted:
                        met = QuestMemory.Instance.IsObjectiveCompleted(cond.TargetID);
                        break;
                    case ConditionType.ItemPossessed:
                        var item = ItemDataBase.Instance.GetItemByID(cond.TargetID);
                        met = item != null && InventoryManager.Instance.HasItem(item, cond.RequiredAmount);
                        break;
                    case ConditionType.GameEventTriggered:
                        met = GameEventManager.Instance.IsEventTriggered(cond.TargetID);
                        break;
                }

                string op = (i < unlockConditions.Count - 1) ? cond.NextOperator.ToString() : "";
                Debug.Log($"[Conditions] {name}: [{i}] {cond.Type} '{cond.TargetID}' → {(met ? "TRUE" : "FALSE")} {op}");
            }
        }

        return allMet;
    }

    public virtual void ShowPrompt()
    {
        if (currentPrompt == null && promptPrefab != null)
        {
            currentPrompt = Instantiate(promptPrefab, WorldCanvasManager.Instance.worldCanvas.transform);
            currentPrompt.GetComponent<TMP_Text>().text = interactionText;
            // Reset position and set proper anchoring
            RectTransform rt = currentPrompt.GetComponent<RectTransform>();
            rt.anchoredPosition = Vector2.zero;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
        }
        if (currentPrompt != null)
        {
            Camera mainCamera = Camera.main;  // Camera rendering the game world
            Camera uiCamera = WorldCanvasManager.Instance.worldCanvas.worldCamera;  // UI rendering 

            // Get world position with offset
            Vector3 worldPos = transform.position + promptOffset;
            Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);

            // Convert to canvas space
            RectTransform canvasRect = WorldCanvasManager.Instance.worldCanvas.GetComponent<RectTransform>();
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPos,
                uiCamera,
                out localPoint
            );

            // Set position
            currentPrompt.GetComponent<RectTransform>().anchoredPosition = localPoint;

            // Visibility check
            bool isVisible = (screenPos.z > 0 &&
                              screenPos.x >= 0 && screenPos.x <= Screen.width &&
                              screenPos.y >= 0 && screenPos.y <= Screen.height);

            currentPrompt.SetActive(isVisible);
        }
    }

    public virtual void HidePrompt()
    {
        if (currentPrompt != null)
        {
            currentPrompt.SetActive(false);
        }
    }

    protected virtual void OnDisable()
    {
        DestroyPrompt();
    }

    protected virtual void DestroyPrompt()
    {
        if (currentPrompt != null)
        {
            Destroy(currentPrompt);
            currentPrompt = null;
        }
    }

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

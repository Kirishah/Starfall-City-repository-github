using System.Collections.Generic;
using UnityEngine;
using static QuestSO;


namespace Interaction
{
    [RequireComponent(typeof(InteractionView))]
    public class InteractionPresenter : MonoBehaviour
    {
        [SerializeField] private string _interactionText = "Interact";
        [SerializeField] private string _objectID = " ";
        [SerializeField] protected List<UnlockCondition> unlockConditions = new();

        protected InteractionModel model;
        protected InteractionView view;

        protected bool isInProximity;
        protected bool isHovered;

        protected virtual void Awake()
        {
            model = new InteractionModel(_objectID, _interactionText, unlockConditions);
            view = GetComponent<InteractionView>();
        }

        protected virtual void Start()
        {
            if (QuestManager.Instance != null)
            {
                QuestManager.OnQuestCompleted += ReevaluateConditions;
                QuestManager.OnObjectiveProgressed += ReevaluateConditions;
            }

            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnInventoryUpdated += ReevaluateConditions;
            }

            model.OnInteractabilityChanged += OnInteractabilityChanged;
        }

        protected virtual void OnDestroy()
        {
            if (QuestManager.Instance != null)
            {
                QuestManager.OnQuestCompleted -= ReevaluateConditions;
                QuestManager.OnObjectiveProgressed -= ReevaluateConditions;
            }

            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnInventoryUpdated -= ReevaluateConditions;
            }

            model.OnInteractabilityChanged -= OnInteractabilityChanged;
        }

        private void ReevaluateConditions(QuestSO _) => model.EvaluateConditions();
        private void ReevaluateConditions(ObjectiveSO _, int __, int ___) => model.EvaluateConditions();
        private void ReevaluateConditions() => model.EvaluateConditions();

        private void OnInteractabilityChanged(bool interactable) => UpdateVisibility();

        public void SetProximity(bool state)
        {
            isInProximity = state;
            UpdateVisibility();
        }

        public void SetHovered(bool state)
        {
            isHovered = state;
            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            if (!model.HasBeenEvaluated && (isInProximity || isHovered))
                model.EvaluateConditions();

            if (model.IsInteractable && (isInProximity || isHovered))
                view.ShowPrompt(model.InteractionText);
            else
                view.HidePrompt();
        }

        public virtual void Interact()
        {
            if (!model.IsInteractable)
            {
                Debug.LogWarning($"Cannot interact with {gameObject.name}: Conditions not met.");
                return;
            }

            PerformInteraction();

            if (!string.IsNullOrEmpty(model.ObjectID))
            {
                QuestManager.Instance.HandleObjectiveUpdate(ObjectiveType.Interaction, model.ObjectID);
            }
        }

        protected virtual void PerformInteraction()
        {
            // Override in derived classes
        }

        public virtual string GetIdentifier() => _objectID;
    }
}

using System;
using System.Collections.Generic;
using static QuestSystem.QuestSO;

namespace Interaction
{
    public class InteractionModel
    {
        public string ObjectID { get; }
        public string InteractionText { get; }
        public List<UnlockCondition> UnlockConditions { get; }

        public bool IsInteractable { get; private set; } = true;
        public bool HasBeenEvaluated { get; private set; }

        public event Action<bool> OnInteractabilityChanged;

        public InteractionModel(string objectID, string interactionText, List<UnlockCondition> conditions)
        {
            ObjectID = objectID;
            InteractionText = interactionText;
            UnlockConditions = conditions ?? new List<UnlockCondition>();
        }

        public void EvaluateConditions()
        {
            bool newState = UnlockConditions.Count == 0 ||
                            core.ConditionEvaluator.EvaluateUnlockConditions(UnlockConditions);

            if (newState != IsInteractable || !HasBeenEvaluated)
            {
                IsInteractable = newState;
                HasBeenEvaluated = true;
                OnInteractabilityChanged?.Invoke(newState);
            }
        }
    }
}

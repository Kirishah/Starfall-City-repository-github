using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace QuestSystem
{
    [CreateAssetMenu(menuName = "Quests/Quest")]
    public class QuestSO : ScriptableObject
    {
        public string QuestID;
        public string Title;
        public string Description;
        public ObjectiveSO[] Objectives;
        public Scene[] AssociatedScenes;
        [SerializeField] private int _moneyReward;
        public int MoneyReward => _moneyReward;

        [SerializeField] private string startingDialogueID;
        public string StartingDialogueID => startingDialogueID;

        [System.Serializable]
        public class FollowUpQuest
        {
            [SerializeField] private QuestSO quest;
            [SerializeField] private string dialogueStartID;
            [SerializeField] private List<UnlockCondition> unlockConditions;

            public QuestSO Quest => quest;
            public string DialogueStartID => dialogueStartID;
            public List<UnlockCondition> UnlockConditions => unlockConditions;
        }

        [System.Serializable]
        public class UnlockCondition
        {
            public enum ConditionType
            {
                QuestCompleted,
                ObjectiveCompleted,
                ItemPossessed,
                GameEventTriggered
            }

            public enum LogicOperator
            {
                AND,
                OR
            }

            [SerializeField] private ConditionType type;
            [SerializeField] private string targetID;
            [SerializeField] private int requiredAmount = 1;
            [SerializeField] private LogicOperator nextOperator = LogicOperator.AND;

            public ConditionType Type => type;
            public string TargetID => targetID;
            public int RequiredAmount => requiredAmount;
            public LogicOperator NextOperator => nextOperator;
        }

        [SerializeField]
        private List<FollowUpQuest> followUpQuests = new(); // Initialize to avoid null

        public List<FollowUpQuest> FollowUpQuests => followUpQuests;

        public List<Objective> GetRuntimeObjectives()
        {
            var runtimeObjectives = new List<Objective>();
            foreach (var objectiveSO in Objectives)
            {
                if (objectiveSO != null)
                {
                    runtimeObjectives.Add(objectiveSO.CreateObjective());
                }
            }
            return runtimeObjectives;
        }
    }
}

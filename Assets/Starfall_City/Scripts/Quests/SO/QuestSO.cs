using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Quest;

[CreateAssetMenu(menuName = "Quests/Quest")]
public class QuestSO : ScriptableObject
{
    public string QuestID;
    public string Title;
    public string Description;
    public ObjectiveSO[] Objectives;
    public Scene[] AssociatedScenes;
    [SerializeField] private int experienceReward;
    [SerializeField] private int moneyReward; 

    public int ExperienceReward => experienceReward;
    public int MoneyReward => moneyReward;

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
            ItemPossessed,
            GameEventTriggered
        }

        [SerializeField] private ConditionType type;
        [SerializeField] private string targetID;
        [SerializeField] private int requiredAmount;

        public ConditionType Type => type;
        public string TargetID => targetID;
        public int RequiredAmount => requiredAmount;

        public bool Evaluate()
        {
            switch (Type)
            {
                case ConditionType.QuestCompleted:
                    return QuestMemory.Instance.IsQuestCompleted(Resources.Load<QuestSO>("Quests/" + TargetID));
                case ConditionType.ItemPossessed:
                    Item item = ItemDataBase.Instance.GetItemByID(TargetID);
                    return item != null && InventoryManager.Instance.HasItem(item, RequiredAmount);
                case ConditionType.GameEventTriggered:
                    return GameEventManager.Instance.IsEventTriggered(TargetID);
                default:
                    return false;
            }
        }
    }

    [SerializeField]
    private List<FollowUpQuest> followUpQuests = new List<FollowUpQuest>(); // Initialize to avoid null

    public List<FollowUpQuest> FollowUpQuests => followUpQuests;

    public List<Objective> GetRuntimeObjectives()
    {
        List<Objective> runtimeObjectives = new List<Objective>();
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

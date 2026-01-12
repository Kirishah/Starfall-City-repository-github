using UnityEngine;

namespace QuestSystem
{
    [CreateAssetMenu(menuName = "Quests/Objectives/QTE")]
    public class QTEObjectiveSO : ObjectiveSO
    {
        public string qteID;
        public int requiredSuccessCount;

        public override Objective CreateObjective() => new QTEObjective(this);
    }
}

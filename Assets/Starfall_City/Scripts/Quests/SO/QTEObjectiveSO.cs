using UnityEngine;

namespace QuestSystem
{
    [CreateAssetMenu(menuName = "Quests/Objectives/QTE")]
    public class QTEObjectiveSO : ObjectiveSO
    {
        public string QTEID { get; }
        public int RequiredSuccessCount { get; }

        public override Objective CreateObjective() => new QTEObjective(this);
    }
}

using UnityEngine;

[CreateAssetMenu(menuName = "Quests/Objectives/QTE")]
public class QTEObjectiveSO : ObjectiveSO
{
    public string QTEID;
    public int RequiredSuccessCount;

    public override Objective CreateObjective() => new QTEObjective(this);
}

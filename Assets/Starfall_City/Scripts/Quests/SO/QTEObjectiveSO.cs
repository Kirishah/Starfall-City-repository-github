using UnityEngine;

[CreateAssetMenu(menuName = "Quests/Objectives/QTE")]
public class QTEObjectiveSO : ObjectiveSO
{
    public QTEType RequiredType;
    public int RequiredSuccessCount;

    public override Objective CreateObjective() => new QTEObjective(this);
}

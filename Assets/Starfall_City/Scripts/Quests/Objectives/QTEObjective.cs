using UnityEngine;

public class QTEObjective : Objective
{
    private QTEObjectiveSO _data;
    private int _successCount;

    public QTEObjective(QTEObjectiveSO data) => _data = data;

    public override void CheckProgress(ObjectiveType type, string identifier)
    {
        if (type != ObjectiveType.QTE) return;
        if (identifier != _data.RequiredType.ToString()) return;

        _successCount++;
        if (_successCount >= _data.RequiredSuccessCount)
        {
            Complete();
        }
    }
}

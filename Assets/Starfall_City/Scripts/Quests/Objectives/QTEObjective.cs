using UnityEngine;

public class QTEObjective : Objective
{
    private readonly string _qteID;
    private int _successCount;
    private readonly int _requiredCount;

    public QTEObjective(QTEObjectiveSO data)
    {
        _data = data;  // Base class storage
        _qteID = data.QTEID;
        _requiredCount = data.RequiredSuccessCount;
        _successCount = 0;
    }

    public override void CheckProgress(ObjectiveType type, string identifier)
    {
        if (type == ObjectiveType.QTE && identifier == _qteID)
        {
            _successCount++;
            UpdateProgress(_successCount, _requiredCount);
            if (_successCount >= _requiredCount)
            {
                Complete();
            }
        }
    }
}

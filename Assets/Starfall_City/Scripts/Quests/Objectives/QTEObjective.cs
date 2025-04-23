using UnityEngine;

public class QTEObjective : Objective
{
    private QTEObjectiveSO _qteData;
    private int _successCount;

    public QTEObjective(QTEObjectiveSO data)
    {
        _data = data;  // Base class storage
        _qteData = data;  // Type-specific storage
    }

    public override void CheckProgress(ObjectiveType type, string identifier)
    {
        if (type == ObjectiveType.QTE && identifier == _qteData.RequiredType.ToString())
        {
            _successCount++;
            UpdateProgress(_successCount, _qteData.RequiredSuccessCount);

            if (_successCount >= _qteData.RequiredSuccessCount)
            {
                Complete();
            }
        }
    }
}

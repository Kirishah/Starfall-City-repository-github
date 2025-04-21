using System;
using UnityEngine;

public static class QTEManager
{
    public static void OnQTESuccess(QTEType qteType)
    {
        QuestManager.Instance.HandleObjectiveUpdate(
            ObjectiveType.QTE,
            qteType.ToString()
        );
    }
}

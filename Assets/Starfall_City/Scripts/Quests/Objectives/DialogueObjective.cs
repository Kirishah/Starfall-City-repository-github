using UnityEngine;

public class DialogueObjective : Objective
{
    public DialogueObjective(DialogueSO data)
    {
        _data = data;  // Store in base class field
    }

    public override void CheckProgress(ObjectiveType type, string identifier)
    {
        if (type == ObjectiveType.Dialogue && identifier == ((DialogueSO)_data).TargetNPCID)
        {
            // Report completion progress
            UpdateProgress(1, 1);  // 1/1 required
            Complete();
        }
    }
}

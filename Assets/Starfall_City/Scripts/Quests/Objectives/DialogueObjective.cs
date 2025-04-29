using UnityEngine;

public class DialogueObjective : Objective
{
    public DialogueObjective(DialogueSO data) : base(data)
    {
        _data = data;  
    }

    public override void CheckProgress(ObjectiveType type, string identifier, string itemID)
    {
        
        if (type == ObjectiveType.Dialogue && identifier == ((DialogueSO)_data).TargetNPCID)
        {
            Debug.Log($"DialogueObjective CheckProgress: type={type}, identifier={identifier}, target={((DialogueSO)_data).TargetNPCID}");
            // Report completion progress
            UpdateProgress(1, 1);  // 1/1 required
            Complete();
        }
    }
}

using UnityEngine;

public class DialogueObjective : Objective
{
    private readonly DialogueSO _data;

    public DialogueObjective(DialogueSO data) => _data = data;

    public override void CheckProgress(ObjectiveType type, string identifier)
    {
        if (type == ObjectiveType.Dialogue && identifier == _data.TargetNPCID)
        {
            Complete();
        }
    }
}

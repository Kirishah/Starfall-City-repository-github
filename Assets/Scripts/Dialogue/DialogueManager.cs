using System.Collections.Generic;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    private List<Dialogue> dialogues;
    private Dialogue currentDialogue;

    void Awake()
    {
        if (Instance == null) Instance = this;
        dialogues = GetComponent<DialogueLoader>().LoadDialogues("Assets/DATA/JSON/Dialogue/Dialogue.json");
    }

    public void StartDialogue(int startID)
    {
        currentDialogue = dialogues.Find(d => d.id == startID);
        UIManager.Instance.ShowDialogue(currentDialogue);
    }

    public void SelectChoice(int targetID)
    {
        if (targetID == -1)
        {
            EndDialogue();
            return;
        }
        currentDialogue = dialogues.Find(d => d.id == targetID);
        UIManager.Instance.ShowDialogue(currentDialogue);
    }

    void EndDialogue()
    {
        UIManager.Instance.HideDialogue();
    }
}

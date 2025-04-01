using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;
    public DialogueLoader dialogueLoader;

    private List<Dialogue> dialogues;
    private Dialogue currentDialogue;

    void Awake()
    {
        if (Instance == null) Instance = this;
        string jsonPath = "Dialogue";
        LoadDialogues(jsonPath);
    }

    private void LoadDialogues(string jsonPath)
    {
        if (dialogueLoader != null)
        {
            dialogues = dialogueLoader.LoadDialogues(jsonPath);
        }
        else
        {
            Debug.LogError("No DialogueLoader component found.");
        }
    }

    public void StartDialogue(int startID)
    {
        currentDialogue = FindDialogue(startID);
        if (currentDialogue == null)
        {
            Debug.LogError($"Dialogue with ID {startID} not found!");
            return;
        }
        UIManager.Instance.ShowDialogue(currentDialogue);
    }

    private Dialogue FindDialogue(int id)
    {

        return dialogues?.Find(d => d.id == id);

    }

    public void SelectChoice(int targetID)
    {
        if (targetID == -1)
        {
            EndDialogue();
            return;
        }
        if (dialogues != null)
        {
            currentDialogue = FindDialogue(targetID);

            if (currentDialogue != null && UIManager.Instance != null)
            {
                UIManager.Instance.ShowDialogue(currentDialogue);
            }
            else
            {
                Debug.LogError("Failed to find dialogue or UIManager instance.");
            }
        }
    }

    void EndDialogue()
    {
        UIManager.Instance.HideDialogue();
    }

    public Dialogue GetCurrentDialogue()
    {
        return currentDialogue;
    }

    public void TransferToLocation(int targetLocation)
    {
        // Let GameManager handle the scene transition
        GameManager.Instance.LoadSceneWithTransition(targetLocation);
    }
}

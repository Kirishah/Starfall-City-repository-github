using System.Collections;
using System.Collections.Generic;
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
        currentDialogue = dialogues.Find(d => d.id == startID);
        if (currentDialogue == null)
        {
            Debug.LogError($"Dialogue with ID {startID} not found!");
            return;
        }
        UIManager.Instance.ShowDialogue(currentDialogue);
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
            currentDialogue = dialogues.Find(d => d.id == targetID);

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
        // Load the target scene or activate/deactivate GameObjects as needed
        StartCoroutine(LoadSceneAsync(targetLocation));

    }

    private IEnumerator LoadSceneAsync(int targetLocation)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(targetLocation);
        operation.allowSceneActivation = false; // Optional: Control when to activate

        while (!operation.isDone)
        {
            Debug.Log("Loading progress: " + operation.progress);

            // Optional: Activate scene when progress reaches 0.9 (90%)
            if (operation.progress >= 0.9f)
            {
                operation.allowSceneActivation = true;
            }

            yield return null; // Wait for next frame
        }
    }

    


}

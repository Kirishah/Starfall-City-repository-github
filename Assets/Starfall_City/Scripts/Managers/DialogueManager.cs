using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;
    public DialogueLoader dialogueLoader;
    private string _currentNPCID;

    // Event to notify when a dialogue line is displayed
    public delegate void DialogueLineDisplayedHandler(string dialogueID, string npcID);
    public static event DialogueLineDisplayedHandler OnDialogueLineDisplayed;

    [Header("UI Components")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Transform choiceContainer;
    [SerializeField] private GameObject choiceButtonPrefab;

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

    public void StartDialogue(string startID, string npcID)
    {
        _currentNPCID = npcID;
        currentDialogue = FindDialogue(startID);
        if (currentDialogue == null)
        {
            Debug.LogError($"Dialogue with ID {startID} not found!");
            return;
        }
        ShowDialogue(currentDialogue);
    }

    private Dialogue FindDialogue(string id)
    {

        return dialogues?.Find(d => d.id == id);

    }

    public void SelectChoice(string targetID)
    {
        if (targetID == "-1")
        {
            EndDialogue();
            return;
        }
        if (dialogues != null)
        {
            currentDialogue = FindDialogue(targetID);

            if (currentDialogue != null && UIManager.Instance != null)
            {
                ShowDialogue(currentDialogue);
            }
            else
            {
                Debug.LogError("Failed to find dialogue or UIManager instance.");
            }
        }
    }

    void EndDialogue()
    {
        HideDialogue();
        // Notify QuestManager when dialogue ends
        if (!string.IsNullOrEmpty(_currentNPCID))
        {
            QuestManager.Instance.HandleObjectiveUpdate(ObjectiveType.Dialogue, _currentNPCID);
            Debug.Log($"Dialogue ended: NPCID={_currentNPCID}");
        }
    }

    public void ShowDialogue(Dialogue dialogue)
    {
        dialoguePanel.SetActive(true);
        if (dialogue == null)
        {
            Debug.LogError("Dialogue is null!");
            return;
        }

        if (speakerText == null || dialogueText == null)
        {
            Debug.LogError("TMP_Text fields are not assigned in the Inspector!");
            return;
        }

        speakerText.text = !string.IsNullOrEmpty(dialogue.speaker) ? dialogue.speaker : "Unknown";
        dialogueText.text = dialogue.text;

        // Notify listeners that a dialogue line is displayed
        OnDialogueLineDisplayed?.Invoke(dialogue.id, _currentNPCID);

        // Clear existing choices
        foreach (Transform child in choiceContainer) Destroy(child.gameObject);

        // Add new choices
        if (dialogue.choices != null && dialogue.choices.Count > 0)
        {
            foreach (Choice choice in dialogue.choices)
            {
                GameObject btn = Instantiate(choiceButtonPrefab, choiceContainer);
                btn.GetComponentInChildren<TMP_Text>().text = choice.text;
                btn.GetComponent<Button>().onClick.AddListener(() => SelectChoice(choice.targetID));
            }
        }
        else
        {
            // Add a "Continue" button if no choices
            GameObject btn = Instantiate(choiceButtonPrefab, choiceContainer);
            btn.GetComponentInChildren<TMP_Text>().text = "Продолжить";
            btn.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (currentDialogue != null)
                {
                    if (currentDialogue.targetLocation != 0)
                    {
                        TransferToLocation(currentDialogue.targetLocation);
                    }
                    else if (currentDialogue.targetID != null)
                    {
                        SelectChoice(currentDialogue.targetID);
                    }
                    else
                    {
                        SelectChoice("-1");
                    }
                }
                else
                {
                    SelectChoice("-1");
                }
            });
        }
    }
    public void HideDialogue()
    {
        dialoguePanel.SetActive(false);
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

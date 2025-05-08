using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class DialogueManager : MonoBehaviour
{
    // Singleton instance
    public static DialogueManager Instance { get; private set; }

    // Dependencies
    [SerializeField] private DialogueLoader dialogueLoader;

    // UI Components
    [Header("UI Components")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Transform choiceContainer;
    [SerializeField] private GameObject choiceButtonPrefab;

    // Dialogue state
    private List<Dialogue> dialogues;
    private Dialogue currentDialogue;
    private string currentNPCID;

    // Event for dialogue line display
    public delegate void DialogueLineDisplayedHandler(string dialogueID, string npcID);
    public static event DialogueLineDisplayedHandler OnDialogueLineDisplayed;

    #region Initialization
    private void Awake()
    {
        InitializeSingleton();
        LoadDialogues("Dialogue");
    }

    private void InitializeSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadDialogues(string jsonPath)
    {
        if (dialogueLoader == null)
        {
            Debug.LogError("DialogueLoader component is missing.");
            return;
        }
        dialogues = dialogueLoader.LoadDialogues(jsonPath);
    }
    #endregion

    #region Dialogue Flow
    public void StartDialogue(string startID, string npcID)
    {
        currentNPCID = npcID;
        currentDialogue = FindDialogue(startID);
        if (currentDialogue == null)
        {
            Debug.LogError($"Dialogue with ID {startID} not found!");
            return;
        }
        ShowDialogue(currentDialogue);
    }

    public void SelectChoice(string targetID)
    {
        if (string.IsNullOrEmpty(targetID))
        {
            EndDialogue();
            return;
        }

        if (targetID.StartsWith("action:"))
        {
            HandleActionChoice(targetID);
        }
        else if (targetID == "-1")
        {
            EndDialogue();
        }
        else
        {
            AdvanceToDialogue(targetID);
        }
    }

    private void HandleActionChoice(string targetID)
    {
        var parts = targetID.Split(':');
        if (parts.Length >= 4 && parts[1] == "GiveItem")
        {
            ProcessGiveItemAction(parts[2], parts[3]);
        }
        else
        {
            Debug.LogError($"Invalid action format: {targetID}");
            EndDialogue();
        }
    }

    private void ProcessGiveItemAction(string itemID, string nextDialogueID)
    {
        Item item = ItemDataBase.Instance.GetItemByID(itemID);
        if (item != null && InventoryManager.Instance.RemoveItem(item, 1))
        {
            QuestManager.Instance.HandleObjectiveUpdate(ObjectiveType.GiveItem, currentNPCID, itemID);
            Debug.Log($"Gave {item.Name} to {currentNPCID}");
            AdvanceToDialogue(nextDialogueID);
        }
        else
        {
            Debug.Log("Failed to give item: not in inventory or invalid item.");
            AdvanceToDialogue("no_item_dialogue");
        }
    }

    private void AdvanceToDialogue(string dialogueID)
    {
        currentDialogue = FindDialogue(dialogueID);
        if (currentDialogue != null)
        {
            ShowDialogue(currentDialogue);
        }
        else
        {
            EndDialogue();
        }
    }

    private void EndDialogue()
    {
        HideDialogue();
        if (!string.IsNullOrEmpty(currentNPCID))
        {
            QuestManager.Instance.HandleObjectiveUpdate(ObjectiveType.Dialogue, currentNPCID);
            Debug.Log($"Dialogue ended: NPCID={currentNPCID}");
        }
    }
    #endregion

    #region UI Management
    public void ShowDialogue(Dialogue dialogue)
    {
        if (!ValidateDialogue(dialogue)) return;

        dialoguePanel.SetActive(true);
        UpdateDialogueUI(dialogue);
        OnDialogueLineDisplayed?.Invoke(dialogue.id, currentNPCID);
        DisplayChoices(dialogue);
    }

    private bool ValidateDialogue(Dialogue dialogue)
    {
        if (dialogue == null)
        {
            Debug.LogError("Dialogue is null!");
            return false;
        }
        if (speakerText == null || dialogueText == null)
        {
            Debug.LogError("TMP_Text fields are not assigned in the Inspector!");
            return false;
        }
        return true;
    }

    private void UpdateDialogueUI(Dialogue dialogue)
    {
        speakerText.text = string.IsNullOrEmpty(dialogue.speaker) ? "Unknown" : dialogue.speaker;
        dialogueText.text = dialogue.text;
    }

    private void DisplayChoices(Dialogue dialogue)
    {
        ClearChoices();
        if (dialogue.choices != null && dialogue.choices.Count > 0)
        {
            CreateChoiceButtons(dialogue.choices);
        }
        else
        {
            CreateContinueButton(dialogue);
        }
    }

    private void ClearChoices()
    {
        foreach (Transform child in choiceContainer)
        {
            Destroy(child.gameObject);
        }
    }

    private void CreateChoiceButtons(List<Choice> choices)
    {
        foreach (Choice choice in choices)
        {
            GameObject button = Instantiate(choiceButtonPrefab, choiceContainer);
            button.GetComponentInChildren<TMP_Text>().text = choice.text;
            button.GetComponent<Button>().onClick.AddListener(() => SelectChoice(choice.targetID));
        }
    }

    private void CreateContinueButton(Dialogue dialogue)
    {
        GameObject button = Instantiate(choiceButtonPrefab, choiceContainer);
        button.GetComponentInChildren<TMP_Text>().text = "Продолжить";
        button.GetComponent<Button>().onClick.AddListener(() => HandleContinueAction(dialogue));
    }

    private void HandleContinueAction(Dialogue dialogue)
    {
        if (dialogue == null)
        {
            SelectChoice("-1");
            return;
        }

        if (dialogue.targetLocation != 0)
        {
            TransferToLocation(dialogue.targetLocation);
            EndDialogue();
        }
        else if (!string.IsNullOrEmpty(dialogue.targetID))
        {
            SelectChoice(dialogue.targetID);
        }
        else
        {
            SelectChoice("-1");
        }
    }

    public void HideDialogue()
    {
        dialoguePanel.SetActive(false);
    }
    #endregion

    #region Utility
    private Dialogue FindDialogue(string id)
    {
        return dialogues?.Find(d => d.id == id);
    }

    public Dialogue GetCurrentDialogue() => currentDialogue;

    public void TransferToLocation(int targetLocation)
    {
        GameManager.Instance.LoadSceneWithTransition(targetLocation);
    }
    #endregion
}

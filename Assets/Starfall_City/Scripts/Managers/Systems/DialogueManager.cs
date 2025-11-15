using QTE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour, QTEGameManager.IRPGComponent
{
    public static DialogueManager Instance { get; private set; }

    [SerializeField] private DialogueLoader dialogueLoader;

    [Header("UI Components")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Transform choiceContainer;
    [SerializeField] private GameObject choiceButtonPrefab;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Image iconImage;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    private List<Dialogue> dialogues;
    private Dialogue currentDialogue;
    private string currentNPCID;

    // Событие для отображения диалоговой строки
    public delegate void DialogueLineDisplayedHandler(string dialogueID, string npcID);
    public static event DialogueLineDisplayedHandler OnDialogueLineDisplayed;

    // Событие для запуска QTE
    public delegate void QTETriggerAction();
    public static event QTETriggerAction OnQTETrigger;

    #region Initialization
    private void Awake()
    {
        InitializeSingleton();
        LoadDialogues("Dialogue");
    }

    private void InitializeSingleton()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
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

    public void SelectChoice(string targetID, bool triggersQTE = false)
    {
        if (triggersQTE)
        {
            OnQTETrigger?.Invoke();
        }

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
        else if (parts.Length >= 4 && parts[1] == "StartQuest")
        {
            QuestSO questSO = Resources.Load<QuestSO>("Quests/" + parts[2]);
            if (questSO != null)
            {
                QuestManager.Instance.StartQuest(questSO);
                AdvanceToDialogue(parts[3]);
            }
            else
            {
                Debug.LogError($"QuestSO not found for StartQuest action: {parts[2]}");
                EndDialogue();
            }
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
        if (audioSource.isPlaying)
        {
            audioSource.Stop(); 
        }
        if (!string.IsNullOrEmpty(currentNPCID))
        {
            QuestManager.Instance.HandleObjectiveUpdate(ObjectiveType.Dialogue, currentNPCID);
            Debug.Log($"Dialogue ended: NPCID={currentNPCID}");
        }
    }
    #endregion

    #region Condition Evaluation
    private bool EvaluateCondition(string condition)
    {
        if (string.IsNullOrEmpty(condition))
            return true;

        Debug.Log($"Evaluating condition for dialogue {currentDialogue?.id}: {condition}");
        // Разделение строки условия на части на основе логических операторов (&&, ||)
        var conditionParts = Regex.Split(condition, @"\s*(&&|\|\|)\s*")
            .Select(part => part.Trim())
            .Where(part => !string.IsNullOrWhiteSpace(part) && part != "&&" && part != "||")
            .ToArray();
        var operators = Regex.Matches(condition, @"\s*(&&|\|\|)\s*")
            .Cast<Match>()
            .Select(match => match.Groups[1].Value)
            .ToList();

        if (conditionParts.Length == 0)
        {
            Debug.LogError($"No valid conditions found in: {condition}");
            return false;
        }

        bool result = EvaluateSingleCondition(conditionParts[0], condition);
        for (int i = 0; i < operators.Count && i + 1 < conditionParts.Length; i++)
        {
            bool nextCondition = EvaluateSingleCondition(conditionParts[i + 1], condition);
            if (operators[i] == "&&")
                result = result && nextCondition;
            else if (operators[i] == "||")
                result = result || nextCondition;
        }

        return result;
    }

    private bool EvaluateCharacteristicCondition(string condition)
    {
        var parts = condition.Split(':');
        if (parts.Length != 3)
        {
            Debug.LogError($"Invalid characteristic condition format: {condition}");
            return false;
        }

        string conditionType = parts[0]; // "Char"
        string charTypeStr = parts[1];
        string requiredValueStr = parts[2];

        if (!Enum.TryParse<CharacteristicType>(charTypeStr, out CharacteristicType charType))
        {
            Debug.LogError($"Unknown characteristic type: {charTypeStr}");
            return false;
        }

        if (!int.TryParse(requiredValueStr, out int requiredValue))
        {
            Debug.LogError($"Invalid required value: {requiredValueStr}");
            return false;
        }

        return CharacteristicsManager.Instance.CheckRequirement(charType, requiredValue);
    }

    private bool EvaluateSingleCondition(string condition, string fullCondition)
    {
        if (string.IsNullOrWhiteSpace(condition))
        {
            Debug.LogError($"Empty condition part in dialogue {currentDialogue?.id}: {fullCondition}");
            return false;
        }

        var parts = condition.Split(':');
        if (parts.Length < 2)
        {
            Debug.LogError($"Invalid condition format in dialogue {currentDialogue?.id}, condition '{fullCondition}': {condition}");
            return false;
        }

        string conditionType = parts[0];

        if (conditionType == "Char")
        {
            return EvaluateCharacteristicCondition(condition);
        }

        switch (conditionType)
        {
            case "QuestCompleted":
                if (parts.Length != 2)
                {
                    Debug.LogError($"QuestCompleted requires 1 parameter in dialogue {currentDialogue?.id}, condition '{fullCondition}': {condition}");
                    return false;
                }
                var questSO = Resources.Load<QuestSO>("Quests/" + parts[1]);
                if (questSO == null)
                {
                    Debug.LogError($"QuestSO not found for QuestCompleted condition in dialogue {currentDialogue?.id}: {parts[1]}");
                    return false;
                }
                return QuestMemory.Instance.IsQuestCompleted(questSO);

            case "HasItem":
                if (parts.Length != 3)
                {
                    Debug.LogError($"HasItem requires 2 parameters in dialogue {currentDialogue?.id}, condition '{fullCondition}': {condition}");
                    return false;
                }
                Item item = ItemDataBase.Instance.GetItemByID(parts[1]);
                if (item == null) return false;
                int requiredAmount;
                if (!int.TryParse(parts[2], out requiredAmount))
                {
                    Debug.LogError($"Invalid amount in HasItem condition in dialogue {currentDialogue?.id}, condition '{fullCondition}': {parts[2]}");
                    return false;
                }
                return InventoryManager.Instance.HasItem(item, requiredAmount);

            case "IsQuestObjectiveActive":
                if (parts.Length != 3)
                {
                    Debug.LogError($"IsQuestObjectiveActive requires 2 parameters in dialogue {currentDialogue?.id}, condition '{fullCondition}': {condition}");
                    return false;
                }
                return IsQuestObjectiveActive(parts[1], parts[2]);

            case "GameEventTriggered":
                if (parts.Length != 2)
                {
                    Debug.LogError($"GameEventTriggered requires 1 parameter in dialogue {currentDialogue?.id}, condition '{fullCondition}': {condition}");
                    return false;
                }
                return GameEventManager.Instance.IsEventTriggered(parts[1]);

            default:
                Debug.LogError($"Unknown condition type in dialogue {currentDialogue?.id}, condition '{fullCondition}': {conditionType}");
                return false;
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

        // Play audio if specified
        if (!string.IsNullOrEmpty(dialogue.audio))
        {
            AudioClip clip = Resources.Load<AudioClip>(dialogue.audio);
            if (clip != null)
            {
                audioSource.Stop(); // Stop any currently playing audio
                audioSource.clip = clip;
                audioSource.Play();
            }
            else
            {
                Debug.LogError($"Audio clip not found: {dialogue.audio}");
            }
        }
    }

    private bool ValidateDialogue(Dialogue dialogue)
    {
        if (dialogue == null)
        {
            Debug.LogError("Dialogue is null!");
            return false;
        }
        if (speakerText == null || dialogueText == null || descriptionText == null || iconImage == null)
        {
            Debug.LogError("TMP_Text fields or Image are not assigned in the Inspector!");
            return false;
        }
        return true;
    }

    private void UpdateDialogueUI(Dialogue dialogue)
    {
        speakerText.text = string.IsNullOrEmpty(dialogue.speaker) ? "Unknown" : dialogue.speaker;
        dialogueText.text = string.IsNullOrEmpty(dialogue.text) ? "" : dialogue.text;

        if (!string.IsNullOrEmpty(dialogue.description))
        {
            descriptionText.text = dialogue.description;
        }
        else
        {
            descriptionText.text = "";
        }

        if (!string.IsNullOrEmpty(dialogue.iconPath))
        {
            Sprite iconSprite = Resources.Load<Sprite>(dialogue.iconPath);
            if (iconSprite != null)
            {
                iconImage.sprite = iconSprite;
                iconImage.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogWarning($"Icon sprite not found at path: {dialogue.iconPath}");
                iconImage.gameObject.SetActive(false);
            }
        }
        else
        {
            iconImage.gameObject.SetActive(false);
        }
    }

    private void DisplayChoices(Dialogue dialogue)
    {
        ClearChoices();
        if (dialogue.choices != null && dialogue.choices.Count > 0)
        {
            foreach (var choice in dialogue.choices)
            {
                if (ShouldShowChoice(choice))
                {
                    GameObject button = Instantiate(choiceButtonPrefab, choiceContainer);
                    button.GetComponentInChildren<TMP_Text>().text = choice.text;
                    button.GetComponent<Button>().onClick.AddListener(() => SelectChoice(choice.targetID, choice.triggersQTE));
                }
            }
        }
        else
        {
            CreateContinueButton(dialogue);
        }
    }

    private bool ShouldShowChoice(Choice choice)
    {
        // Чек если у выбора есть условие, и оно не выполнено
        if (choice.condition != null)
        {
            Debug.Log($"Evaluating condition for choice '{choice.text}' in dialogue {currentDialogue?.id}: {choice.condition}");
            return EvaluateCondition(choice.condition);
        }
        return true;
    }

    private void ClearChoices()
    {
        foreach (Transform child in choiceContainer)
        {
            Destroy(child.gameObject);
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

    public bool IsQuestObjectiveActive(string npcID, string itemID)
    {
        var activeQuests = QuestManager.Instance.GetActiveQuests();
        foreach (var quest in activeQuests)
        {
            foreach (var objective in quest.GetCurrentObjective() != null ? new[] { quest.GetCurrentObjective() } : new Objective[0])
            {
                if (objective.Type == ObjectiveType.GiveItem && objective is GiveItemObjective giveItemObjective)
                {
                    if (giveItemObjective.TargetNPCID == npcID && giveItemObjective.TargetItemID == itemID && !objective.IsCompleted)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }
    #endregion
}

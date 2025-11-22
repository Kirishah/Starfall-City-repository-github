using core;
using QTE;
using System.Collections.Generic;
using UnityEngine;

public class DialogueManager_UIToolkit : MonoBehaviour, QTEGameManager.IRPGComponent
{
    public static DialogueManager_UIToolkit Instance { get; private set; }

    [SerializeField] private DialogueLoader dialogueLoader;

    [Header("UI Handler")]
    [SerializeField] private DialogueUI_Toolkit uiHandler;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    private List<Dialogue> dialogues;
    private Dialogue currentDialogue;
    private string currentNPCID;
    public string currentStartID {  get; private set; } 
    private int currentDeltaPoints;

    // Событие для отображения диалоговой строки
    public delegate void DialogueLineDisplayedHandler(string dialogueID, string npcID);
    public static event DialogueLineDisplayedHandler OnDialogueLineDisplayed;
    public static System.Action OnDialogueStarted;
    public static System.Action OnDialogueEnded;

    // Событие для запуска QTE
    public delegate void QTETriggerAction();
    public static event QTETriggerAction OnQTETrigger;

    #region Initialization
    private void Awake()
    {
        InitializeSingleton();
        if (uiHandler == null)
        {
            Debug.LogError("DialogueUI_Toolkit not found! Assign in Inspector.");
        }
        LoadDialogues("Dialogue");
        uiHandler.turnOffPicking();
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
        currentStartID = startID;
        currentNPCID = npcID;
        currentDeltaPoints = 0;
        currentDialogue = FindDialogue(startID);
        if (currentDialogue == null)
        {
            Debug.LogError($"Dialogue with ID {startID} not found!");
            return;
        }
        uiHandler.ClearHistory();
        uiHandler.ShowPanel();
        ShowDialogue(currentDialogue);

        OnDialogueStarted?.Invoke();
    }

    // Keep the old 3-param overload for Continue button 
    public void SelectChoice(string choiceText, string targetID, bool triggersQTE = false)
    {
        SelectChoice(choiceText, targetID, triggersQTE, 0);
    }

    // 4-ARG OVERLOAD: For actual choice buttons (with deltaPoints)
    public void SelectChoice(string choiceText, string targetID, bool triggersQTE, int deltaPoints)
    {
        // Append player choice to history if applicable
        if (!string.IsNullOrEmpty(choiceText))
        {
            uiHandler.AddToHistory("You", choiceText, isPlayer: true);
        }

        // Accumulate points from this choice
        currentDeltaPoints += deltaPoints;

        Choice selectedChoice = currentDialogue?.choices?.Find(c =>
            c.text == choiceText &&
            (c.targetID ?? "") == (targetID ?? ""));

        if (selectedChoice != null && !string.IsNullOrEmpty(selectedChoice.publishEvent))
        {
            var dict = BuildParamsFromChoice(selectedChoice);
            if (dict?.Count > 0)
                EventBus.Instance.Publish(selectedChoice.publishEvent, dict);
            else
                EventBus.Instance.Publish(selectedChoice.publishEvent);

            Debug.Log($"[Dialogue] Published event from choice: {selectedChoice.publishEvent}");
        }

        if (triggersQTE) OnQTETrigger?.Invoke();

        if (string.IsNullOrEmpty(targetID) || targetID == "-1")
        {
            EndDialogue();
            return;
        }

        if (targetID.StartsWith("action:"))
            HandleActionChoice(targetID);
        else
            AdvanceToDialogue(targetID);
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
                Debug.Log($"Branch action started quest '{questSO.Title}' from dialogue '{currentStartID}' (dialogue node).");

                string nextID = parts[3];
                if (string.IsNullOrEmpty(nextID) || nextID == "-1")
                {
                    EndDialogue();  // Preserves currentDialogue → honors skipAutoStart=true
                }
                else
                {
                    AdvanceToDialogue(nextID);
                }
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
            if (string.IsNullOrEmpty(nextDialogueID) || nextDialogueID == "-1")
            {
                EndDialogue();  // Preserves currentDialogue
            }
            else
            {
                AdvanceToDialogue(nextDialogueID);
            }
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
        uiHandler.HidePanel();
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        if (!string.IsNullOrEmpty(currentNPCID))
        {
            QuestManager.Instance.HandleObjectiveUpdate(ObjectiveType.Dialogue, currentNPCID);
            Debug.Log($"Dialogue ended: NPCID={currentNPCID}");
        }

        // ALWAYS apply effects before any early returns
        string effectType = GetEffectType();
        Debug.Log($"EndDialogue: Applying effects - effectType='{effectType}', totalPoints={currentDeltaPoints}");  
        EffectsManager.Instance?.ApplyPoints(effectType, currentDeltaPoints);
        currentDeltaPoints = 0;


        if (!string.IsNullOrEmpty(currentStartID))
        {
            // Only skip if the CURRENT node (not the start node!) has skipAutoStart = true
            bool skipThisTime = currentDialogue?.skipAutoStart ?? false;

            if (!skipThisTime)
            {
                QuestSO questToStart = FindQuestByStartingDialogue(currentStartID);
                if (questToStart != null &&
                    !QuestManager.Instance.IsQuestActive(questToStart) &&
                    !QuestMemory.Instance.IsQuestCompleted(questToStart))
                {
                    QuestManager.Instance.StartQuest(questToStart);
                    Debug.Log($"AUTO-STARTED quest '{questToStart.Title}' from dialogue '{currentStartID}'");
                }
            }
            else
            {
                Debug.Log($"skipAutoStart=true on current node → no auto-start this time");
            }
        }

        OnDialogueEnded?.Invoke();
        currentStartID = null;
    }
    #endregion

    #region Dialogue Display
    public void ShowDialogue(Dialogue dialogue)
    {
        if (!ValidateDialogue(dialogue)) return;

        uiHandler.UpdateSpeakerAndIcon(dialogue.speaker, dialogue.iconPath);

        // Append to history
        if (!string.IsNullOrEmpty(dialogue.description))
        {
            uiHandler.AddToHistory("", dialogue.description, isNarrative: true);
        }
        if (!string.IsNullOrEmpty(dialogue.speaker) && !string.IsNullOrEmpty(dialogue.text))
        {
            uiHandler.AddToHistory(dialogue.speaker, dialogue.text);
        }

        OnDialogueLineDisplayed?.Invoke(dialogue.id, currentNPCID);

        // Display choices or continue
        uiHandler.DisplayChoices(
             dialogue.choices,
             dialogue,
            (text, target, qte, delta) => SelectChoice(text, target, qte, delta)
        );

        if (dialogue.choices == null || dialogue.choices.Count == 0)
        {
            uiHandler.ShowContinueButton(() => HandleContinueAction(dialogue));
        }

        // Audio
        if (!string.IsNullOrEmpty(dialogue.audio))
        {
            AudioClip clip = Resources.Load<AudioClip>(dialogue.audio);
            if (clip != null)
            {
                audioSource.Stop();
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
        if (dialogue == null || uiHandler == null)
        {
            Debug.LogError(dialogue == null ? "Dialogue is null!" : "DialogueUI_Toolkit not assigned!");
            return false;
        }
        return true;
    }

    private void HandleContinueAction(Dialogue d)
    {
        if (d.targetLocation != 0)
        {
            TransferToLocation(d.targetLocation);
            EndDialogue();
            return;
        }

        if (!string.IsNullOrEmpty(d.targetID))
        {
            if (d.targetID.StartsWith("action:"))
            {
                HandleActionChoice(d.targetID);
            }
            else
            {
                AdvanceToDialogue(d.targetID);
            }
            return;
        }

        EndDialogue();
    }
    #endregion

    #region Utility
    private Dialogue FindDialogue(string id)
    {
        return dialogues?.Find(d => d.id == id);
    }

    public void TransferToLocation(int targetLocation)
    {
        GameManager.Instance.LoadSceneWithTransition(targetLocation);
    }

    private QuestSO FindQuestByStartingDialogue(string startingDialogueID)
    {
        QuestSO[] allQuests = Resources.LoadAll<QuestSO>("Quests");
        foreach (var quest in allQuests)
        {
            if (quest.StartingDialogueID == startingDialogueID)
            {
                return quest;
            }
        }
        return null;
    }

    private string GetEffectType()
    {
        var startDialogue = FindDialogue(currentStartID);
        return startDialogue?.effectType ?? currentDialogue?.effectType ?? "";
    }

    private Dictionary<string, object> BuildParamsFromChoice(Choice choice)
    {
        if (choice.eventParams == null || choice.eventParams.Count == 0)
            return null;

        var dict = new Dictionary<string, object>();
        foreach (var p in choice.eventParams)
        {
            switch (p.type)
            {
                case ParameterType.String:
                    dict[p.key] = p.stringValue;
                    break;
                case ParameterType.GameObject:
                    dict[p.key] = p.objectValue;
                    break;
                case ParameterType.Vector3:
                    dict[p.key] = p.vectorValue;
                    break;
            }
        }
        return dict;
    }
    #endregion
}
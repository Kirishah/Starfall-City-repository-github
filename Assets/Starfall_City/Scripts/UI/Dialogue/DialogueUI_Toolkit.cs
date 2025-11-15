using core;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;
using UnityEngine.UIElements;

public class DialogueUI_Toolkit : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    private List<HistoryEntry> history = new List<HistoryEntry>();

    private VisualElement GetRoot() => uiDocument.rootVisualElement.Q<VisualElement>("Root");
    private VisualElement GetPanel() => uiDocument.rootVisualElement.Q<VisualElement>("DialoguePanel");
    private VisualElement GetHistoryContent() => GetPanel().Q<VisualElement>("HistoryContent");
    private ScrollView GetHistoryScroll() => GetPanel().Q<ScrollView>("HistoryScrollView");
    private Label GetCharName() => GetPanel().Q<Label>("CharName");
    private Image GetIcon() => GetPanel().Q<Image>("Icon");
    private VisualElement GetChoiceContainer() => GetPanel().Q<VisualElement>("ChoiceContainer");

    public void turnOffPicking() => GetRoot().pickingMode = PickingMode.Ignore;
    public void ShowPanel()
    {
        GetPanel().style.display = DisplayStyle.Flex;
        GetPanel().pickingMode = PickingMode.Ignore;
    }
    public void HidePanel() => GetPanel().style.display = DisplayStyle.None;

    public void UpdateSpeakerAndIcon(string speaker, string iconPath)
    {
        GetCharName().text = string.IsNullOrEmpty(speaker) ? "Unknown" : speaker;
        var iconElement = GetIcon();
        iconElement.image = null; // Clear old
        iconElement.style.display = DisplayStyle.None; // Default hide
        if (!string.IsNullOrEmpty(iconPath))
        {
            Debug.Log($"Attempting to load sprite from: {iconPath}");
            var sprite = Resources.Load<Sprite>(iconPath);
            if (sprite != null)
            {
                Debug.Log($"Loaded icon successfully: {iconPath}");
                iconElement.image = sprite.texture;
                iconElement.style.unitySliceScale = 1f;
                iconElement.style.display = DisplayStyle.Flex;
            }
            else
            {
                iconElement.style.display = DisplayStyle.None;
                Debug.LogWarning($"Failed to load sprite from: {iconPath} (Check: File exists in Resources/Icons/? Import as Sprite? Path exact?)");
            }
        }
        else
        {
            iconElement.style.display = DisplayStyle.None;
            Debug.LogWarning("iconPath is null or empty—skipping load.");
        }
    }

    public void AddToHistory(string speaker, string text, bool isNarrative = false, bool isPlayer = false)
    {
        history.Add(new HistoryEntry(speaker, text, isNarrative, isPlayer));
        var label = new Label((string.IsNullOrEmpty(speaker) ? "" : speaker + ": ") + text);
        label.AddToClassList("history-entry");
        if (isNarrative) label.AddToClassList("history-narrative");
        if (isPlayer) label.AddToClassList("history-player");
        GetHistoryContent().Add(label);
        // Scroll after one frame
        StartCoroutine(ScrollToBottom());
    }

    private IEnumerator ScrollToBottom()
    {
        yield return null;
        var scroll = GetHistoryScroll();
        scroll.scrollOffset = new Vector2(0, scroll.contentContainer.resolvedStyle.height);
    }

    public void DisplayChoices(List<Choice> choices, Dialogue current, System.Action<string, string, bool, int> onChoiceSelected)
    {
        var container = GetChoiceContainer();
        container.Clear();
        if (choices != null && choices.Count > 0)
        {
            foreach (var choice in choices)
            {
                if (choice.condition == null || ConditionEvaluator.Evaluate(choice.condition, current))
                {
                    var btn = new Button { text = choice.text };
                    btn.AddToClassList("choice-button");
                    btn.clicked += () => onChoiceSelected(choice.text, choice.targetID, choice.triggersQTE, choice.deltaPoints);
                    container.Add(btn);
                }
            }
        }
    }

    public void ShowContinueButton(System.Action onClick)
    {
        var container = GetChoiceContainer();
        container.Clear();
        var btn = new Button { text = "Continue" };
        btn.AddToClassList("choice-button");
        btn.clicked += () => onClick?.Invoke();
        container.Add(btn);
    }

    public void ClearHistory()
    {
        history.Clear();
        GetHistoryContent().Clear(); // Remove all rendered labels
    }
}

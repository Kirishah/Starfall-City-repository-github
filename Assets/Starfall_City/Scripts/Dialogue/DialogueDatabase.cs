using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DialogueDatabase : MonoBehaviour
{
    public static DialogueDatabase Instance { get; private set; }

    [SerializeField] private DialogueLoader loader;
    [SerializeField] private string dialogueJsonPath = "Dialogue"; // Resources folder

    private List<Dialogue> _dialogues;
    private Dictionary<string, Dialogue> _dialogueById; // For fast lookup

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        LoadDialogues();
    }

    private void LoadDialogues()
    {
        if (loader == null)
        {
            Debug.LogError("DialogueLoader not assigned in DialogueDatabase!");
            return;
        }

        _dialogues = loader.LoadDialogues(dialogueJsonPath);

        if (_dialogues == null || _dialogues.Count == 0)
        {
            Debug.LogError("No dialogues loaded!");
            return;
        }

        // Build fast lookup dictionary
        _dialogueById = _dialogues.ToDictionary(d => d.id, d => d);

        Debug.Log($"DialogueDatabase: Loaded {_dialogues.Count} dialogues.");
    }

    public Dialogue GetDialogue(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;

        _dialogueById.TryGetValue(id, out var dialogue);
        if (dialogue == null)
        {
            Debug.LogWarning($"Dialogue with ID '{id}' not found!");
        }
        return dialogue;
    }

    public IReadOnlyList<Dialogue> GetAllDialogues() => _dialogues.AsReadOnly();
}

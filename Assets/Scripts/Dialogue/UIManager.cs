using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public GameObject dialoguePanel;
    public TMP_Text speakerText;
    public TMP_Text dialogueText;
    public Transform choiceContainer;
    public GameObject choiceButtonPrefab;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void ShowDialogue(Dialogue dialogue)
    {
        dialoguePanel.SetActive(true);
        speakerText.text = dialogue.speaker;
        dialogueText.text = dialogue.text;

        // Clear existing choices
        foreach (Transform child in choiceContainer) Destroy(child.gameObject);

        // Add new choices
        if (dialogue.choices != null && dialogue.choices.Count > 0)
        {
            foreach (Choice choice in dialogue.choices)
            {
                GameObject btn = Instantiate(choiceButtonPrefab, choiceContainer);
                btn.GetComponentInChildren<TMP_Text>().text = choice.text;
                btn.GetComponent<Button>().onClick.AddListener(() => {
                    DialogueManager.Instance.SelectChoice(choice.targetID);
                });
            }
        }
        else
        {
            // Add a "Continue" button if no choices
            GameObject btn = Instantiate(choiceButtonPrefab, choiceContainer);
            btn.GetComponentInChildren<TMP_Text>().text = "Continue";
            btn.GetComponent<Button>().onClick.AddListener(() => {
                DialogueManager.Instance.SelectChoice(-1); // End dialogue
            });
        }
    }

    public void HideDialogue()
    {
        dialoguePanel.SetActive(false);
    }
}

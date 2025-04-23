using NUnit.Framework.Interfaces;
using TMPro;
using UnityEngine;

public class QuestEntryUI : MonoBehaviour
{
    public QuestSO QuestData { get; private set; }
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _descriptionText;
    [SerializeField] private Transform _objectivesContainer;
    [SerializeField] private ObjectiveDisplay _objectivePrefab;

    public void Initialize(QuestSO questData)
    {
        QuestData = questData;
        _titleText.text = questData.Title;
        _descriptionText.text = questData.Description;

        foreach (var objective in questData.Objectives)
        {
            var display = Instantiate(_objectivePrefab, _objectivesContainer);
            display.Initialize(objective);
        }
    }
}

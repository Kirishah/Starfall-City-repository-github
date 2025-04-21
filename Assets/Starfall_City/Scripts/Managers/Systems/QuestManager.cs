using System.Collections.Generic;
using UnityEngine;
using static Quest;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    private List<Quest> _activeQuests = new List<Quest>();
    private Queue<Quest> _questPoolQueue = new Queue<Quest>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePool(10); // Initial pool size
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializePool(int capacity)
    {
        for (int i = 0; i < capacity; i++)
        {
            Quest quest = new Quest();
            _questPoolQueue.Enqueue(quest);
        }
    }

    public void StartQuest(QuestSO questSO)
    {
        if (_questPoolQueue.Count == 0) InitializePool(3);

        Quest quest = _questPoolQueue.Dequeue();
        quest.Initialize(questSO);
        _activeQuests.Add(quest);
        quest.StartQuest();
    }

    public void CompleteQuest(Quest quest)
    {
        quest.Cleanup();
        _activeQuests.Remove(quest);
        _questPoolQueue.Enqueue(quest);
    }

    // Called from other systems via events
    public void HandleObjectiveUpdate(ObjectiveType type, string identifier)
    {
        foreach (Quest quest in _activeQuests)
        {
            quest.ProcessObjectiveEvent(type, identifier);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestUI : MonoBehaviour
{
    public static QuestUI Instance { get; private set; }
    [Header("References")]
    [SerializeField] private GameObject _questLogPanel;
    [SerializeField] private Transform _activeQuestsContainer;
    [SerializeField] private QuestEntryUI _questEntryPrefab;
    [SerializeField] private GameObject _notificationBar;
    [SerializeField] private TextMeshProUGUI _objectiveNotificationText;


    [Header("Settings")]
    [SerializeField] private float _notificationDuration = 3f;
    [SerializeField] private AudioClip _newQuestSound;
    [SerializeField] private AudioClip _objectiveCompleteSound;

    private Queue<string> _notificationQueue = new Queue<string>();
    private bool _isShowingNotification;
    private AudioSource _audioSource;
    private Animator _notificationAnimator;
    private CanvasGroup _notificationCanvasGroup;

    void Awake()
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
        _audioSource = GetComponent<AudioSource>();
        _questLogPanel.SetActive(false);

        // Get required components with null checks
        _notificationAnimator = _objectiveNotificationText.GetComponent<Animator>();
        _notificationCanvasGroup = _objectiveNotificationText.GetComponent<CanvasGroup>();

        if (_notificationBar != null)
        {
            _notificationBar.SetActive(false);
        }

        if (!_notificationCanvasGroup)
        {
            _notificationCanvasGroup = _objectiveNotificationText.gameObject.AddComponent<CanvasGroup>();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            QuestUI.Instance.ToggleQuestLog();
        }
    }

    void OnEnable()
    {
        QuestManager.OnQuestStarted += HandleQuestStarted;
        QuestManager.OnQuestCompleted += HandleQuestCompleted;
        QuestManager.OnObjectiveProgressed += HandleObjectiveProgressed;
    }

    void OnDisable()
    {
        QuestManager.OnQuestStarted -= HandleQuestStarted;
        QuestManager.OnQuestCompleted -= HandleQuestCompleted;
        QuestManager.OnObjectiveProgressed -= HandleObjectiveProgressed;
    }

    private void HandleQuestStarted(QuestSO quest)
    {
        // Instantiate new quest entry
        if (_activeQuestsContainer == null || _questEntryPrefab == null) return;

        var entry = Instantiate(_questEntryPrefab, _activeQuestsContainer);
        entry.Initialize(quest);

        PlaySound(_newQuestSound);
        ShowNotification($"New Quest: {quest.Title}");
    }

    private void HandleQuestCompleted(QuestSO quest)
    {
        ShowNotification($"Quest Complete: {quest.Title}");
        RemoveQuestFromUI(quest);
        if (_questLogPanel.activeSelf)
        {
            RefreshQuestLog();
        }
        // You could move the entry to a completed quests section here
    }

    private void HandleObjectiveProgressed(ObjectiveSO objective, int current, int required)
    {
        PlaySound(_objectiveCompleteSound);
        ShowNotification($"{objective.Description} ({current}/{required})");
        if (_questLogPanel.activeSelf)
        {
            RefreshQuestLog();
        }
    }

    private void ShowNotification(string message)
    {
        if (_objectiveNotificationText == null) return;

        if (_notificationBar != null)
        {
            _notificationBar.SetActive(true);
        }
        _notificationQueue.Enqueue(message);
        if (!_isShowingNotification)
        {
            StartCoroutine(NotificationRoutine());
        }
    }

    private IEnumerator NotificationRoutine()
    {
        _isShowingNotification = true;

        while (_notificationQueue.Count > 0)
        {
            if (_notificationAnimator != null)
            {
                _objectiveNotificationText.text = _notificationQueue.Dequeue();
                _notificationAnimator.SetBool("Show", true);

                yield return new WaitForSeconds(_notificationDuration);

                _notificationAnimator.SetBool("Show", false);
                yield return new WaitForSeconds(0.2f); // Wait for fade-out animation
            }
            else
            {
                // Fallback if animator fails
                _objectiveNotificationText.text = _notificationQueue.Dequeue();
                yield return new WaitForSeconds(_notificationDuration);
                _objectiveNotificationText.text = string.Empty;
            }
        }

        if (_notificationBar != null)
        {
            _notificationBar.SetActive(false);
        }
        _isShowingNotification = false;
    }

    public void ToggleQuestLog()
    {
        if (_questLogPanel == null) return;

        _questLogPanel.SetActive(!_questLogPanel.activeSelf);

        if (_questLogPanel.activeSelf)
        {
            RefreshQuestLog();
        }
    }

    private void RefreshQuestLog()
    {
        if (_activeQuestsContainer == null) return;
        // Clear existing entries
        foreach (Transform child in _activeQuestsContainer)
        {
            Destroy(child.gameObject);
        }

        // Get updated quest list
        var activeQuests = QuestManager.Instance.GetActiveQuests();
        if (activeQuests == null) return;

        // Repopulate entries
        foreach (var quest in activeQuests)
        {
            if (quest == null) continue;

            var entry = Instantiate(_questEntryPrefab, _activeQuestsContainer);
            entry.Initialize(quest.Data);
        }
    }

    private void RemoveQuestFromUI(QuestSO quest)
    {
        if (_activeQuestsContainer == null) return;

        foreach (Transform child in _activeQuestsContainer)
        {
            var entry = child.GetComponent<QuestEntryUI>();
            if (entry != null && entry.QuestData == quest)
            {
                Destroy(child.gameObject);
                break;
            }
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(clip);
        }
    }
}

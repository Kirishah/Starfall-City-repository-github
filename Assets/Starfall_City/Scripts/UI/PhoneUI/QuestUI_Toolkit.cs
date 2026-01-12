using System.Collections;
using System.Collections.Generic;
using Interaction;
using UnityEngine;
using UnityEngine.UIElements;

namespace QuestSystem
{
    public class QuestUI_Toolkit : MonoBehaviour
    {
        public static QuestUI_Toolkit Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private VisualTreeAsset _questEntryUXML;
        [SerializeField] private VisualTreeAsset _objectiveDisplayUXML;
        [SerializeField] private Sprite _completeIcon;
        [SerializeField] private Sprite _incompleteIcon;
        [SerializeField] private AudioClip _newQuestSound;
        [SerializeField] private AudioClip _objectiveCompleteSound;

        [Header("Global Notifications")]
        [SerializeField] private VisualTreeAsset _globalNotificationUXML;
        [SerializeField] private UIDocument _mainUIDocument;
        private VisualElement _globalNotificationRoot;
        private VisualElement _globalNotificationBar;
        private Label _globalNotificationLabel;

        [Header("Settings")]
        [SerializeField] private float _notificationDuration = 3f;

        private VisualElement _root;
        private ScrollView _activeQuestsContainer;

        private readonly Queue<string> _notificationQueue = new();
        private bool _isShowingNotification;
        private AudioSource _audioSource;
        private readonly Dictionary<QuestSO, GameObject> _questEntryGameObjects = new(); // Track GOs for cleanup
        private readonly Dictionary<QuestSO, VisualElement> _questEntryElements = new(); // Same
        private bool _isUIInitialized = false;

        // Public read-only access for other scripts
        public VisualTreeAsset QuestEntryUXML => _questEntryUXML;
        public VisualTreeAsset ObjectiveDisplayUXML => _objectiveDisplayUXML;
        public Sprite CompleteIcon => _completeIcon;
        public Sprite IncompleteIcon => _incompleteIcon;
        public AudioClip NewQuestSound => _newQuestSound;
        public AudioClip ObjectiveCompleteSound => _objectiveCompleteSound;
        public VisualTreeAsset GlobalNotificationUXML => _globalNotificationUXML;
        public UIDocument MainUIDocument => _mainUIDocument;
        public float NotificationDuration => _notificationDuration;


        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _audioSource = GetComponent<AudioSource>();
        }

        public bool IsInitialized() => _isUIInitialized;

        public void Initialize(VisualElement uiRoot)
        {
            _root = uiRoot;
            if (_root == null)
            {
                Debug.LogError("QuestUI: rootVisualElement is null!");
                return;
            }

            _activeQuestsContainer = _root.Q<ScrollView>("ActiveQuestsContainer");

            if (_activeQuestsContainer == null)
            {
                Debug.LogError("QuestUI: ActiveQuestsContainer not found in UXML!");
                // Try to find it by other common names
                _activeQuestsContainer = _root.Q<ScrollView>("QuestList");
                if (_activeQuestsContainer == null)
                {
                    Debug.LogError("QuestUI: Could not find quests container with any name!");
                }
            }
            else
            {
                _isUIInitialized = true;
                Debug.Log("QuestUI: UI initialized successfully");
            }

            InitializeGlobalNotifications();
            SubscribeToEvents();
        }

        private void InitializeGlobalNotifications()
        {
            if (_globalNotificationUXML == null)
            {
                Debug.LogWarning("QuestUI: globalNotificationUXML is null! Assign your notification UXML file.");
                return;
            }

            if (_mainUIDocument == null)
            {
                _mainUIDocument = FindFirstObjectByType<UIDocument>();  // Fallback: Grab any UIDocument (e.g., HUD)
                if (_mainUIDocument == null)
                {
                    Debug.LogError("QuestUI: No UIDocument fallback found!");
                    return;
                }
                Debug.Log($"QuestUI: Using fallback UIDocument: {_mainUIDocument.name}");
            }
            else if (_mainUIDocument.rootVisualElement == null)
            {
                Debug.LogError("QuestUI: Main UI root is null!");
                return;
            }

            // Use the same root as your main quest UI instead of finding a separate UIDocument
            _globalNotificationRoot = _globalNotificationUXML.Instantiate();
            Debug.Log("QuestUI: Notification root instantiated.");
            _mainUIDocument.rootVisualElement.Add(_globalNotificationRoot);
            Debug.Log($"QuestUI: Notification added to {_mainUIDocument.name} root.");
            _globalNotificationRoot.style.position = Position.Absolute;
            _globalNotificationRoot.style.left = new StyleLength(Length.Percent(2));  // Your left offset
            _globalNotificationRoot.style.top = new StyleLength(Length.Percent(20));
            _globalNotificationRoot.style.width = new StyleLength(Length.Percent(30));

            _globalNotificationBar = _globalNotificationRoot.Q<VisualElement>("NotificationBar");
            _globalNotificationLabel = _globalNotificationRoot.Q<Label>("NotificationText");

            if (_globalNotificationBar == null || _globalNotificationLabel == null)
            {
                Debug.LogError("QuestUI: 'NotificationBar' or 'NotificationText' not found in Notification UXML!");
                return;
            }

            _globalNotificationBar.style.display = DisplayStyle.None;
            Debug.Log("QuestUI: Global notifications initialized successfully from UXML.");
        }

        void OnEnable()
        {
            // Only refresh if UI is properly initialized
            if (_isUIInitialized)
            {
                RefreshQuestLog();
            }
        }

        void OnDisable() => UnsubscribeFromEvents();

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            ClearAllEntries();

            if (_globalNotificationRoot != null && _globalNotificationRoot.parent != null)
            {
                _globalNotificationRoot.RemoveFromHierarchy();
            }
        }

        private void SubscribeToEvents()
        {
            if (QuestManager.Instance != null)
            {
                QuestManager.OnQuestStarted += HandleQuestStarted;
                QuestManager.OnQuestCompleted += HandleQuestCompleted;
                QuestManager.OnObjectiveProgressed += HandleObjectiveProgressed;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (QuestManager.Instance != null)
            {
                QuestManager.OnQuestStarted -= HandleQuestStarted;
                QuestManager.OnQuestCompleted -= HandleQuestCompleted;
                QuestManager.OnObjectiveProgressed -= HandleObjectiveProgressed;
            }
        }

        private void HandleQuestStarted(QuestSO quest)
        {
            PlaySound(_newQuestSound);
            if (!_isUIInitialized) return;

            CreateQuestEntry(quest);
            ShowNotification($"New Quest: {quest.Title}");
        }

        private void HandleQuestCompleted(QuestSO quest)
        {
            var rewardText = quest.MoneyReward > 0 ? $" (+{quest.MoneyReward} Money)" : "";
            ShowNotification($"Quest Complete: {quest.Title} {rewardText}");
            if (!_isUIInitialized) return;

            RemoveQuestFromUI(quest);
            RefreshQuestLog();  // Refresh to update list
        }

        private void HandleObjectiveProgressed(ObjectiveSO objective, int current, int required)
        {
            PlaySound(_objectiveCompleteSound);
            if (!_isUIInitialized) return;

            ShowNotification($"{objective.Description} ({current}/{required})");
            RefreshQuestLog();  // Refresh to update progress in entries
        }

        private void CreateQuestEntry(QuestSO quest)
        {
            if (!_isUIInitialized || _activeQuestsContainer == null)
            {
                Debug.LogWarning("QuestUI: Cannot create quest entry - UI not initialized");
                return;
            }

            // Prevent duplicates
            if (_questEntryGameObjects.ContainsKey(quest))
            {
                Debug.Log($"QuestUI: Entry for {quest.Title} already exists - skipping creation.");
                return;
            }

            if (quest == null)
            {
                Debug.LogError("QuestUI: CreateQuestEntry - quest is null!");
                return;
            }

            if (_questEntryUXML == null)
            {
                Debug.LogError("QuestUI: questEntryUXML is null!");
                return;
            }

            // Instantiate UXML directly into container
            var entryElement = _questEntryUXML.Instantiate();
            _activeQuestsContainer.contentContainer.Add(entryElement);

            // Track the element for removal
            _questEntryElements[quest] = entryElement;

            // Create component GO (no UIDocument)
            var entryGO = new GameObject("QuestEntry");
            var entry = entryGO.AddComponent<QuestEntryUI_Toolkit>();

            entry.Initialize(entryElement, quest,
                _objectiveDisplayUXML, _completeIcon, _incompleteIcon);

            // Track for cleanup
            _questEntryGameObjects[quest] = entryGO;

            // Force layout refresh
            _activeQuestsContainer.contentContainer.MarkDirtyRepaint();
            if (_root != null)
            {
                _root.schedule.Execute(() => _activeQuestsContainer.contentContainer.MarkDirtyRepaint()).StartingIn(0);
            }
        }

        public void RefreshQuestLog()
        {
            if (!_isUIInitialized)
            {
                Debug.LogWarning("QuestUI: Cannot refresh - UI not initialized");
                return;
            }

            Debug.Log($"QuestUI: RefreshQuestLog called. Active quests count: {QuestManager.Instance.GetActiveQuests()?.Count ?? 0}");
            if (_activeQuestsContainer == null)
            {
                Debug.LogError("QuestUI: activeQuestsContainer is null!");
                return;
            }

            ClearAllEntries();

            var activeQuests = QuestManager.Instance.GetActiveQuests();
            if (activeQuests == null)
            {
                Debug.LogWarning("QuestUI: No active quests from manager.");
                return;
            }

            foreach (var quest in activeQuests)
            {
                if (quest != null)
                {
                    Debug.Log($"QuestUI: Creating entry for {quest.Data.Title}");
                    CreateQuestEntry(quest.Data);
                }
                else
                {
                    Debug.LogWarning("QuestUI: Null quest in active list.");
                }
            }

            // Force full refresh after all adds
            _activeQuestsContainer.contentContainer.MarkDirtyRepaint();
        }

        private void RemoveQuestFromUI(QuestSO quest)
        {
            // Use tracked element for precise removal instead of clearing parent
            if (_questEntryElements.TryGetValue(quest, out var entryElement))
            {
                if (entryElement != null && entryElement.parent != null)
                {
                    entryElement.RemoveFromHierarchy();
                }
                _questEntryElements.Remove(quest);
            }

            if (_questEntryGameObjects.TryGetValue(quest, out var entryGO))
            {
                Destroy(entryGO);
                _questEntryGameObjects.Remove(quest);
            }
        }

        private void ClearAllEntries()
        {
            if (_activeQuestsContainer != null)
            {
                _activeQuestsContainer.contentContainer.Clear();
            }
            // Clear the tracking dict
            _questEntryElements.Clear();
            foreach (var go in _questEntryGameObjects.Values)
            {
                if (go != null) Destroy(go);
            }
            _questEntryGameObjects.Clear();
        }

        private void ShowNotification(string message)
        {
            if (_globalNotificationLabel == null)
            {
                Debug.LogWarning("QuestUI: Global notification label not found - Ensure InitializeGlobalNotifications succeeded!");
                return;
            }

            Debug.Log($"QuestUI: ShowNotification called with: {message}");
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
                var message = _notificationQueue.Dequeue();
                Debug.Log("QuestUI: Dequeued message, setting display Flex.");
                _globalNotificationLabel.text = message;

                if (_globalNotificationBar != null)
                {
                    _globalNotificationBar.AddToClassList("visible");
                    _globalNotificationBar.style.display = DisplayStyle.Flex;
                    Debug.Log($"QuestUI: Bar display set to Flex. Opacity: {_globalNotificationBar.style.opacity.value}, Height: {_globalNotificationBar.resolvedStyle.height}");
                }

                yield return new WaitForSeconds(_notificationDuration);

                Debug.Log("QuestUI: Hiding notification after duration.");

                if (_globalNotificationBar != null)
                {
                    _globalNotificationBar.RemoveFromClassList("visible");
                }

                yield return new WaitForSeconds(0.2f);  // Brief pause between notifications

                _globalNotificationBar.style.display = DisplayStyle.None;
                _globalNotificationLabel.text = string.Empty;
            }

            _isShowingNotification = false;
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(clip);
            }
        }

        public void CopyConfigurationFromQUI(QuestUI_Toolkit source)
        {
            if (source == null)
            {
                Debug.LogError("QuestUI_Toolkit: CopyConfigurationFrom called with null source.");
                return;
            }

            // These are the private serialized fields - we can assign them directly since we're inside the class
            _questEntryUXML = source._questEntryUXML;
            _objectiveDisplayUXML = source._objectiveDisplayUXML;
            _completeIcon = source._completeIcon;
            _incompleteIcon = source._incompleteIcon;
            _newQuestSound = source._newQuestSound;
            _objectiveCompleteSound = source._objectiveCompleteSound;
            _globalNotificationUXML = source._globalNotificationUXML;
            _mainUIDocument = source._mainUIDocument;
            _notificationDuration = source._notificationDuration;
        }

        public void SetNotificationRootDocument(UIDocument hudDocument)
        {
            if (hudDocument != null)
            {
                _mainUIDocument = hudDocument;
                Debug.Log("QuestUI_Toolkit: Notification root set to HUD UIDocument.");
            }
            else
            {
                Debug.LogWarning("QuestUI_Toolkit: Attempted to set null HUD document for notifications.");
            }
        }
    }
}

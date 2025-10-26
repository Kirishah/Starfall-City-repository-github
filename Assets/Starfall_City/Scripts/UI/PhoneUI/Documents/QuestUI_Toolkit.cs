using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class QuestUI_Toolkit : MonoBehaviour
{
    public static QuestUI_Toolkit Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] public VisualTreeAsset questEntryUXML;  
    [SerializeField] public VisualTreeAsset objectiveDisplayUXML;  
    [SerializeField] public Sprite completeIcon;  
    [SerializeField] public Sprite incompleteIcon;  
    [SerializeField] public AudioClip newQuestSound;
    [SerializeField] public AudioClip objectiveCompleteSound;

    [Header("Global Notifications")]
    [SerializeField] public VisualTreeAsset globalNotificationUXML;
    [SerializeField] public UIDocument mainUIDocument;
    private VisualElement globalNotificationRoot;
    private VisualElement globalNotificationBar;
    private Label globalNotificationLabel;

    [Header("Settings")]
    [SerializeField] public float notificationDuration = 3f;

    private VisualElement root;
    private ScrollView activeQuestsContainer;

    private Queue<string> notificationQueue = new Queue<string>();
    private bool isShowingNotification;
    private AudioSource audioSource;
    private Dictionary<QuestSO, GameObject> questEntryGameObjects = new Dictionary<QuestSO, GameObject>(); // Track GOs for cleanup
    private Dictionary<QuestSO, VisualElement> questEntryElements = new Dictionary<QuestSO, VisualElement>(); // Same
    private bool isUIInitialized = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        audioSource = GetComponent<AudioSource>();
    }

    public bool IsInitialized() => isUIInitialized;

    public void Initialize(VisualElement uiRoot)
    {
        root = uiRoot;
        if (root == null)
        {
            Debug.LogError("QuestUI: rootVisualElement is null!");
            return;
        }

        activeQuestsContainer = root.Q<ScrollView>("ActiveQuestsContainer");

        if (activeQuestsContainer == null)
        {
            Debug.LogError("QuestUI: ActiveQuestsContainer not found in UXML!");
            // Try to find it by other common names
            activeQuestsContainer = root.Q<ScrollView>("QuestList");
            if (activeQuestsContainer == null)
            {
                Debug.LogError("QuestUI: Could not find quests container with any name!");
            }
        }
        else
        {
            isUIInitialized = true;
            Debug.Log("QuestUI: UI initialized successfully");
        }

        InitializeGlobalNotifications();
        SubscribeToEvents();
    }

    private void InitializeGlobalNotifications()
    {
        if (globalNotificationUXML == null)
        {
            Debug.LogWarning("QuestUI: globalNotificationUXML is null! Assign your notification UXML file.");
            return;
        }

        if (mainUIDocument == null)
        {
            mainUIDocument = FindObjectOfType<UIDocument>();  // Fallback: Grab any UIDocument (e.g., HUD)
            if (mainUIDocument == null)
            {
                Debug.LogError("QuestUI: No UIDocument fallback found!");
                return;
            }
            Debug.Log($"QuestUI: Using fallback UIDocument: {mainUIDocument.name}");
        }
        else if (mainUIDocument.rootVisualElement == null)
        {
            Debug.LogError("QuestUI: Main UI root is null!");
            return;
        }

        // Use the same root as your main quest UI instead of finding a separate UIDocument
        globalNotificationRoot = globalNotificationUXML.Instantiate();
        Debug.Log("QuestUI: Notification root instantiated.");
        mainUIDocument.rootVisualElement.Add(globalNotificationRoot);
        Debug.Log($"QuestUI: Notification added to {mainUIDocument.name} root.");
        globalNotificationRoot.style.position = Position.Absolute;
        globalNotificationRoot.style.left = new StyleLength(Length.Percent(2));  // Your left offset
        globalNotificationRoot.style.top = new StyleLength(Length.Percent(20));
        globalNotificationRoot.style.width = new StyleLength(Length.Percent(30));

        globalNotificationBar = globalNotificationRoot.Q<VisualElement>("NotificationBar");
        globalNotificationLabel = globalNotificationRoot.Q<Label>("NotificationText");

        if (globalNotificationBar == null || globalNotificationLabel == null)
        {
            Debug.LogError("QuestUI: 'NotificationBar' or 'NotificationText' not found in Notification UXML!");
            return;
        }

        globalNotificationBar.style.display = DisplayStyle.None;
        Debug.Log("QuestUI: Global notifications initialized successfully from UXML.");
    }

    void Start()
    {
       
    }

    void OnEnable()
    {
        // Only refresh if UI is properly initialized
        if (isUIInitialized)
        {
            RefreshQuestLog();
        } 
    }

    void OnDisable()
    {
        UnsubscribeFromEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
        ClearAllEntries();

        if (globalNotificationRoot != null && globalNotificationRoot.parent != null)
        {
            globalNotificationRoot.RemoveFromHierarchy();
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
        PlaySound(newQuestSound);
        if (!isUIInitialized) return;

        CreateQuestEntry(quest);
        ShowNotification($"New Quest: {quest.Title}");
    }

    private void HandleQuestCompleted(QuestSO quest)
    {
        string rewardText = quest.MoneyReward > 0 ? $" (+{quest.MoneyReward} Money)" : "";
        ShowNotification($"Quest Complete: {quest.Title} {rewardText}");
        if (!isUIInitialized) return;
        
        RemoveQuestFromUI(quest);
        RefreshQuestLog();  // Refresh to update list
    }

    private void HandleObjectiveProgressed(ObjectiveSO objective, int current, int required)
    {
        PlaySound(objectiveCompleteSound);
        if (!isUIInitialized) return;

        ShowNotification($"{objective.Description} ({current}/{required})");
        RefreshQuestLog();  // Refresh to update progress in entries
    }

    private void CreateQuestEntry(QuestSO quest)
    {
        if (!isUIInitialized || activeQuestsContainer == null)
        {
            Debug.LogWarning("QuestUI: Cannot create quest entry - UI not initialized");
            return;
        }

        // Prevent duplicates
        if (questEntryGameObjects.ContainsKey(quest))
        {
            Debug.Log($"QuestUI: Entry for {quest.Title} already exists - skipping creation.");
            return;
        }

        if (quest == null)
        {
            Debug.LogError("QuestUI: CreateQuestEntry - quest is null!");
            return;
        }

        if (questEntryUXML == null)
        {
            Debug.LogError("QuestUI: questEntryUXML is null!");
            return;
        }

        // Instantiate UXML directly into container
        var entryElement = questEntryUXML.Instantiate();
        activeQuestsContainer.contentContainer.Add(entryElement);

        // Track the element for removal
        questEntryElements[quest] = entryElement;

        // Create component GO (no UIDocument)
        var entryGO = new GameObject("QuestEntry");
        var entry = entryGO.AddComponent<QuestEntryUI_Toolkit>();

        // Assign serialized fields to the new entry component (were missing, causing null cascade to ObjectiveDisplay)
        entry.questEntryUXML = questEntryUXML;
        entry.objectiveDisplayUXML = objectiveDisplayUXML;
        entry.completeIcon = completeIcon;
        entry.incompleteIcon = incompleteIcon;

        // Pass the instantiated entryElement
        entry.Initialize(entryElement, quest);

        // Track for cleanup
        questEntryGameObjects[quest] = entryGO;

        // Force layout refresh
        activeQuestsContainer.contentContainer.MarkDirtyRepaint();
        if (root != null)
        {
            root.schedule.Execute(() =>
            {
                activeQuestsContainer.contentContainer.MarkDirtyRepaint();
            }).StartingIn(0);
        }
    }

    public void RefreshQuestLog()
    {
        if (!isUIInitialized)
        {
            Debug.LogWarning("QuestUI: Cannot refresh - UI not initialized");
            return;
        }

        Debug.Log($"QuestUI: RefreshQuestLog called. Active quests count: {(QuestManager.Instance?.GetActiveQuests()?.Count ?? 0)}");
        if (activeQuestsContainer == null)
        {
            Debug.LogError("QuestUI: activeQuestsContainer is null!");
            return;
        }

        ClearAllEntries();

        var activeQuests = QuestManager.Instance?.GetActiveQuests();
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
        activeQuestsContainer.contentContainer.MarkDirtyRepaint();
    }

    private void RemoveQuestFromUI(QuestSO quest)
    {
        // Use tracked element for precise removal instead of clearing parent
        if (questEntryElements.TryGetValue(quest, out var entryElement))
        {
            if (entryElement != null && entryElement.parent != null)
            {
                entryElement.RemoveFromHierarchy();
            }
            questEntryElements.Remove(quest);
        }

        if (questEntryGameObjects.TryGetValue(quest, out var entryGO))
        {
            Destroy(entryGO);
            questEntryGameObjects.Remove(quest);
        }
    }

    private void ClearAllEntries()
    {
        if (activeQuestsContainer != null)
        {
            activeQuestsContainer.contentContainer.Clear();
        }
        // Clear the tracking dict
        questEntryElements.Clear();
        foreach (var go in questEntryGameObjects.Values)
        {
            if (go != null) Destroy(go);
        }
        questEntryGameObjects.Clear();
    }

    private void ShowNotification(string message)
    {
        if (globalNotificationLabel == null)
        {
            Debug.LogWarning("QuestUI: Global notification label not found - Ensure InitializeGlobalNotifications succeeded!");
            return;
        }

        Debug.Log($"QuestUI: ShowNotification called with: {message}");
        notificationQueue.Enqueue(message);
        if (!isShowingNotification)
        {
            StartCoroutine(NotificationRoutine());
        }
    }

    private IEnumerator NotificationRoutine()
    {
        isShowingNotification = true;

        while (notificationQueue.Count > 0)
        {
            string message = notificationQueue.Dequeue();
            Debug.Log("QuestUI: Dequeued message, setting display Flex.");
            globalNotificationLabel.text = message;

            if (globalNotificationBar != null)
            {
                globalNotificationBar.AddToClassList("visible");
                globalNotificationBar.style.display = DisplayStyle.Flex;
                Debug.Log($"QuestUI: Bar display set to Flex. Opacity: {globalNotificationBar.style.opacity.value}, Height: {globalNotificationBar.resolvedStyle.height}");
            }

            yield return new WaitForSeconds(notificationDuration);

            Debug.Log("QuestUI: Hiding notification after duration.");

            if (globalNotificationBar != null)
            {
                globalNotificationBar.RemoveFromClassList("visible");
            }

            yield return new WaitForSeconds(0.2f);  // Brief pause between notifications

            globalNotificationBar.style.display = DisplayStyle.None;
            globalNotificationLabel.text = string.Empty;
        }

        isShowingNotification = false;
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}

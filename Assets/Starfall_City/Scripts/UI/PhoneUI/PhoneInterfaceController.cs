using UnityEngine;
using UnityEngine.UIElements;

public class PhoneInterfaceController : MonoBehaviour
{
    [SerializeField] private UIDocument phoneDocument;
    [SerializeField] private UIDocument questUIPrefab; 
    [SerializeField] private UIDocument characteristicsUIPrefab; 

    private VisualElement root;
    private UIDocument currentQuestUIInstance;
    private UIDocument currentCharacteristicsUIInstance;
    private QuestUI_Toolkit questUI;
    private GameObject questGO;  // Track for OnDestroy

    private VisualElement questsPanel;
    private VisualElement personalityPanel;
    private VisualElement reputationPanel;
    private VisualElement codexPanel;

    private Button questsTab;
    private Button personalityTab;
    private Button reputationTab;
    private Button codexTab;
    private Button closeButton;
    private Label sectionTitle;

    private bool isInitialized = false;
    private string currentActiveTab = "quests";

    private void Start()
    {
        InitializePhoneUI();
    }

    private void InitializePhoneUI()
    {
        if (phoneDocument == null)
        {
            Debug.LogError("PhoneInterfaceController: phoneDocument is not assigned!");
            return;
        }

        root = phoneDocument.rootVisualElement;

        if (root == null)
        {
            Debug.LogError("PhoneInterfaceController: rootVisualElement is null!");
            return;
        }

        // Get shared title
        sectionTitle = root.Q<Label>("SectionTitle");
        if (sectionTitle == null)
        {
            Debug.LogError("SectionTitle not found in Phone.uxml!");
        }

        // Get panel references
        questsPanel = root.Q<VisualElement>("QuestsPanel");
        personalityPanel = root.Q<VisualElement>("PersonalityPanel");
        reputationPanel = root.Q<VisualElement>("ReputationPanel");
        codexPanel = root.Q<VisualElement>("CodexPanel");

        // Get tab references
        questsTab = root.Q<Button>("QuestsTab");
        personalityTab = root.Q<Button>("PersonalityTab");
        reputationTab = root.Q<Button>("ReputationTab");
        codexTab = root.Q<Button>("CodexTab");
        closeButton = root.Q<Button>("CloseButton");

        // Only register events if elements are found
        if (questsTab != null) questsTab.clicked += () => SwitchTab("quests");
        if (personalityTab != null) personalityTab.clicked += () => SwitchTab("personality");
        if (reputationTab != null) reputationTab.clicked += () => SwitchTab("reputation");
        if (codexTab != null) codexTab.clicked += () => SwitchTab("codex");
        if (closeButton != null) closeButton.clicked += ClosePhone;

        isInitialized = true;

        // Initialize persistent QuestUI
        if (questUIPrefab != null)
        {
            var questUXML = questUIPrefab.visualTreeAsset;
            if (questUXML != null)
            {
                var questRoot = questUXML.Instantiate();
                if (questsPanel != null)
                {
                    questsPanel.Add(questRoot);
                }

                questGO = new GameObject("QuestUI");
                questGO.transform.SetParent(transform);

                questUI = questGO.AddComponent<QuestUI_Toolkit>();

                // Get the QuestUI_Toolkit component from the prefab to copy serialized fields
                var prefabQuestUI = questUIPrefab.GetComponent<QuestUI_Toolkit>();
                if (prefabQuestUI != null)
                {
                    questUI.questEntryUXML = prefabQuestUI.questEntryUXML;
                    questUI.objectiveDisplayUXML = prefabQuestUI.objectiveDisplayUXML;
                    questUI.completeIcon = prefabQuestUI.completeIcon;
                    questUI.incompleteIcon = prefabQuestUI.incompleteIcon;
                    questUI.newQuestSound = prefabQuestUI.newQuestSound;
                    questUI.objectiveCompleteSound = prefabQuestUI.objectiveCompleteSound;
                    questUI.globalNotificationUXML = prefabQuestUI.globalNotificationUXML;
                    questUI.mainUIDocument = prefabQuestUI.mainUIDocument;
                    questUI.notificationDuration = prefabQuestUI.notificationDuration;
                }
                else
                {
                    Debug.LogError("PhoneInterfaceController: No QuestUI_Toolkit on questUIPrefab! Check prefab setup.");
                }

                // Assign HUD for global notifications (overrides phone fallback)
                UIDocument hudDocument = null;
                var hudGO = GameObject.FindWithTag("HUD");
                if (hudGO != null)
                {
                    hudDocument = hudGO.GetComponent<UIDocument>();
                }

                if (hudDocument != null)
                {
                    questUI.mainUIDocument = hudDocument;
                    Debug.Log("QuestUI: HUD UIDocument assigned for global notifications.");
                }
                else
                {
                    Debug.LogWarning("QuestUI: HUD UIDocument not found! Notifications will use phone UI or fail.");
                }

                // Initialize with the instantiated root (integrated into phone hierarchy)
                questUI.Initialize(questRoot);
            }
        }

        // Start with quests tab
        SwitchTab("quests");
        SetPhoneVisible(false);  // Start closed
    }

    public void SwitchTab(string tabName)
    {
        if (!isInitialized) return;

        if (tabName != currentActiveTab)
        {
            CleanupPreviousTab(currentActiveTab);
        }

        // Update title
        if (sectionTitle != null)
        {
            sectionTitle.text = tabName switch
            {
                "quests" => "Quests",
                "personality" => "Personality",
                "reputation" => "Reputation",
                "codex" => "Codex",
                _ => "Phone"
            };
        }

        // Deactivate all, activate new
        DeactivateAllPanels();
        DeactivateAllTabs();

        switch (tabName)
        {
            case "quests":
                if (questsPanel != null) questsPanel.AddToClassList("active");
                if (questsTab != null) questsTab.AddToClassList("active");
                if (questUI != null)
                {
                    questUI.RefreshQuestLog();
                }
                break;
            case "personality":
                if (personalityPanel != null) personalityPanel.AddToClassList("active");
                if (personalityTab != null) personalityTab.AddToClassList("active");
                InitializeCharacteristicsUI();
                break;
            case "reputation":
                if (reputationPanel != null) reputationPanel.AddToClassList("active");
                if (reputationTab != null) reputationTab.AddToClassList("active");
                InitializeReputationUI();
                break;
            case "codex":
                if (codexPanel != null) codexPanel.AddToClassList("active");
                if (codexTab != null) codexTab.AddToClassList("active");
                InitializeCodexUI();
                break;
        }

        currentActiveTab = tabName;
    }

    private void CleanupPreviousTab(string prevTab)
    {
        switch (prevTab)
        {
            case "quests":
                DisableQuestUI();
                break;
            case "personality":
                CleanupCharacteristicsUI();
                break;
            case "reputation":
                CleanupReputationUI();
                break;
            case "codex":
                CleanupCodexUI();
                break;
        }
    }

    private void DeactivateAllPanels()
    {
        if (questsPanel != null) questsPanel.RemoveFromClassList("active");
        if (personalityPanel != null) personalityPanel.RemoveFromClassList("active");
        if (reputationPanel != null) reputationPanel.RemoveFromClassList("active");
        if (codexPanel != null) codexPanel.RemoveFromClassList("active");
    }

    private void DeactivateAllTabs()
    {
        if (questsTab != null) questsTab.RemoveFromClassList("active");
        if (personalityTab != null) personalityTab.RemoveFromClassList("active");
        if (reputationTab != null) reputationTab.RemoveFromClassList("active");
        if (codexTab != null) codexTab.RemoveFromClassList("active");
    }

    private void InitializeCharacteristicsUI()
    {
        if (personalityPanel == null || characteristicsUIPrefab == null)
        {
            Debug.LogError("Characteristics setup missing!");
            return;
        }

        CleanupCharacteristicsUI();  // Helper

        var charGO = Instantiate(characteristicsUIPrefab);
        currentCharacteristicsUIInstance = charGO.GetComponent<UIDocument>();
        var charRoot = currentCharacteristicsUIInstance.rootVisualElement;
        if (charRoot != null)
        {
            personalityPanel.Clear();
            personalityPanel.Add(charRoot);
            var charUI = charGO.GetComponent<CharacteristicsUI_Toolkit>();
            if (charUI != null) charUI.UpdateAllBars();
        }
    }

    // Helpers for cleanup (call in SwitchTab if switching away)
    private void DisableQuestUI()  // RENAMED
    {
        if (currentQuestUIInstance != null)
        {
            Destroy(currentQuestUIInstance.gameObject);
            currentQuestUIInstance = null;
        }
    }

    private void CleanupCharacteristicsUI()
    {
        if (currentCharacteristicsUIInstance != null)
        {
            Destroy(currentCharacteristicsUIInstance.gameObject);
            currentCharacteristicsUIInstance = null;
        }
    }

    private void CleanupReputationUI()
    {
        // TODO: Implement cleanup for reputation UI if instantiated
    }

    private void CleanupCodexUI()
    {
        // TODO: Implement cleanup for codex UI if instantiated
    }

    private void InitializeReputationUI()
    {
        // TODO: Implement reputation UI
        Debug.Log("Reputation UI - To be implemented");
    }

    private void InitializeCodexUI()
    {
        // TODO: Implement codex UI
        Debug.Log("Codex UI - To be implemented");
    }

    public void TogglePhone()
    {
        if (!isInitialized || root == null)
        {
            Debug.LogWarning("PhoneInterface not properly initialized. Cannot toggle.");
            return;
        }

        bool isVisible = root.style.display == DisplayStyle.Flex;
        SetPhoneVisible(!isVisible);
    }

    private void SetPhoneVisible(bool visible)
    {
        if (!isInitialized || root == null) return;

        root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        if (visible)
        {
            // Refresh the current tab when opening phone
            RefreshCurrentTab();
        }
        else
        {
            CleanupUIInstances();
        }
    }

    private void RefreshCurrentTab()
    {
        if (!isInitialized) return;

        // Re-initialize the current active tab
        SwitchTab(currentActiveTab);
    }

    private void CleanupUIInstances()
    {
        DisableQuestUI();
        CleanupCharacteristicsUI();
        CleanupReputationUI();
        CleanupCodexUI();
    }

    private void ClosePhone()
    {
        SetPhoneVisible(false);
    }

    public bool IsPhoneVisible()
    {
        if (!isInitialized || root == null) return false;
        return root.style.display == DisplayStyle.Flex;
    }

    private void Update()
    {
        // Example: Toggle phone with P key
        if (Input.GetKeyDown(KeyCode.P))
        {
            TogglePhone();
        }
    }

    private void OnDestroy()
    {
        CleanupUIInstances();
        if (questGO != null) Destroy(questGO);

        // Unregister events
        if (questsTab != null) questsTab.clicked -= () => SwitchTab("quests");
        if (personalityTab != null) personalityTab.clicked -= () => SwitchTab("personality");
        if (reputationTab != null) reputationTab.clicked -= () => SwitchTab("reputation");
        if (codexTab != null) codexTab.clicked -= () => SwitchTab("codex");
        if (closeButton != null) closeButton.clicked -= ClosePhone;
    }
}

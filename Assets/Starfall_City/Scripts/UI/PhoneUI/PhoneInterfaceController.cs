using CharacteristicsSystem;
using QuestSystem;
using UnityEngine;
using UnityEngine.UIElements;

public class PhoneInterfaceController : MonoBehaviour
{
    [SerializeField] private UIDocument _phoneDocument;
    [SerializeField] private UIDocument _questUIPrefab;
    [SerializeField] private UIDocument _characteristicsUIPrefab;
    [SerializeField] private UIDocument _reputationUIPrefab;

    private VisualElement _root;
    private UIDocument _currentQuestUIInstance;
    private UIDocument _currentCharacteristicsUIInstance;
    private UIDocument _currentReputationUIInstance;
    private QuestUI_Toolkit _questUI;
    private GameObject _questGO;  // Track for OnDestroy

    private VisualElement _questsPanel;
    private VisualElement _personalityPanel;
    private VisualElement _reputationPanel;
    private VisualElement _codexPanel;

    private Button _questsTab;
    private Button _personalityTab;
    private Button _reputationTab;
    private Button _codexTab;
    private Button _closeButton;
    private Label _sectionTitle;

    private bool _isInitialized = false;
    private string _currentActiveTab = "quests";

    private void Start() => InitializePhoneUI();

    private void InitializePhoneUI()
    {
        if (_phoneDocument == null)
        {
            Debug.LogError("PhoneInterfaceController: phoneDocument is not assigned!");
            return;
        }

        _root = _phoneDocument.rootVisualElement;

        if (_root == null)
        {
            Debug.LogError("PhoneInterfaceController: rootVisualElement is null!");
            return;
        }

        // Get shared title
        _sectionTitle = _root.Q<Label>("SectionTitle");
        if (_sectionTitle == null)
        {
            Debug.LogError("SectionTitle not found in Phone.uxml!");
        }

        // Get panel references
        _questsPanel = _root.Q<VisualElement>("QuestsPanel");
        _personalityPanel = _root.Q<VisualElement>("PersonalityPanel");
        _reputationPanel = _root.Q<VisualElement>("ReputationPanel");
        _codexPanel = _root.Q<VisualElement>("CodexPanel");

        // Get tab references
        _questsTab = _root.Q<Button>("QuestsTab");
        _personalityTab = _root.Q<Button>("PersonalityTab");
        _reputationTab = _root.Q<Button>("ReputationTab");
        _codexTab = _root.Q<Button>("CodexTab");
        _closeButton = _root.Q<Button>("CloseButton");

        // Only register events if elements are found
        if (_questsTab != null) _questsTab.clicked += () => SwitchTab("quests");
        if (_personalityTab != null) _personalityTab.clicked += () => SwitchTab("personality");
        if (_reputationTab != null) _reputationTab.clicked += () => SwitchTab("reputation");
        if (_codexTab != null) _codexTab.clicked += () => SwitchTab("codex");
        if (_closeButton != null) _closeButton.clicked += ClosePhone;

        _isInitialized = true;

        // Initialize persistent QuestUI
        if (_questUIPrefab != null)
        {
            var questUXML = _questUIPrefab.visualTreeAsset;
            if (questUXML != null)
            {
                var questRoot = questUXML.Instantiate();
                if (_questsPanel != null)
                {
                    _questsPanel.Add(questRoot);
                }

                _questGO = new GameObject("QuestUI");
                _questGO.transform.SetParent(transform);

                _questUI = _questGO.AddComponent<QuestUI_Toolkit>();

                // Get the QuestUI_Toolkit component from the prefab to copy serialized fields
                if (_questUIPrefab.TryGetComponent<QuestUI_Toolkit>(out var prefabQuestUI))
                {
                    _questUI.CopyConfigurationFromQUI(prefabQuestUI);
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
                    _questUI.SetNotificationRootDocument(hudDocument);
                }
                else
                {
                    Debug.LogWarning("QuestUI: HUD UIDocument not found! Notifications will use phone UI or fail.");
                }

                // Initialize with the instantiated root (integrated into phone hierarchy)
                _questUI.Initialize(questRoot);
            }
        }

        // Start with quests tab
        SwitchTab("quests");
        SetPhoneVisible(false);  // Start closed
    }

    public void SwitchTab(string tabName)
    {
        if (!_isInitialized) return;

        if (tabName != _currentActiveTab)
        {
            CleanupPreviousTab(_currentActiveTab);
        }

        // Update title
        if (_sectionTitle != null)
        {
            _sectionTitle.text = tabName switch
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
                if (_questsPanel != null) _questsPanel.AddToClassList("active");
                if (_questsTab != null) _questsTab.AddToClassList("active");
                if (_questUI != null)
                {
                    _questUI.RefreshQuestLog();
                }
                break;
            case "personality":
                if (_personalityPanel != null) _personalityPanel.AddToClassList("active");
                if (_personalityTab != null) _personalityTab.AddToClassList("active");
                InitializeCharacteristicsUI();
                break;
            case "reputation":
                if (_reputationPanel != null) _reputationPanel.AddToClassList("active");
                if (_reputationTab != null) _reputationTab.AddToClassList("active");
                InitializeReputationUI();
                break;
            case "codex":
                if (_codexPanel != null) _codexPanel.AddToClassList("active");
                if (_codexTab != null) _codexTab.AddToClassList("active");
                InitializeCodexUI();
                break;
        }

        _currentActiveTab = tabName;
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
        if (_questsPanel != null) _questsPanel.RemoveFromClassList("active");
        if (_personalityPanel != null) _personalityPanel.RemoveFromClassList("active");
        if (_reputationPanel != null) _reputationPanel.RemoveFromClassList("active");
        if (_codexPanel != null) _codexPanel.RemoveFromClassList("active");
    }

    private void DeactivateAllTabs()
    {
        if (_questsTab != null) _questsTab.RemoveFromClassList("active");
        if (_personalityTab != null) _personalityTab.RemoveFromClassList("active");
        if (_reputationTab != null) _reputationTab.RemoveFromClassList("active");
        if (_codexTab != null) _codexTab.RemoveFromClassList("active");
    }

    private void InitializeCharacteristicsUI()
    {
        if (_personalityPanel == null || _characteristicsUIPrefab == null)
        {
            Debug.LogError("Characteristics setup missing!");
            return;
        }

        CleanupCharacteristicsUI();  // Helper

        var charGO = Instantiate(_characteristicsUIPrefab);
        _currentCharacteristicsUIInstance = charGO.GetComponent<UIDocument>();
        var charRoot = _currentCharacteristicsUIInstance.rootVisualElement;
        if (charRoot != null)
        {
            _personalityPanel.Clear();
            _personalityPanel.Add(charRoot);
            if (charGO.TryGetComponent<CharacteristicsUI_Toolkit>(out var charUI)) charUI.UpdateAllBars();
        }
    }

    // Helpers for cleanup (call in SwitchTab if switching away)
    private void DisableQuestUI()  // RENAMED
    {
        if (_currentQuestUIInstance != null)
        {
            Destroy(_currentQuestUIInstance.gameObject);
            _currentQuestUIInstance = null;
        }
    }

    private void CleanupCharacteristicsUI()
    {
        if (_currentCharacteristicsUIInstance != null)
        {
            Destroy(_currentCharacteristicsUIInstance.gameObject);
            _currentCharacteristicsUIInstance = null;
        }
    }

    private void CleanupReputationUI()
    {
        if (_currentReputationUIInstance != null)
        {
            Destroy(_currentReputationUIInstance.gameObject);
            _currentReputationUIInstance = null;
        }
    }

    private void CleanupCodexUI()
    {
        // TODO: Implement cleanup for codex UI if instantiated
    }

    private void InitializeReputationUI()
    {
        if (_reputationPanel == null || _reputationUIPrefab == null)
        {
            Debug.LogError("Reputation setup missing! Assign reputationUIPrefab in Inspector.");
            return;
        }

        CleanupReputationUI();  // Helper

        var repGO = Instantiate(_reputationUIPrefab);
        _currentReputationUIInstance = repGO.GetComponent<UIDocument>();
        var repRoot = _currentReputationUIInstance.rootVisualElement;
        if (repRoot != null)
        {
            _reputationPanel.Clear();
            _reputationPanel.Add(repRoot);
            if (repGO.TryGetComponent<ReputationUI_Toolkit>(out var repUI)) repUI.RefreshUI();
        }
    }

    private void InitializeCodexUI() =>
        // TODO: Implement codex UI
        Debug.Log("Codex UI - To be implemented");

    public void TogglePhone()
    {
        if (!_isInitialized || _root == null)
        {
            Debug.LogWarning("PhoneInterface not properly initialized. Cannot toggle.");
            return;
        }

        bool isVisible = _root.style.display == DisplayStyle.Flex;
        SetPhoneVisible(!isVisible);
    }

    private void SetPhoneVisible(bool visible)
    {
        if (!_isInitialized || _root == null) return;

        _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

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
        if (!_isInitialized) return;

        // Re-initialize the current active tab
        SwitchTab(_currentActiveTab);
    }

    private void CleanupUIInstances()
    {
        DisableQuestUI();
        CleanupCharacteristicsUI();
        CleanupReputationUI();
        CleanupCodexUI();
    }

    private void ClosePhone() => SetPhoneVisible(false);

    public bool IsPhoneVisible()
    {
        if (!_isInitialized || _root == null) return false;
        return _root.style.display == DisplayStyle.Flex;
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
        if (_questGO != null) Destroy(_questGO);

        // Unregister events
        if (_questsTab != null) _questsTab.clicked -= () => SwitchTab("quests");
        if (_personalityTab != null) _personalityTab.clicked -= () => SwitchTab("personality");
        if (_reputationTab != null) _reputationTab.clicked -= () => SwitchTab("reputation");
        if (_codexTab != null) _codexTab.clicked -= () => SwitchTab("codex");
        if (_closeButton != null) _closeButton.clicked -= ClosePhone;
    }
}

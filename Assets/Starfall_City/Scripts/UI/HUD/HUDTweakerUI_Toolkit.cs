using InventorySystem;
using UnityEngine;
using UnityEngine.UIElements;

public class HUDTweakerUI_Toolkit : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private UIDocument _hudDocument;
    [SerializeField] private InventoryUI _iTweaker;
    [SerializeField] private PhoneInterfaceController _phoneController;
    [SerializeField] private UIManager _menuTweaker;

    // Button references for cleanup
    private Button _inventoryBtn;
    private Button _phoneBtn;
    private Button _settingsBtn;

    private void Awake()
    {
        // Auto-find UIDocument if not assigned (fallback)
        if (_hudDocument == null)
        {
            _hudDocument = GetComponent<UIDocument>();
            if (_hudDocument == null)
            {
                Debug.LogError("HudTweaker: No UIDocument found on this GameObject! Assign manually.");
                return;
            }
        }
    }

    private void Start() => InitializeButtons();

    private void InitializeButtons()
    {
        if (_hudDocument == null || _hudDocument.rootVisualElement == null)
        {
            Debug.LogError("HudTweaker: HUD root is null! Cannot initialize buttons.");
            return;
        }

        var root = _hudDocument.rootVisualElement;

        // Query buttons by class names (as defined in HUD.uxml)
        _inventoryBtn = root.Q<Button>(className: "inventory-btn");
        _phoneBtn = root.Q<Button>(className: "phone-btn");
        _settingsBtn = root.Q<Button>(className: "settings-btn");

        // Hook up events
        if (_inventoryBtn != null)
        {
            _inventoryBtn.clicked += InventoryOpen;
            Debug.Log("HudTweaker: Inventory button hooked up.");
        }
        else
        {
            Debug.LogWarning("HudTweaker: Inventory button not found! Check class name 'inventory-btn' in HUD.uxml.");
        }

        if (_phoneBtn != null)
        {
            _phoneBtn.clicked += JournalOpen;
            Debug.Log("HudTweaker: Phone (Journal) button hooked up.");
        }
        else
        {
            Debug.LogWarning("HudTweaker: Phone button not found! Check class name 'phone-btn' in HUD.uxml.");
        }

        if (_settingsBtn != null)
        {
            _settingsBtn.clicked += MenuOpen;
            Debug.Log("HudTweaker: Settings (Menu) button hooked up.");
        }
        else
        {
            Debug.LogWarning("HudTweaker: Settings button not found! Check class name 'settings-btn' in HUD.uxml.");
        }
    }

    public void InventoryOpen()
    {
        if (_iTweaker != null)
        {
            _iTweaker.ToggleInventory();
        }
        else
        {
            Debug.LogWarning("HudTweaker: InventoryUI reference is null! Assign in Inspector.");
        }
    }

    public void JournalOpen()
    {
        if (_phoneController != null)
        {
            _phoneController.TogglePhone();
        }
        else
        {
            Debug.LogWarning("HudTweaker: PhoneInterfaceController reference is null! Assign in Inspector.");
        }
    }

    public void MenuOpen()
    {
        if (_menuTweaker != null)
        {
            _menuTweaker.ToggleInGameMenu();
        }
        else
        {
            Debug.LogWarning("HudTweaker: UIManager reference is null! Assign in Inspector.");
        }
    }

    private void OnDestroy()
    {
        // Unregister events to prevent leaks
        if (_inventoryBtn != null)
        {
            _inventoryBtn.clicked -= InventoryOpen;
        }
        if (_phoneBtn != null)
        {
            _phoneBtn.clicked -= JournalOpen;
        }
        if (_settingsBtn != null)
        {
            _settingsBtn.clicked -= MenuOpen;
        }
    }
}

using UnityEngine;
using UnityEngine.UIElements;

public class HUDTweakerUI_Toolkit : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private UIDocument hudDocument;  
    [SerializeField] private InventoryUI iTweaker;
    [SerializeField] private PhoneInterfaceController phoneController;
    [SerializeField] private UIManager menuTweaker;

    // Button references for cleanup
    private Button inventoryBtn;
    private Button phoneBtn;
    private Button settingsBtn;

    private void Awake()
    {
        // Auto-find UIDocument if not assigned (fallback)
        if (hudDocument == null)
        {
            hudDocument = GetComponent<UIDocument>();
            if (hudDocument == null)
            {
                Debug.LogError("HudTweaker: No UIDocument found on this GameObject! Assign manually.");
                return;
            }
        }
    }

    private void Start()
    {
        InitializeButtons();
    }

    private void InitializeButtons()
    {
        if (hudDocument == null || hudDocument.rootVisualElement == null)
        {
            Debug.LogError("HudTweaker: HUD root is null! Cannot initialize buttons.");
            return;
        }

        var root = hudDocument.rootVisualElement;

        // Query buttons by class names (as defined in HUD.uxml)
        inventoryBtn = root.Q<Button>(className: "inventory-btn");
        phoneBtn = root.Q<Button>(className: "phone-btn");
        settingsBtn = root.Q<Button>(className: "settings-btn");

        // Hook up events
        if (inventoryBtn != null)
        {
            inventoryBtn.clicked += InventoryOpen;
            Debug.Log("HudTweaker: Inventory button hooked up.");
        }
        else
        {
            Debug.LogWarning("HudTweaker: Inventory button not found! Check class name 'inventory-btn' in HUD.uxml.");
        }

        if (phoneBtn != null)
        {
            phoneBtn.clicked += JournalOpen;
            Debug.Log("HudTweaker: Phone (Journal) button hooked up.");
        }
        else
        {
            Debug.LogWarning("HudTweaker: Phone button not found! Check class name 'phone-btn' in HUD.uxml.");
        }

        if (settingsBtn != null)
        {
            settingsBtn.clicked += MenuOpen;
            Debug.Log("HudTweaker: Settings (Menu) button hooked up.");
        }
        else
        {
            Debug.LogWarning("HudTweaker: Settings button not found! Check class name 'settings-btn' in HUD.uxml.");
        }
    }

    public void InventoryOpen()
    {
        if (iTweaker != null)
        {
            iTweaker.ToggleInventory();
        }
        else
        {
            Debug.LogWarning("HudTweaker: InventoryUI reference is null! Assign in Inspector.");
        }
    }

    public void JournalOpen()
    {
        if (phoneController != null)
        {
            phoneController.TogglePhone();
        }
        else
        {
            Debug.LogWarning("HudTweaker: PhoneInterfaceController reference is null! Assign in Inspector.");
        }
    }

    public void MenuOpen()
    {
        if (menuTweaker != null)
        {
            menuTweaker.ToggleInGameMenu();
        }
        else
        {
            Debug.LogWarning("HudTweaker: UIManager reference is null! Assign in Inspector.");
        }
    }

    private void OnDestroy()
    {
        // Unregister events to prevent leaks
        if (inventoryBtn != null)
        {
            inventoryBtn.clicked -= InventoryOpen;
        }
        if (phoneBtn != null)
        {
            phoneBtn.clicked -= JournalOpen;
        }
        if (settingsBtn != null)
        {
            settingsBtn.clicked -= MenuOpen;
        }
    }
}

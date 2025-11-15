using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryUI : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private KeyCode _toggleKey = KeyCode.I;

    [Header("Manual Assignments (Optional)")]
    [SerializeField] private Transform _slotsParent;
    [SerializeField] private GameObject _slotPrefab;
    [SerializeField] private GameObject _inventoryPanel;
    [SerializeField] private GameObject _inventoryName;

    [Header("Fallback Settings")]
    [SerializeField] private string _panelTag = "InventoryPanel";
    [SerializeField] private string _iNameTag = "iNamePanel";
    [SerializeField] private string _slotsParentTag = "InventoryPanel";
    [SerializeField] private string _slotPrefabPath = "Prefabs/UI/InventorySlot";

    [SerializeField] private Canvas _inventoryCanvas;
    private static bool _hasSpawned;

    private bool _isInitialized;

    private void InitializeReferences()
    {
        if (_isInitialized) return;

        // 1. Try to find panel and slots parent if not assigned
        if (_inventoryPanel == null)
            _inventoryPanel = GameObject.FindGameObjectWithTag(_panelTag);

        if (_inventoryName == null)
            _inventoryName = GameObject.FindGameObjectWithTag(_iNameTag);

        if (_slotsParent == null)
            _slotsParent = GameObject.FindGameObjectWithTag(_slotsParentTag)?.transform;

        // 2. Fallback to Resources for prefab
        LoadSlotPrefab();

        // 3. Error prevention
        ValidateCriticalComponents();

        _isInitialized = true;
    }

    // ================== RESOURCES FALLBACK ================== //
    private void LoadSlotPrefab()
    {
        if (_slotPrefab != null) return;

        _slotPrefab = Resources.Load<GameObject>(_slotPrefabPath);
    }

    // ================== ERROR PREVENTION ================== //
    private void ValidateCriticalComponents()
    {
        if (_inventoryPanel == null)
            Debug.LogError($"Inventory Panel missing! Assign it or tag an object with '{_panelTag}'");

        if (_slotsParent == null)
            Debug.LogError($"Slots Parent missing! Assign it or tag a GridLayout object with '{_slotsParentTag}'");

        if (_slotPrefab == null)
            Debug.LogError($"Slot prefab missing! Create one at: Resources/{_slotPrefabPath}");
    }

    private void Awake() 
    {
        // Singleton pattern with DontDestroyOnLoad
        if (_hasSpawned)
        {
            Destroy(gameObject);
            return;
        }

        _hasSpawned = true;
        InitializeReferences(); 
    }

    private void Start()
    {
        // Additional safety
        if (!_isInitialized) InitializeReferences();

        _inventoryPanel.SetActive(false);
        _inventoryName.SetActive(false);
        InventoryManager.Instance.OnInventoryUpdated += RefreshAllSlots;
        InitializeSlots();
    }

    private void Update()
    {
        // Toggle with keyboard
        if (Input.GetKeyDown(_toggleKey))
        {
            Debug.Log("New Input System detected 'I' key");
            ToggleInventory();
        }
    }

    public void ToggleInventory()
    {
        if (_inventoryPanel == null)
        {
            Debug.LogError("Inventory panel reference is null!");
            return;
        }

        try
        {
            _inventoryPanel.SetActive(!_inventoryPanel.activeSelf);
            _inventoryName.SetActive(!_inventoryName.activeSelf);
        }
        catch (MissingReferenceException)
        {
            Debug.LogError("Panel reference was lost - reinitializing...");
            InitializeReferences();
        }
    
    // Optional: Pause game when inventory is open
    // Time.timeScale = _inventoryPanel.activeSelf ? 0 : 1;

    }

    private void InitializeSlots()
    {
        if (_slotsParent == null)
        {
            Debug.LogError("Cannot initialize slots - parent transform missing!");
            return;
        }

        // Clear existing slots safely
        foreach (Transform child in _slotsParent)
        {
            if (child != null)
            {
                Destroy(child.gameObject);
            }
        }

        // Reinitialize with current manager slots
        for (int i = 0; i < InventoryManager.Instance.Slots.Count; i++)
        {
            if (_slotPrefab == null)
            {
                Debug.LogError("Slot prefab missing!");
                return;
            }

            GameObject slot = Instantiate(_slotPrefab, _slotsParent);
            slot.GetComponent<InventorySlotUI>().Initialize(i);
        }
    }

    private void RefreshAllSlots()
    {
        if (_slotsParent == null)
        {
            Debug.LogError("Slots parent reference lost!");
            return;
        }

        foreach (Transform child in _slotsParent)
        {
            if (child == null) continue; // Skip destroyed objects

            InventorySlotUI slotUI = child.GetComponent<InventorySlotUI>();
            if (slotUI != null)
            {
                slotUI.Refresh();
            }
        }
    }
}

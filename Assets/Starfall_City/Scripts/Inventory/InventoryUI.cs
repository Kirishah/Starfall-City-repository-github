using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private Transform _slotsParent; // The GridLayoutGroup panel
    [SerializeField] private GameObject _slotPrefab;

    private void Start()
    {
        // Initialize the UI when the game starts
        InventoryManager.Instance.OnInventoryUpdated += RefreshAllSlots;
        InitializeSlots();
    }

    private void InitializeSlots()
    {
        // Clear existing slots (if any)
        foreach (Transform child in _slotsParent) Destroy(child.gameObject);

        // Spawn slots equal to the inventory size
        for (int i = 0; i < InventoryManager.Instance.Slots.Count; i++)
        {
            GameObject slot = Instantiate(_slotPrefab, _slotsParent);
            slot.GetComponent<InventorySlotUI>().Initialize(i);
        }
    }

    private void RefreshAllSlots()
    {
        foreach (Transform child in _slotsParent)
        {
            child.GetComponent<InventorySlotUI>().Refresh();
        }
    }
}

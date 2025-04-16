using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _quantityText;

    private int _slotIndex; // Index in the InventoryManager.Slots list

    public void Initialize(int index)
    {
        _slotIndex = index;
        Refresh();
    }

    public void Refresh()
    {
        // Defer if Unity is updating assets
#if UNITY_EDITOR
        if (UnityEditor.EditorApplication.isUpdating)
        {
            UnityEditor.EditorApplication.delayCall += Refresh;
            return;
        }
#endif
        InventorySlot slot = InventoryManager.Instance.Slots[_slotIndex];

        // Update icon and quantity
        _icon.sprite = slot.Item?.Icon;
        _quantityText.text = (slot.Item != null && slot.Item.IsStackable)
            ? slot.Quantity.ToString()
            : "";

        _icon.gameObject.SetActive(slot.Item != null);
    }
}

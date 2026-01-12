using UnityEngine;

namespace InventorySystem
{
    [CreateAssetMenu(fileName = "Item", menuName = "Inventory/Item")]
    public class Item : ScriptableObject
    {
        [SerializeField] private string _itemID; // Still useful for saves, quests, debugging
        [SerializeField] private string _displayName;
        [SerializeField] private Sprite _icon;
        [SerializeField] private bool _isStackable = true;
        [SerializeField, Min(1)] private int _maxStack = 100;
        [TextArea][SerializeField] private string _description;

        public string ItemID => _itemID;
        public string Name => _displayName;
        public Sprite Icon => _icon;
        public bool IsStackable => _isStackable;
        public int MaxStack => _maxStack;
        public string Description => _description;

        private void OnValidate()
        {
            // Ensure MaxStack is 1 if not stackable
            if (!_isStackable)
                _maxStack = 1;

            // Optional: Auto-generate ID from name if empty (prevents blank IDs)
            if (string.IsNullOrEmpty(_itemID) && !string.IsNullOrEmpty(_displayName))
            {
                _itemID = _displayName.Replace(" ", "_").ToUpperInvariant();
            }
        }
    }
}

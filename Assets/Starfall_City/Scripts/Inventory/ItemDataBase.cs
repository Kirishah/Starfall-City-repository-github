using System.Collections.Generic;
using UnityEngine;

namespace InventorySystem
{
    public class ItemDataBase : MonoBehaviour
    {
        public static ItemDataBase Instance { get; private set; }

        [SerializeField] private List<Item> _items = new();

        // Cache 1: String ID → Item (for save/load)
        private readonly Dictionary<string, Item> _idToItem = new();

        // Cache 2: Item instance → Item (identity map, O(1), GC-free)
        // We use the object reference as key (possible because ScriptableObjects are singletons per asset)
        private readonly Dictionary<Item, Item> _instanceToItem = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            BuildCache();
        }

        private void BuildCache()
        {
            _idToItem.Clear();
            _instanceToItem.Clear();

            foreach (var item in _items)
            {
                if (item == null) continue;

                // String ID cache (for saves)
                if (!string.IsNullOrEmpty(item.ItemID))
                {
                    if (_idToItem.ContainsKey(item.ItemID))
                        Debug.LogWarning($"Duplicate ItemID: {item.ItemID} ({item.Name})");
                    else
                        _idToItem[item.ItemID] = item;
                }
                else
                {
                    Debug.LogWarning($"Item {item.name} has no ItemID! Auto-generating recommended.");
                }

                // Instance reference cache
                if (!_instanceToItem.ContainsKey(item))
                    _instanceToItem[item] = item;
                else
                    Debug.LogWarning($"Duplicate item instance detected: {item.Name}");
            }
        }

        // Fastest: Direct reference lookup (use this whenever you have an Item reference)
        public bool TryGetItem(Item itemReference, out Item item)
        {
            if (itemReference == null)
            {
                item = null;
                return false;
            }

            return _instanceToItem.TryGetValue(itemReference, out item);
        }

        // Fallback: Lookup by string ID (needed for loading saves)
        public Item GetItemByID(string itemID)
        {
            if (string.IsNullOrEmpty(itemID))
                return null;

            _idToItem.TryGetValue(itemID, out var item);
            if (item == null)
            {
                Debug.LogWarning($"Item with ID '{itemID}' not found in database.");
            }
            return item;
        }

        // Optional: Expose all items
        public IReadOnlyList<Item> AllItems => _items;

#if UNITY_EDITOR
        // Helper for editor: Rebuild cache when items change
        private void OnValidate()
        {
            if (Application.isPlaying)
                BuildCache();
        }
#endif
    }
}

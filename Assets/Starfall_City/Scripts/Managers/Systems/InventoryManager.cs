using QuestSystem;
using QTE;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace InventorySystem
{
    public class InventoryManager : MonoBehaviour, QTEGameManager.IRPGComponent
    {
        public static InventoryManager Instance { get; private set; }

        [SerializeField] private int _inventorySize = 20;
        public List<InventorySlot> Slots = new();

        // Уведомление UI об изменении инвентаря
        public UnityAction OnInventoryUpdated;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializeSlots();
        }

        private void InitializeSlots()
        {
            Slots.Clear();
            for (var i = 0; i < _inventorySize; i++)
            {
                Slots.Add(new InventorySlot(null, 0));
            }
        }

        public bool AddItem(Item item, int quantity)
        {
            if (item == null || quantity <= 0)
            {
                Debug.LogWarning("Attempted to add invalid item or quantity.");
                return false;
            }

            int initialQuantity = GetTotalQuantity(item);
            var added = false;

            if (item.IsStackable)
            {
                // Check how many items in the inventory
                var stack = Slots.Find(slot => slot.Item == item && slot.Quantity < item.MaxStack);
                if (stack != null)
                {
                    var spaceRemaining = stack.Item.MaxStack - stack.Quantity;
                    var addAmount = Mathf.Min(spaceRemaining, quantity);
                    stack.Add(addAmount);
                    quantity -= addAmount;
                    added = true;
                }
            }

            // Adding to new slots
            while (quantity > 0)
            {
                var emptySlot = Slots.Find(slot => slot.Item == null);
                if (emptySlot == null)
                {
                    Debug.Log("Inventory full!");
                    return false; // Inventory full
                }

                var addAmount = Mathf.Min(item.MaxStack, quantity);
                emptySlot.Item = item;
                emptySlot.Quantity = addAmount;
                quantity -= addAmount;
                added = true;
            }
            if (added)
            {
                var newQuantity = GetTotalQuantity(item);
                var addedQuantity = newQuantity - initialQuantity;
                // Уведомление QuestManager об обновлении инвентаря
                QuestManager.Instance.HandleObjectiveUpdate(ObjectiveType.Collection, item.ItemID);
                Debug.Log($"Added to new slot: ItemID={item.ItemID}, Amount={addedQuantity}");
            }

            OnInventoryUpdated?.Invoke(); // Обновление UI
            return true;
        }

        public bool RemoveItem(Item item, int quantity)
        {
            if (quantity <= 0)
            {
                Debug.LogWarning("Attempted to remove zero or negative quantity.");
                return false;
            }

            int totalAvailable = GetTotalQuantity(item);
            if (totalAvailable < quantity)
            {
                Debug.Log($"Not enough {item.Name} to remove (need {quantity}, have {totalAvailable}).");
                return false;
            }

            if (item.IsStackable)
            {
                // Чек сколько удалить из слотов, которые могут стакаться
                var remainingToRemove = quantity;
                for (var i = Slots.Count - 1; i >= 0; i--)
                {
                    var slot = Slots[i];
                    if (slot.Item == item && slot.Quantity > 0)
                    {
                        int removable = Mathf.Min(remainingToRemove, slot.Quantity);
                        slot.Remove(removable);
                        remainingToRemove -= removable;

                        if (remainingToRemove <= 0) break;
                    }
                }
            }
            else
            {
                // Удаление из слотов, которые не могут стакаться
                var removedCount = 0;
                foreach (var slot in Slots)
                {
                    if (slot.Item == item)
                    {
                        slot.Remove(1);
                        removedCount++;

                        if (removedCount >= quantity) break;
                    }
                }
            }

            OnInventoryUpdated?.Invoke();
            return true;
        }

        public bool HasItem(Item item, int quantity)
        {
            if (item == null || quantity <= 0)
            {
                Debug.LogWarning("Attempted to check invalid item or quantity.");
                return false;
            }
            int totalAvailable = GetTotalQuantity(item);
            return totalAvailable >= quantity;
        }

        private int GetTotalQuantity(Item item)
        {
            var total = 0;
            foreach (var slot in Slots)
            {
                if (slot.Item == item)
                {
                    total += slot.Quantity;
                }
            }
            return total;
        }

        public void SaveInventory(ref SaveData data)
        {
            data.InventoryItemIDs.Clear();
            data.InventoryQuantities.Clear();

            foreach (var slot in Slots)
            {
                data.InventoryItemIDs.Add(slot.Item != null ? slot.Item.ItemID : null);
                data.InventoryQuantities.Add(slot.Quantity);
            }
        }

        public void LoadInventory(SaveData data)
        {
            for (int i = 0; i < Slots.Count && i < data.InventoryItemIDs.Count; i++)
            {
                string itemID = data.InventoryItemIDs[i];
                Item item = string.IsNullOrEmpty(itemID)
                    ? null
                    : ItemDataBase.Instance.GetItemByID(itemID);

                Slots[i].Item = item;
                Slots[i].Quantity = data.InventoryQuantities[i];
            }
            OnInventoryUpdated?.Invoke();
        }
    }
}

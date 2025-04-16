using System.Collections.Generic;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.Events;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [SerializeField] private int _inventorySize = 20;
    public List<InventorySlot> Slots = new List<InventorySlot>();

    // Event to notify UI when inventory changes
    public UnityAction OnInventoryUpdated;

    private void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;

        InitializeSlots();
    }

    private void InitializeSlots()
    {
        for (int i = 0; i < _inventorySize; i++)
        {
            Slots.Add(new InventorySlot(null, 0));
        }
    }

    public bool AddItem(Item item, int quantity)
    {
        if (item.IsStackable)
        {
            // Check for existing stack
            InventorySlot stack = Slots.Find(slot => slot.Item == item && slot.Quantity < item.MaxStack);
            if (stack != null)
            {
                int spaceRemaining = stack.Item.MaxStack - stack.Quantity;
                int addAmount = Mathf.Min(spaceRemaining, quantity);
                stack.Add(addAmount);
                quantity -= addAmount;
            }
        }

        // Add remaining quantity to empty slots
        while (quantity > 0)
        {
            InventorySlot emptySlot = Slots.Find(slot => slot.Item == null);
            if (emptySlot == null)
            {
                Debug.Log("Inventory full!");
                return false; // Inventory full
            }

            int addAmount = Mathf.Min(item.MaxStack, quantity);
            emptySlot.Item = item;
            emptySlot.Quantity = addAmount;
            quantity -= addAmount;
        }

        OnInventoryUpdated?.Invoke(); // Refresh UI
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
            // Remove from newest stacks first (iterate backwards)
            int remainingToRemove = quantity;
            for (int i = Slots.Count - 1; i >= 0; i--)
            {
                InventorySlot slot = Slots[i];
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
            // Remove individual non-stackable items
            int removedCount = 0;
            foreach (InventorySlot slot in Slots)
            {
                if (slot.Item == item)
                {
                    slot.Remove(1); // Will clear the slot (Quantity becomes 0)
                    removedCount++;

                    if (removedCount >= quantity) break;
                }
            }
        }

        OnInventoryUpdated?.Invoke();
        return true;
    }

    private int GetTotalQuantity(Item item)
    {
        int total = 0;
        foreach (InventorySlot slot in Slots)
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

        foreach (InventorySlot slot in Slots)
        {
            data.InventoryItemIDs.Add(slot.Item?.ItemID ?? "");
            data.InventoryQuantities.Add(slot.Quantity);
        }
    }

    public void LoadInventory(SaveData data)
    {
        for (int i = 0; i < Slots.Count; i++)
        {
            string itemID = data.InventoryItemIDs[i];
            Item item = ItemDataBase.Instance.GetItemByID(itemID);
            Slots[i].Item = item;
            Slots[i].Quantity = data.InventoryQuantities[i];
        }
        OnInventoryUpdated?.Invoke();
    }
}

using UnityEngine;

[System.Serializable]
public class InventorySlot
{
    public Item Item;
    public int Quantity;

    public InventorySlot(Item item, int quantity)
    {
        Item = item;
        Quantity = quantity;
    }

    public void Add(int amount) => Quantity += amount;
    public void Remove(int amount) => Quantity -= amount;
}

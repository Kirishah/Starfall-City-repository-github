using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Inventory", menuName = "Inventory/Inventory")]
public class Inventory : ScriptableObject
{
    public List<Item> Items = new List<Item>();

    public void AddItem(Item item) => Items.Add(item);
    public void RemoveItem(Item item) => Items.Remove(item);
}

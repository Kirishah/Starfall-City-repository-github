using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveData : MonoBehaviour
{
    public Vector3 Position;
    public Quaternion Rotation;
    public string CurrentOutfit;
    public int Health;
    // Add stats, quest progress, etc.

    // Inventory
    public List<string> InventoryItemIDs = new List<string>();
    public List<int> InventoryQuantities = new List<int>();

}

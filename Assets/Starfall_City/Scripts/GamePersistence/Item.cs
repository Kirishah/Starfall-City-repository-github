using UnityEngine;

[System.Serializable]
public class Item
{
    public string ItemID;
    public string Name;
    public Sprite Icon;
}

[System.Serializable]
public class Equipment : Item
{
    public EquipmentSlot Slot; // e.g., Head, Body, Weapon
}

[System.Serializable]
public enum EquipmentSlot
{
    Head,
    Body,
    Legs,
    Weapon,
    Accessory
    // Add more slots as needed
}

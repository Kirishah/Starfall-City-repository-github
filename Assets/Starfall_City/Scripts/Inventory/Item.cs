using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    public string ItemID;
    public string Name;
    public Sprite Icon;
    public bool IsStackable = true;
    public int MaxStack = 99;
    [TextArea] public string Description;

    private void OnValidate()
    {
        if (!IsStackable)
        {
            MaxStack = 1; 
        }
    }
}

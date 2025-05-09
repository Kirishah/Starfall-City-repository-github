using System.Collections.Generic;
using UnityEngine;

public class ItemDataBase : MonoBehaviour
{
    public static ItemDataBase Instance { get; private set; }

    [SerializeField] private List<Item> _items;

    private void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;
    }

    public Item GetItemByID(string itemID)
    {
        return _items.Find(item => item.ItemID == itemID);
    }
}

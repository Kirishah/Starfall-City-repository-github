using TMPro;
using UnityEngine;

public class ObjectInteractable : Interactable
{
    [Header("Item Settings")]
    [SerializeField] private Item _item; // SO в инспекторе
    [SerializeField] private int _quantity = 1;
    [SerializeField] private string itemID;

    private bool _isPickedUp;

    public override void Interact()
    {
        if (_isPickedUp) return;

        // Добавление предмета в инвентарь
        bool success = InventoryManager.Instance.AddItem(_item, _quantity);
        if (success)
        {
            _isPickedUp = true;
            HidePrompt();
            gameObject.SetActive(false);
        }
        else
        {
            Debug.Log("Inventory full!");
        }

        // Call base for quest progress (e.g., if this is a Collection or Interaction objective)
        base.Interact();
    }
    public override string GetIdentifier() => itemID;
}

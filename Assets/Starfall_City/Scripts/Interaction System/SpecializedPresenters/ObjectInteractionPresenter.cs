using InventorySystem;
using UnityEngine;


namespace Interaction
{
    public class ObjectInteractionPresenter : InteractionPresenter
    {
        [SerializeField] private Item _item;
        [SerializeField] private int _quantity = 1;

        private bool _isPickedUp;

        protected override void PerformInteraction()
        {
            if (_isPickedUp || _item == null) return;

            bool success = InventoryManager.Instance.AddItem(_item, _quantity);
            if (success)
            {
                _isPickedUp = true;
                view.HidePrompt();
                gameObject.SetActive(false);
            }
            else
            {
                Debug.Log("Inventory full!");
            }
        }

        public override string GetIdentifier() => _item != null ? _item.ItemID : "";
    }
}

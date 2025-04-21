using UnityEngine;

public class HudTweaker : MonoBehaviour
{
    public InventoryUI iTweaker;

    private void InventoryOpen()
    {
        iTweaker.ToggleInventory();
    }
}

using TMPro;
using UnityEngine;

public class HudTweaker : MonoBehaviour
{
    public InventoryUI iTweaker;
    public PhoneInterfaceController phoneController;
    public UIManager menuTweaker;

    public void InventoryOpen()
    {
        iTweaker.ToggleInventory();
    }

    public void JournalOpen()
    {
        phoneController.TogglePhone();
    }

}

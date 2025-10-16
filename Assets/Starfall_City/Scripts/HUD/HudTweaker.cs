using TMPro;
using UnityEngine;

public class HudTweaker : MonoBehaviour
{
    public InventoryUI iTweaker;
    public QuestUI qTweaker;
    public UIManager menuTweaker;

    public void InventoryOpen()
    {
        iTweaker.ToggleInventory();
    }

    public void JournalOpen()
    {
        qTweaker.ToggleQuestLog();
    }

    public void MenuOpen()
    {
        menuTweaker.ToggleMenuBar();
    }
}

using UnityEngine;

public class QuestItemPickup : MonoBehaviour
{
    [SerializeField] private QuestSO _questToStart;
    [SerializeField] private string _itemID; 

    // Вызывается системой инвентаря при взаимодействии с предметом
    public void OnItemPickedUp(string pickedUpItemID)
    {
        if (pickedUpItemID == _itemID && !QuestManager.Instance.IsQuestActive(_questToStart))
        {
            QuestManager.Instance.StartQuest(_questToStart);
            Debug.Log($"Quest {_questToStart.name} started by picking up item {_itemID}.");
        }
    }
}

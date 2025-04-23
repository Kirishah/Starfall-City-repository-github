using UnityEngine;

public class QuestLocationTrigger : MonoBehaviour
{
    [SerializeField] private QuestSO _questToStart;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !QuestManager.Instance.IsQuestActive(_questToStart))
        {
            QuestManager.Instance.StartQuest(_questToStart);
            Debug.Log($"Quest {_questToStart.name} started by entering location.");
        }
    }
}

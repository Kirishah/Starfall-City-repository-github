using UnityEngine;

namespace QuestSystem
{
    public class QuestLocationTrigger : MonoBehaviour
    {
        [SerializeField] private QuestSO _questToStart;

        private void OnTriggerEnter(Collider other)
        {
            Debug.Log("Trigger entered by: " + other.gameObject.name);
            if (QuestManager.Instance == null) { Debug.LogError("QuestManager instance is null!"); }
            if (other.CompareTag("Player") && !QuestManager.Instance.IsQuestActive(_questToStart))
            {
                QuestManager.Instance.StartQuest(_questToStart);
                Debug.Log($"Quest {_questToStart.name} started by entering location.");
            }
        }
    }
}

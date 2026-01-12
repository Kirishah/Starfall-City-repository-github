using UnityEngine;

namespace QuestSystem
{
    public class LocationTrigger : MonoBehaviour
    {
        [SerializeField] private string _locationID;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                Debug.Log($"Player entered location: {_locationID}");
                QuestManager.Instance.HandleObjectiveUpdate(ObjectiveType.Exploration, _locationID);
            }
        }
    }
}

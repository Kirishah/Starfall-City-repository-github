using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    [Tooltip("Unique ID used by ScriptedEvent to place objects here")]
    public string spawnId; // e.g., "Player_Start", "Boss_Entrance"
}

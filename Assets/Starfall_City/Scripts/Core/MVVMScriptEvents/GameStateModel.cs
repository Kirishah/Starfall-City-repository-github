using System.Collections.Generic;
using UnityEngine;

public class GameStateModel : MonoBehaviour
{
    public static GameStateModel Instance { get; private set; }
    public Dictionary<string, bool> EventFlags = new Dictionary<string, bool>(); // e.g., HasCompletedIntro

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public bool IsEventCompleted(string eventId) => EventFlags.TryGetValue(eventId, out bool completed) && completed;
    public void SetEventCompleted(string eventId, bool completed = true) => EventFlags[eventId] = completed;
}

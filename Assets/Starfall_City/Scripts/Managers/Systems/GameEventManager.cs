using System.Collections.Generic;
using UnityEngine;

public class GameEventManager : MonoBehaviour
{
    public static GameEventManager Instance { get; private set; }
    private HashSet<string> _triggeredEvents = new HashSet<string>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        LoadEvents();
    }

    public void TriggerEvent(string eventID)
    {
        if (!_triggeredEvents.Contains(eventID))
        {
            _triggeredEvents.Add(eventID);
            SaveEvents();
            Debug.Log($"Game event triggered: {eventID}");
        }
    }

    public bool IsEventTriggered(string eventID)
    {
        return _triggeredEvents.Contains(eventID);
    }

    private void SaveEvents()
    {
        string json = string.Join(",", _triggeredEvents);
        PlayerPrefs.SetString("TriggeredEvents", json);
        PlayerPrefs.Save();
    }

    private void LoadEvents()
    {
        string json = PlayerPrefs.GetString("TriggeredEvents", "");
        if (!string.IsNullOrEmpty(json))
        {
            string[] events = json.Split(',');
            _triggeredEvents.Clear();
            foreach (string eventID in events)
            {
                if (!string.IsNullOrEmpty(eventID))
                {
                    _triggeredEvents.Add(eventID);
                }
            }
            Debug.Log($"Loaded triggered events: {string.Join(", ", _triggeredEvents)}");
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

public class EventBus : MonoBehaviour
{
    public static EventBus Instance { get; private set; }
    private Dictionary<string, System.Action> simpleSubscribers = new Dictionary<string, System.Action>();
    private Dictionary<string, System.Action<Dictionary<string, object>>> paramSubscribers = new Dictionary<string, System.Action<Dictionary<string, object>>>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // Simple publish/subscribe (for triggers without params)
    public void Subscribe(string eventType, System.Action callback)
    {
        if (!simpleSubscribers.ContainsKey(eventType)) simpleSubscribers[eventType] = callback;
        else simpleSubscribers[eventType] += callback;
    }

    public void Publish(string eventType)
    {
        if (simpleSubscribers.TryGetValue(eventType, out System.Action callback)) callback?.Invoke();
    }

    // Parameterized: Publish with overrides (e.g., { "deskPos", Vector3 }, { "bossInitialPos", Vector3 })
    public void Subscribe<T>(string eventType, System.Action<T> callback) where T : new()
    {
        // Simplified: Use Dictionary<string, object> as param type
        if (!paramSubscribers.ContainsKey(eventType)) paramSubscribers[eventType] = (params) => callback((T)(object)params);
        else paramSubscribers[eventType] += (params) => callback((T)(object)params);
    }

    public void Publish<T>(string eventType, T parameters) where T : new()
    {
        if (paramSubscribers.TryGetValue(eventType, out var callback))
        {
            var dict = parameters as Dictionary<string, object> ?? new T() as Dictionary<string, object>;
            callback(dict);
        }
    }

}

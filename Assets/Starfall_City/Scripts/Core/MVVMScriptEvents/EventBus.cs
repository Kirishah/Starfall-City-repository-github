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

    // Simple
    public void Subscribe(string eventType, System.Action callback)
    {
        if (!simpleSubscribers.ContainsKey(eventType)) simpleSubscribers[eventType] = callback;
        else simpleSubscribers[eventType] += callback;
    }

    public void Unsubscribe(string eventType, System.Action callback)
    {
        if (simpleSubscribers.TryGetValue(eventType, out var subs)) subs -= callback;
    }

    public void Publish(string eventType)
    {
        if (simpleSubscribers.TryGetValue(eventType, out System.Action callback)) callback?.Invoke();
    }

    // Parameterized (FIXED: Direct Dictionary, no brittle <T>)
    public void Subscribe(string eventType, System.Action<Dictionary<string, object>> callback)
    {
        if (!paramSubscribers.ContainsKey(eventType)) paramSubscribers[eventType] = callback;
        else paramSubscribers[eventType] += callback;
    }

    public void Unsubscribe(string eventType, System.Action<Dictionary<string, object>> callback)
    {
        if (paramSubscribers.TryGetValue(eventType, out var subs)) subs -= callback;
    }

    public void Publish(string eventType, Dictionary<string, object> parameters = null)
    {
        if (paramSubscribers.TryGetValue(eventType, out var callback))
        {
            callback(parameters ?? new Dictionary<string, object>());
        }
    }
}
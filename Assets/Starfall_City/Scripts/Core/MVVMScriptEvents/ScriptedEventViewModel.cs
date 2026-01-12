using System.Collections.Generic;
using UnityEngine;


namespace core
{
    public class ScriptedEventViewModel : MonoBehaviour
    {
        [SerializeField] private EventSubscriptionConfig _eventSubscriptionConfig;
        [SerializeField] private ScriptedEventExecutor _executor;

        private Dictionary<string, System.Action> _simpleCallbacks = new();
        private Dictionary<string, System.Action<Dictionary<string, object>>> _paramCallbacks = new();

        private bool _subscriptionsSetup = false;

        private void Awake() => TrySetupSubscriptions();
        private void Start() => TrySetupSubscriptions();

        private void TrySetupSubscriptions()
        {
            if (_subscriptionsSetup || EventBus.Instance == null) return;
            SetupSubscriptions();
        }

        private void SetupSubscriptions()
        {
            _subscriptionsSetup = true;
            if (_eventSubscriptionConfig == null) { Debug.LogWarning("No EventSubscriptionConfig!"); return; }

            foreach (var sub in _eventSubscriptionConfig.subscriptions)
            {
                if (string.IsNullOrEmpty(sub.sourceEventType) || string.IsNullOrEmpty(sub.targetEventId)) continue;

                if (sub.expectsParams)
                {
                    var callback = new System.Action<Dictionary<string, object>>(p => _ = _executor.ExecuteEventAsync(sub.targetEventId, p));
                    _paramCallbacks[sub.sourceEventType] = callback;
                    EventBus.Instance.Subscribe(sub.sourceEventType, callback);
                }
                else
                {
                    var callback = new System.Action(() => _ = _executor.ExecuteEventAsync(sub.targetEventId));
                    _simpleCallbacks[sub.sourceEventType] = callback;
                    EventBus.Instance.Subscribe(sub.sourceEventType, callback);
                }
            }
        }

        private void OnDestroy()
        {
            if (EventBus.Instance == null) return;
            foreach (var kvp in _simpleCallbacks) EventBus.Instance.Unsubscribe(kvp.Key, kvp.Value);
            foreach (var kvp in _paramCallbacks) EventBus.Instance.Unsubscribe(kvp.Key, kvp.Value);
        }
    }
}

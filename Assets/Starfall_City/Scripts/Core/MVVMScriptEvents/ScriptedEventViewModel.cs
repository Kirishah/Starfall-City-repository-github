using System.Collections.Generic;
using UnityEngine;


namespace core
{
    public class ScriptedEventViewModel : MonoBehaviour
    {
        [SerializeField] private EventSubscriptionConfig eventSubscriptionConfig;
        [SerializeField] private ScriptedEventExecutor executor;

        private Dictionary<string, System.Action> simpleCallbacks = new Dictionary<string, System.Action>();
        private Dictionary<string, System.Action<Dictionary<string, object>>> paramCallbacks = new Dictionary<string, System.Action<Dictionary<string, object>>>();

        private bool subscriptionsSetup = false;

        private void Awake() => TrySetupSubscriptions();
        private void Start() => TrySetupSubscriptions();

        private void TrySetupSubscriptions()
        {
            if (subscriptionsSetup || EventBus.Instance == null) return;
            SetupSubscriptions();
        }

        private void SetupSubscriptions()
        {
            subscriptionsSetup = true;
            if (eventSubscriptionConfig == null) { Debug.LogWarning("No EventSubscriptionConfig!"); return; }

            foreach (var sub in eventSubscriptionConfig.subscriptions)
            {
                if (string.IsNullOrEmpty(sub.sourceEventType) || string.IsNullOrEmpty(sub.targetEventId)) continue;

                if (sub.expectsParams)
                {
                    var callback = new System.Action<Dictionary<string, object>>(p => _ = executor.ExecuteEventAsync(sub.targetEventId, p));
                    paramCallbacks[sub.sourceEventType] = callback;
                    EventBus.Instance.Subscribe(sub.sourceEventType, callback);
                }
                else
                {
                    var callback = new System.Action(() => _ = executor.ExecuteEventAsync(sub.targetEventId));
                    simpleCallbacks[sub.sourceEventType] = callback;
                    EventBus.Instance.Subscribe(sub.sourceEventType, callback);
                }
            }
        }

        private void OnDestroy()
        {
            if (EventBus.Instance == null) return;
            foreach (var kvp in simpleCallbacks) EventBus.Instance.Unsubscribe(kvp.Key, kvp.Value);
            foreach (var kvp in paramCallbacks) EventBus.Instance.Unsubscribe(kvp.Key, kvp.Value);
        }
    }
}
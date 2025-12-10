using System;
using System.Collections.Generic;
using UnityEngine;

namespace core
{
    [CreateAssetMenu(fileName = "EventSubscriptionConfig", menuName = "Game/EventSubscriptionConfig")]
    public class EventSubscriptionConfig : ScriptableObject
    {
        [System.Serializable]
        public class Subscription
        {
            [Tooltip("The event type published by the EventBus that triggers this subscription. Why: Maps external events (e.g., scene loads or dialogue ends) to internal scripted events for decoupled triggering. Example: 'SceneLoaded:Office' to auto-run setup actions on scene entry.")]
            public string sourceEventType; // e.g., "SceneLoaded:Office"

            [Tooltip("The ID of the ScriptedEvent asset to execute when the source event is published. Why: Identifies which predefined event sequence to run, enabling dynamic execution without hardcoded mappings. Example: 'PreIntro' for initial player positioning and animations.")]
            public string targetEventId; // e.g., "PreIntro"

            [Tooltip("Whether the callback expects a Dictionary<string, object> parameter from the publisher. Why: Determines if the event execution should handle runtime parameters (e.g., positions) or run parameter-free; ensures type-safe invocation. Example: True for 'IntroStart' which passes dynamic 'deskPos' for boss movement.")]
            public bool expectsParams; // True for dict callbacks
        }

        [Tooltip("List of all event subscriptions to configure dynamically at runtime. Why: Centralizes subscription mappings in a single, editable asset for easy management, testing, and reuse across scenes without code changes. Example: Add entries for scene loads, dialogue ends, or custom triggers.")]
        public List<Subscription> subscriptions = new List<Subscription>();
    } 
}
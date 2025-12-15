using UnityEngine;
using UnityEngine.UIElements;

namespace QTE
{
    [CreateAssetMenu(fileName = "QTEConfig", menuName = "QTE/QTEConfig")]
    public class QTEConfig : ScriptableObject
    {
        [Tooltip("Unique identifier for this QTE configuration. Used by ScriptedEvent and QuestManager.")]
        public string qteId = "default";

        [Header("Tutorial")]
        [Tooltip("If true, a tutorial banner will be shown at the very start of this QTE. Player must press the close button to continue.")]
        public bool showTutorialBanner = false;

        [Tooltip("Optional reference to a custom UIToolkit VisualTreeAsset that will be displayed as the tutorial banner. If null, the default one from QTEGameManager will be used.")]
        public VisualTreeAsset tutorialBannerAsset;

        [Header("Arrow Movement")]
        [SerializeField, Tooltip("Speed at which arrows move across the screen (pixels per second).")]
        public float arrowMoveSpeed = 200f;

        [SerializeField, Tooltip("Range of the hit zone in pixels, relative to canvas width (e.g., 1920).")]
        public Vector2 hitZoneRange = new(-100f, 0f);

        [SerializeField, Tooltip("Time window for detecting a double click (seconds).")]
        public float doubleClickThreshold = 0.2f;

        [SerializeField, Tooltip("Time interval between arrow spawns (seconds).")]
        public float beatInterval = 0.5f;

        [SerializeField, Tooltip("Duration required to hold an arrow to succeed (seconds).")]
        public float holdDuration = 2f;

        [SerializeField, Tooltip("Time in seconds before first arrow spawns")]
        [Min(0f)] public float initialSpawnDelay = 2.0f;

        [Header("Scoring")]
        [SerializeField, Tooltip("Base points awarded for a successful QTE input.")]
        [Min(0)] public int basePoints = 100;

        [SerializeField, Tooltip("Multiplier applied to score based on combo count.")]
        [Range(0f, 1f)] public float comboMultiplier = 0.1f;

        [Header("QTE Timing")]
        [SerializeField, Tooltip("Total duration of the QTE (seconds).")]
        [Min(0f)] public float qteDuration = 90f;

        [Header("Audio")]
        [SerializeField, Tooltip("Number of AudioSource components in the pool for one-shot sounds.")]
        [Min(1)] public int audioSourcePoolSize = 10;

        [Header("Music")]
        [Tooltip("Music track played during this specific QTE. Overrides DanceGameManager default if assigned.")]
        public AudioClip musicTrack;

        [Header("NPC Rival AI")]
        [Tooltip("Enable NPC rival dancer in this QTE.")]
        public bool enableRival = true;

        [Tooltip("NPC reaction accuracy: 1.0 = perfect, 0.9 = misses 10% of inputs.")]
        [Range(0.7f, 1.0f)] public float rivalAccuracy = 0.95f;

        [Tooltip("NPC reaction delay in seconds (simulates human-like delay).")]
        [Range(0.0f, 0.3f)] public float rivalReactionDelay = 0.08f;

        [Tooltip("Chance (0–1) that NPC intentionally misses a Hold or Double arrow to feel human.")]
        [Range(0f, 0.3f)] public float rivalIntentionalMissChance = 0.1f;

        [Tooltip("NPC combo streak bonus scaling (higher = more aggressive comeback).")]
        [Range(0.5f, 2f)] public float rivalComboAggression = 1.2f;

        private void OnValidate()
        {
            if (arrowMoveSpeed < 0f)
            {
                Debug.LogWarning("ArrowMoveSpeed should be non-negative.", this);
                arrowMoveSpeed = 0f;
            }
            if (hitZoneRange.x > hitZoneRange.y)
            {
                Debug.LogWarning("HitZoneRange.x should be less than or equal to HitZoneRange.y.", this);
                hitZoneRange.x = hitZoneRange.y;
            }
            if (beatInterval < 0.1f)
            {
                Debug.LogWarning("BeatInterval should be at least 0.1 seconds.", this);
                beatInterval = 0.1f;
            }
            if (holdDuration < 0f)
            {
                Debug.LogWarning("HoldDuration should be non-negative.", this);
                holdDuration = 0f;
            }
            if (doubleClickThreshold < 0f)
            {
                Debug.LogWarning("DoubleClickThreshold should be non-negative.", this);
                doubleClickThreshold = 0f;
            }
            if (basePoints < 0)
            {
                Debug.LogWarning("BasePoints should be non-negative.", this);
                basePoints = 0;
            }
            if (comboMultiplier < 0f || comboMultiplier > 1f)
            {
                Debug.LogWarning("ComboMultiplier should be between 0 and 1.", this);
                comboMultiplier = Mathf.Clamp(comboMultiplier, 0f, 1f);
            }
            if (qteDuration < 0f)
            {
                Debug.LogWarning("QTEDuration should be non-negative.", this);
                qteDuration = 0f;
            }
            if (initialSpawnDelay < 0f)
            {
                Debug.LogWarning("initialSpawnDelay should be non-negative.", this);
                initialSpawnDelay = 0f;
            }
            if (audioSourcePoolSize < 1)
            {
                Debug.LogWarning("AudioSourcePoolSize should be at least 1.", this);
                audioSourcePoolSize = 1;
            }
            rivalAccuracy = Mathf.Clamp01(rivalAccuracy);
            rivalReactionDelay = Mathf.Clamp(rivalReactionDelay, 0f, 0.3f);
            rivalIntentionalMissChance = Mathf.Clamp01(rivalIntentionalMissChance);
        }
    } 
}

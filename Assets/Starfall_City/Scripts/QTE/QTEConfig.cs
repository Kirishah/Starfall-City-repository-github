using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace QTE
{
    [CreateAssetMenu(fileName = "QTEConfig", menuName = "QTE/QTEConfig")]
    public class QTEConfig : ScriptableObject
    {
        [Tooltip("Unique ID used by quests and events. Used by ScriptedEvent and QuestManager.")]
        public string qteId = "default";


        [Header("Tutorial")]
        [Tooltip("If true, a tutorial banner will be shown at the very start of this QTE.")]
        public bool showTutorialBanner = false;
        [Tooltip("Optional reference to a custom UIToolkit VisualTreeAsset that will be displayed as the tutorial banner. If null, the default one from QTEGameManager will be used.")]
        public VisualTreeAsset tutorialBannerAsset;

        [Header("Hit Detection")]
        [Tooltip("Range of the hit zone in pixels, relative to canvas width (e.g., 1920).")]
        public Vector2 hitZoneRange = new(-100f, 0f);
        [Tooltip("Time window for detecting a double click (seconds).")]
        public float doubleClickThreshold = 0.2f;
        [Tooltip("Duration required to hold an arrow to succeed (seconds).")]
        public float holdDuration = 2f;


        [Header("Scoring")]
        [Tooltip("Base points awarded for a successful QTE input.")]
        [Min(0)] public int basePoints = 100;
        [Tooltip("Multiplier applied to score based on combo count.")]
        [Range(0f, 1f)] public float comboMultiplier = 0.1f;


        [Header("Rhythm & Timing")]
        [Tooltip("Exact spawn times (seconds from song start). If empty → uniform BPM spawning.")]
        public List<float> beatSpawnTimes = new();
        [Tooltip("Beats Per Minute — used for uniform spawning and arrow travel speed.")]
        [Min(60f)] public float bpm = 120f;
        [Tooltip("How many beats an arrow takes to reach the hit zone (e.g., 4 = one full measure).")]
        [Range(1, 8)] public int beatsToHitZone = 4;
        [Tooltip("Global offset in seconds (positive = spawn earlier, negative = later).")]
        public float beatOffset = 0f;
        [Tooltip("Total duration of the QTE (seconds).")]
        [Min(0f)] public float qteDuration = 90f;
        [Tooltip("Delay before the arrows start spawn")]
        [Min(0f)] public float initialSpawnDelay = 2f;


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

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Assets/Generate Uniform Beats")]
        private static void GenerateUniformBeats()
        {
            if (UnityEditor.Selection.activeObject is not QTEConfig config) return;

            config.beatSpawnTimes.Clear();
            float beatDuration = 60f / config.bpm;
            float maxTime = config.qteDuration;
            for (float t = config.beatOffset; t < maxTime; t += beatDuration)
            {
                config.beatSpawnTimes.Add(t);
            }
            UnityEditor.EditorUtility.SetDirty(config);
            Debug.Log($"Generated {config.beatSpawnTimes.Count} uniform beats for {config.name}");
        }

        [MenuItem("Assets/Generate Uniform Beats", true)]
        private static bool ValidateGenerateUniformBeats() => Selection.activeObject is QTEConfig;
#endif
    }
}

using UnityEngine;

[CreateAssetMenu(fileName = "QTEConfig", menuName = "QTE/QTEConfig")]
public class QTEConfig : ScriptableObject
{
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
        if (audioSourcePoolSize < 1)
        {
            Debug.LogWarning("AudioSourcePoolSize should be at least 1.", this);
            audioSourcePoolSize = 1;
        }
    }
}

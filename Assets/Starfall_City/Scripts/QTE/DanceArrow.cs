using UnityEngine;
using DanceInputActions;
using UnityEngine.UI;

public class DanceArrow : MonoBehaviour
{
    public enum ArrowType
    {
        Single,  // Click once
        Hold,    // Hold the key
        Double   // Click twice
    }
    public ArrowType type = ArrowType.Single; // Default to single-click
    public string direction; // "Up", "Down", "Left", "Right"
    private float moveSpeed = 200f;
    private RectTransform rectTransform;
    private Vector2 startPosition;

    public bool IsInHitZone { get; private set; }
    private bool hasPassedHitZone;

    // Visual elements
    [SerializeField] private GameObject singleClickVisual; // Basic arrow
    [SerializeField] private GameObject holdVisual;        // Arrow with tail or bar
    [SerializeField] private GameObject doubleClickVisual; // Stacked arrow or "2x" symbol
    [SerializeField] private Image holdProgressBar; // Progress bar for hold notes
    public float holdDuration = 2f;

    private float holdTimer;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError($"RectTransform missing on {gameObject.name}! Ensure this is a UI element with a RectTransform.", this);
            return;
        }
        startPosition = new Vector2(1000, rectTransform.anchoredPosition.y);
    }

    void Start()
    {
        hasPassedHitZone = false;
        IsInHitZone = false;

        // Enable the correct visual based on arrow type
        singleClickVisual.SetActive(type == ArrowType.Single);
        holdVisual.SetActive(type == ArrowType.Hold);
        doubleClickVisual.SetActive(type == ArrowType.Double);
    }

    public void ResetArrow()
    {
        if (rectTransform == null)
        {
            Debug.LogError($"RectTransform is null in ResetArrow for {gameObject.name}!", this);
            return;
        }
        rectTransform.anchoredPosition = startPosition;
        gameObject.SetActive(true);
        hasPassedHitZone = false;
        IsInHitZone = false;
        holdTimer = 0f; // Reset hold timer
        if (holdProgressBar != null) holdProgressBar.fillAmount = 0f;
        DanceInput.Instance?.RegisterArrow(this);
    }

    public void UpdateHoldProgress(float elapsedTime)
    {
        if (type == ArrowType.Hold && holdProgressBar != null)
        {
            holdTimer = elapsedTime;
            holdProgressBar.fillAmount = Mathf.Clamp01(holdTimer / holdDuration);
        }
    }

    void Update()
    {
        if (!QTEGameManager.IsQTEActive) return;

        if (rectTransform == null) return;
        // Move arrow leftward (adjust axis based on your UI setup)
        if (!DanceInput.IsHolding) // Only move when not holding
        {
            rectTransform.anchoredPosition += Vector2.left * moveSpeed * Time.unscaledDeltaTime;
        }


        // Define hit zone (e.g., between x = -100 and x = 0)
        float xPos = rectTransform.anchoredPosition.x;
        IsInHitZone = xPos <= 0 && xPos >= -100;

        // Check if arrow has passed the hit zone
        if (xPos < -100 && !hasPassedHitZone)
        {
            hasPassedHitZone = true;
            if (gameObject.activeInHierarchy) // Only trigger miss if not hit
            {
                DanceGameManager.Instance.HandleMiss();
                DanceInput.Instance?.ReturnArrowToPool(this);
            }
        }

        // Deactivate if off-screen
        if (xPos < -1000)
        {
            gameObject.SetActive(false);
            DanceInput.Instance?.ReturnArrowToPool(this);
        }
    }
}

using UnityEngine;
using DanceInputActions;

public class DanceArrow : MonoBehaviour
{
    public string direction; // "Up", "Down", "Left", "Right"
    private float moveSpeed = 200f;
    private RectTransform rectTransform;
    private Vector2 startPosition;

    public bool IsInHitZone { get; private set; }
    private bool hasPassedHitZone;

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
        DanceInput.Instance?.RegisterArrow(this);
        if (DanceInput.Instance != null)
        {
            DanceInput.Instance.RegisterArrow(this);
        }
        else
        {
            Debug.LogWarning("DanceInput.Instance is null in DanceArrow.ResetArrow. Ensure DanceInput is in the scene and initialized.");
        }
    }

    void Update()
    {
        if (rectTransform == null) return;
        // Move arrow leftward (adjust axis based on your UI setup)
        rectTransform.anchoredPosition += Vector2.left * moveSpeed * Time.unscaledDeltaTime;

        
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

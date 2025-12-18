using UnityEngine;
using System;

namespace QTE
{
    public class DanceArrow : MonoBehaviour
    {
        public enum ArrowType
        {
            Single,  // Click once
            Hold,    // Hold the key
            Double   // Click twice
        }
        public ArrowType type = ArrowType.Single; // Default to single-click
        public ArrowDirection direction; // "Up", "Down", "Left", "Right"
        [SerializeField] private QTEConfig config; // Centralized config

        private Canvas canvas;
        private RectTransform rectTransform;
        private Vector2 startPosition;
        private float currentTravelTime;

        public bool IsInHitZone { get; private set; }
        public bool hasPassedHitZone { get; private set; }

        // Event fired when arrow enters hit zone (for rival AI)
        public static event Action<DanceArrow> OnArrowEnteredHitZone;

        // Visual elements
        [SerializeField] private GameObject singleClickVisual; // Basic arrow
        [SerializeField] private GameObject holdVisual;        // Arrow with tail or bar
        [SerializeField] private GameObject doubleClickVisual; // Stacked arrow or "2x" symbol

        void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null || config == null || singleClickVisual == null || holdVisual == null || doubleClickVisual == null)
            {
                Debug.LogError($"Missing required components on {gameObject.name}!", this);
                enabled = false;
                return;
            }
            canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                float canvasWidth = canvas.GetComponent<RectTransform>().rect.width;
                startPosition = new Vector2(canvasWidth / 2f + 100f, rectTransform.anchoredPosition.y); // Start off-screen right
            }
            else
            {
                Debug.LogError($"Canvas not found in parent hierarchy of {gameObject.name}!", this);
                enabled = false;
            }
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
            currentTravelTime = 0f; // Will be set by spawner
            gameObject.SetActive(true);
            hasPassedHitZone = false;
            IsInHitZone = false;

            DanceInput.Instance?.RegisterArrow(this);
            // Explicitly disable all visuals before re-enabling correct one
            if (singleClickVisual != null) singleClickVisual.SetActive(false);
            if (holdVisual != null) holdVisual.SetActive(false);
            if (doubleClickVisual != null) doubleClickVisual.SetActive(false);

            // Re-apply visual states on reset
            if (singleClickVisual != null)
                singleClickVisual.SetActive(type == ArrowType.Single);
            if (holdVisual != null)
                holdVisual.SetActive(type == ArrowType.Hold);
            if (doubleClickVisual != null)
                doubleClickVisual.SetActive(type == ArrowType.Double);
            Debug.Log($"ResetArrow: Type={type}, " +
                $"singleClickVisual={singleClickVisual != null && singleClickVisual.activeSelf}, " +
                $"holdVisual={holdVisual != null && holdVisual.activeSelf}, " +
                $"doubleClickVisual={doubleClickVisual != null && doubleClickVisual.activeSelf}", this);
        }

        void Update()
        {
            if (!QTEGameManager.IsQTEActive || QTEGameManager.IsQTEPaused || rectTransform == null) return;

            bool wasInHitZone = IsInHitZone;

            if (currentTravelTime > 0f && canvas != null && this != DanceInput.CurrentHoldArrow)
            {
                // Calculate required speed: distance / time
                float canvasWidth = canvas.GetComponent<RectTransform>().rect.width;
                float distanceToHitZone = startPosition.x - config.hitZoneRange.y; // from start to right edge of hit zone

                float speed = distanceToHitZone / currentTravelTime;
                rectTransform.anchoredPosition += Vector2.left * speed * Time.deltaTime;
            }

            // Define hit zone (e.g., between x = -100 and x = 0)
            float xPos = rectTransform.anchoredPosition.x;
            float hitZoneStart = canvas != null ? canvas.GetComponent<RectTransform>().rect.width * config.hitZoneRange.x / 1920f : config.hitZoneRange.x;
            float hitZoneEnd = config.hitZoneRange.y;
            IsInHitZone = xPos <= hitZoneEnd && xPos >= hitZoneStart;

            if (!wasInHitZone && IsInHitZone)
            {
                OnArrowEnteredHitZone?.Invoke(this);
                if (DanceInput.IsHolding && this != DanceInput.CurrentHoldArrow)
                {
                    DanceGameManager.Instance.HandleArrowEvent(direction, type, true);
                    DanceInput.Instance?.ReturnArrowToPool(this);
                    return;  // Skip miss/off-screen checks
                }
            }

            // Check if arrow has passed the hit zone (Skip for hold arrow)
            if (this != DanceInput.CurrentHoldArrow && xPos < hitZoneStart && !hasPassedHitZone)
            {
                hasPassedHitZone = true;
                if (gameObject.activeInHierarchy) 
                {
                    DanceGameManager.Instance.HandleMiss();
                    DanceInput.Instance?.ReturnArrowToPool(this);
                }
            }

            // Deactivate if off-screen
            if ( this != DanceInput.CurrentHoldArrow && xPos < -canvas.GetComponent<RectTransform>().rect.width * 0.5f)
            {
                gameObject.SetActive(false);
                DanceInput.Instance?.ReturnArrowToPool(this);
            }
        }

        public void SetTravelTime(float travelTime)
        {
            currentTravelTime = travelTime;
        }
    } 
}

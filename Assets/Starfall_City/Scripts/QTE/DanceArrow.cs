using UnityEngine;
using DanceInputActions;
using UnityEngine.UI;
using UnityEngine.UIElements;

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
        private RectTransform rectTransform;
        private Vector2 startPosition;

        public bool IsInHitZone { get; private set; }
        private bool hasPassedHitZone;

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
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                float canvasWidth = canvas.GetComponent<RectTransform>().rect.width;
                startPosition = new Vector2(canvasWidth * 0.5f, rectTransform.anchoredPosition.y); // Start off-screen right
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

            // Move arrow leftward (adjust axis based on your UI setup)
            if (!DanceInput.IsHolding) // Only move when not holding
            {
                rectTransform.anchoredPosition += Vector2.left * config.arrowMoveSpeed * Time.deltaTime;
            }


            // Define hit zone (e.g., between x = -100 and x = 0)
            float xPos = rectTransform.anchoredPosition.x;
            Canvas canvas = GetComponentInParent<Canvas>();
            float hitZoneStart = canvas != null ? canvas.GetComponent<RectTransform>().rect.width * config.hitZoneRange.x / 1920f : config.hitZoneRange.x;
            float hitZoneEnd = config.hitZoneRange.y;
            IsInHitZone = xPos <= hitZoneEnd && xPos >= hitZoneStart;

            // Check if arrow has passed the hit zone
            if (xPos < hitZoneStart && !hasPassedHitZone)
            {
                hasPassedHitZone = true;
                if (gameObject.activeInHierarchy) // Only trigger miss if not hit
                {
                    DanceGameManager.Instance.HandleMiss();
                    DanceInput.Instance?.ReturnArrowToPool(this);
                }
            }

            // Deactivate if off-screen
            if (xPos < -canvas.GetComponent<RectTransform>().rect.width * 0.5f)
            {
                gameObject.SetActive(false);
                DanceInput.Instance?.ReturnArrowToPool(this);
            }
        }
    } 
}

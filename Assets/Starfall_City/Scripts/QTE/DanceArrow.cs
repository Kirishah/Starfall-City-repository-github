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
        [SerializeField] private QTEConfig _config; // Centralized config

        private Canvas _canvas;
        private RectTransform _rectTransform;
        private Vector2 _startPosition;
        private float _currentTravelTime;

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
            _rectTransform = GetComponent<RectTransform>();
            if (_rectTransform == null || _config == null || singleClickVisual == null || holdVisual == null || doubleClickVisual == null)
            {
                Debug.LogError($"Missing required components on {gameObject.name}!", this);
                enabled = false;
                return;
            }
            _canvas = GetComponentInParent<Canvas>();
            if (_canvas != null)
            {
                float canvasWidth = _canvas.GetComponent<RectTransform>().rect.width;
                _startPosition = new Vector2((canvasWidth / 2f) + 100f, _rectTransform.anchoredPosition.y); // Start off-screen right
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
            if (_rectTransform == null)
            {
                Debug.LogError($"RectTransform is null in ResetArrow for {gameObject.name}!", this);
                return;
            }
            _rectTransform.anchoredPosition = _startPosition;
            _currentTravelTime = 0f; // Will be set by spawner
            gameObject.SetActive(true);
            hasPassedHitZone = false;
            IsInHitZone = false;

            DanceInput.Instance.RegisterArrow(this);
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
            if (!QTEGameManager.IsQTEActive || QTEGameManager.IsQTEPaused || _rectTransform == null) return;

            var wasInHitZone = IsInHitZone;

            if (_currentTravelTime > 0f && _canvas != null && this != DanceInput.CurrentHoldArrow)
            {
                // Calculate required speed: distance / time
                float distanceToHitZone = _startPosition.x - _config.hitZoneRange.y; // from start to right edge of hit zone

                float speed = distanceToHitZone / _currentTravelTime;
                _rectTransform.anchoredPosition += speed * Time.deltaTime * Vector2.left;
            }

            // Define hit zone (e.g., between x = -100 and x = 0)
            float xPos = _rectTransform.anchoredPosition.x;
            float hitZoneStart = _canvas != null ? _canvas.GetComponent<RectTransform>().rect.width * _config.hitZoneRange.x / 1920f : _config.hitZoneRange.x;
            float hitZoneEnd = _config.hitZoneRange.y;
            IsInHitZone = xPos <= hitZoneEnd && xPos >= hitZoneStart;

            if (!wasInHitZone && IsInHitZone)
            {
                OnArrowEnteredHitZone?.Invoke(this);
                if (DanceInput.IsHolding && this != DanceInput.CurrentHoldArrow)
                {
                    DanceGameManager.Instance.HandleArrowEvent(direction, type, true);
                    DanceInput.Instance.ReturnArrowToPool(this);
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
                    DanceInput.Instance.ReturnArrowToPool(this);
                }
            }

            // Deactivate if off-screen
            if ( this != DanceInput.CurrentHoldArrow && xPos < -_canvas.GetComponent<RectTransform>().rect.width * 0.5f)
            {
                gameObject.SetActive(false);
                DanceInput.Instance.ReturnArrowToPool(this);
            }
        }

        public void SetTravelTime(float travelTime) => _currentTravelTime = travelTime;
    }
}

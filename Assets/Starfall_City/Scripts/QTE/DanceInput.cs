using PlayerInputActions;
using MagicPigGames;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace QTE
{
    public class DanceInput : MonoBehaviour
    {
        public static DanceInput Instance { get; private set; }
        public static bool IsHolding { get; private set; } // Flag to pause flow

        public static DanceArrow CurrentHoldArrow { get; private set; }

        public delegate void ArrowEvent(ArrowDirection direction, DanceArrow.ArrowType type, bool success);
        public static event ArrowEvent OnArrowEvent;

        [SerializeField] private QTEConfig _config;
        [SerializeField] private DanceArrowPool _arrowPool;
        [SerializeField] private ProgressBar _holdProgressBar;
        private readonly Dictionary<ArrowDirection, List<DanceArrow>> _activeArrowsByDirection = new();

        private float _holdStartTime;
        private readonly Dictionary<ArrowDirection, float> _lastPressTimes = new(); // Track last press time per direction

        // Add a flag to track if we're waiting for a second click for double arrows
        private readonly Dictionary<ArrowDirection, DanceArrow> _pendingDoubleClickArrows = new();

        private PlayerControls _controls;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _controls = new PlayerControls();

            // Initialize all direction lists
            _activeArrowsByDirection[ArrowDirection.Up] = new List<DanceArrow>();
            _activeArrowsByDirection[ArrowDirection.Down] = new List<DanceArrow>();
            _activeArrowsByDirection[ArrowDirection.Left] = new List<DanceArrow>();
            _activeArrowsByDirection[ArrowDirection.Right] = new List<DanceArrow>();
        }

        // Called by QTEGameManager to enable input actions
        public void EnableInput()
        {
            if (_controls == null)
            {
                Debug.LogError("DanceControls is null in DanceInput", this);
                return;
            }
            // Clear any previous subscriptions
            DisableInput();

            // Subscribe to all events
            _controls.DanceActions.Up.performed += OnUpPerformed;
            _controls.DanceActions.Up.canceled += OnUpCanceled;
            _controls.DanceActions.Down.performed += OnDownPerformed;
            _controls.DanceActions.Down.canceled += OnDownCanceled;
            _controls.DanceActions.Left.performed += OnLeftPerformed;
            _controls.DanceActions.Left.canceled += OnLeftCanceled;
            _controls.DanceActions.Right.performed += OnRightPerformed;
            _controls.DanceActions.Right.canceled += OnRightCanceled;

            _controls.Enable();
            if (_holdProgressBar != null) _holdProgressBar.gameObject.SetActive(false);
        }

        // Called by QTEGameManager to disable input actions
        public void DisableInput()
        {
            if (_controls != null)
            {
                // Unsubscribe from all events
                _controls.DanceActions.Up.performed -= OnUpPerformed;
                _controls.DanceActions.Up.canceled -= OnUpCanceled;
                _controls.DanceActions.Down.performed -= OnDownPerformed;
                _controls.DanceActions.Down.canceled -= OnDownCanceled;
                _controls.DanceActions.Left.performed -= OnLeftPerformed;
                _controls.DanceActions.Left.canceled -= OnLeftCanceled;
                _controls.DanceActions.Right.performed -= OnRightPerformed;
                _controls.DanceActions.Right.canceled -= OnRightCanceled;

                _controls.Disable();
            }
            ClearRegisteredArrows();
        }

        // Separate methods for each input to ensure proper subscription
        private void OnUpPerformed(InputAction.CallbackContext context) => HandleInput(ArrowDirection.Up);
        private void OnUpCanceled(InputAction.CallbackContext context) => HandleInputRelease(ArrowDirection.Up);
        private void OnDownPerformed(InputAction.CallbackContext context) => HandleInput(ArrowDirection.Down);
        private void OnDownCanceled(InputAction.CallbackContext context) => HandleInputRelease(ArrowDirection.Down);
        private void OnLeftPerformed(InputAction.CallbackContext context) => HandleInput(ArrowDirection.Left);
        private void OnLeftCanceled(InputAction.CallbackContext context) => HandleInputRelease(ArrowDirection.Left);
        private void OnRightPerformed(InputAction.CallbackContext context) => HandleInput(ArrowDirection.Right);
        private void OnRightCanceled(InputAction.CallbackContext context) => HandleInputRelease(ArrowDirection.Right);

        // Register an arrow when spawned
        public void RegisterArrow(DanceArrow arrow)
        {
            if (!_activeArrowsByDirection.ContainsKey(arrow.direction))
                _activeArrowsByDirection[arrow.direction] = new List<DanceArrow>();
            if (!_activeArrowsByDirection[arrow.direction].Contains(arrow))
            {
                _activeArrowsByDirection[arrow.direction].Add(arrow);
                Debug.Log($"Registered {arrow.direction} arrow. Total in direction: {_activeArrowsByDirection[arrow.direction].Count}");
            }
        }

        // Unregister an arrow when returned to pool
        public void UnregisterArrow(DanceArrow arrow)
        {
            if (_activeArrowsByDirection.ContainsKey(arrow.direction))
            {
                _activeArrowsByDirection[arrow.direction].Remove(arrow);
                Debug.Log($"Unregistered {arrow.direction} arrow. Total in direction: {_activeArrowsByDirection[arrow.direction].Count}");
            }

            // Also remove from pending double clicks if needed
            if (_pendingDoubleClickArrows.ContainsKey(arrow.direction) && _pendingDoubleClickArrows[arrow.direction] == arrow)
            {
                _pendingDoubleClickArrows.Remove(arrow.direction);
            }
        }

        public void ClearRegisteredArrows()
        {
            foreach (var kvp in _activeArrowsByDirection)
            {
                kvp.Value.Clear();
            }
            _pendingDoubleClickArrows.Clear();
            _lastPressTimes.Clear();
            IsHolding = false;
            CurrentHoldArrow = null;
            if (_holdProgressBar != null)
            {
                _holdProgressBar.SetProgress(0f);
                _holdProgressBar.gameObject.SetActive(false);
            }
            Debug.Log("Cleared all registered arrows and reset input state in DanceInput", this);
        }

        private void HandleInput(ArrowDirection direction)
        {
            if (!QTEGameManager.IsQTEActive || QTEGameManager.IsQTEPaused || _config == null) return; // Exit early if QTE not active

            float currentTime = Time.time;
            var isDoubleClick = false;

            // Check if this is a potential double-click
            if (_lastPressTimes.ContainsKey(direction))
            {
                float timeSinceLastPress = currentTime - _lastPressTimes[direction];
                if (timeSinceLastPress <= _config.doubleClickThreshold) // 0.2 seconds threshold for double-click
                {
                    isDoubleClick = true;
                    _lastPressTimes.Remove(direction); // Reset after detecting double-click
                }
            }
            _lastPressTimes[direction] = currentTime; // Update last press time

            // Check if we have a pending double-click arrow for this direction
            if (_pendingDoubleClickArrows.ContainsKey(direction) && isDoubleClick)
            {
                var arrow = _pendingDoubleClickArrows[direction];
                if (arrow != null && arrow.gameObject.activeInHierarchy && arrow.IsInHitZone)
                {
                    OnArrowEvent?.Invoke(direction, arrow.type, true);
                    ReturnArrowToPool(arrow);
                    _pendingDoubleClickArrows.Remove(direction);
                    return;
                }
            }

            if (!_activeArrowsByDirection.ContainsKey(direction) || _activeArrowsByDirection[direction].Count == 0)
            {
                OnArrowEvent?.Invoke(direction, DanceArrow.ArrowType.Single, false);
                return;
            }

            // Check active arrows for a hit
            foreach (var arrow in _activeArrowsByDirection[direction])
            {
                if (arrow.IsInHitZone && arrow.gameObject.activeInHierarchy)
                {

                    if (arrow.type == DanceArrow.ArrowType.Hold)
                    {
                        IsHolding = true;
                        CurrentHoldArrow = arrow;
                        _holdStartTime = Time.time;

                        if (_holdProgressBar != null)
                        {
                            _holdProgressBar.SetProgress(0f);
                            _holdProgressBar.gameObject.SetActive(true); // Optional: Show if hidden
                        }

                        OnArrowEvent?.Invoke(direction, arrow.type, true);
                    }
                    else if (arrow.type == DanceArrow.ArrowType.Single)
                    {
                        OnArrowEvent?.Invoke(direction, arrow.type, true);
                        ReturnArrowToPool(arrow);
                    }
                    else if (arrow.type == DanceArrow.ArrowType.Double)
                    {
                        if (isDoubleClick)
                        {
                            OnArrowEvent?.Invoke(direction, arrow.type, true);
                            ReturnArrowToPool(arrow);
                        }
                        else
                        {
                            // Store for potential double-click
                            _pendingDoubleClickArrows[direction] = arrow;
                        }
                    }
                    return;
                }
            }

            // No matching arrow in hit zone
            OnArrowEvent?.Invoke(direction, DanceArrow.ArrowType.Single, false);

            // If no arrow matched, check if it was a stale double-click attempt
            if (_pendingDoubleClickArrows.ContainsKey(direction))
            {
                _pendingDoubleClickArrows.Remove(direction);
            }
        }

        private void HandleInputRelease(ArrowDirection direction)
        {
            if (!QTEGameManager.IsQTEActive || QTEGameManager.IsQTEPaused || !IsHolding || CurrentHoldArrow == null ||
                CurrentHoldArrow.direction != direction) return;

            float elapsed = Time.time - _holdStartTime;
            var success = elapsed >= _config.holdDuration;
            OnArrowEvent?.Invoke(direction, DanceArrow.ArrowType.Hold, success);

            // Reset the progress bar before returning to pool
            if (_holdProgressBar != null)
            {
                _holdProgressBar.SetProgress(0f);
                _holdProgressBar.gameObject.SetActive(false); // Optional: Hide when not holding
            }

            if (CurrentHoldArrow != null)
            {
                ReturnArrowToPool(CurrentHoldArrow);
            }
            IsHolding = false;
            CurrentHoldArrow = null;

        }

        private void Update()
        {
            if (!QTEGameManager.IsQTEActive || QTEGameManager.IsQTEPaused || !IsHolding || CurrentHoldArrow == null || _config == null) return;

            float elapsed = Time.time - _holdStartTime;
            float progress = Mathf.Clamp01(elapsed / _config.holdDuration);

            if (_holdProgressBar != null)
            {
                _holdProgressBar.SetProgress(progress);
            }

            if (elapsed >= _config.holdDuration)
            {
                DanceGameManager.Instance.HandleArrowEvent(CurrentHoldArrow.direction, DanceArrow.ArrowType.Hold, true);
                // Reset the progress bar before returning to pool
                if (_holdProgressBar != null)
                {
                    _holdProgressBar.SetProgress(0f);
                    _holdProgressBar.gameObject.SetActive(false); // Optional
                }
                ReturnArrowToPool(CurrentHoldArrow);
                IsHolding = false;
                CurrentHoldArrow = null;
            }

            // Cleanup stale lastPressTimes and pending doubles
            List<ArrowDirection> toRemove = new List<ArrowDirection>();
            foreach (var kvp in _lastPressTimes)
            {
                if (Time.time - kvp.Value > _config.doubleClickThreshold * 2) // Twice threshold for safety
                {
                    toRemove.Add(kvp.Key);
                }
            }
            foreach (var dir in toRemove)
            {
                _lastPressTimes.Remove(dir);
                if (_pendingDoubleClickArrows.ContainsKey(dir))
                {
                    _pendingDoubleClickArrows.Remove(dir);
                }
            }
        }

        // Pooling version of input handling
        public void ReturnArrowToPool(DanceArrow arrow)
        {
            if (_arrowPool != null && arrow != null)
            {
                _arrowPool.ReturnArrow(arrow);
                UnregisterArrow(arrow);
            }
            else
            {
                Debug.LogError($"ArrowPool or arrow is null in DanceInput! arrowPool={_arrowPool}, arrow={arrow}", this);
            }
        }

        // Clean up when destroyed
        private void OnDestroy()
        {
            DisableInput();
            if (_controls != null)
            {
                _controls.Dispose();
            }
        }
    }
}

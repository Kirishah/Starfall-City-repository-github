using DanceInputActions;
using MagicPigGames;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using static Unity.Collections.AllocatorManager;

namespace QTE
{
    public class DanceInput : MonoBehaviour
    {
        public static DanceInput Instance { get; private set; }
        public static bool IsHolding { get; private set; } // Flag to pause flow

        public static DanceArrow CurrentHoldArrow { get; private set; }

        public delegate void ArrowEvent(ArrowDirection direction, DanceArrow.ArrowType type, bool success);
        public static event ArrowEvent OnArrowEvent;

        [SerializeField] private QTEConfig config;
        [SerializeField] private DanceArrowPool arrowPool;
        [SerializeField] private ProgressBar holdProgressBar;
        private Dictionary<ArrowDirection, List<DanceArrow>> activeArrowsByDirection = new();

        private float holdStartTime;
        private Dictionary<ArrowDirection, float> lastPressTimes = new(); // Track last press time per direction

        // Add a flag to track if we're waiting for a second click for double arrows
        private Dictionary<ArrowDirection, DanceArrow> pendingDoubleClickArrows = new();

        private @DanceControls controls;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            controls = new DanceControls();

            // Initialize all direction lists
            activeArrowsByDirection[ArrowDirection.Up] = new List<DanceArrow>();
            activeArrowsByDirection[ArrowDirection.Down] = new List<DanceArrow>();
            activeArrowsByDirection[ArrowDirection.Left] = new List<DanceArrow>();
            activeArrowsByDirection[ArrowDirection.Right] = new List<DanceArrow>();
        }

        // Called by QTEGameManager to enable input actions
        public void EnableInput()
        {
            if (controls == null)
            {
                Debug.LogError("DanceControls is null in DanceInput", this);
                return;
            }
            // Clear any previous subscriptions
            DisableInput();

            // Subscribe to all events
            controls.DanceActions.Up.performed += OnUpPerformed;
            controls.DanceActions.Up.canceled += OnUpCanceled;
            controls.DanceActions.Down.performed += OnDownPerformed;
            controls.DanceActions.Down.canceled += OnDownCanceled;
            controls.DanceActions.Left.performed += OnLeftPerformed;
            controls.DanceActions.Left.canceled += OnLeftCanceled;
            controls.DanceActions.Right.performed += OnRightPerformed;
            controls.DanceActions.Right.canceled += OnRightCanceled;

            controls.Enable();
            if (holdProgressBar != null) holdProgressBar.gameObject.SetActive(false);
        }

        // Called by QTEGameManager to disable input actions
        public void DisableInput()
        {
            if (controls != null)
            {
                // Unsubscribe from all events
                controls.DanceActions.Up.performed -= OnUpPerformed;
                controls.DanceActions.Up.canceled -= OnUpCanceled;
                controls.DanceActions.Down.performed -= OnDownPerformed;
                controls.DanceActions.Down.canceled -= OnDownCanceled;
                controls.DanceActions.Left.performed -= OnLeftPerformed;
                controls.DanceActions.Left.canceled -= OnLeftCanceled;
                controls.DanceActions.Right.performed -= OnRightPerformed;
                controls.DanceActions.Right.canceled -= OnRightCanceled;

                controls.Disable();
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
            if (!activeArrowsByDirection.ContainsKey(arrow.direction))
                activeArrowsByDirection[arrow.direction] = new List<DanceArrow>();
            if (!activeArrowsByDirection[arrow.direction].Contains(arrow))
            {
                activeArrowsByDirection[arrow.direction].Add(arrow);
                Debug.Log($"Registered {arrow.direction} arrow. Total in direction: {activeArrowsByDirection[arrow.direction].Count}");
            }
        }

        // Unregister an arrow when returned to pool
        public void UnregisterArrow(DanceArrow arrow)
        {
            if (activeArrowsByDirection.ContainsKey(arrow.direction))
            {
                activeArrowsByDirection[arrow.direction].Remove(arrow);
                Debug.Log($"Unregistered {arrow.direction} arrow. Total in direction: {activeArrowsByDirection[arrow.direction].Count}");
            }

            // Also remove from pending double clicks if needed
            if (pendingDoubleClickArrows.ContainsKey(arrow.direction) && pendingDoubleClickArrows[arrow.direction] == arrow)
            {
                pendingDoubleClickArrows.Remove(arrow.direction);
            }
        }

        public void ClearRegisteredArrows()
        {
            foreach (var kvp in activeArrowsByDirection)
            {
                kvp.Value.Clear();
            }
            pendingDoubleClickArrows.Clear();
            lastPressTimes.Clear();
            IsHolding = false;
            CurrentHoldArrow = null;
            if (holdProgressBar != null)
            {
                holdProgressBar.SetProgress(0f);
                holdProgressBar.gameObject.SetActive(false);
            }
            Debug.Log("Cleared all registered arrows and reset input state in DanceInput", this);
        }

        private void HandleInput(ArrowDirection direction)
        {
            if (!QTEGameManager.IsQTEActive || QTEGameManager.IsQTEPaused || config == null) return; // Exit early if QTE not active

            float currentTime = Time.time;
            bool isDoubleClick = false;

            // Check if this is a potential double-click
            if (lastPressTimes.ContainsKey(direction))
            {
                float timeSinceLastPress = currentTime - lastPressTimes[direction];
                if (timeSinceLastPress <= config.doubleClickThreshold) // 0.2 seconds threshold for double-click
                {
                    isDoubleClick = true;
                    lastPressTimes.Remove(direction); // Reset after detecting double-click
                }
            }
            lastPressTimes[direction] = currentTime; // Update last press time

            // Check if we have a pending double-click arrow for this direction
            if (pendingDoubleClickArrows.ContainsKey(direction) && isDoubleClick)
            {
                DanceArrow arrow = pendingDoubleClickArrows[direction];
                if (arrow != null && arrow.gameObject.activeInHierarchy && arrow.IsInHitZone)
                {
                    OnArrowEvent?.Invoke(direction, arrow.type, true);
                    ReturnArrowToPool(arrow);
                    pendingDoubleClickArrows.Remove(direction);
                    return;
                }
            }

            if (!activeArrowsByDirection.ContainsKey(direction) || activeArrowsByDirection[direction].Count == 0)
            {
                OnArrowEvent?.Invoke(direction, DanceArrow.ArrowType.Single, false);
                return;
            }

            // Check active arrows for a hit
            foreach (DanceArrow arrow in activeArrowsByDirection[direction])
            {
                if (arrow.IsInHitZone && arrow.gameObject.activeInHierarchy)
                {

                    if (arrow.type == DanceArrow.ArrowType.Hold)
                    {
                        IsHolding = true;
                        CurrentHoldArrow = arrow;
                        holdStartTime = Time.time;

                        if (holdProgressBar != null)
                        {
                            holdProgressBar.SetProgress(0f);
                            holdProgressBar.gameObject.SetActive(true); // Optional: Show if hidden
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
                            pendingDoubleClickArrows[direction] = arrow;
                        }
                    }
                    return;
                }
            }

            // No matching arrow in hit zone
            OnArrowEvent?.Invoke(direction, DanceArrow.ArrowType.Single, false);

            // If no arrow matched, check if it was a stale double-click attempt
            if (pendingDoubleClickArrows.ContainsKey(direction))
            {
                pendingDoubleClickArrows.Remove(direction);
            }
        }

        private void HandleInputRelease(ArrowDirection direction)
        {
            if (!QTEGameManager.IsQTEActive || QTEGameManager.IsQTEPaused || !IsHolding || CurrentHoldArrow == null || 
                CurrentHoldArrow.direction != direction) return;

            float elapsed = Time.time - holdStartTime;
            bool success = elapsed >= config.holdDuration;
            OnArrowEvent?.Invoke(direction, DanceArrow.ArrowType.Hold, success);

            // Reset the progress bar before returning to pool
            if (holdProgressBar != null)
            {
                holdProgressBar.SetProgress(0f);
                holdProgressBar.gameObject.SetActive(false); // Optional: Hide when not holding
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
            if (!QTEGameManager.IsQTEActive || QTEGameManager.IsQTEPaused || !IsHolding || CurrentHoldArrow == null || config == null) return;

            float elapsed = Time.time - holdStartTime;
            float progress = Mathf.Clamp01(elapsed / config.holdDuration);

            if (holdProgressBar != null)
            {
                holdProgressBar.SetProgress(progress);
            }

            if (elapsed >= config.holdDuration)
            {
                DanceGameManager.Instance?.HandleArrowEvent(CurrentHoldArrow.direction, DanceArrow.ArrowType.Hold, true);
                // Reset the progress bar before returning to pool
                if (holdProgressBar != null)
                {
                    holdProgressBar.SetProgress(0f);
                    holdProgressBar.gameObject.SetActive(false); // Optional
                }
                ReturnArrowToPool(CurrentHoldArrow);
                IsHolding = false;
                CurrentHoldArrow = null;
            }

            // Cleanup stale lastPressTimes and pending doubles
            List<ArrowDirection> toRemove = new List<ArrowDirection>();
            foreach (var kvp in lastPressTimes)
            {
                if (Time.time - kvp.Value > config.doubleClickThreshold * 2) // Twice threshold for safety
                {
                    toRemove.Add(kvp.Key);
                }
            }
            foreach (var dir in toRemove)
            {
                lastPressTimes.Remove(dir);
                if (pendingDoubleClickArrows.ContainsKey(dir))
                {
                    pendingDoubleClickArrows.Remove(dir);
                }
            }
        }

        // Pooling version of input handling
        public void ReturnArrowToPool(DanceArrow arrow)
        {
            if (arrowPool != null && arrow != null)
            {
                arrowPool.ReturnArrow(arrow);
                UnregisterArrow(arrow);
            }
            else
            {
                Debug.LogError($"ArrowPool or arrow is null in DanceInput! arrowPool={arrowPool}, arrow={arrow}", this);
            }
        }

        // Clean up when destroyed
        private void OnDestroy()
        {
            DisableInput();
            if (controls != null)
            {
                controls.Dispose();
            }
        }
    } 
}

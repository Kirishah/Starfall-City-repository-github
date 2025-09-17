using DanceInputActions;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace QTE
{
    public class DanceInput : MonoBehaviour
    {
        public static DanceInput Instance { get; private set; }
        public static bool IsHolding { get; private set; } // Flag to pause flow

        public delegate void ArrowEvent(ArrowDirection direction, DanceArrow.ArrowType type, bool success);
        public static event ArrowEvent OnArrowEvent;

        [SerializeField] private QTEConfig config;
        [SerializeField] private DanceArrowPool arrowPool;
        private Dictionary<ArrowDirection, List<DanceArrow>> activeArrowsByDirection = new();
        private DanceArrow currentHoldArrow;
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
            DontDestroyOnLoad(gameObject);
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
                if (arrow != null && arrow.gameObject.activeInHierarchy)
                {
                    OnArrowEvent?.Invoke(direction, arrow.type, true);
                    DanceGameManager.Instance?.HandleArrowEvent(direction, arrow.type, true);
                    ReturnArrowToPool(arrow);
                    pendingDoubleClickArrows.Remove(direction);
                    return;
                }
            }

            if (!activeArrowsByDirection.ContainsKey(direction) || activeArrowsByDirection[direction].Count == 0)
            {
                OnArrowEvent?.Invoke(direction, DanceArrow.ArrowType.Single, false);
                DanceGameManager.Instance?.HandleMiss();
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
                        currentHoldArrow = arrow;
                        holdStartTime = Time.time;
                        OnArrowEvent?.Invoke(direction, arrow.type, true);
                    }
                    else if (arrow.type == DanceArrow.ArrowType.Single)
                    {
                        OnArrowEvent?.Invoke(direction, arrow.type, true);
                        DanceGameManager.Instance?.HandleArrowEvent(direction, arrow.type, true);
                        ReturnArrowToPool(arrow);
                    }
                    else if (arrow.type == DanceArrow.ArrowType.Double)
                    {
                        if (isDoubleClick)
                        {
                            OnArrowEvent?.Invoke(direction, arrow.type, true);
                            DanceGameManager.Instance?.HandleArrowEvent(direction, arrow.type, true);
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
            DanceGameManager.Instance?.HandleArrowEvent(direction, DanceArrow.ArrowType.Single, false);
        }

        private void HandleInputRelease(ArrowDirection direction)
        {
            if (!QTEGameManager.IsQTEActive || QTEGameManager.IsQTEPaused || !IsHolding || currentHoldArrow == null || currentHoldArrow.direction != direction) return;

            float elapsed = Time.time - holdStartTime;
            bool success = elapsed >= config.holdDuration;
            OnArrowEvent?.Invoke(direction, DanceArrow.ArrowType.Hold, success);
            DanceGameManager.Instance?.HandleArrowEvent(direction, DanceArrow.ArrowType.Hold, success);
            if (currentHoldArrow != null) currentHoldArrow.UpdateHoldProgress(elapsed); // Update progress bar before returning
            ReturnArrowToPool(currentHoldArrow);
            IsHolding = false;
            currentHoldArrow = null;

        }

        private void Update()
        {
            if (!QTEGameManager.IsQTEActive || QTEGameManager.IsQTEPaused || !IsHolding || currentHoldArrow == null || config == null) return;

            float elapsed = Time.time - holdStartTime;
            currentHoldArrow.UpdateHoldProgress(elapsed);

            if (elapsed >= config.holdDuration)
            {
                DanceGameManager.Instance?.HandleArrowEvent(currentHoldArrow.direction, DanceArrow.ArrowType.Hold, true);
                ReturnArrowToPool(currentHoldArrow);
                IsHolding = false;
                currentHoldArrow = null;
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

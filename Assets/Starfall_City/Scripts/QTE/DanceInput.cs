using DanceInputActions;
using System.Collections.Generic;
using UnityEngine;

public class DanceInput : MonoBehaviour
{
    public static DanceInput Instance { get; private set; }
    public static bool IsHolding { get; private set; } // Flag to pause flow

    public delegate void ArrowPressed(string direction);
    public static event ArrowPressed OnArrowPressed;

    [SerializeField] private DanceArrowPool arrowPool;
    private List<DanceArrow> activeArrows = new List<DanceArrow>();
    private DanceArrow currentHoldArrow;
    private float holdStartTime;
    private Dictionary<string, float> lastPressTimes = new Dictionary<string, float>(); // Track last press time per direction

    private @DanceControls controls;

    private void Awake()
    {
        controls = new DanceControls();

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return; 
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        controls.DanceActions.Up.performed += _ => HandleInput("Up");
        controls.DanceActions.Down.performed += _ => HandleInput("Down");
        controls.DanceActions.Left.performed += _ => HandleInput("Left");
        controls.DanceActions.Right.performed += _ => HandleInput("Right");
        controls.Enable();
    }

    private void OnDisable()
    {
        if (controls != null)
        {
            controls.Disable();
        }
    }

    // Register an arrow when spawned
    public void RegisterArrow(DanceArrow arrow)
    {
        if (!activeArrows.Contains(arrow))
        {
            activeArrows.Add(arrow);
        }
    }

    // Unregister an arrow when returned to pool
    public void UnregisterArrow(DanceArrow arrow)
    {
        activeArrows.Remove(arrow);
    }

    private void HandleInput(string direction)
    {
        float currentTime = Time.unscaledTime;
        bool isDoubleClick = false;

        // Check if this is a potential double-click
        if (lastPressTimes.ContainsKey(direction))
        {
            float timeSinceLastPress = currentTime - lastPressTimes[direction];
            if (timeSinceLastPress <= 0.2f) // 0.2 seconds threshold for double-click
            {
                isDoubleClick = true;
                lastPressTimes.Remove(direction); // Reset after detecting double-click
            }
        }
        lastPressTimes[direction] = currentTime; // Update last press time

        // Check active arrows for a hit
        foreach (DanceArrow arrow in activeArrows)
        {
            if (arrow.IsInHitZone && arrow.direction == direction && arrow.gameObject.activeInHierarchy)
            {
                if (arrow.type == DanceArrow.ArrowType.Hold)
                {
                    IsHolding = true;
                    currentHoldArrow = arrow;
                    holdStartTime = Time.unscaledTime; // Use unscaled for paused time
                }
                else if (arrow.type == DanceArrow.ArrowType.Single)
                {
                    DanceGameManager.Instance.HandleArrowPress(direction);
                    ReturnArrowToPool(arrow);
                    OnArrowPressed?.Invoke(direction);
                }
                else if (arrow.type == DanceArrow.ArrowType.Double)
                {
                    if (isDoubleClick)
                    {
                        DanceGameManager.Instance.HandleArrowPress(direction);
                        ReturnArrowToPool(arrow);
                        OnArrowPressed?.Invoke(direction);
                    }
                    // If not a double-click, wait for the second press
                    return; // Exit to allow time for second press
                }

                return;
            }
        }

        // No matching arrow in hit zone
        DanceGameManager.Instance.HandleMiss(); // No matching arrow in hit zone
    }

    private void Update()
    {
        if (!QTEGameManager.IsQTEActive) return;

        if (IsHolding && currentHoldArrow != null)
        {
            float elapsed = Time.unscaledTime - holdStartTime;
            currentHoldArrow.UpdateHoldProgress(elapsed);

            if (elapsed >= currentHoldArrow.holdDuration)
            {
                DanceGameManager.Instance.HandleArrowPress(currentHoldArrow.direction);
                ReturnArrowToPool(currentHoldArrow);
                IsHolding = false;
                currentHoldArrow = null;
            }
            else if (Input.GetKeyUp(KeyCode.UpArrow) && currentHoldArrow.direction == "Up" ||
                     Input.GetKeyUp(KeyCode.DownArrow) && currentHoldArrow.direction == "Down" ||
                     Input.GetKeyUp(KeyCode.LeftArrow) && currentHoldArrow.direction == "Left" ||
                     Input.GetKeyUp(KeyCode.RightArrow) && currentHoldArrow.direction == "Right")
            {
                if (elapsed < currentHoldArrow.holdDuration)
                {
                    DanceGameManager.Instance.HandleMiss();
                }
                ReturnArrowToPool(currentHoldArrow);
                IsHolding = false;
                currentHoldArrow = null;
            }
        }
    }

    // Pooling version of input handling
    public void ReturnArrowToPool(DanceArrow arrow)
    {
        if (arrowPool != null)
        {
            arrowPool.ReturnArrow(arrow);
            UnregisterArrow(arrow);
        }
        else
        {
            Debug.LogError("ArrowPool is null in DanceInput!");
        }
    }
}

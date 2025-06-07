using DanceInputActions;
using System.Collections.Generic;
using UnityEngine;

public class DanceInput : MonoBehaviour
{
    public static DanceInput Instance { get; private set; }

    private @DanceControls controls;
    public delegate void ArrowPressed(string direction);
    public static event ArrowPressed OnArrowPressed;

    [SerializeField] private DanceArrowPool arrowPool;
    private List<DanceArrow> activeArrows = new List<DanceArrow>();

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
        // Check active arrows for a hit
        foreach (DanceArrow arrow in activeArrows)
        {
            if (arrow.IsInHitZone && arrow.direction == direction && arrow.gameObject.activeInHierarchy)
            {
                DanceGameManager.Instance?.HandleArrowPress(direction);
                ReturnArrowToPool(arrow);
                OnArrowPressed?.Invoke(direction);
                return;
            }
        }

        // No matching arrow in hit zone
        DanceGameManager.Instance.HandleMiss(); // No matching arrow in hit zone
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

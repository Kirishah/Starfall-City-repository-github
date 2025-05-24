using DanceInputActions;
using UnityEngine;

public class DanceInput : MonoBehaviour
{
    public static DanceInput Instance { get; private set; }

    private @DanceControls controls;
    public delegate void ArrowPressed(string direction);
    public static event ArrowPressed OnArrowPressed;

    [SerializeField] private DanceArrowPool arrowPool;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
        controls = new DanceControls();
    }

    private void OnEnable()
    {
        controls.DanceActions.Up.performed += _ => OnArrowPressed?.Invoke("Up");
        controls.DanceActions.Down.performed += _ => OnArrowPressed?.Invoke("Down");
        controls.DanceActions.Left.performed += _ => OnArrowPressed?.Invoke("Left");
        controls.DanceActions.Right.performed += _ => OnArrowPressed?.Invoke("Right");
        controls.Enable();
    }

    private void OnDisable()
    {
        controls.Disable();
    }

    // Pooling version of input handling
    public void ReturnArrowToPool(DanceArrow arrow)
    {
        if (arrowPool != null)
            arrowPool.ReturnArrow(arrow);
    }
}

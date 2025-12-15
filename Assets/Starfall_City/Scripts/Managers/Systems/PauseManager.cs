using UnityEngine;
using UnityEngine.InputSystem;
using QTE;

namespace Core
{
    public class PauseManager : MonoBehaviour
    {
        public static PauseManager Instance { get; private set; }
        public static bool IsPaused { get; private set; }

        [SerializeField] private InputAction pauseAction; // Bind to pause button

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            pauseAction.performed += _ => TogglePause();
        }

        private void OnEnable()
        {
            pauseAction.Enable();
        }

        private void OnDisable()
        {
            pauseAction.Disable();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void TogglePause()
        {
            IsPaused = !IsPaused;
            Debug.Log($"Game paused: {IsPaused}", this);
            // Notify QTEGameManager if QTE is active
            if (QTEGameManager.IsQTEActive && QTEGameManager.Instance != null)
            {
                QTEGameManager.Instance.SetQTEPaused(IsPaused);
            }
        }
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace core
{
    public interface IPersistentId
    {
        string Id { get; }
    }

    // Put this on GameObjects
    [DefaultExecutionOrder(-100)]
    public class PersistentObjectId : MonoBehaviour, IPersistentId
    {
        [SerializeField] private string id = "";
        public string Id => string.IsNullOrEmpty(id) ? (id = Generate()) : id;

        private bool _registered = false;

        private string Generate()
        {
            id = $"{gameObject.name}_{System.Guid.NewGuid():N}".Substring(0, 32);
            return id;
        }

        private void Reset() => id = Generate();

        private void Awake()
        {
            // Ensure we survive scene loads and always retry registration
            TryRegisterWithRetry();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            TryRegisterWithRetry();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _registered = false;
            TryRegisterWithRetry();
        }

        private void TryRegisterWithRetry()
        {
            if (_registered || string.IsNullOrEmpty(Id)) return;

            // Cancel any previous retry
            StopAllCoroutines();
            StartCoroutine(RegisterWhenReady());
        }

        private IEnumerator RegisterWhenReady()
        {
            // Wait until registry exists
            while (PersistentRegistry.Instance == null)
                yield return null;

            // Double-check we're still valid
            if (this == null || string.IsNullOrEmpty(Id)) yield break;

            PersistentRegistry.Instance.Register(this);
            _registered = true;

            Debug.Log($"[PersistentObjectId] Successfully registered: {gameObject.name} → {Id}", this);
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            if (_registered && PersistentRegistry.Instance != null)
            {
                PersistentRegistry.Instance.Unregister(this);
            }
        }

        // Critical: Public method to force immediate registration (use after transfer!)
        public void ForceRegisterNow()
        {
            StopAllCoroutines();
            _registered = false;
            if (PersistentRegistry.Instance != null && !string.IsNullOrEmpty(Id))
            {
                PersistentRegistry.Instance.Register(this);
                _registered = true;
                Debug.Log($"[PersistentObjectId] FORCE registered: {name} → {Id}", this);
            }
            else
            {
                StartCoroutine(RegisterWhenReady());
            }
        }
    }
}

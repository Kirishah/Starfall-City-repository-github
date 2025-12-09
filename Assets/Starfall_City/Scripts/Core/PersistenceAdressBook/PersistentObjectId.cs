using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        private static readonly Dictionary<string, PersistentObjectId> _existingById = new();

        private string Generate()
        {
            id = $"{gameObject.name}_{System.Guid.NewGuid():N}".Substring(0, 32);
            return id;
        }

        private void Reset() => id = Generate();

        private void Awake()
        {
            // Ensure we survive scene loads and always retry registration
            TryRegisterWithDeduplication();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TryRegisterWithDeduplication();
        }

        private void TryRegisterWithDeduplication()
        {
            if (string.IsNullOrEmpty(Id)) return;

            // If another object with same ID already exists globally → deduplicate
            if (_existingById.TryGetValue(Id, out var existing))
            {
                if (existing == this)
                {
                    // We are the one already registered → nothing to do
                    return;
                }

                Debug.Log($"[PersistentObjectId] Duplicate detected! Keeping traveler, removing fresh copy: {gameObject.name} (ID: {Id})");

                // === KEEP THE TRAVELER (the one with DontDestroyOnLoadTag) ===
                // Destroy the fresh one (this), keep the persistent one
                if (this.TryGetComponent<DontDestroyOnLoadTag>(out _))
                {
                    // This is the traveler → destroy the stale one instead
                    Destroy(existing.gameObject);
                    _existingById.Remove(Id); // Remove old reference
                }
                else
                {
                    // This is the fresh scene instance → destroy ourselves
                    Destroy(gameObject);
                    return;
                }
            }

            // No duplicate → register this one
            _existingById[Id] = this;

            // Force registration in PersistentRegistry if you still use it
            if (PersistentRegistry.Instance != null && !_registered)
            {
                PersistentRegistry.Instance.Register(this);
                _registered = true;
            }

            Debug.Log($"[PersistentObjectId] Registered unique object: {gameObject.name} → {Id}", this);
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            if (_existingById.TryGetValue(Id, out var value) && value == this)
            {
                _existingById.Remove(Id);
            }

            if (_registered && PersistentRegistry.Instance != null)
            {
                PersistentRegistry.Instance.Unregister(this);
            }
        }

        // Critical: Public method to force immediate registration (use after transfer!)
        public void ForceRegisterNow()
        {
            _registered = false;
            TryRegisterWithDeduplication();
        }
    }
}

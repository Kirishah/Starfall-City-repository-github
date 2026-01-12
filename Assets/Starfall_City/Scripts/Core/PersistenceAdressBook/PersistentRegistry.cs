using System.Collections.Generic;
using UnityEngine;

namespace core
{
    [DefaultExecutionOrder(-9999)]
    public class PersistentRegistry : MonoBehaviour
    {
        public static PersistentRegistry Instance { get; private set; }

        private readonly Dictionary<string, Object> _registry = new();

        public static event System.Action OnReady;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
                name = "[PersistentRegistry]";
            }
            else
            {
                // In edit mode, allow temporary existence
                hideFlags = HideFlags.HideAndDontSave;
            }

            Debug.Log("PersistentRegistry: READY — all references can now be safely resolved");
        }

        private void Start() => OnReady?.Invoke();

        public void Register(IPersistentId obj)
        {
            if (string.IsNullOrEmpty(obj.Id)) return;
            if (obj is not Object unityObj) return;

            // Store the GameObject
            GameObject gameObjectToRegister = null;

            if (unityObj is Component component)
            {
                gameObjectToRegister = component.gameObject;
            }
            else if (unityObj is GameObject go)
            {
                gameObjectToRegister = go;
            }

            if (gameObjectToRegister == null)
            {
                Debug.LogError($"[PersistentRegistry] Cannot register: {unityObj} has no GameObject");
                return;
            }

            if (_registry.TryGetValue(obj.Id, out var existing))
            {
                if (existing != null && existing != gameObjectToRegister)
                {
                    Debug.LogWarning($"[PersistentRegistry] ID conflict! Replacing {existing} with {gameObjectToRegister.name}");
                }
                else if (existing == gameObjectToRegister)
                {
                    return;
                }
            }

            _registry[obj.Id] = gameObjectToRegister;
            Debug.Log($"[Registry] Registered ID {obj.Id} → {gameObjectToRegister.name}");
        }

        public void Unregister(IPersistentId obj)
        {
            if (string.IsNullOrEmpty(obj.Id)) return;
            if (_registry.TryGetValue(obj.Id, out var existing) && existing == (obj as Object))
            {
                _registry.Remove(obj.Id);
            }
        }

        public Object Find(string id) => _registry.TryGetValue(id, out var obj) ? obj : null;
        public T Find<T>(string id) where T : Object => Find(id) as T;
    }
}

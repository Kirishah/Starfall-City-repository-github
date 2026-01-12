using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Universal persistent reference to any UnityEngine.Object (GameObject, ScriptableObject, etc.)
/// with full editor support.
/// </summary>
namespace core
{
    [System.Serializable]
    public class PersistentReference
    {
        [SerializeField] private string id = "";
        [SerializeField] private string debugName = "";
        [SerializeField] private string objectType = ""; // For editor display

        public string Id => id;
        public Object Target
        {
            get
            {
                var obj = PersistentRegistry.Instance.Find(id);
                if (obj == null || obj.Equals(null)) // Unity magic: destroyed objects are "null" but not actually null
                    return null;
                return obj;
            }
        }

        public bool IsAssigned => !string.IsNullOrEmpty(id);
        public bool IsValid => IsAssigned && Target != null && !Target.Equals(null);

        public bool TryGet<T>(out T result) where T : Object
        {
            result = Target as T;
            return result != null;
        }

        public T Get<T>() where T : Object => Target as T;

        public static implicit operator Object(PersistentReference pref) => pref.Target;

        public void Assign(Object obj)
        {
            if (obj == null)
            {
                Clear();
                return;
            }

            var pid = obj switch
            {
                GameObject go => go.GetComponent<IPersistentId>(),
                ScriptableObject so => so as IPersistentId,
                Component c => c.GetComponent<IPersistentId>(),
                _ => null
            };

            if (pid == null || string.IsNullOrEmpty(pid.Id))
            {
                Debug.LogError($"[PersistentReference] Cannot assign '{obj.name}' ({obj.GetType()}): missing or empty IPersistentId!", obj);
                return;
            }

            id = pid.Id;
            debugName = obj.name;
            objectType = obj.GetType().Name;

#if UNITY_EDITOR
            // So the change appears immediately in the inspector when done via script
            EditorUtility.SetDirty(this);
#endif
        }

        public void Clear()
        {
            id = "";
            debugName = "";
            objectType = "";
        }

#if UNITY_EDITOR
        [ContextMenu("Assign from Selection")]
        private void AssignFromSelection()
        {
            if (Selection.activeObject != null)
                Assign(Selection.activeObject);
        }

        [ContextMenu("Ping Target")]
        private void Ping()
        {
            var target = Target;
            if (target != null)
            {
                EditorGUIUtility.PingObject(target);
                Selection.activeObject = target;
            }
            else
            {
                Debug.LogWarning($"[PersistentReference] No object found for ID: {id}");
            }
        }
#endif
    }
}

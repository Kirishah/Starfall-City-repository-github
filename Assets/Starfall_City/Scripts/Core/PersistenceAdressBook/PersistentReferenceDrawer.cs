#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace core
{
    [CustomPropertyDrawer(typeof(PersistentReference))]
    public class PersistentReferenceDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var idProp = property.FindPropertyRelative("id");
            var nameProp = property.FindPropertyRelative("debugName");
            var typeProp = property.FindPropertyRelative("objectType");

            // ── 1. Show the label
            EditorGUI.BeginProperty(position, label, property);
            var labelRect = EditorGUI.PrefixLabel(position, label);

            // ── 2. Layout
            float buttonWidth = 60f;
            float pingWidth = 40f;
            float spacing = 4f;

            var fieldRect = new Rect(labelRect.x, position.y,
                position.width - labelRect.width - buttonWidth - pingWidth - (spacing * 2),
                EditorGUIUtility.singleLineHeight);

            var assignRect = new Rect(fieldRect.xMax + spacing, position.y, buttonWidth, EditorGUIUtility.singleLineHeight);
            var pingRect = new Rect(assignRect.xMax + spacing, position.y, pingWidth, EditorGUIUtility.singleLineHeight);

            // ── 3. Resolve target (safe even if registry not ready yet)
            Object target = null;
            var displayName = nameProp.stringValue;
            var displayType = typeProp.stringValue;

            if (!string.IsNullOrEmpty(idProp.stringValue))
            {
                // We don't rely on PersistentRegistry.Instance in edit mode anymore
                displayName = string.IsNullOrEmpty(displayName) ? "Unknown" : displayName;
                displayType = string.IsNullOrEmpty(displayType) ? "" : displayType;

                if (Application.isPlaying && PersistentRegistry.Instance != null)
                    target = PersistentRegistry.Instance.Find(idProp.stringValue);
            }

            // ── 4. Draw preview field (always enabled for dragging)
            EditorGUI.BeginChangeCheck();
            var dropped = EditorGUI.ObjectField(fieldRect, target, typeof(Object), true);
            if (EditorGUI.EndChangeCheck() && dropped != null)
            {
                AssignFromObject(property, dropped);
            }

            // ── 5. Status text
            var status = target != null
                ? $"<b>{displayName}</b> <color=#888>({displayType})</color>"
                : string.IsNullOrEmpty(idProp.stringValue)
                    ? "<i>(Drag here or click Assign)</i>"
                    : $"<color=#ff6666>MISSING</color> ({displayName})";

            EditorGUI.LabelField(fieldRect, new GUIContent(status), EditorStyles.miniLabel);

            // ── 6. Assign button
            if (GUI.Button(assignRect, "Assign", EditorStyles.miniButtonLeft))
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("From Selection"), false, () =>
                {
                    if (Selection.activeObject != null)
                        AssignFromObject(property, Selection.activeObject);
                });
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Clear"), false, () =>
                {
                    idProp.stringValue = "";
                    nameProp.stringValue = "";
                    typeProp.stringValue = "";
                    property.serializedObject.ApplyModifiedProperties();
                });
                menu.ShowAsContext();
            }

            // ── 7. Ping button (only when we actually have the object at runtime)
            var canPing = target != null;
            EditorGUI.BeginDisabledGroup(!canPing);
            if (GUI.Button(pingRect, "Ping", EditorStyles.miniButtonRight))
            {
                EditorGUIUtility.PingObject(target);
                Selection.activeObject = target;
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.EndProperty();
        }

        private void AssignFromObject(SerializedProperty property, Object obj)
        {
            var pid = obj switch
            {
                GameObject go => go.GetComponent<IPersistentId>(),
                Component c => c.GetComponent<IPersistentId>(),
                ScriptableObject so => so as IPersistentId,
                _ => null
            };

            if (pid == null)
            {
                if (obj is GameObject go && EditorUtility.DisplayDialog("Add ID?",
                    $"Add PersistentObjectId to '{obj.name}'?", "Yes", "No"))
                {
                    Undo.AddComponent<PersistentObjectId>(go);
                    pid = go.GetComponent<IPersistentId>();
                }
                else return;
            }

            if (pid != null)
            {
                property.FindPropertyRelative("id").stringValue = pid.Id;
                property.FindPropertyRelative("debugName").stringValue = obj.name;
                property.FindPropertyRelative("objectType").stringValue = obj.GetType().Name;
                property.serializedObject.ApplyModifiedProperties();
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => EditorGUIUtility.singleLineHeight * 2f;
    }
}
#endif



#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Transform))]
public class WorldPos : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var transform = (Transform)target;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("World Position", transform.position.ToString());
    }
}
#endif

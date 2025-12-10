using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(Transform))]
public class WorldPos : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        Transform transform = (Transform)target;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("World Position", transform.position.ToString());
    }
}

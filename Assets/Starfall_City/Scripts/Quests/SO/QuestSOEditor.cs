using System;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

#if UNITY_EDITOR
[CustomEditor(typeof(QuestSO))]
public class QuestSOEditor : Editor
{
    // Cache property names to avoid string lookups
    private readonly string[] _mainProperties = { "QuestID", "Title", "Description" };
    private readonly string[] _sceneProperties = { "AssociatedScenes" };
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Draw main header
        EditorGUILayout.LabelField("Quest Configuration", EditorStyles.boldLabel);

        // Draw core properties
        foreach (var property in _mainProperties)
        {
            DrawProperty(property);
        }

        // Draw objectives section with a help box
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Objectives define what the player needs to complete this quest.", MessageType.Info);
        DrawProperty("Objectives");
        DrawObjectiveCreationButtons();

        // Draw scene references
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Scene Requirements", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Scenes needed for this quest (optional)", MessageType.None);
        DrawProperty("AssociatedScenes");

        serializedObject.ApplyModifiedProperties();
    }

    // Helper method to draw properties safely
    private void DrawProperty(string propertyName)
    {
        var prop = serializedObject.FindProperty(propertyName);
        if (prop != null)
        {
            EditorGUILayout.PropertyField(prop, true);
        }
        else
        {
            EditorGUILayout.HelpBox($"Missing property: {propertyName}", MessageType.Error);
        }
    }
    private void DrawObjectiveCreationButtons()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Create New Objective", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("QTE Objective"))
        {
            CreateObjective<QTEObjectiveSO>("QTE");
        }
        if (GUILayout.Button("Dialogue Objective"))
        {
            CreateObjective<DialogueSO>("Dialogue");
        }
        if (GUILayout.Button("Exploration Objective"))
        {
            CreateObjective<ExplorationSO>("Exploration");
        }
        if (GUILayout.Button("Interaction Objective"))
        {
            CreateObjective<InteractionSO>("Interaction");
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Collection Objective"))
        {
            CreateObjective<CollectItemSO>("Collection");
        }
        if (GUILayout.Button("Performance Objective"))
        {
            CreateObjective<PerformanceSO>("Performance");
        }
        if (GUILayout.Button("GiveItem Objective"))
        {
            CreateObjective<GiveItemSO>("GiveItem");
        }
        EditorGUILayout.EndHorizontal();
    }

    private void CreateObjective<T>(string typeName) where T : ObjectiveSO
    {
        // Register undo before making changes
        Undo.RecordObject(target, "Create Objective");
        // Get current quest
        var quest = target as QuestSO;
        if (quest == null) return;

        // Create new objective
        var objective = ScriptableObject.CreateInstance<T>();
        objective.name = $"{quest.QuestID}_{typeName}_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
        objective.ObjectiveID = $"obj_{Guid.NewGuid().ToString("N").Substring(0, 4)}";
        objective.Description = "New Objective";

        // Create folder if needed
        string path = Path.GetDirectoryName(AssetDatabase.GetAssetPath(quest));
        string objectiveFolder = Path.Combine(path, "Objectives");
        if (!Directory.Exists(objectiveFolder))
        {
            Directory.CreateDirectory(objectiveFolder);
        }

        // Save asset
        string assetPath = Path.Combine(objectiveFolder, $"{objective.name}.asset");
        AssetDatabase.CreateAsset(objective, assetPath);

        // Add to quest
        SerializedProperty objectivesProp = serializedObject.FindProperty("Objectives");
        objectivesProp.arraySize++;
        objectivesProp.GetArrayElementAtIndex(objectivesProp.arraySize - 1).objectReferenceValue = objective;

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(quest); // MARK AS DIRTY
        AssetDatabase.Refresh();

        // Focus in project window
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = objective;
    }
}
#endif

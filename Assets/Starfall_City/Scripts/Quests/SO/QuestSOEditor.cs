#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using System.IO;

namespace QuestSystem
{
    [CustomEditor(typeof(QuestSO))]
    public class QuestSOEditor : Editor
    {
        private SerializedProperty _questID;
        private SerializedProperty _title;
        private SerializedProperty _description;
        private SerializedProperty _startingDialogueID;
        private SerializedProperty _moneyReward;
        private SerializedProperty _objectives;
        private SerializedProperty _associatedScenes;
        private SerializedProperty _followUpQuests;

        private void OnEnable()
        {
            // Cache properties 
            _questID = serializedObject.FindProperty("QuestID"); // public field 
            _title = serializedObject.FindProperty("Title");     // public field
            _description = serializedObject.FindProperty("Description"); // public field

            // These are private backing fields for read-only properties
            _startingDialogueID = serializedObject.FindProperty("startingDialogueID");
            _moneyReward = serializedObject.FindProperty("_moneyReward");
            _followUpQuests = serializedObject.FindProperty("followUpQuests");

            // These are public fields/arrays
            _objectives = serializedObject.FindProperty("Objectives");
            _associatedScenes = serializedObject.FindProperty("AssociatedScenes");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawMainProperties();
            DrawRewardsSection();
            DrawObjectivesSection();
            DrawFollowUpQuestsSection();
            DrawSceneRequirementsSection();

            if (serializedObject.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(target);
            }
        }

        private void DrawMainProperties()
        {
            EditorGUILayout.LabelField("Quest Configuration", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_questID, new GUIContent("Quest ID"));
            EditorGUILayout.PropertyField(_title);
            EditorGUILayout.PropertyField(_description);
            EditorGUILayout.PropertyField(_startingDialogueID, new GUIContent("Starting Dialogue ID"));
        }

        private void DrawRewardsSection()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Rewards", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Experience and money awarded upon quest completion.", MessageType.Info);

            EditorGUILayout.PropertyField(_moneyReward, new GUIContent("Money Reward"));
        }

        private void DrawObjectivesSection()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox("Objectives define what the player needs to complete this quest.", MessageType.Info);

            EditorGUILayout.PropertyField(_objectives, new GUIContent("Objectives"), true);

            DrawObjectiveCreationButtons();
        }

        private void DrawObjectiveCreationButtons()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Create New Objective", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("QTE Objective")) CreateObjective<QTEObjectiveSO>("QTE");
            if (GUILayout.Button("Dialogue Objective")) CreateObjective<DialogueSO>("Dialogue");
            if (GUILayout.Button("Exploration Objective")) CreateObjective<ExplorationSO>("Exploration");
            if (GUILayout.Button("Interaction Objective")) CreateObjective<InteractionSO>("Interaction");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Collection Objective")) CreateObjective<CollectItemSO>("Collection");
            if (GUILayout.Button("Performance Objective")) CreateObjective<PerformanceSO>("Performance");
            if (GUILayout.Button("GiveItem Objective")) CreateObjective<GiveItemSO>("GiveItem");
            EditorGUILayout.EndHorizontal();
        }

        private void CreateObjective<T>(string typeName) where T : ObjectiveSO
        {
            var quest = (QuestSO)target;
            if (quest == null) return;

            Undo.RecordObject(quest, "Create Objective");

            var objective = ScriptableObject.CreateInstance<T>();
            objective.name = $"{quest.QuestID}_{typeName}_{Guid.NewGuid():N8}";
            objective.ObjectiveID = $"obj_{Guid.NewGuid():N4}";
            objective.Description = "New Objective";

            // Create Objectives folder next to quest asset
            string questPath = AssetDatabase.GetAssetPath(quest);
            string folderPath = Path.Combine(Path.GetDirectoryName(questPath), "Objectives");

            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                string parentGuid = AssetDatabase.AssetPathToGUID(Path.GetDirectoryName(questPath));
                AssetDatabase.CreateFolder(Path.GetDirectoryName(questPath), "Objectives");
            }

            string assetPath = Path.Combine(folderPath, $"{objective.name}.asset");
            AssetDatabase.CreateAsset(objective, assetPath);
            AssetDatabase.SaveAssets();

            // Add to objectives array
            var index = _objectives.arraySize;
            _objectives.InsertArrayElementAtIndex(index);
            _objectives.GetArrayElementAtIndex(index).objectReferenceValue = objective;

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(quest);

            // Select new objective
            Selection.activeObject = objective;
            EditorGUIUtility.PingObject(objective);
        }

        private void DrawFollowUpQuestsSection()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Follow-Up Quests", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Define quests that unlock after this quest is completed, with conditions.", MessageType.Info);

            EditorGUILayout.PropertyField(_followUpQuests, new GUIContent("Follow-Up Quests"), true);
        }

        private void DrawSceneRequirementsSection()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Scene Requirements", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Scenes needed for this quest (optional)", MessageType.None);

            EditorGUILayout.PropertyField(_associatedScenes, true);
        }
    }
}
#endif

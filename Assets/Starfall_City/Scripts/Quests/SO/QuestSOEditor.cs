using System;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

#if UNITY_EDITOR
[CustomEditor(typeof(QuestSO))]
public class QuestSOEditor : Editor
{
    // Кэширование имен свойств, чтобы избежать поиска строк
    private readonly string[] _mainProperties = { "QuestID", "Title", "Description", "startingDialogueID" };
    private readonly string[] _rewardProperties = { "experienceReward", "moneyReward" };
    private readonly string[] _sceneProperties = { "AssociatedScenes" };
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Главный заголовок
        EditorGUILayout.LabelField("Quest Configuration", EditorStyles.boldLabel);

        // Основные свойства квеста
        foreach (var property in _mainProperties)
        {
            DrawProperty(property);
        }

        // Раздел наград
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Rewards", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Experience and money awarded upon quest completion.", MessageType.Info);
        foreach (var property in _rewardProperties)
        {
            DrawProperty(property);
        }

        // Раздел для целей квеста
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Objectives define what the player needs to complete this quest.", MessageType.Info);
        DrawProperty("Objectives");
        DrawObjectiveCreationButtons();

        // Раздел для последующих квестов
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Follow-Up Quests", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Define quests that unlock after this quest is completed, with conditions.", MessageType.Info);
        DrawFollowUpQuests();

        // Рефы сцен
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Scene Requirements", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Scenes needed for this quest (optional)", MessageType.None);
        DrawProperty("AssociatedScenes");

        serializedObject.ApplyModifiedProperties();
    }

    // Метод для отрисовки свойств
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
        // Регаем отмену перед внесением изменений
        Undo.RecordObject(target, "Create Objective");
        // Текущий квест
        var quest = target as QuestSO;
        if (quest == null) return;

        // Создание нового объекта цели
        var objective = ScriptableObject.CreateInstance<T>();
        objective.name = $"{quest.QuestID}_{typeName}_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
        objective.ObjectiveID = $"obj_{Guid.NewGuid().ToString("N").Substring(0, 4)}";
        objective.Description = "New Objective";

        // Создание папки для целей, если она не существует
        string path = Path.GetDirectoryName(AssetDatabase.GetAssetPath(quest));
        string objectiveFolder = Path.Combine(path, "Objectives");
        if (!Directory.Exists(objectiveFolder))
        {
            Directory.CreateDirectory(objectiveFolder);
        }

        // Сохраняем цель в папку
        string assetPath = Path.Combine(objectiveFolder, $"{objective.name}.asset");
        AssetDatabase.CreateAsset(objective, assetPath);

        // Добавляем цель в массив целей квеста
        SerializedProperty objectivesProp = serializedObject.FindProperty("Objectives");
        objectivesProp.arraySize++;
        objectivesProp.GetArrayElementAtIndex(objectivesProp.arraySize - 1).objectReferenceValue = objective;

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(quest); // Маркер квеста как измененный
        AssetDatabase.Refresh();

        // Фокус на созданном объекте
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = objective;
    }

    private void DrawFollowUpQuests()
    {
        var followUpQuestsProp = serializedObject.FindProperty("followUpQuests");
        if (followUpQuestsProp == null)
        {
            EditorGUILayout.HelpBox("FollowUpQuests property not found.", MessageType.Error);
            return;
        }

        EditorGUI.indentLevel++;
        int newSize = EditorGUILayout.IntField("Size", followUpQuestsProp.arraySize);
        if (newSize != followUpQuestsProp.arraySize)
        {
            followUpQuestsProp.arraySize = newSize;
        }

        for (int i = 0; i < followUpQuestsProp.arraySize; i++)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            var followUpQuest = followUpQuestsProp.GetArrayElementAtIndex(i);
            if (followUpQuest == null)
            {
                EditorGUILayout.EndVertical(); // Закрываем вертикальную группу, если null
                continue;
            }

            // Поле квеста
            EditorGUILayout.PropertyField(followUpQuest.FindPropertyRelative("quest"), new GUIContent("Quest"));

            // Dialogue Start ID
            EditorGUILayout.PropertyField(followUpQuest.FindPropertyRelative("dialogueStartID"), new GUIContent("Dialogue Start ID"));

            // Unlock Conditions
            var unlockConditionsProp = followUpQuest.FindPropertyRelative("unlockConditions");
            if (unlockConditionsProp != null)
            {
                EditorGUILayout.PropertyField(unlockConditionsProp, new GUIContent("Unlock Conditions"), true);

                // добавление/удаление unlock conditions
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Add Condition"))
                {
                    unlockConditionsProp.arraySize++;
                }
                if (unlockConditionsProp.arraySize > 0 && GUILayout.Button("Remove Condition"))
                {
                    unlockConditionsProp.arraySize--;
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }
        EditorGUI.indentLevel--;
    }
}
#endif

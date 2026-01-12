#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class PlayModeReset
{
    static PlayModeReset()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            Debug.Log("Exiting Play Mode: Clearing ALL PlayerPrefs for fresh start.");

            // Clear all quest-related keys
            PlayerPrefs.DeleteKey("CompletedQuests");
            PlayerPrefs.DeleteKey("CompletedObjectives");
            PlayerPrefs.DeleteKey("ActiveQuests");

            // Clear any quest-specific progress keys
            DeleteAllQuestProgressKeys();

            // Clear other game data
            PlayerPrefs.DeleteKey("TriggeredEvents");
            PlayerPrefs.DeleteKey("Inventory");
            PlayerPrefs.DeleteKey("Characteristics");

            PlayerPrefs.Save();
            Debug.Log("All PlayerPrefs cleared for fresh play session.");
        }
    }

    private static void DeleteAllQuestProgressKeys()
    {
        // Delete all keys that start with "Quest_"
        if (PlayerPrefs.HasKey("ActiveQuests"))
        {
            var activeQuestsData = PlayerPrefs.GetString("ActiveQuests");
            if (!string.IsNullOrEmpty(activeQuestsData))
            {
                var activeQuestNames = activeQuestsData.Split(',');
                foreach (var questName in activeQuestNames)
                {
                    if (!string.IsNullOrEmpty(questName))
                    {
                        PlayerPrefs.DeleteKey($"Quest_{questName}");
                        PlayerPrefs.DeleteKey($"Quest_{questName}_CurrentObjective");
                    }
                }
            }
        }
    }
}
#endif

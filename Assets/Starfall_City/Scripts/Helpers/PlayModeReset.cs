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
            Debug.Log("Exiting Play Mode: Clearing PlayerPrefs for quest and event progress.");
            PlayerPrefs.DeleteKey("CompletedQuests"); // Clear quest completion data
            PlayerPrefs.DeleteKey("TriggeredEvents"); // Clear game event data
            PlayerPrefs.Save(); // Ensure changes are saved
        }
    }
}
#endif

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class PlayModeReset
{
    static PlayModeReset()
    {
        // Subscribe to the play mode state change event
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        // Clear PlayerPrefs when exiting Play Mode
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

#if DEVELOPMENT_BUILD || UNITY_EDITOR
using UnityEngine;

[ExecuteInEditMode]  // Makes it run even in Edit Mode if you want
public class ShowConsoleButton : MonoBehaviour
{
    private void Awake()
    {
        Debug.developerConsoleVisible = true;
        Debug.LogError("Test");
    }
    private void OnGUI()
    {
        // Draw on top of everything
        GUI.depth = -1000;

        if (GUI.Button(new Rect(20, 20, 220, 50), "<size=14><b>Open Developer Console</b></size>"))
        {
            Debug.developerConsoleVisible = true;
        }
    }
}
#endif

using core;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Bootstrapper
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    // public static void Execute() => Object.DontDestroyOnLoad(Object.Instantiate(Resources.Load("Systems")));
    public static void Execute()
    {
        // Only create systems if they're needed for the initial scene
        if (SceneNeedsSystems(SceneManager.GetActiveScene().name))
        {
            Object.Instantiate(Resources.Load("Systems"));
        }
    }

    private static bool SceneNeedsSystems(string sceneName)
    {
        // List scenes that require systems at launch
        return sceneName != "MainMenu";
    }
}

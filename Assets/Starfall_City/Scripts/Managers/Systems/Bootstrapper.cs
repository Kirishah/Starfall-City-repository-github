using UnityEngine;
using UnityEngine.SceneManagement;

public static class Bootstrapper
{
    private static bool _hasInstantiatedSystems = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        // Only subscribe once
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_hasInstantiatedSystems)
            return; // Already have Systems from previous gameplay scene → PersistentSystems kept them

        if (SceneNeedsSystems(scene.name))
        {
            Debug.Log($"Bootstrapper: First time entering gameplay scene '{scene.name}' → instantiating Systems");
            var systemsObj = Object.Instantiate(Resources.Load<GameObject>("Systems"));
            // Optional: name it so it's easy to spot
            systemsObj.name = "[Systems]";
            _hasInstantiatedSystems = true;
        }
        else
        {
            Debug.Log($"Bootstrapper: Entered non-gameplay scene '{scene.name}' → no Systems needed");
        }
    }

    private static bool SceneNeedsSystems(string sceneName)
    {
        // Your original whitelist (or blacklist) logic
        return sceneName != "Menu"; // or use a string[] like PersistentSystems
    }
}

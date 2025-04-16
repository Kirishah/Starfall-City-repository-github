using UnityEngine;
using UnityEngine.SceneManagement;

public class PersistentSystems : MonoBehaviour
{
    private static PersistentSystems _instance;

    // Scenes where the systems should persist
    [SerializeField] private string[] _persistentInScenes = { "Apartment", "Mansion" };

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject); // Prevent duplicates
            return;
        }

        _instance = this;
        SceneManager.sceneLoaded += OnSceneLoaded;
        DontDestroyOnLoad(gameObject);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Destroy systems if entering a non-persistent scene (like menu)
        if (!ShouldPersistInScene(scene.name))
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Destroy(gameObject);
        }
    }

    private bool ShouldPersistInScene(string sceneName)
    {
        foreach (string validScene in _persistentInScenes)
        {
            if (sceneName == validScene) return true;
        }
        return false;
    }
}

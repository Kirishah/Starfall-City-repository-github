using UnityEngine;
using UnityEngine.SceneManagement;

public class PersistentSystems : MonoBehaviour
{
    private static PersistentSystems _instance;

    // Scenes where the systems should persist
    [SerializeField] private string[] _persistentInScenes = { "Apartment", "Mansion", "DemoScene" };

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        // Check immediately (important when Systems are created in a non-persistent scene)
        if (!ShouldPersistInScene(SceneManager.GetActiveScene().name))
        {
            Debug.Log($"PersistentSystems: Current scene '{SceneManager.GetActiveScene().name}' is not persistent → destroying Systems.");
            Destroy(gameObject);
            return;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Destroy systems if entering a non-persistent scene (like menu)
        if (!ShouldPersistInScene(scene.name))
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Debug.Log("Sytems object has been deleted.");
            Destroy(gameObject);
        }
    }

    private bool ShouldPersistInScene(string sceneName)
    {
        foreach (var validScene in _persistentInScenes)
        {
            if (sceneName == validScene) return true;
        }
        return false;
    }
}

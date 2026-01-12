using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
public class DontDestroyOnLoadTag : MonoBehaviour
{
    private Scene _originalScene;

    private void Awake()
    {
        _originalScene = gameObject.scene;
        DontDestroyOnLoad(gameObject);

        // Optional: Clean name
        if (transform.parent == null)
            gameObject.name = "[PERSISTENT] " + name.Replace("(Clone)", "");
    }

    // Call this when transfer is complete (optional!)
    public void MakeNormalAgain()
    {
        // Optional: only do this if we're in a real scene (not DDOL)
        if (gameObject.scene.buildIndex == -1) // DDOL scene
        {
            var targetScene = SceneManager.GetActiveScene();
            if (targetScene.isLoaded)
                SceneManager.MoveGameObjectToScene(gameObject, targetScene);
        }
        else
        {
            // Already in a real scene
            SceneManager.MoveGameObjectToScene(gameObject, gameObject.scene);
        }

        // Remove the tag component
        Destroy(this);

        Debug.Log($"[DontDestroyOnLoadTag] Made normal again: {gameObject.name}");
    }
}

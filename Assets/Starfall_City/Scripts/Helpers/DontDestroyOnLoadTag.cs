using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
public class DontDestroyOnLoadTag : MonoBehaviour
{
    private Scene originalScene;

    private void Awake()
    {
        originalScene = gameObject.scene;
        DontDestroyOnLoad(gameObject);

        // Optional: Clean name
        if (transform.parent == null)
            gameObject.name = "[PERSISTENT] " + name.Replace("(Clone)", "");
    }

    // Call this when transfer is complete (optional!)
    public void MakeNormalAgain()
    {
        SceneManager.MoveGameObjectToScene(gameObject, originalScene);

        // If the original scene is already unloaded, move to current active scene
        if (!originalScene.isLoaded)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.isLoaded)
                SceneManager.MoveGameObjectToScene(gameObject, activeScene);
        }

        Destroy(this); // Remove the component
    }
}

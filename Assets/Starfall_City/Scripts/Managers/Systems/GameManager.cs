using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Persistent Data")]
    public SaveData PlayerData { get; private set; }

    public GameObject playerObject;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); 
            InitializeData();
        }
        else
        {
            Destroy(gameObject); 
        }
    }
    void InitializeData()
    {
        PlayerData = new SaveData();
    }

    public void LoadSceneWithTransition(int targetLocation)
    {
        SaveBeforeSceneTransition();
        StartCoroutine(LoadSceneAsync(targetLocation));
    }

    private IEnumerator LoadSceneAsync(int targetLocation)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(targetLocation);
        operation.allowSceneActivation = false;

        while (!operation.isDone)
        {
            if (operation.progress >= 0.9f)
            {
                operation.allowSceneActivation = true;
            }
            yield return null;
        }

        
    }

    public void SaveBeforeSceneTransition()
    {
        // Save player position/rotation
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerData.Position = player.transform.position;
            PlayerData.Rotation = player.transform.rotation;
        }
        else
        {
            Debug.LogError("Player object not found!");
        }
    }
}

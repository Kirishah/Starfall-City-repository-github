using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Persistent Data")]
    public SaveData PlayerData { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        InitializeData();
    }

    void Start()
    {
        // Optional: Validate Player exists on start
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("GameManager: Player not found on scene start -- will retry on save.");
        }
    }
    void InitializeData() => PlayerData = gameObject.AddComponent<SaveData>();

    public void LoadSceneWithTransition(int targetLocation)
    {
        SaveBeforeSceneTransition();
        StartCoroutine(LoadScene(targetLocation));
    }

    private IEnumerator LoadScene(int targetLocation)
    {
        var operation = SceneManager.LoadSceneAsync(targetLocation);
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

    public void SaveBeforeSceneTransition() =>
        // Save player position/rotation
        StartCoroutine(SavePlayerDataCoroutine());

    private IEnumerator SavePlayerDataCoroutine()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        for (var retries = 0; player == null && retries < 5; retries++)
        {
            yield return null; // Wait a frame
            player = GameObject.FindGameObjectWithTag("Player");
        }

        if (player != null)
        {
            PlayerData.Position = player.transform.position;
            PlayerData.Rotation = player.transform.rotation;
            Debug.Log("GameManager: Saved player data.");
        }
        else
        {
            Debug.LogError("GameManager: Player object not found after retries!");
        }
    }

    // For scene reloads (refresh data if needed)
    void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;

    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) =>
        // Optional: Restore saved position if loading saved scene
        // GameObject player = GameObject.FindGameObjectWithTag("Player");
        // if (player != null) { player.transform.SetPositionAndRotation(PlayerData.Position, PlayerData.Rotation); }
        Debug.Log("GameManager: Scene loaded -- persistent data ready.");
}

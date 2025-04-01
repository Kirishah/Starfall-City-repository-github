using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Persistent Data")]
    public PlayerData PlayerData { get; private set; }
    public Inventory Inventory { get; private set; }

    // В случае компаньонов: public List<CompanionData> Companions { get; private set; }

    public GameObject playerPrefab;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist across scenes
            InitializeData();
        }
        else
        {
            Destroy(gameObject); // Prevent duplicates
        }
    }
    void InitializeData()
    {
        PlayerData = new PlayerData();
        Inventory = new Inventory();
       // Если будут компаньоны: Companions = new List<CompanionData>();
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

        // Scene is fully loaded here
        LoadAfterSceneTransition();
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
        // Если будут companions:
        /* Companions.Clear();
        foreach (GameObject companion in GameObject.FindGameObjectsWithTag("Companion"))
        {
            Companions.Add(new CompanionData
            {
                Position = companion.transform.position,
                Rotation = companion.transform.rotation,
                // ... other fields
            });
        }
        */
    }
    public void LoadAfterSceneTransition()
    {
        // Load player
        if (playerPrefab != null)
        {
            Instantiate(playerPrefab, PlayerData.Position, PlayerData.Rotation);
        }
        else
        {
            Debug.LogError("Player prefab not assigned!");
        }

        /* Load companions
        foreach (CompanionData data in Companions)
        {
            Instantiate(companionPrefab, data.Position, data.Rotation);
        }
        */
    }
}

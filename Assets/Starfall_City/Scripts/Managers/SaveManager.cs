using UnityEditor.Overlays;
using UnityEngine;
using System.IO;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5))
        {
            SaveManager.Instance.SaveGame("quicksave");
        }
    }

    public void OnSaveButtonClicked()
    {
        SaveManager.Instance.SaveGame("quicksave");
    }

    public void OnLoadButtonClicked()
    {
        SaveManager.Instance.LoadGame("quicksave");
    }

    public void SaveGame(string saveFileName)
    {
        SaveData data = new SaveData();

        // Save inventory
        InventoryManager.Instance.SaveInventory(ref data);

        // Save other systems (e.g., player, quests)
        // PlayerManager.Instance.SavePlayer(ref data);

        // Convert to JSON and save
        string json = JsonUtility.ToJson(data, prettyPrint: true);
        string savePath = Path.Combine(Application.persistentDataPath, $"{saveFileName}.save");
        File.WriteAllText(savePath, json);
    }

    public void LoadGame(string saveFileName)
    {
        string savePath = Path.Combine(Application.persistentDataPath, $"{saveFileName}.save");
        if (!File.Exists(savePath)) return;

        string json = File.ReadAllText(savePath);
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        // Load inventory
        InventoryManager.Instance.LoadInventory(data);

        // Load other systems
        // PlayerManager.Instance.LoadPlayer(data);
    }
}

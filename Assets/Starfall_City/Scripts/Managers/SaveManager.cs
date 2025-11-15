using Core;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5))
        {
            SaveGame("quicksave");
        }
    }

    public void OnSaveButtonClicked()
    {
        SaveGame("quicksave");
    }

    public void OnLoadButtonClicked()
    {
        LoadGame("quicksave");
    }

    public void SaveGame(string saveFileName)
    {
        SaveCharacteristics();

        SaveQuestProgress();

        SaveInventory();

        SavePlayerData();

        PlayerPrefs.Save();
        Debug.Log($"Game saved: {saveFileName}");
    }

    public void LoadGame(string saveFileName)
    {
        LoadCharacteristics();

        LoadQuestProgress();

        LoadInventory();

        LoadPlayerData();

        Debug.Log($"Game loaded: {saveFileName}");
    }

    #region Characteristic Save/Load
    private void SaveCharacteristics()
    {
        if (CharacteristicsManager.Instance != null)
        {
            CharacteristicsManager.Instance.SaveCharacteristics();
        }
    }

    private void LoadCharacteristics()
    {
        if (CharacteristicsManager.Instance != null)
        {
            CharacteristicsManager.Instance.LoadCharacteristics();
        }
    }
    #endregion

    #region Quest Save/Load
    private void SaveQuestProgress()
    {
        // Save completed quests
        if (QuestMemory.Instance != null)
        {
            PlayerPrefs.SetString("CompletedQuests", string.Join(",", QuestMemory.Instance.GetCompletedQuestNames()));
        }

        // Save active quests progress
        if (QuestManager.Instance != null)
        {
            var activeQuests = QuestManager.Instance.GetActiveQuests();
            List<string> activeQuestNames = new List<string>();
            foreach (var quest in activeQuests)
            {
                activeQuestNames.Add(quest.Data.name);
                // Save individual quest progress
                SaveQuestObjectiveProgress(quest);
            }
            PlayerPrefs.SetString("ActiveQuests", string.Join(",", activeQuestNames));
        }
    }

    private void SaveQuestObjectiveProgress(Quest quest)
    {
        string questKey = $"Quest_{quest.Data.name}";
        var currentObjective = quest.GetCurrentObjective();
        if (currentObjective != null)
        {
            PlayerPrefs.SetString($"{questKey}_CurrentObjective", currentObjective.ObjectiveID);
            PlayerPrefs.SetInt($"{questKey}_{currentObjective.ObjectiveID}_Progress", currentObjective.CurrentProgress);
        }
    }

    private void LoadQuestProgress()
    {
        // Load completed quests
        if (QuestMemory.Instance != null)
        {
            // QuestMemory handles its own loading in Awake()
        }

        // Load active quests - this would need to be implemented in QuestManager
        // You'll need to add a method in QuestManager to restore quest state
        if (QuestManager.Instance != null && PlayerPrefs.HasKey("ActiveQuests"))
        {
            string activeQuestsData = PlayerPrefs.GetString("ActiveQuests");
            string[] activeQuestNames = activeQuestsData.Split(',');

            foreach (string questName in activeQuestNames)
            {
                if (!string.IsNullOrEmpty(questName))
                {
                    // You'll need to implement this method in QuestManager
                    // QuestManager.Instance.RestoreQuestState(questName);
                }
            }
        }
    }
    #endregion

    #region Inventory Save/Load
    private void SaveInventory()
    {
        if (InventoryManager.Instance != null)
        {
            // Get inventory data and save to PlayerPrefs
            // This would require adding a SaveInventory method to InventoryManager
            // that returns serializable data
        }
    }

    private void LoadInventory()
    {
        if (InventoryManager.Instance != null)
        {
            // Load inventory data from PlayerPrefs
            // This would require adding a LoadInventory method to InventoryManager
        }
    }
    #endregion

    #region Player Data Save/Load
    private void SavePlayerData()
    {
        // Save player position, rotation, scene, etc.
        // Example:
        /*
        if (PlayerController.Instance != null)
        {
            PlayerPrefs.SetFloat("PlayerPosX", PlayerController.Instance.transform.position.x);
            PlayerPrefs.SetFloat("PlayerPosY", PlayerController.Instance.transform.position.y);
            PlayerPrefs.SetFloat("PlayerPosZ", PlayerController.Instance.transform.position.z);
        }
        */
    }

    private void LoadPlayerData()
    {
        // Load player position, rotation, scene, etc.
        // Example:
        /*
        if (PlayerController.Instance != null)
        {
            float x = PlayerPrefs.GetFloat("PlayerPosX", 0);
            float y = PlayerPrefs.GetFloat("PlayerPosY", 0);
            float z = PlayerPrefs.GetFloat("PlayerPosZ", 0);
            PlayerController.Instance.transform.position = new Vector3(x, y, z);
        }
        */
    }
    #endregion

    public void DeleteSave(string saveFileName)
    {
        PlayerPrefs.DeleteKey(saveFileName);
        PlayerPrefs.Save();
        Debug.Log($"Save deleted: {saveFileName}");
    }
}

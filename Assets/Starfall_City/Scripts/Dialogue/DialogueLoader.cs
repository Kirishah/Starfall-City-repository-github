using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

public class DialogueLoader : MonoBehaviour
{
    public List<Dialogue> LoadDialogues(string path)
    {
        // Load the JSON file from Resources
        TextAsset jsonFile = Resources.Load<TextAsset>(path);
        if (jsonFile == null)
        {
            Debug.LogError($"Failed to load JSON file at path: {path}");
            return null;
        }

        // Deserialize JSON to List<Dialogue>
        try
        {
            List<Dialogue> dialogues = JsonConvert.DeserializeObject<List<Dialogue>>(jsonFile.text);
            Debug.Log($"Successfully loaded {dialogues.Count} dialogues.");
            return dialogues; // Returns the list and forgets it
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to deserialize JSON: {e.Message}");
            return null;
        }
    }
}

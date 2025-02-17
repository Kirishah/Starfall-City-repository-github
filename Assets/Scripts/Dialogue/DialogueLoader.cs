using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class DialogueLoader : MonoBehaviour
{
    public List<Dialogue> LoadDialogues(string path)
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(path);
        return JsonConvert.DeserializeObject<List<Dialogue>>(jsonFile.text);
    }
}

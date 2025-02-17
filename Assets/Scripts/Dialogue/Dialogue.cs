using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class Dialogue : MonoBehaviour
{
    public int id;
    public string speaker;
    public string text;
    public List<Choice> choices;
}

[System.Serializable]
public class Choice
{
    public string text;
    public int targetID;
}

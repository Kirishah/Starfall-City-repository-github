using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class Dialogue
{
    [SerializeField] public string id;
    [SerializeField] public int targetLocation;
    [SerializeField] public string speaker;
    [SerializeField] public string targetID;
    [SerializeField] public string text;
    [SerializeField] public string audio;
    [SerializeField] public string description;
    [SerializeField] public string iconPath;
    [SerializeField] public List<Choice> choices;
}

[System.Serializable]
public class Choice
{
    public string text;
    public string targetID;
    public string condition;
    public bool triggersQTE;
}

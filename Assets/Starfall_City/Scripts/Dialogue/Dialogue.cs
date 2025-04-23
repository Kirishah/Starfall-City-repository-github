using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class Dialogue
{
    [SerializeField]
    public int id;
    [SerializeField]
    public int targetLocation;
    [SerializeField]
    public string speaker;
    [SerializeField]
    public int targetID;
    [SerializeField]
    public string text;
    [SerializeField]
    public List<Choice> choices;
}

[System.Serializable]
public class Choice
{
    public string text;
    public int targetID;
}

using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;
using core;

namespace DialogueSystem
{
    [System.Serializable]
    public class Dialogue
    {
        [SerializeField] public string id;
        [SerializeField] public int targetLocation;
        [SerializeField] public string speaker;
        [SerializeField] public string targetID;
        [SerializeField] public string qteID;
        [SerializeField] public string text;
        [SerializeField] public string audio;
        [SerializeField] public string description;
        [SerializeField] public string iconPath;
        [SerializeField] public List<Choice> choices;
        [SerializeField] public string effectType;
        [SerializeField] public bool skipAutoStart = false;
    }


    [System.Serializable]
    public class Choice
    {
        public string text;
        public string targetID;
        public string condition;
        public string publishEvent;
        public List<EventParameter> eventParams;
        public bool triggersQTE;
        public string qteID;
        [JsonProperty("delta_points")]
        public int deltaPoints;
    }
}

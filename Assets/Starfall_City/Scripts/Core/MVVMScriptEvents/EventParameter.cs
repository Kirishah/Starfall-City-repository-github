using UnityEngine;

[System.Serializable]
public class EventParameter
{
    public string key;
    public ParameterType type;
    public string stringValue;
    public GameObject objectValue;
    public Vector3 vectorValue;
}

public enum ParameterType { None, String, GameObject, Vector3 /* add more as needed */ }

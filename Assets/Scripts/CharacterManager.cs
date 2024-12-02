using System.Runtime.Serialization;
using UnityEngine;

public class CharacterManager : MonoBehaviour
{
    public GameObject[] objects = new GameObject[3]; // Array to hold your objects
    public GameObject currentObject;

    void Awake()
    {
        DontDestroyOnLoad(gameObject); // Make this object persistent
    }

    public void SetObject(int index)
    {
        if (currentObject != null)
        {
            Destroy(currentObject); // Destroy the current object if it exists
        }
        currentObject = Instantiate(objects[index]); // Instantiate the new object
    }
}

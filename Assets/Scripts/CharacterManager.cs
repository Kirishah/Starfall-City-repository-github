using System.Runtime.Serialization;
using UnityEngine;

public class CharacterManager : MonoBehaviour
{
    public static CharacterManager Instance;
    public GameObject[] objects = new GameObject[3]; // Array to hold your objects
    public GameObject currentObject;
    public int selectedIndex;

    void Awake()
    {
        // Check if an instance already exists
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Destroy this instance if one already exists
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Make this object persistent
    }

    public void SetObject(int index)
    {
        if (currentObject != null)
        {
            Destroy(currentObject);
            
        }

        currentObject = Instantiate(objects[index]);
        currentObject.transform.position = new Vector3(5.3f, 0.2f, 5.5f);
        selectedIndex = index;
    }
}

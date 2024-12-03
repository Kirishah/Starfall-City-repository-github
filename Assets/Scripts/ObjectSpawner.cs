using UnityEngine;

public class ObjectSpawner : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (CharacterManager.Instance.selectedIndex < 3)
        {
            // Instantiate the previously selected object
            CharacterManager.Instance.SetObject(CharacterManager.Instance.selectedIndex);
        }

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

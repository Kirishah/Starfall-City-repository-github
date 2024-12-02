using System.Runtime.Serialization;
using UnityEngine;

public class RedactorCharPicker : MonoBehaviour
{
    public void OnButton1Clicked()
    {
        if (CharacterManager.Instance == null)
        {
            Debug.LogError("objectManager is null! Cannot change object.");
            return; 
        }
        CharacterManager.Instance.SetObject(0); // Change to the first object
    }

    public void OnButton2Clicked()
    {
        if (CharacterManager.Instance == null)
        {
            Debug.LogError("objectManager is null! Cannot change object.");
            return; 
        }
        CharacterManager.Instance.SetObject(1); // Change to the second object
    }

    public void OnButton3Clicked()
    {
        if (CharacterManager.Instance == null)
        {
            Debug.LogError("objectManager is null! Cannot change object.");
            return; 
        }
        CharacterManager.Instance.SetObject(2); // Change to the third object
    }

}

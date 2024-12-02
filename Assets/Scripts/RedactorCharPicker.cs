using System.Runtime.Serialization;
using UnityEngine;

public class RedactorCharPicker : MonoBehaviour
{
    public CharacterManager objectManager; // Reference to the CharacterManager

    public void OnButton1Clicked()
    {
        objectManager.SetObject(0); // Change to the first object
    }

    public void OnButton2Clicked()
    {
        objectManager.SetObject(1); // Change to the second object
    }

    public void OnButton3Clicked()
    {
        objectManager.SetObject(2); // Change to the third object
    }

}

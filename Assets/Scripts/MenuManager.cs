using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public CharacterManager objectManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void NewGame()
    {
        SceneManager.LoadScene(1);
    }

    public void BackToMenu()
    {
        SceneManager.LoadScene(0);
    }


    void Start()
    {
        objectManager = FindObjectOfType<CharacterManager>();
        if (objectManager != null && CharacterManager.currentObject != null)
        {
            Instantiate(CharacterManager.currentObject); // Instantiate the current object in the new scene
        }
    }
}

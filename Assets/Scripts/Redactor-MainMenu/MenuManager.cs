using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void NewGame()
    {
        SceneManager.LoadScene(1);
    }

    public void BackToMenu()
    {
        SceneManager.LoadScene(0);
    }
}

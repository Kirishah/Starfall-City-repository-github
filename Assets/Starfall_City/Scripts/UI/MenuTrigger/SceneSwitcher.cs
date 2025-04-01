using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{
    public MenuTrigger menuTrigger;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // General switches in Game, in Main Menu or in Character Redactor
    public void Options()
    {

    }
    public void ExitGame()
    {
#if UNITY_EDITOR
        // If we are in the editor, stop play mode
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // If we are in a standalone build, quit the application
        Application.Quit();
#endif
    }
    public void BackToMenu()
    {
        SceneManager.LoadScene(0);
    }


    //Switches in Game Menu Bar
    public void ContinueGame()
    {
        menuTrigger.ToggleMenuBar();
    }
    public void Load()
    {

    }
    public void Save()
    {

    }
    
    

    //Switches in Main Menu
    public void LoadLastSave()
    {

    }
    public void NewGame()
    {
        SceneManager.LoadScene(1);
    }
    public void LoadSaveFromMenu()
    {

    }
    public void OpenCredits()
    {

    }

    //Switches in Character Redactor
    public void StartGame()
    {
        SceneManager.LoadScene(2);
    }
}

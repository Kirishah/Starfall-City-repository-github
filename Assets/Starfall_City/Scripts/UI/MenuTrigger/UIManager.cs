using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [SerializeField] private GameObject menuBar;
    [SerializeField] private bool pauseTime = true;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (menuBar != null)
        {
            menuBar.SetActive(false);
        }
        else
        {
            Debug.LogError("MenuBar reference is not set", this);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleMenuBar();
        }
    }

    public void ToggleMenuBar()
    {
        if (menuBar == null) return;

        bool newState = !menuBar.activeSelf;
        menuBar.SetActive(newState);

        if (pauseTime)
        {
            Time.timeScale = newState ? 0 : 1;
            Debug.Log($"Game {(newState ? "Paused" : "Resumed")}");
        }
    }

    private void OnDestroy()
    {
        if (pauseTime)
        {
            Time.timeScale = 1;
        }
    }

   


    //Switches in Game Menu Bar
    public void ContinueGame()
    {
        ToggleMenuBar();
    }
    public void Load()
    {

    }
    public void Save()
    {

    }
    public void Options()
    {

    }
    public void BackToMenu()
    {
        SceneManager.LoadScene(0);
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
}

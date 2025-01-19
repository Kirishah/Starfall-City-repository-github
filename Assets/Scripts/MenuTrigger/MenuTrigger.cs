using UnityEngine;

public class MenuTrigger : MonoBehaviour
{
    public GameObject menuBar;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        menuBar.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("Escape key pressed. Current Menu Bar Active: " + menuBar.activeSelf);
            ToggleMenuBar();
        }
    }

    public void ToggleMenuBar()
    {
        bool isActive = menuBar.activeSelf;
        menuBar.SetActive(!isActive); // Toggle the menu
        if (menuBar.activeSelf)
        {
            Time.timeScale = 0; // Pause the game
            Debug.Log("Game Paused");
        }
        else
        {
            Time.timeScale = 1; // Resume the game
            Debug.Log("Game Resumed");
        }
    }
}

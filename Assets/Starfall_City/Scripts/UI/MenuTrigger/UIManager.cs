using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;



public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Documents")]
    [SerializeField] private UIDocument mainMenuDocument;
    [SerializeField] private UIDocument inGameMenuDocument;
    [SerializeField] private bool pauseTimeOnMenu = true;

    private VisualElement mainMenuRoot;
    private VisualElement inGameMenuRoot;
    private VisualElement menuBar;
    private VisualElement backdrop;

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
        SceneManager.sceneLoaded += OnSceneLoaded;
        InitializeCurrentSceneUI();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void InitializeCurrentSceneUI()
    {
        if (SceneManager.GetActiveScene().buildIndex == 0)
            InitializeMainMenu();
        else
            InitializeInGameMenu();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => InitializeCurrentSceneUI();

    private void InitializeMainMenu()
    {
        if (mainMenuDocument == null) return;

        mainMenuRoot = mainMenuDocument.rootVisualElement;

        var buttons = mainMenuRoot.Query<Button>().ToList();
        foreach (var btn in buttons)
        {
            btn.text = btn.name switch
            {
                "Continue" => "Continue",
                "NewGame" => "New Game",
                "Load" => "Load Game",
                "Credits" => "Credits",
                "Exit" => "Exit",
                _ => btn.text
            };

            AddButtonEffects(btn);
        }

        // Bind actions
        mainMenuRoot.Q<Button>("NewGame")?.RegisterCallback<ClickEvent>(_ => StartNewGame());
        // loadBtn?.RegisterCallback<ClickEvent>(_ => LoadGame());
        // creditsBtn?.RegisterCallback<ClickEvent>(_ => OpenCredits());
        mainMenuRoot.Q<Button>("Exit")?.RegisterCallback<ClickEvent>(_ => ExitGame());

        // Continue button: try to load last save or disable if none exists
        /*
        if (continueBtn != null)
        {
            bool hasSave = PlayerPrefs.HasKey("LastSavedScene"); // Example check
            continueBtn.SetEnabled(hasSave);
            continueBtn.text = hasSave ? "Continue" : "No Save";
            if (hasSave)
                continueBtn.RegisterCallback<ClickEvent>(_ => ContinueGame());
        }
        */
    }

    private void InitializeInGameMenu()
    {
        if (inGameMenuDocument == null) return;
        inGameMenuDocument.sortingOrder = -1000;
        inGameMenuRoot = inGameMenuDocument.rootVisualElement;
        menuBar = inGameMenuRoot.Q<VisualElement>("MenuBar");

        // Simple backdrop
        backdrop = new VisualElement { name = "Backdrop" };
        backdrop.style.position = Position.Absolute;
        backdrop.style.top = backdrop.style.bottom = backdrop.style.left = backdrop.style.right = 0;
        backdrop.style.backgroundColor = new Color(0, 0, 0, 0);
        inGameMenuRoot.Insert(0, backdrop);
        backdrop.pickingMode = PickingMode.Ignore;

        // Initial state
        menuBar.style.display = DisplayStyle.None;

        var buttons = inGameMenuRoot.Query<Button>().ToList();
        foreach (var btn in buttons) AddButtonEffects(btn);

        // Hook up in-game menu buttons
        inGameMenuRoot.Q<Button>("Continue")?.RegisterCallback<ClickEvent>(_ => ToggleInGameMenu());
        //inGameMenuRoot.Q<Button>("Save")?.RegisterCallback<ClickEvent>(_ => SaveGame());
        //inGameMenuRoot.Q<Button>("Load")?.RegisterCallback<ClickEvent>(_ => LoadGame());
        //inGameMenuRoot.Q<Button>("Options")?.RegisterCallback<ClickEvent>(_ => OpenOptions());
        inGameMenuRoot.Q<Button>("ExitToMainMenu")?.RegisterCallback<ClickEvent>(_ => BackToMainMenu());
        inGameMenuRoot.Q<Button>("Exit")?.RegisterCallback<ClickEvent>(_ => ExitGame());
    }

    public void ToggleInGameMenu()
    {
        bool show = menuBar.style.display.value == DisplayStyle.None;

        menuBar.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        backdrop.style.backgroundColor = show ? new Color(0, 0, 0, 0.6f) : new Color(0, 0, 0, 0);
        // block clicks only when menu is open
        inGameMenuDocument.sortingOrder = show ? 1000 : -1000;
        backdrop.pickingMode = show ? PickingMode.Position : PickingMode.Ignore;

        if (pauseTimeOnMenu) Time.timeScale = show ? 0f : 1f;
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().buildIndex != 0 && Input.GetKeyDown(KeyCode.Escape))
            ToggleInGameMenu();
    }

    // BEAUTIFUL SIMPLE BUTTON ANIMATIONS
    private void AddButtonEffects(Button btn)
    {
        // Hover: scale up + bright
        btn.RegisterCallback<MouseEnterEvent>(e =>
        {
            btn.style.scale = new Scale(new Vector2(1.08f, 1.08f));
            btn.style.transitionDuration = new List<TimeValue> { new TimeValue(0.15f, TimeUnit.Second) };
            btn.style.transitionProperty = new List<StylePropertyName> { "scale", "background-color" };
        });

        // Leave: back to normal
        btn.RegisterCallback<MouseLeaveEvent>(e =>
        {
            btn.style.scale = new Scale(new Vector2(1f, 1f));
        });

        // Click: punch effect
        btn.RegisterCallback<MouseDownEvent>(e =>
        {
            if (e.button == 0)
                btn.style.scale = new Scale(new Vector2(0.95f, 0.95f));
        });

        btn.RegisterCallback<MouseUpEvent>(e =>
        {
            if (e.button == 0)
                btn.style.scale = new Scale(new Vector2(1.08f, 1.08f)); // back to hover state
        });
    }

    private void StartNewGame() { Time.timeScale = 1f; SceneManager.LoadScene(1); }

    private void BackToMainMenu() { Time.timeScale = 1f; SceneManager.LoadScene(0); }
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

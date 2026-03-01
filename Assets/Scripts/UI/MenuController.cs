using UnityEngine;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    public Button newGameButton;
    public Button loadGameButton;
    public Button controlsButton;
    public Button optionsButton;
    public Button quitButton;

    public GameObject controlsPanel;
    public GameObject optionsPanel;

    void Start()
    {
        if (GameManager.Instance == null)
        {
            GameObject gm = new GameObject("GameManager");
            gm.AddComponent<GameManager>();
        }

        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);

        SetupButtons();
    }

    void SetupButtons()
    {
        if (newGameButton != null)
        {
            newGameButton.onClick.AddListener(OnNewGame);
        }

        if (loadGameButton != null)
        {
            loadGameButton.onClick.AddListener(OnLoadGame);
        }

        if (controlsButton != null)
        {
            controlsButton.onClick.AddListener(OnControls);
        }

        if (optionsButton != null)
        {
            optionsButton.onClick.AddListener(OnOptions);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuit);
        }
    }

    void OnNewGame()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.NewGame();
        }
    }

    void OnLoadGame()
    {
        Debug.Log("Load Game - Not implemented yet");
    }

    void OnControls()
    {
        if (controlsPanel != null)
        {
            controlsPanel.SetActive(!controlsPanel.activeSelf);
            if (optionsPanel != null) optionsPanel.SetActive(false);
        }
    }

    void OnOptions()
    {
        if (optionsPanel != null)
        {
            optionsPanel.SetActive(!optionsPanel.activeSelf);
            if (controlsPanel != null) controlsPanel.SetActive(false);
        }
    }

    void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (controlsPanel != null && controlsPanel.activeSelf)
            {
                controlsPanel.SetActive(false);
            }
            else if (optionsPanel != null && optionsPanel.activeSelf)
            {
                optionsPanel.SetActive(false);
            }
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

public class MenuControllerUI : MonoBehaviour
{
    public GameObject newGameButton;
    public GameObject loadGameButton;
    public GameObject controlsButton;
    public GameObject optionsButton;
    public GameObject quitButton;

    void Start()
    {
        // Ensure GameManager exists
        if (GameManager.Instance == null)
        {
            GameObject gm = new GameObject("GameManager");
            gm.AddComponent<GameManager>();
        }

        // Wire up button events
        if (newGameButton != null)
        {
            var btn = newGameButton.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(OnNewGame);
        }
        if (loadGameButton != null)
        {
            var btn = loadGameButton.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(OnLoadGame);
        }
        if (controlsButton != null)
        {
            var btn = controlsButton.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(OnControls);
        }
        if (optionsButton != null)
        {
            var btn = optionsButton.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(OnOptions);
        }
        if (quitButton != null)
        {
            var btn = quitButton.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(OnQuit);
        }
    }

    void OnNewGame()
    {
        Debug.Log("New Game clicked");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.NewGame();
        }
    }

    void OnLoadGame()
    {
        Debug.Log("Load Game - Not implemented");
    }

    void OnControls()
    {
        Debug.Log("Controls - Not implemented");
    }

    void OnOptions()
    {
        Debug.Log("Options - Not implemented");
    }

    void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

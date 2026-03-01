using UnityEngine;
using UnityEngine.UI;

public class GameOverSceneController : MonoBehaviour
{
    public Button restartButton;
    public Button menuButton;

    void Start()
    {
        if (GameManager.Instance == null)
        {
            GameObject gm = new GameObject("GameManager");
            gm.AddComponent<GameManager>();
        }

        SetupButtons();
    }

    void SetupButtons()
    {
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestart);
        }

        if (menuButton != null)
        {
            menuButton.onClick.AddListener(OnMenu);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            OnRestart();
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnMenu();
        }
    }

    void OnRestart()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetHealth();
            GameManager.Instance.GoToPreviousScene();
        }
    }

    void OnMenu()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReturnToMenu();
        }
    }
}

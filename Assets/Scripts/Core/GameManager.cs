using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public int playerHealth = 7;
    public int maxPlayerHealth = 7;
    public int playerCoins = 0;

    public bool isInDialogue = false;
    public bool isTransitioning = false;
    public string previousScene = "";

    public bool battle1Complete = false;
    public bool battle2Complete = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void NewGame()
    {
        Debug.Log("GameManager.NewGame() called");
        playerHealth = maxPlayerHealth;
        playerCoins = 0;
        battle1Complete = false;
        battle2Complete = false;
        previousScene = "";
        Debug.Log("Loading TownScene...");
        GoToScene("TownScene");
    }

    public void TakeDamage(int amount)
    {
        playerHealth = Mathf.Max(0, playerHealth - amount);
        if (playerHealth <= 0)
        {
            GameOver();
        }
    }

    public void Heal(int amount)
    {
        playerHealth = Mathf.Min(maxPlayerHealth, playerHealth + amount);
    }

    public void ResetHealth()
    {
        playerHealth = maxPlayerHealth;
    }

    public void GameOver()
    {
        GoToScene("GameOverScene");
    }

    public void ReturnToMenu()
    {
        GoToScene("MenuScene");
    }

    public void GoToScene(string sceneName)
    {
        previousScene = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(sceneName);
    }

    public void GoToPreviousScene()
    {
        if (!string.IsNullOrEmpty(previousScene))
        {
            SceneManager.LoadScene(previousScene);
        }
    }
}

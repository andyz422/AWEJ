using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// How the player arrived at the current location; decides where they spawn.
public enum AreaEntry
{
    Start,
    FromPreviousArea,
    FromNextArea,
    FromBuilding,
    IntoBuilding,
    Respawn
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public int playerHealth = 7;
    public int maxPlayerHealth = 7;
    public int playerCoins = 0;

    public bool isInDialogue = false;
    public bool isTransitioning = false;
    public string previousScene = "";

    [Header("World Progress")]
    public int currentArea = 0;
    public string currentBuilding = "";
    public string lastBuilding = "";
    public AreaEntry pendingEntry = AreaEntry.Start;
    public string pendingMessage = "";
    public int loopCount = 0;
    public List<string> clearedBattles = new List<string>();

    [Header("Character")]
    // Chosen on the character select screen before NewGame(); see PlayerCharacters.
    public string playerCharacter = PlayerCharacters.DefaultId;

    [Header("Loadout")]
    public string equippedWeapon = WeaponCatalog.StarterWeapon;
    public List<string> ownedWeapons = new List<string> { WeaponCatalog.StarterWeapon };

    private readonly Dictionary<string, int> upgradeLevels = new Dictionary<string, int>();

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
        maxPlayerHealth = 7;
        playerHealth = maxPlayerHealth;
        playerCoins = 0;
        previousScene = "";

        currentArea = 0;
        currentBuilding = "";
        lastBuilding = "";
        pendingEntry = AreaEntry.Start;
        pendingMessage = "";
        loopCount = 0;
        clearedBattles.Clear();

        equippedWeapon = WeaponCatalog.StarterWeapon;
        ownedWeapons.Clear();
        ownedWeapons.Add(WeaponCatalog.StarterWeapon);
        upgradeLevels.Clear();

        Debug.Log("Loading TownScene...");
        GoToScene("TownScene");
    }

    public int GetUpgradeLevel(string upgradeId)
    {
        int level;
        return upgradeLevels.TryGetValue(upgradeId, out level) ? level : 0;
    }

    public void SetUpgradeLevel(string upgradeId, int level)
    {
        upgradeLevels[upgradeId] = level;
    }

    public bool IsBattleCleared(string buildingId)
    {
        return clearedBattles.Contains(buildingId);
    }

    public void MarkBattleCleared(string buildingId)
    {
        if (!clearedBattles.Contains(buildingId))
        {
            clearedBattles.Add(buildingId);
        }
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

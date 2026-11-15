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

// State that belongs to one player. In co-op every player keeps their own (health, the weapons
// and upgrades they bought); GameManager.Me is the player on this device.
[System.Serializable]
public class PlayerProfile
{
    public const int StartHealth = 7;

    public int health = StartHealth;
    public int maxHealth = StartHealth;
    public string equippedWeapon = WeaponCatalog.StarterWeapon;
    public List<string> ownedWeapons = new List<string> { WeaponCatalog.StarterWeapon };
    public readonly Dictionary<string, int> upgrades = new Dictionary<string, int>();

    public void Reset()
    {
        maxHealth = StartHealth;
        health = StartHealth;
        equippedWeapon = WeaponCatalog.StarterWeapon;
        ownedWeapons.Clear();
        ownedWeapons.Add(WeaponCatalog.StarterWeapon);
        upgrades.Clear();
    }
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // This device's player. The fields below forward to it so existing code keeps working.
    public PlayerProfile Me = new PlayerProfile();

    public int playerHealth { get { return Me.health; } set { Me.health = value; } }
    public int maxPlayerHealth { get { return Me.maxHealth; } set { Me.maxHealth = value; } }
    public string equippedWeapon { get { return Me.equippedWeapon; } set { Me.equippedWeapon = value; } }
    public List<string> ownedWeapons { get { return Me.ownedWeapons; } }

    // The party's shared purse: every coin picked up and every battle reward goes here,
    // and every player spends from it on their own upgrades. In co-op the host's PartyState
    // holds it; only the host changes it (guests ask through PartyState.RequestPurchase).
    private int localCoins = 0;

    public int LocalCoins
    {
        get { return localCoins; }
    }

    public int playerCoins
    {
        get
        {
            PartyState party = PartyState.Instance;
            return Net.Active && party != null && party.IsSpawned ? party.Coins.Value : localCoins;
        }
        set
        {
            PartyState party = PartyState.Instance;
            if (Net.Active && party != null && party.IsSpawned)
            {
                if (Net.IsAuthority)
                {
                    party.Coins.Value = value;
                }
                else
                {
                    Debug.LogWarning("Guests can't change the shared purse directly.");
                }
                return;
            }
            localCoins = value;
        }
    }

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
        ResetRun();
        Debug.Log("Loading TownScene...");
        GoToScene("TownScene");
    }

    // Fresh run: this player's profile, the purse and world progress. Co-op hosts and guests call
    // this when a session starts (the host's PartyState then shares the world state).
    public void ResetRun()
    {
        Me.Reset();
        localCoins = 0;
        previousScene = "";
        isTransitioning = false;
        isInDialogue = false;

        currentArea = 0;
        currentBuilding = "";
        lastBuilding = "";
        pendingEntry = AreaEntry.Start;
        pendingMessage = "";
        loopCount = 0;
        clearedBattles.Clear();
    }

    public int GetUpgradeLevel(string upgradeId)
    {
        int level;
        return Me.upgrades.TryGetValue(upgradeId, out level) ? level : 0;
    }

    public void SetUpgradeLevel(string upgradeId, int level)
    {
        Me.upgrades[upgradeId] = level;
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
        if (Net.Active && NetSession.Instance != null)
        {
            NetSession.Instance.Leave();
            return;
        }
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

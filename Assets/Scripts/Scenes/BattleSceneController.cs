using UnityEngine;

public class BattleSceneController : MonoBehaviour
{
    public bool isBossBattle = false;
    public int enemyMaxHealth = 7;
    public int phase2Threshold = 7;

    public GameObject playerObject;
    public GameObject enemyObject;
    public EnemyController enemyController;
    public PlayerHealth playerHealth;

    public HealthBar playerHealthBar;
    public HealthBar enemyHealthBar;

    public Sprite[] victoryDialogueSprites;

    private bool battleEnded = false;

    void Start()
    {
        if (GameManager.Instance == null)
        {
            GameObject gm = new GameObject("GameManager");
            gm.AddComponent<GameManager>();
        }

        SetupBattle();
    }

    void SetupBattle()
    {
        if (enemyController != null)
        {
            enemyController.maxHealth = enemyMaxHealth;
            enemyController.currentHealth = enemyMaxHealth;

            if (isBossBattle)
            {
                enemyController.hasTwoPhases = true;
                enemyController.phase2HealthThreshold = phase2Threshold;
                enemyController.trackPlayerInPhase2 = true;
            }
        }
    }

    void Update()
    {
        if (battleEnded)
        {
            return;
        }

        if (enemyController != null && enemyController.IsDead())
        {
            OnEnemyDefeated();
        }
    }

    void OnEnemyDefeated()
    {
        if (battleEnded)
        {
            return;
        }

        battleEnded = true;

        if (GameManager.Instance != null)
        {
            if (isBossBattle)
            {
                GameManager.Instance.battle2Complete = true;
            }
            else
            {
                GameManager.Instance.battle1Complete = true;
            }
        }

        if (DialogueSystem.Instance != null && victoryDialogueSprites != null && victoryDialogueSprites.Length > 0)
        {
            DialogueSystem.Instance.StartDialogue(victoryDialogueSprites, OnVictoryDialogueComplete);
        }
        else
        {
            OnVictoryDialogueComplete();
        }
    }

    void OnVictoryDialogueComplete()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GoToScene("TownScene");
        }
    }
}

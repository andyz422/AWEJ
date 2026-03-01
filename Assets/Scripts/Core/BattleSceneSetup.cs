using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class BattleSceneSetup : MonoBehaviour
{
    private EnemyController enemyController;
    private PlayerHealth playerHealth;
    private Text playerHealthText;
    private Text enemyHealthText;
    private bool isBossBattle;

    void Start()
    {
        isBossBattle = SceneManager.GetActiveScene().name == "Battle2Scene";
        SetupScene();
    }

    void SetupScene()
    {
        // Create EventSystem if needed
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        // Find existing objects in scene
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerHealth = player.GetComponent<PlayerHealth>();

            // Setup projectile prefab for player weapon
            Weapon playerWeapon = player.GetComponent<Weapon>();
            if (playerWeapon != null)
            {
                playerWeapon.projectilePrefab = CreateProjectilePrefab(true);
            }
        }

        GameObject enemy = GameObject.Find("Enemy");
        if (enemy == null) enemy = GameObject.Find("Boss");
        if (enemy != null)
        {
            enemyController = enemy.GetComponent<EnemyController>();

            // Setup projectile prefab for enemy weapon
            Weapon enemyWeapon = enemy.GetComponent<Weapon>();
            if (enemyWeapon != null)
            {
                enemyWeapon.projectilePrefab = CreateProjectilePrefab(false);
                enemyController.weapon = enemyWeapon;
            }
        }

        // Create UI
        CreateBattleUI();

        Debug.Log((isBossBattle ? "Boss" : "Battle") + " Scene Setup - A/D to move, Space to shoot");
    }

    void Update()
    {
        // Update health displays
        if (playerHealthText != null && playerHealth != null)
        {
            playerHealthText.text = "Player: " + playerHealth.currentHealth;
        }

        if (enemyHealthText != null && enemyController != null)
        {
            enemyHealthText.text = "Enemy: " + enemyController.currentHealth;
        }

        // Check win condition
        if (enemyController != null && enemyController.IsDead())
        {
            OnVictory();
        }
    }

    void OnVictory()
    {
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
            GameManager.Instance.GoToScene("TownScene");
        }
    }

    void CreateBattleUI()
    {
        GameObject canvasObj = new GameObject("BattleCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        // Player health
        playerHealthText = CreateText(canvasObj.transform, "Player: 7", new Vector2(-300, -450), TextAnchor.MiddleLeft);

        // Enemy health
        enemyHealthText = CreateText(canvasObj.transform, "Enemy: 7", new Vector2(300, 450), TextAnchor.MiddleRight);

        // Instructions
        CreateText(canvasObj.transform, "A/D to move - Space to shoot", new Vector2(0, -480), TextAnchor.MiddleCenter);
    }

    Text CreateText(Transform parent, string content, Vector2 position, TextAnchor alignment)
    {
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(parent, false);

        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(400, 50);

        Text text = textObj.AddComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 28;
        text.alignment = alignment;
        text.color = Color.white;

        return text;
    }

    GameObject CreateProjectilePrefab(bool isPlayerProjectile)
    {
        GameObject prefab = new GameObject("ProjectilePrefab");
        prefab.SetActive(false);

        SpriteRenderer sprite = prefab.AddComponent<SpriteRenderer>();
        sprite.sprite = LoadSprite("Weapons/laser");
        sprite.color = isPlayerProjectile ? Color.cyan : Color.red;
        prefab.transform.localScale = new Vector3(0.5f, 0.5f, 1);

        BoxCollider2D collider = prefab.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(1, 1);

        prefab.AddComponent<Projectile>();

        return prefab;
    }

    Sprite CreateSquareSprite()
    {
        Texture2D tex = new Texture2D(32, 32);
        Color[] colors = new Color[32 * 32];
        for (int i = 0; i < colors.Length; i++)
            colors[i] = Color.white;
        tex.SetPixels(colors);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);
    }

    Sprite LoadSprite(string path)
    {
        // Try loading as Sprite first
        Sprite sprite = Resources.Load<Sprite>(path);
        if (sprite != null)
        {
            return sprite;
        }

        // Try loading as Texture2D and converting
        Texture2D tex = Resources.Load<Texture2D>(path);
        if (tex != null)
        {
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100);
        }

        Debug.LogWarning("Could not load sprite: " + path);
        return CreateSquareSprite();
    }
}

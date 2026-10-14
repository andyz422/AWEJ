using System.Collections.Generic;
using UnityEngine;

public enum EnemyType { Slime, Charger, Spitter, Brute, Boss }

// Battle-room enemy built from primitives. Moves with a CharacterController so it respects walls and cover.
[RequireComponent(typeof(CharacterController))]
public class Enemy : MonoBehaviour
{
    public static readonly List<Enemy> Alive = new List<Enemy>();

    const float PlayerRadius = 0.5f;
    const float SpawnDuration = 0.35f;

    public EnemyType type;
    public int maxHealth = 3;
    public int health;
    public float moveSpeed = 2.5f;
    public int contactDamage = 1;
    public int coinValue = 1;
    public float radius = 0.7f;
    public float height = 1.4f;
    public Color color = Color.green;
    public BattleArena arena;
    public System.Action<Enemy> onDeath;

    private enum Mode { Approach, Windup, Dash, Recover }

    private static GameObject projectilePrefab;

    private CharacterController controller;
    private Transform player;
    private PlayerHealth playerHealth;
    private Transform body;
    private Transform hpBar;
    private Transform hpFill;
    private Renderer[] renderers;
    private Material[] originalMaterials;
    private float flashTimer;
    private float spawnTimer = SpawnDuration;
    private float verticalVelocity;
    private Mode mode = Mode.Approach;
    private float modeTimer;
    private Vector3 dashDirection;
    private float attackTimer;
    private float strafeSign = 1f;
    private float strafeTimer;
    private float summonTimer = 6f;
    private bool burstAimed;

    public static Enemy Spawn(EnemyType type, Vector3 position, float healthScale, BattleArena arena)
    {
        GameObject obj = new GameObject(type.ToString());
        obj.transform.position = position;
        Enemy enemy = obj.AddComponent<Enemy>();
        enemy.arena = arena;
        enemy.Configure(type, healthScale);
        return enemy;
    }

    void OnEnable()
    {
        Alive.Add(this);
    }

    void OnDisable()
    {
        Alive.Remove(this);
    }

    void Configure(EnemyType enemyType, float healthScale)
    {
        type = enemyType;
        int baseHealth;

        switch (type)
        {
            case EnemyType.Charger:
                baseHealth = 4; moveSpeed = 2.4f; coinValue = 2; radius = 0.75f; height = 1.5f;
                color = new Color(0.35f, 0.28f, 0.55f);
                attackTimer = Random.Range(0.5f, 1.5f);
                break;
            case EnemyType.Spitter:
                baseHealth = 3; moveSpeed = 2.8f; coinValue = 2; radius = 0.6f; height = 2f;
                color = new Color(0.75f, 0.3f, 0.85f);
                attackTimer = Random.Range(1f, 2f);
                break;
            case EnemyType.Brute:
                baseHealth = 12; moveSpeed = 1.8f; coinValue = 4; contactDamage = 2; radius = 1.1f; height = 2.4f;
                color = new Color(0.8f, 0.25f, 0.2f);
                break;
            case EnemyType.Boss:
                baseHealth = 70; moveSpeed = 1.9f; coinValue = 30; contactDamage = 2; radius = 2f; height = 4.2f;
                color = new Color(1f, 0.4f, 0.1f);
                attackTimer = 2.5f;
                break;
            default:
                baseHealth = 3; moveSpeed = 3f; coinValue = 1; radius = 0.7f; height = 1.4f;
                color = new Color(0.4f, 0.85f, 0.35f);
                break;
        }

        maxHealth = Mathf.CeilToInt(baseHealth * healthScale);
        health = maxHealth;

        controller = GetComponent<CharacterController>();
        controller.radius = radius;
        controller.height = height;
        controller.center = new Vector3(0f, height * 0.5f, 0f);
        controller.skinWidth = 0.05f;
        controller.stepOffset = 0.2f;

        BuildVisual();
    }

    void BuildVisual()
    {
        body = new GameObject("Body").transform;
        body.SetParent(transform, false);
        Color dark = Color.Lerp(color, Color.black, 0.45f);

        switch (type)
        {
            case EnemyType.Charger:
                WorldKit.Prim(PrimitiveType.Sphere, body, new Vector3(0f, 0.65f, 0f), new Vector3(1.4f, 1.1f, 1.8f), color, false);
                WorldKit.Prim(PrimitiveType.Cube, body, new Vector3(0f, 1.05f, -0.1f), new Vector3(0.08f, 0.2f, 1.5f), dark, false);
                WorldKit.Prim(PrimitiveType.Cube, body, new Vector3(0f, 0.85f, 1.05f), new Vector3(0.22f, 0.22f, 0.8f), new Color(0.9f, 0.85f, 0.7f), false)
                    .transform.localRotation = Quaternion.Euler(-25f, 0f, 0f);
                AddEyes(new Vector3(0f, 0.8f, 0.8f), 0.32f, 0.22f);
                for (int side = -1; side <= 1; side += 2)
                {
                    for (int leg = -1; leg <= 1; leg++)
                    {
                        WorldKit.Prim(PrimitiveType.Cube, body, new Vector3(side * 0.75f, 0.2f, leg * 0.5f), new Vector3(0.5f, 0.12f, 0.12f), dark, false);
                    }
                }
                break;
            case EnemyType.Spitter:
                WorldKit.Prim(PrimitiveType.Cylinder, body, new Vector3(0f, 0.7f, 0f), new Vector3(1.1f, 0.7f, 1.1f), color, false);
                WorldKit.Prim(PrimitiveType.Sphere, body, new Vector3(0f, 1.6f, 0f), Vector3.one * 1.25f, color, false);
                WorldKit.Prim(PrimitiveType.Cylinder, body, new Vector3(0f, 1.5f, 0.6f), new Vector3(0.45f, 0.25f, 0.45f), dark, false)
                    .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                AddEyes(new Vector3(0f, 1.9f, 0.5f), 0.35f, 0.24f);
                for (int i = 0; i < 3; i++)
                {
                    float angle = i * 120f * Mathf.Deg2Rad;
                    WorldKit.Prim(PrimitiveType.Sphere, body, new Vector3(Mathf.Sin(angle) * 0.4f, 2.3f, Mathf.Cos(angle) * 0.4f), Vector3.one * 0.3f, dark, false);
                }
                break;
            case EnemyType.Brute:
                WorldKit.Prim(PrimitiveType.Cube, body, new Vector3(0f, 1.15f, 0f), new Vector3(2f, 2.1f, 1.7f), color, false);
                WorldKit.Prim(PrimitiveType.Cube, body, new Vector3(-1.25f, 1f, 0.2f), new Vector3(0.6f, 1.6f, 0.6f), dark, false);
                WorldKit.Prim(PrimitiveType.Cube, body, new Vector3(1.25f, 1f, 0.2f), new Vector3(0.6f, 1.6f, 0.6f), dark, false);
                WorldKit.Prim(PrimitiveType.Cube, body, new Vector3(0f, 1.95f, 0.86f), new Vector3(1.4f, 0.18f, 0.1f), dark, false);
                AddEyes(new Vector3(0f, 1.65f, 0.85f), 0.5f, 0.3f);
                break;
            case EnemyType.Boss:
                WorldKit.Prim(PrimitiveType.Sphere, body, new Vector3(0f, 2.1f, 0f), Vector3.one * 4.2f, color, false);
                WorldKit.Prim(PrimitiveType.Sphere, body, new Vector3(0f, 1.2f, 1.5f), new Vector3(2.4f, 0.6f, 1f), dark, false);
                AddEyes(new Vector3(0f, 2.8f, 1.65f), 0.9f, 0.55f);
                Color gold = new Color(1f, 0.82f, 0.2f);
                WorldKit.Prim(PrimitiveType.Cylinder, body, new Vector3(0f, 4.15f, 0f), new Vector3(2.2f, 0.25f, 2.2f), gold, false);
                for (int i = 0; i < 6; i++)
                {
                    float angle = i * 60f * Mathf.Deg2Rad;
                    GameObject spike = WorldKit.Prim(PrimitiveType.Cube, body, new Vector3(Mathf.Sin(angle) * 0.9f, 4.65f, Mathf.Cos(angle) * 0.9f), new Vector3(0.35f, 0.8f, 0.35f), gold, false);
                    spike.transform.localRotation = Quaternion.Euler(0f, i * 60f, 45f);
                }
                break;
            default:
                WorldKit.Prim(PrimitiveType.Sphere, body, new Vector3(0f, 0.6f, 0f), new Vector3(1.5f, 1.2f, 1.5f), color, false);
                WorldKit.Prim(PrimitiveType.Sphere, body, new Vector3(0.35f, 0.95f, 0.2f), Vector3.one * 0.3f, Color.Lerp(color, Color.white, 0.6f), false);
                AddEyes(new Vector3(0f, 0.75f, 0.68f), 0.3f, 0.22f);
                break;
        }

        renderers = body.GetComponentsInChildren<Renderer>();
        originalMaterials = new Material[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i].sharedMaterial;
        }

        BuildHealthBar();
        body.localScale = Vector3.one * 0.01f;
    }

    void AddEyes(Vector3 center, float spacing, float size)
    {
        for (int side = -1; side <= 1; side += 2)
        {
            Vector3 position = center + new Vector3(side * spacing * 0.5f, 0f, 0f);
            WorldKit.Prim(PrimitiveType.Sphere, body, position, Vector3.one * size, Color.white, false);
            WorldKit.Prim(PrimitiveType.Sphere, body, position + new Vector3(0f, 0f, size * 0.4f), Vector3.one * size * 0.5f, Color.black, false);
        }
    }

    void BuildHealthBar()
    {
        hpBar = new GameObject("HealthBar").transform;
        hpBar.SetParent(transform, false);
        hpBar.localPosition = new Vector3(0f, height + 0.6f, 0f);
        hpBar.gameObject.AddComponent<Billboard>();

        float width = type == EnemyType.Boss ? 3f : 1.2f;
        WorldKit.Prim(PrimitiveType.Cube, hpBar, Vector3.zero, new Vector3(width + 0.08f, 0.2f, 0.02f), new Color(0.1f, 0.1f, 0.1f), false);
        hpFill = WorldKit.Prim(PrimitiveType.Cube, hpBar, new Vector3(0f, 0f, -0.02f), new Vector3(width, 0.14f, 0.02f), new Color(1f, 0.25f, 0.2f), false).transform;
        hpBar.gameObject.SetActive(type == EnemyType.Boss);
    }

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerHealth = playerObj.GetComponent<PlayerHealth>();
        }
    }

    void Update()
    {
        UpdateFlash();

        if (spawnTimer > 0f)
        {
            spawnTimer -= Time.deltaTime;
            float t = 1f - Mathf.Clamp01(spawnTimer / SpawnDuration);
            body.localScale = Vector3.one * Mathf.Max(0.01f, t + Mathf.Sin(t * Mathf.PI) * 0.25f);
            return;
        }

        if (player == null || (GameManager.Instance != null && GameManager.Instance.isTransitioning))
        {
            return;
        }

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        float distance = toPlayer.magnitude;
        Vector3 direction = distance > 0.01f ? toPlayer / distance : transform.forward;

        Vector3 move;
        switch (type)
        {
            case EnemyType.Charger: move = UpdateCharger(direction, distance); break;
            case EnemyType.Spitter: move = UpdateSpitter(direction, distance); break;
            case EnemyType.Boss: move = UpdateBoss(direction, distance); break;
            case EnemyType.Brute:
                float rage = health < maxHealth * 0.5f ? 1.5f : 1f;
                move = Chase(direction, distance, moveSpeed * rage);
                break;
            default: move = UpdateSlime(direction, distance); break;
        }

        if (mode != Mode.Dash)
        {
            Face(direction);
        }

        verticalVelocity = controller.isGrounded ? -2f : verticalVelocity - 20f * Time.deltaTime;
        move.y = verticalVelocity;
        CollisionFlags flags = controller.Move(move * Time.deltaTime);

        if (mode == Mode.Dash && (flags & CollisionFlags.Sides) != 0)
        {
            EndDash();
        }

        if (playerHealth != null && distance < radius + PlayerRadius + 0.35f)
        {
            playerHealth.TakeDamage(contactDamage);
        }

        UpdateHealthBar();
    }

    Vector3 Chase(Vector3 direction, float distance, float speed)
    {
        // Stop at arm's length instead of shoving into the player.
        return distance > radius + PlayerRadius + 0.1f ? direction * speed : Vector3.zero;
    }

    void Face(Vector3 direction)
    {
        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 8f * Time.deltaTime);
        }
    }

    Vector3 UpdateSlime(Vector3 direction, float distance)
    {
        float hop = Mathf.Abs(Mathf.Sin(Time.time * 5f + GetInstanceID()));
        body.localScale = new Vector3(1f + (1f - hop) * 0.15f, 0.85f + hop * 0.25f, 1f + (1f - hop) * 0.15f);
        return Chase(direction, distance, moveSpeed * (0.4f + hop));
    }

    Vector3 UpdateCharger(Vector3 direction, float distance)
    {
        modeTimer -= Time.deltaTime;
        attackTimer -= Time.deltaTime;

        switch (mode)
        {
            case Mode.Windup:
                // Rattle in place, then lunge along the locked-in direction.
                body.localPosition = new Vector3(Mathf.Sin(Time.time * 60f) * 0.08f, 0f, 0f);
                if (modeTimer <= 0f)
                {
                    body.localPosition = Vector3.zero;
                    mode = Mode.Dash;
                    modeTimer = 0.6f;
                }
                return Vector3.zero;
            case Mode.Dash:
                if (modeTimer <= 0f)
                {
                    EndDash();
                }
                return dashDirection * 15f;
            case Mode.Recover:
                if (modeTimer <= 0f)
                {
                    mode = Mode.Approach;
                }
                return Vector3.zero;
        }

        if (distance < 11f && attackTimer <= 0f)
        {
            mode = Mode.Windup;
            modeTimer = 0.7f;
            dashDirection = direction;
            transform.rotation = Quaternion.LookRotation(direction);
            return Vector3.zero;
        }

        return Chase(direction, distance, moveSpeed);
    }

    void EndDash()
    {
        mode = Mode.Recover;
        modeTimer = 0.8f;
        attackTimer = Random.Range(1.2f, 2f);
    }

    Vector3 UpdateSpitter(Vector3 direction, float distance)
    {
        attackTimer -= Time.deltaTime;
        strafeTimer -= Time.deltaTime;

        if (strafeTimer <= 0f)
        {
            strafeSign = Random.value < 0.5f ? -1f : 1f;
            strafeTimer = Random.Range(1.5f, 3f);
        }

        if (attackTimer <= 0f && distance < 24f)
        {
            FireAt(direction, 9f);
            attackTimer = Random.Range(1.6f, 2.4f);
        }

        // Keep a comfortable firing distance and circle the player.
        if (distance > 11f) return direction * moveSpeed;
        if (distance < 6f) return -direction * moveSpeed;
        return Vector3.Cross(Vector3.up, direction) * strafeSign * moveSpeed * 0.8f;
    }

    Vector3 UpdateBoss(Vector3 direction, float distance)
    {
        attackTimer -= Time.deltaTime;
        summonTimer -= Time.deltaTime;

        float pulse = 1f + Mathf.Sin(Time.time * 3f) * 0.04f;
        body.localScale = new Vector3(pulse, 2f - pulse, pulse);

        if (attackTimer <= 0f)
        {
            if (burstAimed)
            {
                for (int i = -2; i <= 2; i++)
                {
                    FireAt(Quaternion.Euler(0f, i * 12f, 0f) * direction, 11f);
                }
            }
            else
            {
                const int shots = 16;
                for (int i = 0; i < shots; i++)
                {
                    FireAt(Quaternion.Euler(0f, i * 360f / shots, 0f) * Vector3.forward, 8f);
                }
            }
            burstAimed = !burstAimed;
            attackTimer = health < maxHealth * 0.5f ? 2f : 2.8f;
        }

        // Once wounded, the boss calls in slime reinforcements.
        if (health < maxHealth * 0.6f && summonTimer <= 0f && arena != null)
        {
            arena.SpawnMinion(EnemyType.Slime, transform.position + Quaternion.Euler(0f, 90f, 0f) * direction * 4f);
            arena.SpawnMinion(EnemyType.Slime, transform.position - Quaternion.Euler(0f, 90f, 0f) * direction * 4f);
            summonTimer = 8f;
        }

        return Chase(direction, distance, moveSpeed);
    }

    void FireAt(Vector3 direction, float speed)
    {
        if (projectilePrefab == null)
        {
            projectilePrefab = Resources.Load<GameObject>("Prefabs/Projectile");
            if (projectilePrefab == null)
            {
                return;
            }
        }

        direction.y = 0f;
        direction.Normalize();
        // Fire at the player's chest height regardless of how tall this enemy is.
        Vector3 origin = transform.position + direction * (radius + 0.4f);
        origin.y = 1f;

        GameObject obj = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(direction));
        Projectile projectile = obj.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.Initialize(direction, speed, 1);
            projectile.Configure(true, false, new Color(1f, 0.2f, 0.45f), 0.45f);
        }
    }

    public void TakeDamage(int amount)
    {
        if (health <= 0 || spawnTimer > 0f)
        {
            return;
        }

        health -= amount;
        flashTimer = 0.08f;
        foreach (Renderer r in renderers)
        {
            r.sharedMaterial = WorldKit.Colored(Color.white);
        }

        if (health <= 0)
        {
            Die();
        }
    }

    void UpdateFlash()
    {
        if (flashTimer <= 0f)
        {
            return;
        }

        flashTimer -= Time.deltaTime;
        if (flashTimer <= 0f)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].sharedMaterial = originalMaterials[i];
            }
        }
    }

    void UpdateHealthBar()
    {
        if (health < maxHealth && !hpBar.gameObject.activeSelf)
        {
            hpBar.gameObject.SetActive(true);
        }

        float fraction = Mathf.Clamp01(health / (float)maxHealth);
        float width = type == EnemyType.Boss ? 3f : 1.2f;
        hpFill.localScale = new Vector3(width * fraction, hpFill.localScale.y, hpFill.localScale.z);
        hpFill.localPosition = new Vector3(-width * 0.5f * (1f - fraction), 0f, -0.02f);
    }

    void Die()
    {
        Vector3 center = transform.position + Vector3.up * height * 0.5f;
        Debris.Burst(center, color, type == EnemyType.Boss ? 24 : 8, type == EnemyType.Boss ? 0.6f : 0.3f);

        int coins = Mathf.Min(coinValue, 6);
        for (int i = 0; i < coins; i++)
        {
            int value = coinValue / coins + (i < coinValue % coins ? 1 : 0);
            CoinPickup.Spawn(center, value);
        }

        onDeath?.Invoke(this);
        Destroy(gameObject);
    }
}

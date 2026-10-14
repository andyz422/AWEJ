using System;
using System.Collections;
using UnityEngine;

// Runs the waves inside a battle building. Spawns are telegraphed by a red marker on the floor.
public class BattleArena : MonoBehaviour
{
    public static BattleArena Instance { get; private set; }

    public bool InProgress { get; private set; }
    public bool Finished { get; private set; }
    public event Action onFinished;

    private BuildingDef building;
    private Rect bounds;
    private float healthScale = 1f;
    private Transform player;
    private int pendingSpawns;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void Begin(BuildingDef battleBuilding, Rect spawnBounds, float enemyHealthScale)
    {
        building = battleBuilding;
        bounds = spawnBounds;
        healthScale = enemyHealthScale;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        player = playerObj != null ? playerObj.transform : null;

        StartCoroutine(Run());
    }

    public void Abort()
    {
        StopAllCoroutines();
        InProgress = false;
    }

    IEnumerator Run()
    {
        InProgress = true;
        EnemyType[][] waves = building.battle.waves;

        Banner(building.name, 2f);
        yield return new WaitForSeconds(2f);

        for (int w = 0; w < waves.Length; w++)
        {
            bool last = w == waves.Length - 1;
            Banner(last && waves.Length > 1 ? "Final Wave!" : "Wave " + (w + 1) + " / " + waves.Length, 1.5f);
            yield return new WaitForSeconds(1f);

            foreach (EnemyType type in waves[w])
            {
                StartCoroutine(SpawnWithTelegraph(type, FindSpawnPoint()));
                yield return new WaitForSeconds(0.3f);
            }

            while (pendingSpawns > 0 || Enemy.Alive.Count > 0)
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.8f);
        }

        Victory();
    }

    void Victory()
    {
        InProgress = false;
        Finished = true;

        GameManager gm = GameManager.Instance;
        if (gm != null)
        {
            bool firstClear = !gm.IsBattleCleared(building.id);
            int reward = firstClear ? building.battle.reward : building.battle.repeatReward;
            gm.playerCoins += reward;
            gm.MarkBattleCleared(building.id);

            if (AreaHUD.Instance != null)
            {
                AreaHUD.Instance.Toast((firstClear ? "First clear! " : "Cleared again! ") + "+" + reward + " coins\nThe doors are open.", 4f);
            }
        }

        Banner("VICTORY!", 2.5f);

        if (onFinished != null)
        {
            onFinished();
        }
    }

    static void Banner(string text, float duration)
    {
        if (AreaHUD.Instance != null)
        {
            AreaHUD.Instance.Banner(text, duration);
        }
    }

    public void SpawnMinion(EnemyType type, Vector3 near)
    {
        Vector3 point = new Vector3(
            Mathf.Clamp(near.x, bounds.xMin, bounds.xMax),
            0.1f,
            Mathf.Clamp(near.z, bounds.yMin, bounds.yMax));
        StartCoroutine(SpawnWithTelegraph(type, point));
    }

    IEnumerator SpawnWithTelegraph(EnemyType type, Vector3 position)
    {
        pendingSpawns++;

        GameObject marker = WorldKit.Prim(PrimitiveType.Cylinder, transform, position + Vector3.up * 0.04f, Vector3.zero, new Color(1f, 0.2f, 0.15f), false);
        float size = type == EnemyType.Boss ? 5f : type == EnemyType.Brute ? 2.8f : 1.8f;

        const float duration = 0.8f;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float pulse = Mathf.Lerp(0.3f, 1f, t / duration) * size;
            marker.transform.localScale = new Vector3(pulse, 0.01f, pulse);
            yield return null;
        }

        Destroy(marker);
        Enemy.Spawn(type, position, healthScale, this);
        pendingSpawns--;
    }

    // Random open spot in the arena, away from the player and not inside cover.
    Vector3 FindSpawnPoint()
    {
        Vector3 best = new Vector3(bounds.center.x, 0.1f, bounds.yMax);
        float bestDistance = -1f;

        for (int attempt = 0; attempt < 25; attempt++)
        {
            Vector3 candidate = new Vector3(UnityEngine.Random.Range(bounds.xMin, bounds.xMax), 0.1f, UnityEngine.Random.Range(bounds.yMin, bounds.yMax));
            if (Physics.CheckSphere(candidate + Vector3.up * 1.3f, 1.1f, ~0, QueryTriggerInteraction.Ignore))
            {
                continue;
            }

            float distance = player != null ? Vector3.Distance(candidate, player.position) : 100f;
            if (distance >= 9f)
            {
                return candidate;
            }

            if (distance > bestDistance)
            {
                bestDistance = distance;
                best = candidate;
            }
        }

        return best;
    }
}

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
    // (spawn spacing uses PlayerRegistry)
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

        StartCoroutine(Run());
    }

    public void Abort()
    {
        StopAllCoroutines();
        InProgress = false;
        SetSharedInProgress(false);
    }

    // Co-op: guests read this to keep the exit barred until the battle is won.
    static void SetSharedInProgress(bool value)
    {
        if (Net.Active && Net.IsAuthority && PartyState.Instance != null && PartyState.Instance.IsSpawned)
        {
            PartyState.Instance.BattleInProgress.Value = value;
        }
    }

    // More players, bigger waves: +50% enemies per extra player (bosses are never duplicated).
    static EnemyType[] ScaleWave(EnemyType[] wave, int players)
    {
        int extra = Mathf.RoundToInt(wave.Length * 0.5f * (players - 1));
        if (extra <= 0)
        {
            return wave;
        }
        System.Collections.Generic.List<EnemyType> pool = new System.Collections.Generic.List<EnemyType>();
        foreach (EnemyType t in wave)
        {
            if (t != EnemyType.Boss)
            {
                pool.Add(t);
            }
        }
        if (pool.Count == 0)
        {
            pool.Add(EnemyType.Slime);
        }
        System.Collections.Generic.List<EnemyType> scaled = new System.Collections.Generic.List<EnemyType>(wave);
        for (int i = 0; i < extra; i++)
        {
            scaled.Add(pool[i % pool.Count]);
        }
        return scaled.ToArray();
    }

    IEnumerator Run()
    {
        InProgress = true;
        SetSharedInProgress(true);
        EnemyType[][] waves = building.battle.waves;
        int players = Net.PlayerCount;

        Banner(building.name, 2f);
        yield return new WaitForSeconds(2f);

        for (int w = 0; w < waves.Length; w++)
        {
            bool last = w == waves.Length - 1;
            Banner(last && waves.Length > 1 ? "Final Wave!" : "Wave " + (w + 1) + " / " + waves.Length, 1.5f);
            yield return new WaitForSeconds(1f);

            foreach (EnemyType type in ScaleWave(waves[w], players))
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
            gm.playerCoins += reward; // the shared purse in co-op
            gm.MarkBattleCleared(building.id);
            if (Net.Active && PartyState.Instance != null)
            {
                PartyState.Instance.MarkCleared(building.id);
            }

            Toast((firstClear ? "First clear! " : "Cleared again! ") + "+" + reward + " coins\nThe doors are open.", 4f);
        }

        SetSharedInProgress(false);
        Banner("VICTORY!", 2.5f);

        if (onFinished != null)
        {
            onFinished();
        }
    }

    // Banners and toasts reach every player in co-op.
    static void Banner(string text, float duration)
    {
        if (Net.Active && PartyState.Instance != null)
        {
            PartyState.Instance.BannerRpc(text, duration);
        }
        else if (AreaHUD.Instance != null)
        {
            AreaHUD.Instance.Banner(text, duration);
        }
    }

    static void Toast(string text, float duration)
    {
        if (Net.Active && PartyState.Instance != null)
        {
            PartyState.Instance.ToastRpc(text, duration);
        }
        else if (AreaHUD.Instance != null)
        {
            AreaHUD.Instance.Toast(text, duration);
        }
    }

    // The red warning circle where an enemy is about to appear (also shown on guests).
    public static void ShowTelegraph(Vector3 position, float size)
    {
        GameObject marker = WorldKit.Prim(PrimitiveType.Cylinder, null, position + Vector3.up * 0.04f, Vector3.zero, new Color(1f, 0.2f, 0.15f), false);
        marker.name = "SpawnTelegraph";
        marker.AddComponent<TelegraphMarker>().size = size;
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

        float size = type == EnemyType.Boss ? 5f : type == EnemyType.Brute ? 2.8f : 1.8f;
        ShowTelegraph(position, size);
        if (Net.Active && PartyState.Instance != null)
        {
            PartyState.Instance.TelegraphRpc(position, size);
        }

        yield return new WaitForSeconds(TelegraphMarker.Duration);

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

            // Keep spawns away from every player, not just one.
            float distance = PlayerRegistry.Count > 0 ? PlayerRegistry.DistanceToNearest(candidate) : 100f;
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

// Grows a red circle on the floor for a moment, then removes itself.
public class TelegraphMarker : MonoBehaviour
{
    public const float Duration = 0.8f;
    public float size = 1.8f;
    private float age;

    void Update()
    {
        age += Time.deltaTime;
        float pulse = Mathf.Lerp(0.3f, 1f, Mathf.Clamp01(age / Duration)) * size;
        transform.localScale = new Vector3(pulse, 0.01f, pulse);
        if (age >= Duration)
        {
            Destroy(gameObject);
        }
    }
}

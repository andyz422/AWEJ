using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 15f;
    public int damage = 1;
    public float lifetime = 5f;
    public bool fromEnemy = false;
    public bool pierce = false;

    // A visual copy of a shot simulated elsewhere (co-op): it stops where it hits but deals no damage.
    public bool cosmetic = false;

    private Vector3 direction = Vector3.forward;
    private bool initialized = false;
    private readonly HashSet<Enemy> alreadyHit = new HashSet<Enemy>();

    private static GameObject prefab;

    public static Projectile Spawn(ShotData shot, bool cosmeticOnly)
    {
        if (prefab == null)
        {
            prefab = Resources.Load<GameObject>("Prefabs/Projectile");
            if (prefab == null)
            {
                return null;
            }
        }
        Vector3 direction = shot.direction.sqrMagnitude > 0.0001f ? shot.direction.normalized : Vector3.forward;
        GameObject obj = Instantiate(prefab, shot.origin, Quaternion.LookRotation(direction));
        Projectile projectile = obj.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.Initialize(direction, shot.speed, shot.damage);
            projectile.Configure(shot.fromEnemy, shot.pierce, shot.color, shot.scale);
            projectile.cosmetic = cosmeticOnly;
        }
        return projectile;
    }

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        if (!initialized)
        {
            return;
        }

        // Sweep along this frame's movement instead of relying on trigger events,
        // which don't fire reliably against CharacterControllers and miss fast shots.
        float distance = speed * Time.deltaTime;
        float radius = transform.localScale.x * 0.5f;
        RaycastHit[] hits = Physics.SphereCastAll(transform.position, radius, direction, distance, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (HandleHit(hit.collider))
            {
                Destroy(gameObject);
                return;
            }
        }

        transform.position += direction * distance;
    }

    public void Initialize(Vector3 dir, float spd, int dmg)
    {
        direction = dir.normalized;
        speed = spd;
        damage = dmg;
        initialized = true;
        transform.rotation = Quaternion.LookRotation(direction);
    }

    public void Configure(bool enemyOwned, bool piercing, Color color, float scale)
    {
        fromEnemy = enemyOwned;
        pierce = piercing;
        transform.localScale = Vector3.one * scale;

        Renderer projectileRenderer = GetComponent<Renderer>();
        if (projectileRenderer != null)
        {
            projectileRenderer.sharedMaterial = WorldKit.Colored(color);
        }
    }

    // Returns true when the projectile should stop here.
    bool HandleHit(Collider other)
    {
        Enemy enemy = other.GetComponentInParent<Enemy>();

        if (fromEnemy)
        {
            if (enemy != null)
            {
                return false;
            }

            if (other.CompareTag("Player"))
            {
                PlayerAvatar avatar = other.GetComponent<PlayerAvatar>();
                if (avatar != null && avatar.IsDowned)
                {
                    return false; // shots pass over downed players
                }
                if (!cosmetic && avatar != null)
                {
                    avatar.Hurt(damage);
                }
            }
            return true;
        }

        if (other.CompareTag("Player"))
        {
            return false;
        }

        if (enemy != null)
        {
            if (alreadyHit.Add(enemy) && !cosmetic)
            {
                enemy.TakeDamage(damage);
            }
            return !pierce;
        }

        return true;
    }
}

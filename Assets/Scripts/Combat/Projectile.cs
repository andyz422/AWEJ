using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    public int damage = 1;
    public float lifetime = 5f;
    public bool isPlayerProjectile = true;

    private Vector2 direction = Vector2.up;
    private bool initialized = false;

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

        transform.position += new Vector3(direction.x, direction.y, 0) * speed * Time.deltaTime;

        Vector3 screenPos = Camera.main.WorldToViewportPoint(transform.position);
        if (screenPos.x < -0.1f || screenPos.x > 1.1f || screenPos.y < -0.1f || screenPos.y > 1.1f)
        {
            Destroy(gameObject);
        }
    }

    public void Initialize(Vector2 dir, float spd, int dmg, bool isPlayer)
    {
        direction = dir.normalized;
        speed = spd;
        damage = dmg;
        isPlayerProjectile = isPlayer;
        initialized = true;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isPlayerProjectile)
        {
            EnemyController enemy = other.GetComponent<EnemyController>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                Destroy(gameObject);
            }
        }
        else
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
                Destroy(gameObject);
            }
        }

        if (other.gameObject.name.StartsWith("Wall"))
        {
            Destroy(gameObject);
        }
    }
}

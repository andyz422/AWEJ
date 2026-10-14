using UnityEngine;

// Damages the player while they stand inside the zone (lava, etc.).
// The shape is a circle around the transform when radius > 0, otherwise a world-space XZ rectangle.
public class HazardZone : MonoBehaviour
{
    public float radius = 0f;
    public Rect area;
    public Rect[] safeAreas = new Rect[0];
    public int damage = 1;

    private PlayerHealth playerHealth;

    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerHealth = player.GetComponent<PlayerHealth>();
        }
    }

    void Update()
    {
        if (playerHealth == null)
        {
            return;
        }

        Vector3 position = playerHealth.transform.position;
        if (Contains(new Vector2(position.x, position.z)))
        {
            // PlayerHealth's invincibility window keeps this to one hit per second.
            playerHealth.TakeDamage(damage);
        }
    }

    public bool Contains(Vector2 point)
    {
        foreach (Rect safe in safeAreas)
        {
            if (safe.Contains(point))
            {
                return false;
            }
        }

        if (radius > 0f)
        {
            Vector3 center = transform.position;
            return Vector2.Distance(point, new Vector2(center.x, center.z)) <= radius;
        }

        return area.Contains(point);
    }
}

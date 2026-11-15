using UnityEngine;

// Damages the player while they stand inside the zone (lava, etc.).
// The shape is a circle around the transform when radius > 0, otherwise a world-space XZ rectangle.
public class HazardZone : MonoBehaviour
{
    public float radius = 0f;
    public Rect area;
    public Rect[] safeAreas = new Rect[0];
    public int damage = 1;

    void Update()
    {
        // Burns every player standing in it; downed players are left alone.
        foreach (PlayerAvatar player in PlayerRegistry.All)
        {
            // In co-op each device burns only its own player (it owns that player's health).
            if (player == null || player.IsDowned || player.Health == null || (Net.Active && !player.IsLocal))
            {
                continue;
            }

            Vector3 position = player.transform.position;
            if (Contains(new Vector2(position.x, position.z)))
            {
                // PlayerHealth's invincibility window keeps this to one hit per second.
                player.Health.TakeDamage(damage);
            }
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

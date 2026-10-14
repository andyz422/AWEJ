using UnityEngine;

public class WeaponDef
{
    public string id;
    public string name;
    public string description;
    public int price;
    public int damage = 1;
    public float fireRate = 0.4f;
    public float projectileSpeed = 15f;
    public int projectileCount = 1;
    public float spreadAngle = 0f;
    public float projectileScale = 0.3f;
    public bool pierce = false;
    public Color color = Color.yellow;
}

public static class WeaponCatalog
{
    public const string StarterWeapon = "pea";

    public static readonly WeaponDef[] All =
    {
        new WeaponDef
        {
            id = "pea", name = "Pea Shooter", description = "Trusty starter blaster.",
            price = 0, damage = 1, fireRate = 0.4f, projectileSpeed = 15f,
            color = new Color(1f, 0.92f, 0.3f)
        },
        new WeaponDef
        {
            id = "twin", name = "Twin Blaster", description = "Fires two bolts side by side.",
            price = 40, damage = 1, fireRate = 0.35f, projectileSpeed = 16f,
            projectileCount = 2, spreadAngle = 8f, color = new Color(0.3f, 0.95f, 1f)
        },
        new WeaponDef
        {
            id = "repeater", name = "Rapid Repeater", description = "Hold fire to unleash a stream.",
            price = 90, damage = 1, fireRate = 0.12f, projectileSpeed = 21f,
            projectileScale = 0.22f, color = new Color(0.45f, 1f, 0.35f)
        },
        new WeaponDef
        {
            id = "scatter", name = "Scatter Cannon", description = "Five-shot spread for crowds.",
            price = 140, damage = 1, fireRate = 0.65f, projectileSpeed = 14f,
            projectileCount = 5, spreadAngle = 42f, color = new Color(1f, 0.6f, 0.15f)
        },
        new WeaponDef
        {
            id = "frost", name = "Frost Lance", description = "Piercing ice bolts.",
            price = 200, damage = 2, fireRate = 0.45f, projectileSpeed = 26f,
            projectileScale = 0.26f, pierce = true, color = new Color(0.7f, 0.9f, 1f)
        },
        new WeaponDef
        {
            id = "ember", name = "Ember Cannon", description = "Huge piercing fireballs.",
            price = 320, damage = 4, fireRate = 0.7f, projectileSpeed = 13f,
            projectileScale = 0.65f, pierce = true, color = new Color(1f, 0.3f, 0.1f)
        }
    };

    public static WeaponDef Get(string id)
    {
        foreach (WeaponDef def in All)
        {
            if (def.id == id)
            {
                return def;
            }
        }
        return null;
    }

    public static WeaponDef GetOrDefault(string id)
    {
        return Get(id) ?? All[0];
    }
}

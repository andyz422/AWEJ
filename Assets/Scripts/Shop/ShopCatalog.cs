using UnityEngine;

public class UpgradeDef
{
    public string id;
    public string name;
    public string description;
    public int basePrice;
    // 0 means a consumable that can be bought any number of times.
    public int maxLevel;
    public Color color;

    public int PriceForLevel(int level)
    {
        return maxLevel == 0 ? basePrice : basePrice * (level + 1);
    }
}

// Everything a shop pedestal can sell: weapons from WeaponCatalog plus upgrades.
public static class ShopCatalog
{
    public const float BootsSpeedPerLevel = 1.5f;
    public const float FireRateMultiplierPerLevel = 0.85f;

    public static readonly UpgradeDef[] Upgrades =
    {
        new UpgradeDef
        {
            id = "potion", name = "Healing Tonic", description = "Restores all health.",
            basePrice = 10, maxLevel = 0, color = new Color(0.3f, 1f, 0.45f)
        },
        new UpgradeDef
        {
            id = "hp", name = "Heart Container", description = "+1 max health.",
            basePrice = 30, maxLevel = 5, color = new Color(1f, 0.25f, 0.35f)
        },
        new UpgradeDef
        {
            id = "dmg", name = "Power Core", description = "+1 damage on every shot.",
            basePrice = 40, maxLevel = 5, color = new Color(1f, 0.45f, 0.1f)
        },
        new UpgradeDef
        {
            id = "rate", name = "Trigger Spring", description = "Fire 15% faster.",
            basePrice = 35, maxLevel = 4, color = new Color(1f, 0.95f, 0.3f)
        },
        new UpgradeDef
        {
            id = "boots", name = "Swift Boots", description = "Move faster.",
            basePrice = 25, maxLevel = 3, color = new Color(0.6f, 0.4f, 0.25f)
        }
    };

    public static UpgradeDef GetUpgrade(string id)
    {
        foreach (UpgradeDef def in Upgrades)
        {
            if (def.id == id)
            {
                return def;
            }
        }
        return null;
    }

    public static string GetItemName(string itemId)
    {
        WeaponDef weapon = WeaponCatalog.Get(itemId);
        if (weapon != null)
        {
            return weapon.name;
        }

        UpgradeDef upgrade = GetUpgrade(itemId);
        return upgrade != null ? upgrade.name : itemId;
    }

    public static Color GetItemColor(string itemId)
    {
        WeaponDef weapon = WeaponCatalog.Get(itemId);
        if (weapon != null)
        {
            return weapon.color;
        }

        UpgradeDef upgrade = GetUpgrade(itemId);
        return upgrade != null ? upgrade.color : Color.white;
    }

    // Short label floating over the pedestal.
    public static string GetLabel(string itemId)
    {
        return GetItemName(itemId) + "\n" + GetPriceText(itemId);
    }

    public static string GetPriceText(string itemId)
    {
        GameManager gm = GameManager.Instance;
        WeaponDef weapon = WeaponCatalog.Get(itemId);

        if (weapon != null)
        {
            if (gm != null && gm.equippedWeapon == itemId)
            {
                return "Equipped";
            }
            if (gm != null && gm.ownedWeapons.Contains(itemId))
            {
                return "Owned";
            }
            return weapon.price + " coins";
        }

        UpgradeDef upgrade = GetUpgrade(itemId);
        if (upgrade == null)
        {
            return "";
        }

        int level = gm != null ? gm.GetUpgradeLevel(itemId) : 0;
        if (upgrade.maxLevel > 0 && level >= upgrade.maxLevel)
        {
            return "Sold out";
        }

        string levelText = upgrade.maxLevel > 0 ? "  (Lv " + level + "/" + upgrade.maxLevel + ")" : "";
        return upgrade.PriceForLevel(level) + " coins" + levelText;
    }

    // Prompt shown at the bottom of the screen when standing at the pedestal.
    public static string GetPrompt(string itemId)
    {
        GameManager gm = GameManager.Instance;
        WeaponDef weapon = WeaponCatalog.Get(itemId);

        if (weapon != null)
        {
            if (gm != null && gm.equippedWeapon == itemId)
            {
                return weapon.name + " (equipped)\n" + weapon.description;
            }
            if (gm != null && gm.ownedWeapons.Contains(itemId))
            {
                return "[E] Equip " + weapon.name + "\n" + weapon.description;
            }
            return "[E] Buy " + weapon.name + " - " + weapon.price + " coins\n" + weapon.description;
        }

        UpgradeDef upgrade = GetUpgrade(itemId);
        if (upgrade == null)
        {
            return "";
        }

        int level = gm != null ? gm.GetUpgradeLevel(itemId) : 0;
        if (upgrade.maxLevel > 0 && level >= upgrade.maxLevel)
        {
            return upgrade.name + " - sold out\n" + upgrade.description;
        }
        return "[E] Buy " + upgrade.name + " - " + upgrade.PriceForLevel(level) + " coins\n" + upgrade.description;
    }

    // Attempts the purchase and returns a message describing what happened.
    public static string Purchase(string itemId)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            return "";
        }

        WeaponDef weapon = WeaponCatalog.Get(itemId);
        if (weapon != null)
        {
            return PurchaseWeapon(gm, weapon);
        }

        UpgradeDef upgrade = GetUpgrade(itemId);
        return upgrade != null ? PurchaseUpgrade(gm, upgrade) : "";
    }

    static string PurchaseWeapon(GameManager gm, WeaponDef weapon)
    {
        if (gm.equippedWeapon == weapon.id)
        {
            return weapon.name + " is already equipped.";
        }

        if (!gm.ownedWeapons.Contains(weapon.id))
        {
            if (gm.playerCoins < weapon.price)
            {
                return "Not enough coins! " + weapon.name + " costs " + weapon.price + ".";
            }

            gm.playerCoins -= weapon.price;
            gm.ownedWeapons.Add(weapon.id);
        }

        gm.equippedWeapon = weapon.id;
        RefreshPlayer();
        return "Equipped " + weapon.name + "!";
    }

    static string PurchaseUpgrade(GameManager gm, UpgradeDef upgrade)
    {
        int level = gm.GetUpgradeLevel(upgrade.id);

        if (upgrade.maxLevel > 0 && level >= upgrade.maxLevel)
        {
            return upgrade.name + " is sold out.";
        }

        if (upgrade.id == "potion" && gm.playerHealth >= gm.maxPlayerHealth)
        {
            return "You're already at full health.";
        }

        int price = upgrade.PriceForLevel(level);
        if (gm.playerCoins < price)
        {
            return "Not enough coins! " + upgrade.name + " costs " + price + ".";
        }

        gm.playerCoins -= price;

        switch (upgrade.id)
        {
            case "potion":
                gm.ResetHealth();
                break;
            case "hp":
                gm.maxPlayerHealth += 1;
                gm.playerHealth += 1;
                break;
        }

        if (upgrade.maxLevel > 0)
        {
            gm.SetUpgradeLevel(upgrade.id, level + 1);
        }

        RefreshPlayer();
        return "Bought " + upgrade.name + "!";
    }

    // Pushes the new loadout and stats onto the live player.
    public static void RefreshPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            return;
        }

        Weapon weapon = player.GetComponent<Weapon>();
        if (weapon != null)
        {
            weapon.RefreshStats();
        }

        ThirdPersonController controller = player.GetComponent<ThirdPersonController>();
        if (controller != null)
        {
            controller.RefreshStats();
        }

        PlayerHealth health = player.GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.SyncFromGameManager();
        }
    }
}

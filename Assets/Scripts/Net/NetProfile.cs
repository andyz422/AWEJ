using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

// A player's personal progress for the current co-op run (health, weapons, upgrades), small enough
// to send over the network. The host keeps one per device so a player who drops and rejoins gets
// their upgrades back.
public struct ProfileSnapshot : INetworkSerializable
{
    public FixedString64Bytes device;
    public int maxHealth;
    public int health;
    public FixedString32Bytes equipped;
    public FixedString128Bytes owned;    // "starter,twin"
    public FixedString128Bytes upgrades; // "dmg:2,rate:1"

    public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
    {
        s.SerializeValue(ref device);
        s.SerializeValue(ref maxHealth);
        s.SerializeValue(ref health);
        s.SerializeValue(ref equipped);
        s.SerializeValue(ref owned);
        s.SerializeValue(ref upgrades);
    }

    public string Signature
    {
        get { return maxHealth + "|" + health + "|" + equipped + "|" + owned + "|" + upgrades; }
    }
}

public static class NetProfile
{
    const string PrefsKey = "AWEJ.DeviceId";
    static string deviceId;

    // Stable per install. Tests can pass "-awejid <name>" so two copies on one PC differ.
    public static string DeviceId
    {
        get
        {
            if (!string.IsNullOrEmpty(deviceId))
            {
                return deviceId;
            }
            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-awejid")
                {
                    deviceId = args[i + 1];
                    return deviceId;
                }
            }
            deviceId = PlayerPrefs.GetString(PrefsKey, "");
            if (string.IsNullOrEmpty(deviceId))
            {
                deviceId = System.Guid.NewGuid().ToString("N");
                PlayerPrefs.SetString(PrefsKey, deviceId);
                PlayerPrefs.Save();
            }
            return deviceId;
        }
    }

    public static ProfileSnapshot Capture(GameManager gm)
    {
        List<string> upgrades = new List<string>();
        foreach (KeyValuePair<string, int> pair in gm.Me.upgrades)
        {
            upgrades.Add(pair.Key + ":" + pair.Value);
        }
        return new ProfileSnapshot
        {
            device = Clip(DeviceId, 60),
            maxHealth = gm.maxPlayerHealth,
            health = gm.playerHealth,
            equipped = Clip(gm.equippedWeapon, 28),
            owned = Clip(string.Join(",", gm.ownedWeapons), 120),
            upgrades = Clip(string.Join(",", upgrades), 120),
        };
    }

    public static void Apply(GameManager gm, ProfileSnapshot s)
    {
        PlayerProfile me = gm.Me;
        me.maxHealth = Mathf.Max(1, s.maxHealth);
        me.health = s.health > 0 ? Mathf.Min(s.health, me.maxHealth) : me.maxHealth;
        me.ownedWeapons.Clear();
        foreach (string id in s.owned.ToString().Split(','))
        {
            if (!string.IsNullOrEmpty(id))
            {
                me.ownedWeapons.Add(id);
            }
        }
        if (me.ownedWeapons.Count == 0)
        {
            me.ownedWeapons.Add(WeaponCatalog.StarterWeapon);
        }
        string equipped = s.equipped.ToString();
        me.equippedWeapon = me.ownedWeapons.Contains(equipped) ? equipped : me.ownedWeapons[0];
        me.upgrades.Clear();
        foreach (string entry in s.upgrades.ToString().Split(','))
        {
            string[] kv = entry.Split(':');
            int level;
            if (kv.Length == 2 && int.TryParse(kv[1], out level))
            {
                me.upgrades[kv[0]] = level;
            }
        }
    }

    static string Clip(string s, int max)
    {
        s = s ?? "";
        return s.Length > max ? s.Substring(0, max) : s;
    }
}

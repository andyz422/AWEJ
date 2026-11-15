using System.Collections.Generic;
using UnityEngine;

// Every player in the session. Use this instead of looking up the "Player" tag:
// Local is the player on this device (camera, UI, interactions); Nearest() is for things that
// should consider everyone, like enemies picking a target or coins flying to the closest player.
public static class PlayerRegistry
{
    static readonly List<PlayerAvatar> players = new List<PlayerAvatar>();

    public static IReadOnlyList<PlayerAvatar> All
    {
        get { return players; }
    }

    public static int Count
    {
        get { return players.Count; }
    }

    // The player on this device, or null before one has spawned.
    public static PlayerAvatar Local { get; private set; }

    public static Transform LocalTransform
    {
        get { return Local != null ? Local.transform : null; }
    }

    // Players who are up (not downed).
    public static int StandingCount
    {
        get
        {
            int n = 0;
            foreach (PlayerAvatar p in players)
            {
                if (p != null && !p.IsDowned)
                {
                    n++;
                }
            }
            return n;
        }
    }

    public static void Register(PlayerAvatar player)
    {
        if (!players.Contains(player))
        {
            players.Add(player);
        }
        if (player.IsLocal)
        {
            Local = player;
        }
    }

    public static void Unregister(PlayerAvatar player)
    {
        players.Remove(player);
        if (Local == player)
        {
            Local = null;
            foreach (PlayerAvatar p in players)
            {
                if (p != null && p.IsLocal)
                {
                    Local = p;
                    break;
                }
            }
        }
    }

    // Called when a player's local/remote status is decided after it registered (networked spawn).
    public static void Refresh(PlayerAvatar player)
    {
        if (player.IsLocal)
        {
            Local = player;
        }
        else if (Local == player || Local == null)
        {
            Local = null;
            foreach (PlayerAvatar p in players)
            {
                if (p != null && p.IsLocal)
                {
                    Local = p;
                    break;
                }
            }
        }
    }

    // Closest player to a point; by default skips downed players.
    public static PlayerAvatar Nearest(Vector3 position, bool includeDowned = false)
    {
        PlayerAvatar best = null;
        float bestDistance = float.MaxValue;
        foreach (PlayerAvatar p in players)
        {
            if (p == null || (!includeDowned && p.IsDowned))
            {
                continue;
            }
            Vector3 offset = p.transform.position - position;
            offset.y = 0f;
            float d = offset.sqrMagnitude;
            if (d < bestDistance)
            {
                bestDistance = d;
                best = p;
            }
        }
        return best;
    }

    // Distance from a point to the nearest player (any state), or float.MaxValue with nobody around.
    public static float DistanceToNearest(Vector3 position)
    {
        PlayerAvatar p = Nearest(position, true);
        if (p == null)
        {
            return float.MaxValue;
        }
        Vector3 offset = p.transform.position - position;
        offset.y = 0f;
        return offset.magnitude;
    }
}

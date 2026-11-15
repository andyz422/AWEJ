using Unity.Netcode;

// Quick answers about the co-op session. Solo play never starts networking, so Active is false
// and every system runs its original single-player path.
public static class Net
{
    // A co-op session is running (hosting or joined).
    public static bool Active
    {
        get { return NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening; }
    }

    // This device decides the shared world: enemies, battles, hits, coins and the purse.
    // True offline too, so single-player code can simply check IsAuthority.
    public static bool IsAuthority
    {
        get { return !Active || NetworkManager.Singleton.IsServer; }
    }

    public static bool IsHost
    {
        get { return Active && NetworkManager.Singleton.IsHost; }
    }

    public static ulong LocalClientId
    {
        get { return Active ? NetworkManager.Singleton.LocalClientId : 0; }
    }

    // Players in the session (1 offline).
    public static int PlayerCount
    {
        get
        {
            if (!Active)
            {
                return 1;
            }
            if (PartyState.Instance != null && PartyState.Instance.IsSpawned)
            {
                return UnityEngine.Mathf.Max(1, PartyState.Instance.PlayerCount.Value);
            }
            return UnityEngine.Mathf.Max(1, PlayerRegistry.Count);
        }
    }
}

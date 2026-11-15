using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

// Where the party is and how they got there. Sent as one value so every device rebuilds the
// same location together; serial changes once per trip.
public struct TravelState : INetworkSerializable, System.IEquatable<TravelState>
{
    public int serial;
    public int area;
    public int entry;
    public int loop;
    public bool restoreHealth;
    public FixedString64Bytes building;
    public FixedString64Bytes lastBuilding;
    public FixedString512Bytes message;
    public FixedString512Bytes cleared; // comma-separated cleared battle ids

    public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
    {
        s.SerializeValue(ref serial);
        s.SerializeValue(ref area);
        s.SerializeValue(ref entry);
        s.SerializeValue(ref loop);
        s.SerializeValue(ref restoreHealth);
        s.SerializeValue(ref building);
        s.SerializeValue(ref lastBuilding);
        s.SerializeValue(ref message);
        s.SerializeValue(ref cleared);
    }

    public bool Equals(TravelState o)
    {
        return serial == o.serial && area == o.area && entry == o.entry && loop == o.loop && restoreHealth == o.restoreHealth
            && building.Equals(o.building) && lastBuilding.Equals(o.lastBuilding) && message.Equals(o.message) && cleared.Equals(o.cleared);
    }
}

// A door/gate vote in progress. Everyone must agree before the party travels.
public struct VoteState : INetworkSerializable, System.IEquatable<VoteState>
{
    public int id;
    public bool active;
    public int yes;
    public int needed;
    public float endsAt; // server time
    public FixedString128Bytes title;

    public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
    {
        s.SerializeValue(ref id);
        s.SerializeValue(ref active);
        s.SerializeValue(ref yes);
        s.SerializeValue(ref needed);
        s.SerializeValue(ref endsAt);
        s.SerializeValue(ref title);
    }

    public bool Equals(VoteState o)
    {
        return id == o.id && active == o.active && yes == o.yes && needed == o.needed && endsAt.Equals(o.endsAt) && title.Equals(o.title);
    }
}

// A shot to show on other devices (or, from a guest, to simulate on the host).
public struct ShotData : INetworkSerializable
{
    public Vector3 origin;
    public Vector3 direction;
    public float speed;
    public int damage;
    public bool pierce;
    public bool fromEnemy;
    public Color color;
    public float scale;
    public ulong shooter;

    public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
    {
        s.SerializeValue(ref origin);
        s.SerializeValue(ref direction);
        s.SerializeValue(ref speed);
        s.SerializeValue(ref damage);
        s.SerializeValue(ref pierce);
        s.SerializeValue(ref fromEnemy);
        s.SerializeValue(ref color);
        s.SerializeValue(ref scale);
        s.SerializeValue(ref shooter);
    }
}

// The host's record of the shared game, mirrored to every guest.
public class PartyState : NetworkBehaviour
{
    public static PartyState Instance { get; private set; }

    public const float VoteSeconds = 20f;

    public readonly NetworkVariable<TravelState> Travel = new NetworkVariable<TravelState>();
    public readonly NetworkVariable<int> Coins = new NetworkVariable<int>();
    public readonly NetworkVariable<bool> BattleInProgress = new NetworkVariable<bool>();
    public readonly NetworkVariable<int> PlayerCount = new NetworkVariable<int>(1);
    public readonly NetworkVariable<VoteState> Vote = new NetworkVariable<VoteState>();

    private int appliedSerial = -1;

    // Server only: the trip waiting on the current vote, and who said yes.
    private TravelState pendingTrip;
    private readonly HashSet<ulong> yesVotes = new HashSet<ulong>();

    public override void OnNetworkSpawn()
    {
        Instance = this;
        Travel.OnValueChanged += OnTravelChanged;

        if (IsServer)
        {
            GameManager gm = GameManager.Instance;
            TravelState start = Capture(gm);
            start.serial = 1;
            start.entry = (int)AreaEntry.Start;
            Travel.Value = start;
            Coins.Value = gm != null ? gm.LocalCoins : 0;
            NetworkManager.OnClientConnectedCallback += OnClientsChanged;
            NetworkManager.OnClientDisconnectCallback += OnClientsChanged;
            OnClientsChanged(0);
        }

        // Guests (and the host) adopt the current trip; AreaManager builds once this is ready.
        ApplyToGameManager(Travel.Value);
        appliedSerial = Travel.Value.serial;
        if (AreaManager.Instance != null)
        {
            AreaManager.Instance.OnPartyReady();
        }
    }

    public override void OnNetworkDespawn()
    {
        Travel.OnValueChanged -= OnTravelChanged;
        if (IsServer && NetworkManager != null)
        {
            NetworkManager.OnClientConnectedCallback -= OnClientsChanged;
            NetworkManager.OnClientDisconnectCallback -= OnClientsChanged;
        }
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void OnClientsChanged(ulong clientId)
    {
        PlayerCount.Value = Mathf.Max(1, NetworkManager.ConnectedClientsIds.Count);
        if (Vote.Value.active)
        {
            // Someone left or joined mid-vote: everyone currently here must agree.
            yesVotes.RemoveWhere(id => !IsConnected(id));
            UpdateVote();
        }
    }

    bool IsConnected(ulong clientId)
    {
        foreach (ulong id in NetworkManager.ConnectedClientsIds)
        {
            if (id == clientId)
            {
                return true;
            }
        }
        return false;
    }

    // ---------------------------------------------------------------- travel

    static TravelState Capture(GameManager gm)
    {
        TravelState t = new TravelState();
        if (gm == null)
        {
            return t;
        }
        t.area = gm.currentArea;
        t.entry = (int)gm.pendingEntry;
        t.loop = gm.loopCount;
        t.building = gm.currentBuilding ?? "";
        t.lastBuilding = gm.lastBuilding ?? "";
        t.message = Truncate(gm.pendingMessage, 500);
        t.cleared = Truncate(string.Join(",", gm.clearedBattles), 500);
        return t;
    }

    static string Truncate(string s, int max)
    {
        s = s ?? "";
        return s.Length > max ? s.Substring(0, max) : s;
    }

    static void ApplyToGameManager(TravelState t)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            return;
        }
        gm.currentArea = t.area;
        gm.currentBuilding = t.building.ToString();
        gm.lastBuilding = t.lastBuilding.ToString();
        gm.pendingEntry = (AreaEntry)t.entry;
        gm.pendingMessage = t.message.ToString();
        gm.loopCount = t.loop;
        gm.clearedBattles.Clear();
        foreach (string id in t.cleared.ToString().Split(','))
        {
            if (!string.IsNullOrEmpty(id))
            {
                gm.clearedBattles.Add(id);
            }
        }
    }

    void OnTravelChanged(TravelState previous, TravelState current)
    {
        ApplyToGameManager(current);
        if (current.serial != appliedSerial)
        {
            appliedSerial = current.serial;
            if (current.restoreHealth && GameManager.Instance != null)
            {
                GameManager.Instance.ResetHealth();
            }
            if (AreaManager.Instance != null)
            {
                AreaManager.Instance.OnTravelSynced();
            }
        }
    }

    // A player used a door or gate. Solo sessions go straight away; otherwise the party votes.
    public void RequestTravel(int area, string building, AreaEntry entry, string fromBuilding, string message, string title, int loop)
    {
        RequestTravelRpc(area, building ?? "", (int)entry, fromBuilding ?? "", Truncate(message, 500), Truncate(title, 120), loop);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void RequestTravelRpc(int area, FixedString64Bytes building, int entry, FixedString64Bytes fromBuilding, FixedString512Bytes message,
        FixedString128Bytes title, int loop, RpcParams rpcParams = default)
    {
        TravelState trip = Capture(GameManager.Instance);
        trip.area = area;
        trip.building = building;
        trip.entry = entry;
        trip.lastBuilding = fromBuilding;
        trip.message = message;
        trip.loop = loop;
        trip.restoreHealth = false;

        ulong voter = rpcParams.Receive.SenderClientId;
        if (Vote.Value.active)
        {
            // Using the same door again counts as a yes.
            CastVote(voter, true);
            return;
        }

        if (PlayerCount.Value <= 1)
        {
            BeginTravel(trip);
            return;
        }

        pendingTrip = trip;
        yesVotes.Clear();
        yesVotes.Add(voter);
        VoteState vote = new VoteState
        {
            id = Vote.Value.id + 1,
            active = true,
            needed = PlayerCount.Value,
            endsAt = (float)NetworkManager.ServerTime.Time + VoteSeconds,
            title = PlayerLabel(voter) + ": " + title.ToString()
        };
        Vote.Value = vote;
        UpdateVote();
    }

    // Server: travel now, no vote (defeat respawns use this).
    public void BeginTravel(TravelState trip)
    {
        if (!IsServer)
        {
            return;
        }
        trip.serial = Travel.Value.serial + 1;
        trip.cleared = Travel.Value.cleared;
        BattleInProgress.Value = false;
        CancelVote(null);
        Travel.Value = trip;
    }

    public void BeginTravel(int area, string building, AreaEntry entry, string fromBuilding, string message, bool restoreHealth)
    {
        TravelState trip = Capture(GameManager.Instance);
        trip.area = area;
        trip.building = building ?? "";
        trip.entry = (int)entry;
        trip.lastBuilding = fromBuilding ?? "";
        trip.message = Truncate(message, 500);
        trip.loop = Travel.Value.loop;
        trip.restoreHealth = restoreHealth;
        BeginTravel(trip);
    }

    // Server: record a cleared battle without moving anyone.
    public void MarkCleared(string buildingId)
    {
        if (!IsServer)
        {
            return;
        }
        TravelState t = Travel.Value;
        string cleared = t.cleared.ToString();
        foreach (string id in cleared.Split(','))
        {
            if (id == buildingId)
            {
                return;
            }
        }
        t.cleared = Truncate(cleared.Length > 0 ? cleared + "," + buildingId : buildingId, 500);
        Travel.Value = t;
    }

    // ---------------------------------------------------------------- votes

    public void SendVote(bool yes)
    {
        CastVoteRpc(Vote.Value.id, yes);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void CastVoteRpc(int voteId, bool yes, RpcParams rpcParams = default)
    {
        if (Vote.Value.active && Vote.Value.id == voteId)
        {
            CastVote(rpcParams.Receive.SenderClientId, yes);
        }
    }

    void CastVote(ulong voter, bool yes)
    {
        if (!yes)
        {
            CancelVote(PlayerLabel(voter) + " voted no. Staying here.");
            return;
        }
        yesVotes.Add(voter);
        UpdateVote();
    }

    void UpdateVote()
    {
        VoteState v = Vote.Value;
        if (!v.active)
        {
            return;
        }
        v.needed = PlayerCount.Value;
        v.yes = yesVotes.Count;
        Vote.Value = v;
        if (v.yes >= v.needed)
        {
            TravelState trip = pendingTrip;
            CancelVote(null);
            BeginTravel(trip);
        }
    }

    void CancelVote(string reason)
    {
        if (!Vote.Value.active)
        {
            return;
        }
        VoteState v = Vote.Value;
        v.active = false;
        Vote.Value = v;
        yesVotes.Clear();
        if (!string.IsNullOrEmpty(reason))
        {
            ToastRpc(reason, 2.5f);
        }
    }

    private bool wipeHandled;

    void Update()
    {
        if (!IsServer)
        {
            return;
        }
        if (Vote.Value.active && NetworkManager.ServerTime.Time > Vote.Value.endsAt)
        {
            CancelVote("Not everyone agreed in time. Staying here.");
        }

        // Everyone is down: the host sends the party somewhere safe (once per wipe).
        bool wiped = PlayerRegistry.Count > 0 && PlayerRegistry.StandingCount == 0;
        if (wiped && !wipeHandled && AreaManager.Instance != null)
        {
            wipeHandled = true;
            AreaManager.Instance.HandlePartyDefeated();
        }
        else if (!wiped)
        {
            wipeHandled = false;
        }
    }

    string PlayerLabel(ulong clientId)
    {
        foreach (PlayerAvatar p in PlayerRegistry.All)
        {
            NetPlayer np = p != null ? p.GetComponent<NetPlayer>() : null;
            if (np != null && np.OwnerClientId == clientId)
            {
                return np.DisplayName;
            }
        }
        return "Player " + (clientId + 1);
    }

    // ---------------------------------------------------------------- per-device profiles (rejoin)

    // Server only: each device's upgrades for this run, so a dropped player gets them back on rejoin.
    private readonly Dictionary<string, ProfileSnapshot> savedProfiles = new Dictionary<string, ProfileSnapshot>();

    public bool TryGetProfile(string device, out ProfileSnapshot snapshot)
    {
        return savedProfiles.TryGetValue(device ?? "", out snapshot);
    }

    public void SaveProfile(ProfileSnapshot snapshot)
    {
        string device = snapshot.device.ToString();
        if (!string.IsNullOrEmpty(device))
        {
            savedProfiles[device] = snapshot;
        }
    }

    // ---------------------------------------------------------------- shared purse

    public void RequestPurchase(string itemId, int price)
    {
        RequestPurchaseRpc(itemId, price);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void RequestPurchaseRpc(FixedString64Bytes itemId, int price, RpcParams rpcParams = default)
    {
        bool ok = price >= 0 && Coins.Value >= price;
        if (ok)
        {
            Coins.Value -= price;
        }
        PurchaseResultRpc(itemId, ok, price, RpcTarget.Single(rpcParams.Receive.SenderClientId, RpcTargetUse.Temp));
    }

    [Rpc(SendTo.SpecifiedInParams)]
    void PurchaseResultRpc(FixedString64Bytes itemId, bool ok, int price, RpcParams rpcParams)
    {
        ShopCatalog.CompletePurchase(itemId.ToString(), ok, price);
    }

    // ---------------------------------------------------------------- shared effects

    // A guest fired: simulate the real shot on the host and show it to everyone else.
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void PlayerShotRpc(ShotData shot, RpcParams rpcParams = default)
    {
        shot.shooter = rpcParams.Receive.SenderClientId;
        Projectile.Spawn(shot, false);
        ShotRpc(shot);
    }

    // Copies of a shot for guests (the shooter already shows their own).
    [Rpc(SendTo.NotServer)]
    public void ShotRpc(ShotData shot)
    {
        if (!shot.fromEnemy && shot.shooter == NetworkManager.LocalClientId)
        {
            return;
        }
        Projectile.Spawn(shot, true);
    }

    [Rpc(SendTo.NotServer)]
    public void TelegraphRpc(Vector3 position, float size)
    {
        BattleArena.ShowTelegraph(position, size);
    }

    [Rpc(SendTo.NotServer)]
    public void DeathFxRpc(Vector3 center, Color color, int pieces, float pieceSize, int coins)
    {
        Debris.Burst(center, color, pieces, pieceSize);
        for (int i = 0; i < coins; i++)
        {
            CoinPickup.Spawn(center, 0, true);
        }
    }

    [Rpc(SendTo.Everyone)]
    public void BannerRpc(FixedString128Bytes text, float duration)
    {
        if (AreaHUD.Instance != null)
        {
            AreaHUD.Instance.Banner(text.ToString(), duration);
        }
    }

    [Rpc(SendTo.Everyone)]
    public void ToastRpc(FixedString512Bytes text, float duration)
    {
        if (AreaHUD.Instance != null)
        {
            AreaHUD.Instance.Toast(text.ToString(), duration);
        }
    }
}

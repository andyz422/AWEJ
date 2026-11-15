using UnityEngine;

// Identifies a player in the world and registers it with PlayerRegistry.
// Solo: the scene's player is local and never downed (running out of health is a defeat).
// Co-op: NetPlayer on the same object says who owns it and whether it is downed.
public class PlayerAvatar : MonoBehaviour
{
    private NetPlayer net;

    // True when this player is controlled on this device. A networked player only counts once it has
    // spawned and we own it (before that, every new player would briefly look local).
    public bool IsLocal
    {
        get { return net == null || (net.IsSpawned && net.IsOwner); }
    }

    // Downed players can't act, enemies ignore them, and a teammate can revive them.
    public bool IsDowned
    {
        get { return net != null && net.IsSpawned && net.Downed.Value; }
    }

    public NetPlayer Net
    {
        get { return net; }
    }

    public PlayerHealth Health { get; private set; }

    void Awake()
    {
        Health = GetComponent<PlayerHealth>();
        net = GetComponent<NetPlayer>();
    }

    void OnEnable()
    {
        PlayerRegistry.Register(this);
    }

    void OnDisable()
    {
        PlayerRegistry.Unregister(this);
    }

    // Damage decided by whoever runs the world (the host in co-op). The owner's device applies it.
    public void Hurt(int amount)
    {
        if (IsDowned)
        {
            return;
        }
        if (IsLocal)
        {
            if (Health != null)
            {
                Health.TakeDamage(amount);
            }
        }
        else if (net != null && net.IsSpawned)
        {
            net.HurtRpc(amount);
        }
    }
}

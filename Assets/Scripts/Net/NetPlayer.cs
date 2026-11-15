using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

// The networked half of a co-op player. Each device owns its own player: it moves it, tracks its
// health and decides when it goes down. Everyone else sees a mirror of it.
public class NetPlayer : NetworkBehaviour
{
    // How long a teammate has to hold Use next to a downed player to get them back up.
    public const float ReviveSeconds = 4f;
    public const float ReviveRange = 2.4f;

    public readonly NetworkVariable<FixedString32Bytes> Character = new NetworkVariable<FixedString32Bytes>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public readonly NetworkVariable<int> Health = new NetworkVariable<int>(
        PlayerProfile.StartHealth, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public readonly NetworkVariable<int> MaxHealth = new NetworkVariable<int>(
        PlayerProfile.StartHealth, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public readonly NetworkVariable<bool> Downed = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private PlayerAvatar avatar;
    private PlayerHealth playerHealth;

    public string CharacterId
    {
        get
        {
            string id = Character.Value.ToString();
            return string.IsNullOrEmpty(id) ? PlayerCharacters.DefaultId : id;
        }
    }

    // The character's name, numbered if two players picked the same one.
    public string DisplayName
    {
        get
        {
            string name = PlayerCharacters.Get(CharacterId).name;
            int same = 0;
            int myIndex = 0;
            foreach (PlayerAvatar p in PlayerRegistry.All)
            {
                NetPlayer other = p != null ? p.GetComponent<NetPlayer>() : null;
                if (other == null || other.CharacterId != CharacterId)
                {
                    continue;
                }
                same++;
                if (other.OwnerClientId < OwnerClientId)
                {
                    myIndex++;
                }
            }
            return same > 1 ? name + " " + (myIndex + 1) : name;
        }
    }

    public override void OnNetworkSpawn()
    {
        avatar = GetComponent<PlayerAvatar>();
        playerHealth = GetComponent<PlayerHealth>();
        PlayerRegistry.Register(avatar);
        PlayerRegistry.Refresh(avatar);

        Character.OnValueChanged += OnCharacterChanged;
        Downed.OnValueChanged += OnDownedChanged;

        if (IsOwner)
        {
            GameManager gm = GameManager.Instance;
            if (gm != null)
            {
                Character.Value = PlayerCharacters.Get(gm.playerCharacter).id;
                Health.Value = gm.playerHealth;
                MaxHealth.Value = gm.maxPlayerHealth;
            }
            Downed.Value = false;
            if (AreaManager.Instance != null)
            {
                AreaManager.Instance.PlaceLocalPlayer();
            }
        }
        else
        {
            // Someone else's player: no local input, weapon or health logic; their device drives it.
            SetEnabled<ThirdPersonController>(false);
            SetEnabled<Weapon>(false);
            SetEnabled<PlayerHealth>(false);
        }

        ApplyCharacter();
    }

    public override void OnNetworkDespawn()
    {
        Character.OnValueChanged -= OnCharacterChanged;
        Downed.OnValueChanged -= OnDownedChanged;
        PlayerRegistry.Unregister(avatar);
    }

    void SetEnabled<T>(bool enabled) where T : Behaviour
    {
        T component = GetComponent<T>();
        if (component != null)
        {
            component.enabled = enabled;
        }
    }

    void OnCharacterChanged(FixedString32Bytes previous, FixedString32Bytes current)
    {
        ApplyCharacter();
    }

    void ApplyCharacter()
    {
        PlayerCharacters.ApplyTo(gameObject, CharacterId);
    }

    void OnDownedChanged(bool previous, bool current)
    {
        if (IsOwner && AreaHUD.Instance != null)
        {
            AreaHUD.Instance.Toast(current ? "You're down! Hang on - a teammate can revive you." : "Back on your feet!", 3f);
        }
    }

    void Update()
    {
        if (!IsSpawned || !IsOwner)
        {
            return;
        }
        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            return;
        }
        if (Health.Value != gm.playerHealth)
        {
            Health.Value = gm.playerHealth;
        }
        if (MaxHealth.Value != gm.maxPlayerHealth)
        {
            MaxHealth.Value = gm.maxPlayerHealth;
        }
    }

    // The host decided this player was hit; the owner applies it (invincibility frames included).
    [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Everyone)]
    public void HurtRpc(int amount)
    {
        if (playerHealth != null && !Downed.Value)
        {
            playerHealth.TakeDamage(amount);
        }
    }

    // Owner: health ran out in co-op.
    public void GoDown()
    {
        if (IsOwner && !Downed.Value)
        {
            Downed.Value = true;
        }
    }

    // A teammate finished reviving this player.
    [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Everyone)]
    public void ReviveRpc()
    {
        if (!Downed.Value)
        {
            return;
        }
        GameManager gm = GameManager.Instance;
        if (gm != null)
        {
            gm.playerHealth = Mathf.Max(1, Mathf.CeilToInt(gm.maxPlayerHealth * 0.5f));
        }
        if (playerHealth != null)
        {
            playerHealth.SyncFromGameManager();
            playerHealth.GrantInvincibility(2f);
        }
        Downed.Value = false;
    }

    // Owner: the party moved on after a wipe; everyone is back up at full health.
    public void ClearDowned()
    {
        if (IsOwner && Downed.Value)
        {
            Downed.Value = false;
        }
    }
}

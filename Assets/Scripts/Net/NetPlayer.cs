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
    // 0..1 while a teammate is reviving this player; shown to everyone as a ring over the player.
    public readonly NetworkVariable<float> ReviveProgress = new NetworkVariable<float>(0f);

    private PlayerAvatar avatar;
    private PlayerHealth playerHealth;
    private ReviveRing ring;
    private float lastReviveReport;

    // Rejoin: the owner waits for the host's answer before backing up its profile, so a fresh
    // (just-rejoined) profile never overwrites the saved one.
    private bool profileSynced;
    private string lastProfileSent = "";
    private float nextProfileCheck;

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
            if (gm != null)
            {
                // "It's me again": the host restores my upgrades if I was here earlier in this run.
                ProfileHelloRpc(NetProfile.Capture(gm));
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

        ring = ReviveRing.Create(transform, this);
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
        if (!IsSpawned)
        {
            return;
        }

        // Host: the ring empties when nobody is reviving any more (or the player is back up).
        if (IsServer && ReviveProgress.Value > 0f && (!Downed.Value || Time.time - lastReviveReport > 0.4f))
        {
            ReviveProgress.Value = 0f;
        }

        if (!IsOwner)
        {
            return;
        }
        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            return;
        }

        // Back up my profile to the host whenever it changes (after the host has answered my hello).
        if (profileSynced && Time.time >= nextProfileCheck)
        {
            nextProfileCheck = Time.time + 0.5f;
            ProfileSnapshot snapshot = NetProfile.Capture(gm);
            string signature = snapshot.Signature;
            if (signature != lastProfileSent)
            {
                lastProfileSent = signature;
                SaveProfileRpc(snapshot);
            }
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

    // ---------------------------------------------------------------- revive ring

    // A teammate's device reports how far along their revive is (0 when they stop).
    public void ReportReviveProgress(float progress)
    {
        ReportReviveRpc(Mathf.Clamp01(progress));
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void ReportReviveRpc(float progress)
    {
        lastReviveReport = Time.time;
        ReviveProgress.Value = Downed.Value ? progress : 0f;
    }

    // ---------------------------------------------------------------- rejoin: keep upgrades

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    void ProfileHelloRpc(ProfileSnapshot current)
    {
        ProfileSnapshot saved = current;
        bool known = PartyState.Instance != null && PartyState.Instance.TryGetProfile(current.device.ToString(), out saved);
        if (!known)
        {
            if (PartyState.Instance != null)
            {
                PartyState.Instance.SaveProfile(current);
            }
            saved = current;
        }
        ProfileReplyRpc(known, saved);
    }

    [Rpc(SendTo.Owner)]
    void ProfileReplyRpc(bool restored, ProfileSnapshot saved)
    {
        GameManager gm = GameManager.Instance;
        if (restored && gm != null)
        {
            NetProfile.Apply(gm, saved);
            ShopCatalog.RefreshPlayer();
            if (AreaHUD.Instance != null)
            {
                AreaHUD.Instance.Toast("Welcome back! Your upgrades are just as you left them.", 3f);
            }
            Debug.Log("[NetPlayer] Restored profile on rejoin (max hp " + gm.maxPlayerHealth + ", weapon " + gm.equippedWeapon + ")");
        }
        lastProfileSent = gm != null ? NetProfile.Capture(gm).Signature : "";
        profileSynced = true;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    void SaveProfileRpc(ProfileSnapshot snapshot)
    {
        if (PartyState.Instance != null)
        {
            PartyState.Instance.SaveProfile(snapshot);
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

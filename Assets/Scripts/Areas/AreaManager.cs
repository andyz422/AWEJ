using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Builds whatever location GameManager says the player is in (an area outdoors, or a building interior),
// places the player, and handles doors, gates, travel and defeat. Travel reloads the scene with new state.
public class AreaManager : MonoBehaviour
{
    public static AreaManager Instance { get; private set; }

    const float GateSpawnDistance = 4.5f;

    public AreaDef Area { get; private set; }
    public BuildingDef Building { get; private set; }

    private GameManager gm;
    private Transform world;
    private AreaHUD hud;
    private BattleArena arena;
    private InputSystem_Actions inputActions;
    private InputAction interactAction;
    private bool travelling;
    private int shopkeeperLine;

    // Co-op
    private bool waitingForParty;
    private GameObject exitBars;
    private float reviveProgress;
    private PlayerAvatar reviveTarget;

    // Where this location puts arriving players (co-op spreads them out around it).
    public Vector3 SpawnPoint { get; private set; }
    public float SpawnYaw { get; private set; }

    void Awake()
    {
        Instance = this;

        if (GameManager.Instance == null)
        {
            new GameObject("GameManager").AddComponent<GameManager>();
        }
        gm = GameManager.Instance;

        if (SceneTransition.Instance == null)
        {
            SceneTransition transition = new GameObject("SceneTransition").AddComponent<SceneTransition>();
            transition.transitionDuration = 0.45f;
        }

        if (Net.Active)
        {
            // Co-op uses networked players; the scene's solo player isn't needed.
            RemoveScenePlayers();
            if (PartyState.Instance == null || !PartyState.Instance.IsSpawned)
            {
                // Build once the host's shared state arrives (PartyState calls OnPartyReady).
                waitingForParty = true;
                return;
            }
        }

        Build();
    }

    void Build()
    {
        gm.currentArea = Mathf.Clamp(gm.currentArea, 0, AreaDatabase.Count - 1);
        Area = AreaDatabase.Get(gm.currentArea);
        Building = Area.FindBuilding(gm.currentBuilding);
        if (Building == null)
        {
            gm.currentBuilding = "";
        }

        world = new GameObject("AreaWorld").transform;
        if (hud == null)
        {
            hud = AreaHUD.Create(Area.name, "");
        }
        else
        {
            hud.SetTitle(Area.name, "");
        }

        Vector3 spawn;
        float yaw;
        if (Building == null)
        {
            BuildOutdoors(out spawn, out yaw);
        }
        else
        {
            BuildInterior(out spawn, out yaw);
        }

        PlacePlayer(spawn, yaw);
        ApplyAtmosphere(Building != null);
        ShowArrivalMessage();
    }

    static void RemoveScenePlayers()
    {
        foreach (PlayerAvatar p in FindObjectsByType<PlayerAvatar>(FindObjectsSortMode.None))
        {
            if (p.GetComponent<Unity.Netcode.NetworkObject>() == null)
            {
                p.gameObject.SetActive(false);
                Destroy(p.gameObject);
            }
        }
    }

    // ---------------------------------------------------------------- co-op travel

    // PartyState has arrived on this device.
    public void OnPartyReady()
    {
        if (waitingForParty)
        {
            waitingForParty = false;
            Build();
        }
    }

    // The host moved the party: rebuild the new location in place, behind a fade.
    public void OnTravelSynced()
    {
        if (waitingForParty)
        {
            OnPartyReady();
            return;
        }
        StartCoroutine(RebuildRoutine());
    }

    System.Collections.IEnumerator RebuildRoutine()
    {
        travelling = true;
        gm.isTransitioning = true;
        if (hud != null)
        {
            hud.SetPrompt(null);
        }
        if (SceneTransition.Instance != null)
        {
            yield return SceneTransition.Instance.FadeTo(1f);
        }

        ClearWorld();

        // Nobody stays down across a trip.
        PlayerAvatar local = PlayerRegistry.Local;
        if (local != null && local.Net != null)
        {
            gm.playerHealth = Mathf.Max(1, gm.playerHealth);
            local.Net.ClearDowned();
            if (local.Health != null)
            {
                local.Health.SyncFromGameManager();
            }
        }

        Build();
        yield return null;

        if (SceneTransition.Instance != null)
        {
            yield return SceneTransition.Instance.FadeTo(0f);
        }
        gm.isTransitioning = false;
        travelling = false;
    }

    void ClearWorld()
    {
        if (world != null)
        {
            world.gameObject.SetActive(false);
            Destroy(world.gameObject);
            world = null;
        }
        arena = null;
        exitBars = null;
        reviveTarget = null;
        reviveProgress = 0f;

        foreach (Enemy enemy in new List<Enemy>(Enemy.Alive))
        {
            Unity.Netcode.NetworkObject no = enemy.GetComponent<Unity.Netcode.NetworkObject>();
            if (no != null && no.IsSpawned)
            {
                if (Net.IsAuthority)
                {
                    no.Despawn(true);
                }
            }
            else
            {
                Destroy(enemy.gameObject);
            }
        }
        foreach (CoinPickup coin in FindObjectsByType<CoinPickup>(FindObjectsSortMode.None))
        {
            Destroy(coin.gameObject);
        }
        foreach (Projectile shot in FindObjectsByType<Projectile>(FindObjectsSortMode.None))
        {
            Destroy(shot.gameObject);
        }
        foreach (TelegraphMarker marker in FindObjectsByType<TelegraphMarker>(FindObjectsSortMode.None))
        {
            Destroy(marker.gameObject);
        }
    }

    // A short description of a trip for the vote ("enter the General Store").
    string DescribeTrip(int areaIndex, string buildingId, AreaEntry entry)
    {
        if (!string.IsNullOrEmpty(buildingId))
        {
            BuildingDef b = AreaDatabase.Get(areaIndex).FindBuilding(buildingId);
            return "Enter " + (b != null ? b.name : buildingId) + "?";
        }
        if (Building != null && areaIndex == gm.currentArea)
        {
            return "Leave " + Building.name + "?";
        }
        return "Travel to " + AreaDatabase.Get(areaIndex).name + "?";
    }

    void OnEnable()
    {
        inputActions = new InputSystem_Actions();
        inputActions.Enable();
        interactAction = inputActions.Player.Interact;
    }

    void OnDisable()
    {
        if (inputActions != null)
        {
            inputActions.Disable();
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // ---------------------------------------------------------------- building the location

    public bool ExitUnlocked
    {
        get { return Area.ClearedBattleCount(gm) >= Area.BattleCount; }
    }

    // Tougher per area and per loop, and +60% per extra co-op player.
    float EnemyHealthScale
    {
        get { return (1f + 0.25f * gm.currentArea + 0.6f * gm.loopCount) * (1f + 0.6f * (Net.PlayerCount - 1)); }
    }

    // Is a battle running here? Guests ask the host's shared state.
    bool BattleInProgress
    {
        get
        {
            if (Net.Active && !Net.IsAuthority)
            {
                return PartyState.Instance != null && PartyState.Instance.BattleInProgress.Value;
            }
            return arena != null && arena.InProgress;
        }
    }

    void BuildOutdoors(out Vector3 spawn, out float yaw)
    {
        AreaBuilder.BuildGround(world, Area.size, Area.groundMaterial, Area.groundColor);
        AreaBuilder.BuildWalls(world, Area.size, Area.wallHeight, Area.wallMaterial, Area.wallColor);
        AreaBuilder.BuildPaths(world, Area.paths, Area.pathMaterial, Area.pathColor);
        AreaBuilder.BuildHazards(world, Area.hazards);

        BuildExitGate();
        if (gm.currentArea > 0)
        {
            BuildEntryGate();
        }

        foreach (BuildingDef building in Area.buildings)
        {
            BuildingDef b = building;
            Interactable door = AreaBuilder.BuildBuilding(world, b, gm.IsBattleCleared(b.id));
            door.prompt = () => BuildingPrompt(b);
            door.onInteract = () => Travel(gm.currentArea, b.id, AreaEntry.IntoBuilding, "", "");
        }

        new AreaDecor(Area, world).Build();

        int cleared = Area.ClearedBattleCount(gm);
        hud.SetTitle(Area.name, "Battles won " + cleared + "/" + Area.BattleCount + "   -   Gate " + (ExitUnlocked ? "open" : "sealed"));

        FindOutdoorSpawn(out spawn, out yaw);
    }

    string BuildingPrompt(BuildingDef b)
    {
        if (b.type == BuildingType.Shop)
        {
            return "[E] Enter " + b.name + "\nShop";
        }

        if (gm.IsBattleCleared(b.id))
        {
            return "[E] Enter " + b.name + "\nRematch for " + b.battle.repeatReward + " coins";
        }
        return "[E] Enter " + b.name + "\nBattle: " + b.battle.waves.Length + " waves, " + b.battle.reward + " coin reward";
    }

    void BuildExitGate()
    {
        int nextIndex = (gm.currentArea + 1) % AreaDatabase.Count;
        string nextName = AreaDatabase.Get(nextIndex).name;
        bool unlocked = ExitUnlocked;
        bool loopsHome = nextIndex == 0;

        string title = unlocked ? (loopsHome ? "Road Home\n" : "To ") + nextName : "SEALED";
        Interactable gate = AreaBuilder.BuildGate(world, Area.WallPoint(Area.exitSide, Area.exitOffset), AreaDef.InwardYaw(Area.exitSide), title, !unlocked, Area.exitSide == WallSide.South);

        gate.prompt = () => unlocked
            ? "[E] Travel to " + nextName
            : "The gate is sealed.\nWin every battle in " + Area.name + " (" + Area.ClearedBattleCount(gm) + "/" + Area.BattleCount + ")";
        gate.onInteract = () =>
        {
            if (unlocked)
            {
                TravelToNextArea();
            }
            else
            {
                hud.Toast("Sealed! Defeat the challengers in this area first.", 2.5f);
            }
        };
    }

    void BuildEntryGate()
    {
        string previousName = AreaDatabase.Get(gm.currentArea - 1).name;
        Interactable gate = AreaBuilder.BuildGate(world, Area.WallPoint(Area.entrySide, Area.entryOffset), AreaDef.InwardYaw(Area.entrySide), "Back to\n" + previousName, false, Area.entrySide == WallSide.South);
        gate.prompt = () => "[E] Return to " + previousName;
        gate.onInteract = () => Travel(gm.currentArea - 1, "", AreaEntry.FromNextArea, "", "");
    }

    void FindOutdoorSpawn(out Vector3 spawn, out float yaw)
    {
        switch (gm.pendingEntry)
        {
            case AreaEntry.FromPreviousArea:
                if (gm.currentArea > 0)
                {
                    GateSpawn(Area.entrySide, Area.entryOffset, out spawn, out yaw);
                    return;
                }
                break;
            case AreaEntry.FromNextArea:
                GateSpawn(Area.exitSide, Area.exitOffset, out spawn, out yaw);
                return;
            case AreaEntry.FromBuilding:
                BuildingDef building = Area.FindBuilding(gm.lastBuilding);
                if (building != null)
                {
                    spawn = building.ExitSpot;
                    yaw = YawOf(building.Front);
                    return;
                }
                break;
            case AreaEntry.Respawn:
                if (gm.currentArea > 0)
                {
                    GateSpawn(Area.entrySide, Area.entryOffset, out spawn, out yaw);
                    return;
                }
                break;
        }

        spawn = WorldKit.Flat(Area.startPosition);
        yaw = 0f;
    }

    void GateSpawn(WallSide side, float offset, out Vector3 spawn, out float yaw)
    {
        Vector3 inward = Quaternion.Euler(0f, AreaDef.InwardYaw(side), 0f) * Vector3.back;
        spawn = Area.WallPoint(side, offset) + inward * GateSpawnDistance;
        yaw = YawOf(inward);
    }

    void BuildInterior(out Vector3 spawn, out float yaw)
    {
        InteriorBuilder.Room room;

        if (Building.type == BuildingType.Shop)
        {
            List<ItemPedestal> pedestals = new List<ItemPedestal>();
            room = InteriorBuilder.BuildShop(world, Building, pedestals);

            foreach (ItemPedestal pedestal in pedestals)
            {
                string itemId = pedestal.itemId;
                Interactable.Create(pedestal.transform, new Vector3(0f, 0f, -0.6f), 1.9f,
                    () => ShopCatalog.GetPrompt(itemId),
                    () => hud.Toast(ShopCatalog.Purchase(itemId), 2.5f));
            }

            Interactable.Create(world, new Vector3(0f, 0f, 3.2f), 2.2f, () => "[E] Talk to the shopkeeper", TalkToShopkeeper);
            hud.SetTitle(Building.name, "Shop  -  " + Area.name);
        }
        else
        {
            room = InteriorBuilder.BuildBattle(world, Building, Area.theme);
            exitBars = room.exitBars;
            exitBars.SetActive(true);

            // The host (or solo player) runs the battle; guests see the host's enemies.
            if (Net.IsAuthority)
            {
                arena = world.gameObject.AddComponent<BattleArena>();
                InteriorBuilder.Room battleRoom = room;
                arena.onFinished += () => battleRoom.exitBars.SetActive(false);

                float hx = room.size.x * 0.5f;
                float hz = room.size.y * 0.5f;
                arena.Begin(Building, Rect.MinMaxRect(-hx + 2.5f, -hz + 3f, hx - 2.5f, hz - 2.5f), EnemyHealthScale);
            }

            bool cleared = gm.IsBattleCleared(Building.id);
            int reward = cleared ? Building.battle.repeatReward : Building.battle.reward;
            hud.SetTitle(Building.name, (cleared ? "Rematch" : "Battle") + "  -  win " + reward + " coins");
        }

        room.exit.prompt = () => BattleInProgress
            ? "The exit is barred until the battle is won!"
            : "[E] Leave " + Building.name;
        room.exit.onInteract = () =>
        {
            if (BattleInProgress)
            {
                hud.Toast("No escape! Win the battle first.", 2f);
                return;
            }
            Travel(gm.currentArea, "", AreaEntry.FromBuilding, Building.id, "");
        };

        spawn = room.spawn;
        yaw = 0f;
    }

    void TalkToShopkeeper()
    {
        string[] lines = Building.shopkeeperLines;
        if (lines.Length == 0)
        {
            return;
        }
        hud.Toast(lines[shopkeeperLine % lines.Length], 4f);
        shopkeeperLine++;
    }

    void PlacePlayer(Vector3 position, float yaw)
    {
        SpawnPoint = position;
        SpawnYaw = yaw;
        PlaceLocalPlayer();
    }

    // Each device places its own player. In co-op they stand side by side around the spawn point,
    // ordered by who joined first, so nobody lands on top of anyone else.
    public void PlaceLocalPlayer()
    {
        PlayerAvatar avatar = PlayerRegistry.Local;
        if (avatar == null || waitingForParty)
        {
            return;
        }
        GameObject player = avatar.gameObject;

        int slot = 0;
        int count = 1;
        if (avatar.Net != null && avatar.Net.IsSpawned)
        {
            count = 0;
            foreach (PlayerAvatar other in PlayerRegistry.All)
            {
                if (other == null || other.Net == null || !other.Net.IsSpawned)
                {
                    continue;
                }
                count++;
                if (other.Net.OwnerClientId < avatar.Net.OwnerClientId)
                {
                    slot++;
                }
            }
            count = Mathf.Max(count, NetSession.MaxPlayers);
        }

        Vector3 right = Quaternion.Euler(0f, SpawnYaw, 0f) * Vector3.right;
        float offset = count > 1 ? (slot - (NetSession.MaxPlayers - 1) * 0.5f) * 1.6f : 0f;
        Vector3 position = SpawnPoint + right * offset;
        position.y = Mathf.Max(position.y, 1.1f);

        ThirdPersonController controller = player.GetComponent<ThirdPersonController>();
        if (controller != null)
        {
            controller.Teleport(position, SpawnYaw);
        }
        else
        {
            player.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, SpawnYaw, 0f));
        }

        // Owner-driven network transform: jump there instead of sliding across the map on other screens.
        Unity.Netcode.Components.NetworkTransform nt = player.GetComponent<Unity.Netcode.Components.NetworkTransform>();
        if (nt != null && nt.IsSpawned && nt.IsOwner)
        {
            nt.Teleport(player.transform.position, player.transform.rotation, player.transform.localScale);
        }
    }

    void ApplyAtmosphere(bool interior)
    {
        AreaAtmosphere.Apply(Area, interior, Camera.main);
    }

    void ShowArrivalMessage()
    {
        bool arrivedFromAfar = Building == null && gm.pendingEntry != AreaEntry.FromBuilding && gm.pendingEntry != AreaEntry.Respawn;
        if (arrivedFromAfar)
        {
            hud.Banner(Area.name, 2.5f);
        }

        if (!string.IsNullOrEmpty(gm.pendingMessage))
        {
            hud.Toast(gm.pendingMessage, 4f);
            gm.pendingMessage = "";
        }
        else if (arrivedFromAfar)
        {
            hud.Toast(Area.tagline, 3f);
        }
    }

    static float YawOf(Vector3 direction)
    {
        return Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
    }

    // ---------------------------------------------------------------- interaction

    void Update()
    {
        if (waitingForParty || hud == null)
        {
            return;
        }

        if (Net.Active)
        {
            UpdateVote();
            if (exitBars != null && !Net.IsAuthority)
            {
                bool barred = BattleInProgress;
                if (exitBars.activeSelf != barred)
                {
                    exitBars.SetActive(barred);
                }
            }
        }

        if (travelling || gm.isTransitioning || gm.isInDialogue)
        {
            hud.SetPrompt(null);
            return;
        }

        if (Net.Active && UpdateDownedAndRevive())
        {
            return;
        }

        Interactable nearest = FindNearestInteractable();
        hud.SetPrompt(nearest != null && nearest.prompt != null ? nearest.prompt() : null);

        if (nearest != null && nearest.onInteract != null && (interactAction.WasPressedThisFrame() || Input.GetKeyDown(KeyCode.E) || MobileControls.InteractPressed))
        {
            nearest.onInteract();
        }
    }

    Interactable FindNearestInteractable()
    {
        Transform player = PlayerRegistry.LocalTransform;
        if (player == null)
        {
            return null;
        }

        Vector3 position = player.transform.position;
        Interactable nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (Interactable interactable in Interactable.All)
        {
            Vector3 offset = interactable.transform.position - position;
            offset.y = 0f;
            float distance = offset.magnitude;
            if (distance <= interactable.radius && distance < nearestDistance)
            {
                nearest = interactable;
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    // ---------------------------------------------------------------- travel

    void TravelToNextArea()
    {
        int next = gm.currentArea + 1;
        int loop = gm.loopCount;
        string message = "";

        if (next >= AreaDatabase.Count)
        {
            next = 0;
            loop++;
            message = "You conquered every area and followed the road home!\nCycle " + (loop + 1) + ": enemies grow stronger.";
        }

        Travel(next, "", AreaEntry.FromPreviousArea, "", message, loop);
    }

    void Travel(int areaIndex, string buildingId, AreaEntry entry, string fromBuilding, string message)
    {
        Travel(areaIndex, buildingId, entry, fromBuilding, message, gm.loopCount);
    }

    void Travel(int areaIndex, string buildingId, AreaEntry entry, string fromBuilding, string message, int loop)
    {
        if (travelling)
        {
            return;
        }

        // Co-op: ask the host. With more than one player the party votes first.
        if (Net.Active)
        {
            if (PartyState.Instance != null)
            {
                PartyState.Instance.RequestTravel(areaIndex, buildingId, entry, fromBuilding, message, DescribeTrip(areaIndex, buildingId, entry), loop);
                if (Net.PlayerCount > 1)
                {
                    hud.Toast("Asking the party...", 1.5f);
                }
            }
            return;
        }

        gm.loopCount = loop;
        travelling = true;
        hud.SetPrompt(null);

        gm.currentArea = areaIndex;
        gm.currentBuilding = buildingId;
        gm.pendingEntry = entry;
        gm.lastBuilding = fromBuilding;
        gm.pendingMessage = message;

        string scene = SceneManager.GetActiveScene().name;
        if (SceneTransition.Instance != null)
        {
            SceneTransition.Instance.TransitionToScene(scene);
        }
        else
        {
            SceneManager.LoadScene(scene);
        }
    }

    // ---------------------------------------------------------------- co-op: votes, downed, revive

    void UpdateVote()
    {
        PartyState party = PartyState.Instance;
        if (party == null || !party.IsSpawned || !party.Vote.Value.active)
        {
            hud.HideVote();
            return;
        }

        VoteState vote = party.Vote.Value;
        float left = Mathf.Max(0f, vote.endsAt - (float)Unity.Netcode.NetworkManager.Singleton.ServerTime.Time);
        hud.ShowVote(vote.id, vote.title.ToString(), vote.yes, vote.needed, left);

        if (Input.GetKeyDown(KeyCode.Y))
        {
            party.SendVote(true);
        }
        else if (Input.GetKeyDown(KeyCode.N))
        {
            party.SendVote(false);
        }
    }

    // Returns true when the downed/revive prompt takes over the Use button this frame.
    bool UpdateDownedAndRevive()
    {
        PlayerAvatar local = PlayerRegistry.Local;
        if (local == null)
        {
            return false;
        }

        if (local.IsDowned)
        {
            hud.SetPrompt("You're down!\nA teammate can hold [E] next to you to revive you.");
            reviveProgress = 0f;
            return true;
        }

        PlayerAvatar downed = null;
        float best = NetPlayer.ReviveRange;
        foreach (PlayerAvatar p in PlayerRegistry.All)
        {
            if (p == null || p == local || !p.IsDowned)
            {
                continue;
            }
            Vector3 offset = p.transform.position - local.transform.position;
            offset.y = 0f;
            if (offset.magnitude <= best)
            {
                best = offset.magnitude;
                downed = p;
            }
        }

        if (downed != reviveTarget)
        {
            reviveTarget = downed;
            reviveProgress = 0f;
        }
        if (downed == null)
        {
            return false;
        }

        bool holding = interactAction.IsPressed() || Input.GetKey(KeyCode.E) || MobileControls.InteractHeld;
        reviveProgress = holding ? reviveProgress + Time.deltaTime : 0f;
        string name = downed.Net != null ? downed.Net.DisplayName : "your teammate";

        if (reviveProgress >= NetPlayer.ReviveSeconds)
        {
            reviveProgress = 0f;
            if (downed.Net != null)
            {
                downed.Net.ReviveRpc();
            }
            hud.Toast("You revived " + name + "!", 2f);
            return true;
        }

        hud.SetPrompt(holding
            ? "Reviving " + name + "...  " + Mathf.FloorToInt(reviveProgress / NetPlayer.ReviveSeconds * 100f) + "%"
            : "Hold [E] to revive " + name);
        return true;
    }

    // Host: everyone is down. The whole party wakes up somewhere safe at full health.
    public void HandlePartyDefeated()
    {
        if (!Net.IsAuthority || PartyState.Instance == null)
        {
            return;
        }
        if (arena != null)
        {
            arena.Abort();
        }
        if (Building != null)
        {
            PartyState.Instance.BeginTravel(gm.currentArea, "", AreaEntry.FromBuilding, Building.id,
                "The whole party was knocked out and dragged out of the " + Building.name + ".\nEveryone is patched up - try again!", true);
        }
        else
        {
            PartyState.Instance.BeginTravel(gm.currentArea, "", AreaEntry.Respawn, "",
                "The whole party collapsed... and woke up somewhere safer.", true);
        }
    }

    // Called by PlayerHealth when health hits zero. Nobody dies for good: you wake up somewhere safe.
    public void HandlePlayerDefeated()
    {
        if (travelling)
        {
            return;
        }

        if (arena != null)
        {
            arena.Abort();
        }

        gm.ResetHealth();

        if (Building != null)
        {
            Travel(gm.currentArea, "", AreaEntry.FromBuilding, Building.id, "Defeated! You were dragged out of the " + Building.name + ".\nHealth restored - try again when ready.");
        }
        else
        {
            Travel(gm.currentArea, "", AreaEntry.Respawn, "", "You collapsed... and woke up somewhere safer.");
        }
    }
}

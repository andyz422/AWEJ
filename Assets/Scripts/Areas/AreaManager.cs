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

        gm.currentArea = Mathf.Clamp(gm.currentArea, 0, AreaDatabase.Count - 1);
        Area = AreaDatabase.Get(gm.currentArea);
        Building = Area.FindBuilding(gm.currentBuilding);
        if (Building == null)
        {
            gm.currentBuilding = "";
        }

        world = new GameObject("AreaWorld").transform;
        hud = AreaHUD.Create(Area.name, "");

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

    float EnemyHealthScale
    {
        get { return 1f + 0.25f * gm.currentArea + 0.6f * gm.loopCount; }
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

            arena = world.gameObject.AddComponent<BattleArena>();
            InteriorBuilder.Room battleRoom = room;
            battleRoom.exitBars.SetActive(true);
            arena.onFinished += () => battleRoom.exitBars.SetActive(false);

            float hx = room.size.x * 0.5f;
            float hz = room.size.y * 0.5f;
            arena.Begin(Building, Rect.MinMaxRect(-hx + 2.5f, -hz + 3f, hx - 2.5f, hz - 2.5f), EnemyHealthScale);

            bool cleared = gm.IsBattleCleared(Building.id);
            int reward = cleared ? Building.battle.repeatReward : Building.battle.reward;
            hud.SetTitle(Building.name, (cleared ? "Rematch" : "Battle") + "  -  win " + reward + " coins");
        }

        room.exit.prompt = () => arena != null && arena.InProgress
            ? "The exit is barred until the battle is won!"
            : "[E] Leave " + Building.name;
        room.exit.onInteract = () =>
        {
            if (arena != null && arena.InProgress)
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
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            return;
        }

        position.y = Mathf.Max(position.y, 1.1f);
        ThirdPersonController controller = player.GetComponent<ThirdPersonController>();
        if (controller != null)
        {
            controller.Teleport(position, yaw);
        }
        else
        {
            player.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
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
        if (travelling || gm.isTransitioning || gm.isInDialogue)
        {
            hud.SetPrompt(null);
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
        GameObject player = GameObject.FindGameObjectWithTag("Player");
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
        string message = "";

        if (next >= AreaDatabase.Count)
        {
            next = 0;
            gm.loopCount++;
            message = "You conquered every area and followed the road home!\nCycle " + (gm.loopCount + 1) + ": enemies grow stronger.";
        }

        Travel(next, "", AreaEntry.FromPreviousArea, "", message);
    }

    void Travel(int areaIndex, string buildingId, AreaEntry entry, string fromBuilding, string message)
    {
        if (travelling)
        {
            return;
        }
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

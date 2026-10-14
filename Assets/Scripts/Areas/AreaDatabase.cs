using UnityEngine;

public enum WallSide { North, South, East, West }
public enum BuildingType { Shop, Battle }
public enum BuildingStyle { House, Tent, Hut, Mushroom, Dome, Tower, Arena, Stump }
public enum AreaTheme { Town, Forest, Desert, Snow, Volcano }

public class BattleDef
{
    public EnemyType[][] waves;
    public int reward;
    public int repeatReward;
    public Vector2 arenaSize = new Vector2(36f, 30f);
    public int coverPillars = 4;
}

public class BuildingDef
{
    public string id;
    public string name;
    public BuildingType type;
    public BuildingStyle style;

    // Center of the footprint on the ground (world X, Z) and its width (local X) / depth (local Z).
    public Vector2 position;
    public Vector2 size = new Vector2(12f, 10f);
    // Yaw in degrees. 0 = door faces south (toward the camera), 90 = west, -90 = east, 180 = north.
    public float facing;

    public Color bodyColor = new Color(0.9f, 0.85f, 0.7f);
    public Color roofColor = new Color(0.6f, 0.25f, 0.2f);
    public Color trimColor = new Color(0.35f, 0.22f, 0.12f);

    // Shop
    public string[] stock = new string[0];
    public string shopkeeperSprite = "Characters/squirtle";
    public string[] shopkeeperLines = new string[0];

    // Battle
    public BattleDef battle;

    // Interior look
    public string interiorFloorMaterial = "";
    public Color interiorFloorColor = new Color(0.55f, 0.38f, 0.22f);
    public Color interiorWallColor = new Color(0.85f, 0.78f, 0.62f);

    public Quaternion Rotation
    {
        get { return Quaternion.Euler(0f, facing, 0f); }
    }

    // World direction the door faces.
    public Vector3 Front
    {
        get { return Rotation * Vector3.back; }
    }

    // Distance from the footprint center to the face of the door; depends on the style's shape.
    public float DoorDepth
    {
        get
        {
            switch (style)
            {
                case BuildingStyle.Mushroom: return size.x * 0.35f;
                case BuildingStyle.Dome: return size.y * 0.5f + 1.5f;
                case BuildingStyle.Hut:
                case BuildingStyle.Tower:
                case BuildingStyle.Arena:
                case BuildingStyle.Stump: return size.x * 0.5f;
                default: return size.y * 0.5f;
            }
        }
    }

    // Where the player stands after walking out of this building.
    public Vector3 ExitSpot
    {
        get { return WorldKit.Flat(position) + Front * (DoorDepth + 3.5f); }
    }
}

public class PathDef
{
    public Vector2 from;
    public Vector2 to;
    public float width = 6f;

    public PathDef(float x1, float z1, float x2, float z2, float width = 6f)
    {
        from = new Vector2(x1, z1);
        to = new Vector2(x2, z2);
        this.width = width;
    }
}

// Lava strip or pool. Bridges are rectangles inside the hazard that are safe to walk on.
public class HazardDef
{
    public Vector2 center;
    public float radius;
    public Vector2 rectSize;
    public Rect[] bridges = new Rect[0];
}

public class AreaDef
{
    public string name;
    public string tagline;
    public AreaTheme theme;
    public Vector2 size = new Vector2(100f, 100f);
    public int seed;

    public string groundMaterial;
    public Color groundColor;
    public string wallMaterial;
    public Color wallColor;
    public string pathMaterial;
    public Color pathColor;
    public float wallHeight = 3f;

    public WallSide exitSide = WallSide.North;
    public float exitOffset;
    public WallSide entrySide = WallSide.South;
    public float entryOffset;
    public Vector2 startPosition;

    public Color skyColor = new Color(0.55f, 0.75f, 0.95f);
    public Color ambientColor = new Color(0.5f, 0.5f, 0.55f);
    public Color lightColor = new Color(1f, 0.96f, 0.84f);
    public float lightIntensity = 1f;
    public float fogDensity = 0f;

    public BuildingDef[] buildings = new BuildingDef[0];
    public PathDef[] paths = new PathDef[0];
    public HazardDef[] hazards = new HazardDef[0];

    public BuildingDef FindBuilding(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        foreach (BuildingDef building in buildings)
        {
            if (building.id == id)
            {
                return building;
            }
        }
        return null;
    }

    public int BattleCount
    {
        get
        {
            int count = 0;
            foreach (BuildingDef building in buildings)
            {
                if (building.type == BuildingType.Battle) count++;
            }
            return count;
        }
    }

    public int ClearedBattleCount(GameManager gm)
    {
        int count = 0;
        foreach (BuildingDef building in buildings)
        {
            if (building.type == BuildingType.Battle && gm != null && gm.IsBattleCleared(building.id)) count++;
        }
        return count;
    }

    // Point on the inside face of a wall, `offset` along it.
    public Vector3 WallPoint(WallSide side, float offset)
    {
        float hx = size.x * 0.5f;
        float hz = size.y * 0.5f;
        switch (side)
        {
            case WallSide.North: return new Vector3(offset, 0f, hz);
            case WallSide.South: return new Vector3(offset, 0f, -hz);
            case WallSide.East: return new Vector3(hx, 0f, offset);
            default: return new Vector3(-hx, 0f, offset);
        }
    }

    // Yaw that makes an object's -Z (front) face into the area from that wall.
    public static float InwardYaw(WallSide side)
    {
        switch (side)
        {
            case WallSide.North: return 0f;
            case WallSide.South: return 180f;
            case WallSide.East: return 90f;
            default: return -90f;
        }
    }
}

// All areas in travel order. The exit gate of the last area loops back to the first.
public static class AreaDatabase
{
    static readonly EnemyType S = EnemyType.Slime;
    static readonly EnemyType C = EnemyType.Charger;
    static readonly EnemyType P = EnemyType.Spitter;
    static readonly EnemyType B = EnemyType.Brute;
    static readonly EnemyType K = EnemyType.Boss;

    private static AreaDef[] areas;

    public static AreaDef[] Areas
    {
        get
        {
            if (areas == null)
            {
                areas = new[] { DustyCrossroads(), WhisperingGrove(), SunscorchDunes(), FrostpeakHollow(), MoltenCitadel() };
            }
            return areas;
        }
    }

    public static int Count
    {
        get { return Areas.Length; }
    }

    public static AreaDef Get(int index)
    {
        return Areas[Mathf.Clamp(index, 0, Areas.Length - 1)];
    }

    static EnemyType[] W(params EnemyType[] enemies)
    {
        return enemies;
    }

    static AreaDef DustyCrossroads()
    {
        return new AreaDef
        {
            name = "Dusty Crossroads",
            tagline = "A sleepy frontier town",
            theme = AreaTheme.Town,
            size = new Vector2(100f, 100f),
            seed = 11,
            groundMaterial = "T_YFGM_GrassLeafs02_s", groundColor = new Color(0.35f, 0.55f, 0.25f),
            wallMaterial = "M_YFGM_SandySoil", wallColor = new Color(0.75f, 0.62f, 0.45f),
            pathMaterial = "M_YFGM_SoilGravel01", pathColor = new Color(0.6f, 0.55f, 0.45f),
            exitSide = WallSide.North, exitOffset = 0f,
            startPosition = new Vector2(0f, -30f),
            skyColor = new Color(0.55f, 0.75f, 0.95f),
            ambientColor = new Color(0.55f, 0.55f, 0.6f),
            paths = new[]
            {
                new PathDef(0f, -50f, 0f, 50f, 8f),
                new PathDef(-17f, 18f, 0f, 18f, 4f),
                new PathDef(-17f, -18f, 0f, -18f, 4f),
                new PathDef(0f, 18f, 17f, 18f, 4f),
                new PathDef(0f, -18f, 17f, -18f, 4f)
            },
            buildings = new[]
            {
                new BuildingDef
                {
                    id = "general_store", name = "General Store", type = BuildingType.Shop, style = BuildingStyle.House,
                    position = new Vector2(-24f, 18f), size = new Vector2(12f, 10f), facing = -90f,
                    bodyColor = new Color(0.95f, 0.88f, 0.7f), roofColor = new Color(0.25f, 0.45f, 0.7f),
                    stock = new[] { "potion", "hp", "boots", "dmg" },
                    shopkeeperSprite = "Characters/squirtle",
                    shopkeeperLines = new[] { "Welcome, traveler! Stock up before heading north.", "The gate north only opens for proven fighters." }
                },
                new BuildingDef
                {
                    id = "tinker_workshop", name = "Tinker's Workshop", type = BuildingType.Shop, style = BuildingStyle.Tower,
                    position = new Vector2(-24f, -18f), size = new Vector2(10f, 10f), facing = -90f,
                    bodyColor = new Color(0.62f, 0.6f, 0.58f), roofColor = new Color(0.8f, 0.55f, 0.15f),
                    stock = new[] { "twin", "repeater", "rate" },
                    shopkeeperSprite = "Characters/diglett",
                    shopkeeperLines = new[] { "Gears, springs, blasters! Mind the sparks.", "Press Q or 1/2 to swap between weapons you own." },
                    interiorFloorColor = new Color(0.4f, 0.4f, 0.42f), interiorWallColor = new Color(0.6f, 0.55f, 0.5f)
                },
                new BuildingDef
                {
                    id = "training_barn", name = "Training Barn", type = BuildingType.Battle, style = BuildingStyle.House,
                    position = new Vector2(24f, 18f), size = new Vector2(14f, 12f), facing = 90f,
                    bodyColor = new Color(0.7f, 0.18f, 0.15f), roofColor = new Color(0.35f, 0.3f, 0.28f), trimColor = Color.white,
                    interiorFloorMaterial = "M_YFGM_Dry01", interiorWallColor = new Color(0.55f, 0.2f, 0.16f),
                    battle = new BattleDef
                    {
                        waves = new[] { W(S, S, S), W(S, S, S, S), W(S, S, C) },
                        reward = 30, repeatReward = 8, arenaSize = new Vector2(32f, 26f)
                    }
                },
                new BuildingDef
                {
                    id = "old_cellar", name = "Old Cellar", type = BuildingType.Battle, style = BuildingStyle.Hut,
                    position = new Vector2(24f, -18f), size = new Vector2(10f, 10f), facing = 90f,
                    bodyColor = new Color(0.55f, 0.5f, 0.45f), roofColor = new Color(0.4f, 0.28f, 0.18f),
                    interiorFloorMaterial = "M_YFGM_GroundStones01", interiorWallColor = new Color(0.4f, 0.37f, 0.35f),
                    battle = new BattleDef
                    {
                        waves = new[] { W(S, S, C), W(C, C, S, S), W(B) },
                        reward = 40, repeatReward = 10
                    }
                }
            }
        };
    }

    static AreaDef WhisperingGrove()
    {
        return new AreaDef
        {
            name = "Whispering Grove",
            tagline = "Fireflies drift between ancient trees",
            theme = AreaTheme.Forest,
            size = new Vector2(130f, 90f),
            seed = 23,
            groundMaterial = "M_YFGM_Grass03", groundColor = new Color(0.25f, 0.5f, 0.2f),
            wallMaterial = "M_YFGM_MossStones", wallColor = new Color(0.35f, 0.45f, 0.3f),
            pathMaterial = "M_YFGM_Ground01", pathColor = new Color(0.45f, 0.35f, 0.25f),
            entrySide = WallSide.South, entryOffset = 0f,
            exitSide = WallSide.East, exitOffset = 15f,
            startPosition = new Vector2(0f, -38f),
            skyColor = new Color(0.4f, 0.6f, 0.55f),
            ambientColor = new Color(0.4f, 0.5f, 0.42f),
            lightColor = new Color(0.95f, 1f, 0.85f), lightIntensity = 0.9f,
            fogDensity = 0.008f,
            paths = new[]
            {
                new PathDef(0f, -45f, 0f, 15f),
                new PathDef(-30f, 15f, 65f, 15f),
                new PathDef(25f, 15f, 25f, 25f, 4f),
                new PathDef(0f, -18f, 34f, -18f, 4f),
                new PathDef(-13f, -25f, 0f, -25f, 4f)
            },
            buildings = new[]
            {
                new BuildingDef
                {
                    id = "herbalist_hut", name = "Herbalist's Hut", type = BuildingType.Shop, style = BuildingStyle.Mushroom,
                    position = new Vector2(-30f, 22f), size = new Vector2(10f, 10f), facing = 0f,
                    bodyColor = new Color(0.95f, 0.9f, 0.78f), roofColor = new Color(0.85f, 0.15f, 0.12f),
                    stock = new[] { "potion", "hp", "boots" },
                    shopkeeperSprite = "Characters/squirtle",
                    shopkeeperLines = new[] { "Herbs for what ails you, friend.", "The Beetle Den beetles charge in straight lines. Sidestep!" },
                    interiorFloorMaterial = "M_YFGM_SoilMoss", interiorWallColor = new Color(0.8f, 0.72f, 0.55f)
                },
                new BuildingDef
                {
                    id = "ranger_outpost", name = "Ranger Outpost", type = BuildingType.Shop, style = BuildingStyle.Tower,
                    position = new Vector2(-18f, -25f), size = new Vector2(9f, 9f), facing = -90f,
                    bodyColor = new Color(0.5f, 0.38f, 0.25f), roofColor = new Color(0.2f, 0.4f, 0.2f),
                    stock = new[] { "twin", "repeater", "scatter", "dmg" },
                    shopkeeperSprite = "Characters/charmander",
                    shopkeeperLines = new[] { "A ranger travels light but shoots heavy.", "Spitters keep their distance. Close in fast." },
                    interiorFloorColor = new Color(0.45f, 0.32f, 0.2f), interiorWallColor = new Color(0.5f, 0.4f, 0.28f)
                },
                new BuildingDef
                {
                    id = "hollow_stump", name = "Hollow Stump", type = BuildingType.Battle, style = BuildingStyle.Stump,
                    position = new Vector2(25f, 32f), size = new Vector2(14f, 14f), facing = 0f,
                    bodyColor = new Color(0.45f, 0.3f, 0.18f), roofColor = new Color(0.75f, 0.6f, 0.4f),
                    interiorFloorMaterial = "M_YFGM_SoilMoss", interiorWallColor = new Color(0.4f, 0.28f, 0.16f),
                    battle = new BattleDef
                    {
                        waves = new[] { W(S, S, P), W(C, C, P, S), W(P, P, S, S, C) },
                        reward = 55, repeatReward = 15, coverPillars = 6
                    }
                },
                new BuildingDef
                {
                    id = "beetle_den", name = "Beetle Den", type = BuildingType.Battle, style = BuildingStyle.Dome,
                    position = new Vector2(40f, -18f), size = new Vector2(12f, 12f), facing = 90f,
                    bodyColor = new Color(0.3f, 0.45f, 0.22f), roofColor = new Color(0.2f, 0.3f, 0.15f),
                    interiorFloorMaterial = "M_YFGM_Ground01", interiorWallColor = new Color(0.25f, 0.35f, 0.2f),
                    battle = new BattleDef
                    {
                        waves = new[] { W(C, C, C), W(C, C, C, C), W(B, C, C) },
                        reward = 60, repeatReward = 15, arenaSize = new Vector2(40f, 30f)
                    }
                }
            }
        };
    }

    static AreaDef SunscorchDunes()
    {
        return new AreaDef
        {
            name = "Sunscorch Dunes",
            tagline = "Endless sand under a blazing sky",
            theme = AreaTheme.Desert,
            size = new Vector2(90f, 140f),
            seed = 37,
            groundMaterial = "M_YFGM_SandySoil", groundColor = new Color(0.85f, 0.72f, 0.45f),
            wallMaterial = "M_YFGM_Clay", wallColor = new Color(0.7f, 0.45f, 0.3f),
            pathMaterial = "M_YFGM_Dry02", pathColor = new Color(0.7f, 0.6f, 0.45f),
            entrySide = WallSide.West, entryOffset = -40f,
            exitSide = WallSide.North, exitOffset = 15f,
            startPosition = new Vector2(-38f, -40f),
            skyColor = new Color(0.95f, 0.8f, 0.55f),
            ambientColor = new Color(0.65f, 0.55f, 0.45f),
            lightColor = new Color(1f, 0.9f, 0.7f), lightIntensity = 1.25f,
            paths = new[]
            {
                new PathDef(-45f, -40f, 0f, -40f),
                new PathDef(0f, -40f, 0f, 40f),
                new PathDef(0f, 40f, 15f, 40f),
                new PathDef(15f, 40f, 15f, 70f),
                new PathDef(-17f, 15f, 0f, 15f, 4f),
                new PathDef(-17f, -15f, 0f, -15f, 4f),
                new PathDef(0f, -10f, 16f, -10f, 4f),
                new PathDef(-11f, 52f, 15f, 52f, 4f)
            },
            buildings = new[]
            {
                new BuildingDef
                {
                    id = "sunset_bazaar", name = "Sunset Bazaar", type = BuildingType.Shop, style = BuildingStyle.Tent,
                    position = new Vector2(-22f, 15f), size = new Vector2(14f, 10f), facing = -90f,
                    bodyColor = new Color(0.9f, 0.3f, 0.25f), roofColor = new Color(0.95f, 0.85f, 0.4f),
                    stock = new[] { "scatter", "repeater", "rate", "dmg" },
                    shopkeeperSprite = "Characters/charmander",
                    shopkeeperLines = new[] { "Finest wares this side of the dunes!", "The Colosseum crowd loves a good Scatter Cannon." },
                    interiorFloorColor = new Color(0.75f, 0.35f, 0.25f), interiorWallColor = new Color(0.95f, 0.8f, 0.45f)
                },
                new BuildingDef
                {
                    id = "oasis_apothecary", name = "Oasis Apothecary", type = BuildingType.Shop, style = BuildingStyle.Dome,
                    position = new Vector2(-22f, -15f), size = new Vector2(10f, 10f), facing = -90f,
                    bodyColor = new Color(0.92f, 0.85f, 0.7f), roofColor = new Color(0.3f, 0.6f, 0.75f),
                    stock = new[] { "potion", "hp", "boots" },
                    shopkeeperSprite = "Characters/squirtle",
                    shopkeeperLines = new[] { "Water and remedies. Both priceless out here.", "Stay hydrated, and stay out of the Scorpion Pit unless you mean it." },
                    interiorFloorColor = new Color(0.8f, 0.72f, 0.55f), interiorWallColor = new Color(0.9f, 0.88f, 0.8f)
                },
                new BuildingDef
                {
                    id = "scorpion_pit", name = "Scorpion Pit", type = BuildingType.Battle, style = BuildingStyle.Arena,
                    position = new Vector2(24f, -10f), size = new Vector2(16f, 16f), facing = 90f,
                    bodyColor = new Color(0.8f, 0.65f, 0.42f), roofColor = new Color(0.6f, 0.2f, 0.15f),
                    interiorFloorMaterial = "M_YFGM_Dry03", interiorWallColor = new Color(0.75f, 0.55f, 0.35f),
                    battle = new BattleDef
                    {
                        waves = new[] { W(C, C, P, P), W(B, S, S, S, S), W(C, C, C, P, P) },
                        reward = 80, repeatReward = 20
                    }
                },
                new BuildingDef
                {
                    id = "sun_colosseum", name = "Sun Colosseum", type = BuildingType.Battle, style = BuildingStyle.Arena,
                    position = new Vector2(-20f, 52f), size = new Vector2(18f, 18f), facing = -90f,
                    bodyColor = new Color(0.9f, 0.82f, 0.62f), roofColor = new Color(0.85f, 0.65f, 0.15f),
                    interiorFloorMaterial = "M_YFGM_SandySoil", interiorWallColor = new Color(0.88f, 0.78f, 0.58f),
                    battle = new BattleDef
                    {
                        waves = new[] { W(P, P, P), W(B, B), W(S, S, S, S, S, S), W(B, C, C, P, P) },
                        reward = 100, repeatReward = 25, arenaSize = new Vector2(42f, 34f), coverPillars = 6
                    }
                }
            }
        };
    }

    static AreaDef FrostpeakHollow()
    {
        return new AreaDef
        {
            name = "Frostpeak Hollow",
            tagline = "Snow falls forever in this frozen valley",
            theme = AreaTheme.Snow,
            size = new Vector2(110f, 110f),
            seed = 41,
            groundMaterial = "M_YFGM_Snow", groundColor = new Color(0.92f, 0.95f, 1f),
            wallMaterial = "M_YFGM_Salt", wallColor = new Color(0.8f, 0.85f, 0.9f),
            pathMaterial = "M_YFGM_FrozenGrass", pathColor = new Color(0.65f, 0.7f, 0.75f),
            entrySide = WallSide.South, entryOffset = 15f,
            exitSide = WallSide.West, exitOffset = 20f,
            startPosition = new Vector2(15f, -48f),
            skyColor = new Color(0.75f, 0.82f, 0.92f),
            ambientColor = new Color(0.6f, 0.65f, 0.75f),
            lightColor = new Color(0.85f, 0.9f, 1f), lightIntensity = 0.95f,
            fogDensity = 0.012f,
            paths = new[]
            {
                new PathDef(15f, -55f, 15f, 20f),
                new PathDef(35f, 20f, -55f, 20f),
                new PathDef(15f, -20f, 29f, -20f, 4f),
                new PathDef(-10f, -20f, 15f, -20f, 4f),
                new PathDef(-20f, 20f, -20f, 34f, 4f),
                new PathDef(35f, 20f, 35f, 30f, 4f)
            },
            buildings = new[]
            {
                new BuildingDef
                {
                    id = "igloo_armory", name = "Igloo Armory", type = BuildingType.Shop, style = BuildingStyle.Dome,
                    position = new Vector2(35f, -20f), size = new Vector2(12f, 12f), facing = 90f,
                    bodyColor = new Color(0.95f, 0.97f, 1f), roofColor = new Color(0.6f, 0.8f, 0.95f),
                    stock = new[] { "frost", "scatter", "rate", "dmg" },
                    shopkeeperSprite = "Characters/squirtle",
                    shopkeeperLines = new[] { "Brr! The Frost Lance pierces right through a line of foes.", "Yetis hit hard. Keep moving." },
                    interiorFloorMaterial = "M_YFGM_FrozenGrass", interiorWallColor = new Color(0.85f, 0.92f, 1f)
                },
                new BuildingDef
                {
                    id = "cocoa_cabin", name = "Cocoa Cabin", type = BuildingType.Shop, style = BuildingStyle.House,
                    position = new Vector2(-15f, -20f), size = new Vector2(12f, 10f), facing = -90f,
                    bodyColor = new Color(0.45f, 0.3f, 0.2f), roofColor = new Color(0.95f, 0.97f, 1f), trimColor = new Color(0.9f, 0.2f, 0.2f),
                    stock = new[] { "potion", "hp", "boots" },
                    shopkeeperSprite = "Characters/charmander",
                    shopkeeperLines = new[] { "Warm up by the fire. Cocoa's on the house, tonics aren't.", "The west gate leads somewhere much, much hotter." },
                    interiorFloorColor = new Color(0.45f, 0.3f, 0.2f), interiorWallColor = new Color(0.6f, 0.42f, 0.28f)
                },
                new BuildingDef
                {
                    id = "yeti_lodge", name = "Yeti Lodge", type = BuildingType.Battle, style = BuildingStyle.House,
                    position = new Vector2(-20f, 40f), size = new Vector2(16f, 12f), facing = 0f,
                    bodyColor = new Color(0.4f, 0.28f, 0.2f), roofColor = new Color(0.95f, 0.97f, 1f), trimColor = new Color(0.3f, 0.5f, 0.7f),
                    interiorFloorMaterial = "M_YFGM_Snow", interiorWallColor = new Color(0.45f, 0.32f, 0.22f),
                    battle = new BattleDef
                    {
                        waves = new[] { W(B, S, S), W(B, B, P), W(B, B, C, C) },
                        reward = 120, repeatReward = 30
                    }
                },
                new BuildingDef
                {
                    id = "ice_cavern", name = "Ice Cavern", type = BuildingType.Battle, style = BuildingStyle.Dome,
                    position = new Vector2(35f, 38f), size = new Vector2(16f, 16f), facing = 0f,
                    bodyColor = new Color(0.6f, 0.8f, 0.95f), roofColor = new Color(0.4f, 0.6f, 0.85f),
                    interiorFloorMaterial = "M_YFGM_FrozenGrass", interiorWallColor = new Color(0.55f, 0.75f, 0.9f),
                    battle = new BattleDef
                    {
                        waves = new[] { W(P, P, P, P), W(C, C, C, C, C), W(P, P, B, B, C) },
                        reward = 130, repeatReward = 30, arenaSize = new Vector2(40f, 32f), coverPillars = 6
                    }
                }
            }
        };
    }

    static AreaDef MoltenCitadel()
    {
        return new AreaDef
        {
            name = "Molten Citadel",
            tagline = "Rivers of lava guard the final throne",
            theme = AreaTheme.Volcano,
            size = new Vector2(120f, 120f),
            seed = 53,
            groundMaterial = "M_YFGM_Burnt", groundColor = new Color(0.25f, 0.2f, 0.18f),
            wallMaterial = "M_YFGM_Mars", wallColor = new Color(0.45f, 0.2f, 0.12f),
            pathMaterial = "M_YFGM_GroundStones01", pathColor = new Color(0.35f, 0.32f, 0.3f),
            entrySide = WallSide.East, entryOffset = -30f,
            exitSide = WallSide.North, exitOffset = 0f,
            startPosition = new Vector2(52f, -30f),
            skyColor = new Color(0.3f, 0.12f, 0.1f),
            ambientColor = new Color(0.45f, 0.28f, 0.22f),
            lightColor = new Color(1f, 0.6f, 0.4f), lightIntensity = 0.8f,
            fogDensity = 0.015f,
            paths = new[]
            {
                new PathDef(60f, -30f, 0f, -30f),
                new PathDef(0f, -30f, 0f, 60f),
                new PathDef(-23f, -5f, 22f, -5f, 4f),
                new PathDef(-17f, 38f, 23f, 38f, 4f)
            },
            hazards = new[]
            {
                new HazardDef
                {
                    center = new Vector2(0f, 18f), rectSize = new Vector2(120f, 5f),
                    bridges = new[] { new Rect(-4f, 15f, 8f, 6f) }
                },
                new HazardDef { center = new Vector2(-40f, -40f), radius = 7f },
                new HazardDef { center = new Vector2(38f, -48f), radius = 6f },
                new HazardDef { center = new Vector2(45f, 48f), radius = 6f }
            },
            buildings = new[]
            {
                new BuildingDef
                {
                    id = "ember_forge", name = "Forge of Embers", type = BuildingType.Shop, style = BuildingStyle.Tower,
                    position = new Vector2(-28f, -5f), size = new Vector2(10f, 10f), facing = -90f,
                    bodyColor = new Color(0.2f, 0.18f, 0.18f), roofColor = new Color(1f, 0.45f, 0.1f),
                    stock = new[] { "ember", "frost", "dmg", "rate" },
                    shopkeeperSprite = "Characters/charmander",
                    shopkeeperLines = new[] { "Forged in the heart of the volcano. Handle with care.", "The Magma King summons slimes when wounded." },
                    interiorFloorMaterial = "M_YFGM_Burnt", interiorWallColor = new Color(0.3f, 0.22f, 0.2f)
                },
                new BuildingDef
                {
                    id = "ash_altar", name = "Ash Altar", type = BuildingType.Shop, style = BuildingStyle.Hut,
                    position = new Vector2(28f, 38f), size = new Vector2(10f, 10f), facing = 90f,
                    bodyColor = new Color(0.35f, 0.33f, 0.33f), roofColor = new Color(0.6f, 0.15f, 0.12f),
                    stock = new[] { "potion", "hp" },
                    shopkeeperSprite = "Characters/diglett",
                    shopkeeperLines = new[] { "Rest a moment. The throne room waits for no one... but it can wait for you.", "Beyond the north gate lies the road home." },
                    interiorFloorMaterial = "M_YFGM_Mars", interiorWallColor = new Color(0.3f, 0.25f, 0.25f)
                },
                new BuildingDef
                {
                    id = "magma_gauntlet", name = "Magma Gauntlet", type = BuildingType.Battle, style = BuildingStyle.Arena,
                    position = new Vector2(30f, -5f), size = new Vector2(16f, 16f), facing = 90f,
                    bodyColor = new Color(0.25f, 0.22f, 0.22f), roofColor = new Color(1f, 0.4f, 0.1f),
                    interiorFloorMaterial = "M_YFGM_Mars", interiorWallColor = new Color(0.3f, 0.2f, 0.18f),
                    battle = new BattleDef
                    {
                        waves = new[] { W(S, S, S, S, S, S, C, C), W(P, P, P, B, B), W(C, C, C, C, B, P, P) },
                        reward = 160, repeatReward = 40, arenaSize = new Vector2(40f, 32f)
                    }
                },
                new BuildingDef
                {
                    id = "citadel_throne", name = "Citadel Throne", type = BuildingType.Battle, style = BuildingStyle.Tower,
                    position = new Vector2(-25f, 38f), size = new Vector2(16f, 16f), facing = -90f,
                    bodyColor = new Color(0.15f, 0.12f, 0.14f), roofColor = new Color(0.85f, 0.15f, 0.1f),
                    interiorFloorMaterial = "M_YFGM_Burnt", interiorWallColor = new Color(0.2f, 0.14f, 0.14f),
                    battle = new BattleDef
                    {
                        waves = new[] { W(B, B, P, P), W(K) },
                        reward = 300, repeatReward = 60, arenaSize = new Vector2(44f, 36f), coverPillars = 4
                    }
                }
            }
        };
    }
}

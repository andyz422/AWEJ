using UnityEngine;

// Builds the outdoor parts shared by every area: ground, rectangular walls, gates, paths,
// hazards and buildings. Theme-specific props live in AreaDecor.
public static class AreaBuilder
{
    static readonly Color DoorColor = new Color(0.22f, 0.13f, 0.07f);
    static readonly Color WindowColor = new Color(1f, 0.88f, 0.5f);
    static readonly Color IronColor = new Color(0.2f, 0.2f, 0.22f);
    static readonly Color ShopLabelColor = new Color(1f, 0.83f, 0.3f);
    static readonly Color BattleLabelColor = new Color(1f, 0.35f, 0.3f);

    public static void BuildGround(Transform root, Vector2 size, string materialName, Color fallback, float tileSize = 10f)
    {
        // Plane primitive is 10x10 units and comes with a MeshCollider.
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(root, false);
        ground.transform.localScale = new Vector3(size.x / 10f, 1f, size.y / 10f);
        ground.GetComponent<Renderer>().sharedMaterial = WorldKit.Textured(materialName, size / tileSize, fallback);
    }

    // Four walls enclosing a size.x by size.y rectangle centered on the root.
    // The south wall is a one-sided quad facing inward so the camera (which always looks north) can see through it.
    public static void BuildWalls(Transform root, Vector2 size, float height, string materialName, Color color)
    {
        GameObject walls = WorldKit.Group("Walls", root, Vector3.zero);
        float hx = size.x * 0.5f;
        float hz = size.y * 0.5f;
        const float thickness = 1f;

        Material alongX = WallMaterial(materialName, color, size.x + 2f, height);
        Material alongZ = WallMaterial(materialName, color, size.y + 2f, height);

        GameObject north = WorldKit.Prim(PrimitiveType.Cube, walls.transform, new Vector3(0f, height * 0.5f, hz + thickness * 0.5f), new Vector3(size.x + 2f * thickness, height, thickness), alongX);
        north.name = "Wall_North";
        GameObject east = WorldKit.Prim(PrimitiveType.Cube, walls.transform, new Vector3(hx + thickness * 0.5f, height * 0.5f, 0f), new Vector3(thickness, height, size.y + 2f * thickness), alongZ);
        east.name = "Wall_East";
        GameObject west = WorldKit.Prim(PrimitiveType.Cube, walls.transform, new Vector3(-hx - thickness * 0.5f, height * 0.5f, 0f), new Vector3(thickness, height, size.y + 2f * thickness), alongZ);
        west.name = "Wall_West";

        GameObject south = WorldKit.Prim(PrimitiveType.Quad, walls.transform, new Vector3(0f, height * 0.5f, -hz), new Vector3(size.x + 2f * thickness, height, 1f), alongX, false);
        south.name = "Wall_South";
        south.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        BoxCollider southCollider = south.AddComponent<BoxCollider>();
        southCollider.size = new Vector3(1f, 1f, thickness);
        southCollider.center = new Vector3(0f, 0f, thickness * 0.5f);
    }

    static Material WallMaterial(string materialName, Color color, float length, float height)
    {
        if (string.IsNullOrEmpty(materialName))
        {
            return WorldKit.Colored(color);
        }
        return WorldKit.Textured(materialName, new Vector2(length / 6f, height / 6f), color);
    }

    public static void BuildPaths(Transform root, PathDef[] paths, string materialName, Color color)
    {
        GameObject group = WorldKit.Group("Paths", root, Vector3.zero);

        for (int i = 0; i < paths.Length; i++)
        {
            Rect rect = PathRect(paths[i]);
            // Tiny height offsets stop overlapping strips from z-fighting.
            float y = 0.015f + i * 0.002f;
            Material material = WorldKit.Textured(materialName, new Vector2(rect.width / 5f, rect.height / 5f), color);
            WorldKit.Prim(PrimitiveType.Cube, group.transform, new Vector3(rect.center.x, y, rect.center.y), new Vector3(rect.width, 0.02f, rect.height), material, false);
        }
    }

    // Paths are axis-aligned strips; extend the ends by half the width so corners join cleanly.
    public static Rect PathRect(PathDef path)
    {
        float half = path.width * 0.5f;
        float minX = Mathf.Min(path.from.x, path.to.x) - half;
        float maxX = Mathf.Max(path.from.x, path.to.x) + half;
        float minZ = Mathf.Min(path.from.y, path.to.y) - half;
        float maxZ = Mathf.Max(path.from.y, path.to.y) + half;
        return Rect.MinMaxRect(minX, minZ, maxX, maxZ);
    }

    public static void BuildHazards(Transform root, HazardDef[] hazards)
    {
        foreach (HazardDef def in hazards)
        {
            GameObject hazard = WorldKit.Group("Lava", root, WorldKit.Flat(def.center));
            HazardZone zone = hazard.AddComponent<HazardZone>();
            zone.safeAreas = def.bridges;

            if (def.radius > 0f)
            {
                zone.radius = def.radius;
                Material lava = WorldKit.Textured("M_YFGM_Lava", Vector2.one * (def.radius / 3f), new Color(1f, 0.4f, 0.05f));
                WorldKit.Prim(PrimitiveType.Cylinder, hazard.transform, new Vector3(0f, 0.03f, 0f), new Vector3(def.radius * 2f, 0.02f, def.radius * 2f), lava, false);
                AddGlow(hazard.transform, new Vector3(0f, 2f, 0f), def.radius * 2.5f);

                // Ring of scorched rocks around the pool.
                int rocks = Mathf.RoundToInt(def.radius * 1.5f);
                for (int i = 0; i < rocks; i++)
                {
                    float angle = i * Mathf.PI * 2f / rocks;
                    Vector3 position = new Vector3(Mathf.Cos(angle), 0.2f, Mathf.Sin(angle)) * (def.radius + 0.5f);
                    position.y = 0.2f;
                    WorldKit.Prim(PrimitiveType.Sphere, hazard.transform, position, new Vector3(1.4f, 0.8f, 1.2f), new Color(0.15f, 0.12f, 0.12f), false);
                }
            }
            else
            {
                zone.area = new Rect(def.center - def.rectSize * 0.5f, def.rectSize);
                Material lava = WorldKit.Textured("M_YFGM_Lava", def.rectSize / 6f, new Color(1f, 0.4f, 0.05f));
                WorldKit.Prim(PrimitiveType.Cube, hazard.transform, new Vector3(0f, 0.03f, 0f), new Vector3(def.rectSize.x, 0.02f, def.rectSize.y), lava, false);

                float length = Mathf.Max(def.rectSize.x, def.rectSize.y);
                int lights = Mathf.Max(1, Mathf.RoundToInt(length / 30f));
                for (int i = 0; i < lights; i++)
                {
                    float t = (i + 0.5f) / lights - 0.5f;
                    Vector3 offset = def.rectSize.x >= def.rectSize.y ? new Vector3(t * def.rectSize.x, 2f, 0f) : new Vector3(0f, 2f, t * def.rectSize.y);
                    AddGlow(hazard.transform, offset, 14f);
                }
            }

            foreach (Rect bridge in def.bridges)
            {
                BuildBridge(root, bridge);
            }
        }
    }

    static void BuildBridge(Transform root, Rect rect)
    {
        GameObject bridge = WorldKit.Group("Bridge", root, new Vector3(rect.center.x, 0f, rect.center.y));
        Color stone = new Color(0.38f, 0.34f, 0.32f);
        WorldKit.Prim(PrimitiveType.Cube, bridge.transform, new Vector3(0f, 0.08f, 0f), new Vector3(rect.width, 0.16f, rect.height), stone, false);

        // Railings keep the player from stepping off into the lava.
        bool alongZ = rect.height >= rect.width;
        for (int side = -1; side <= 1; side += 2)
        {
            Vector3 position = alongZ ? new Vector3(side * rect.width * 0.5f, 0.6f, 0f) : new Vector3(0f, 0.6f, side * rect.height * 0.5f);
            Vector3 scale = alongZ ? new Vector3(0.4f, 1.2f, rect.height) : new Vector3(rect.width, 1.2f, 0.4f);
            WorldKit.Prim(PrimitiveType.Cube, bridge.transform, position, scale, new Color(0.25f, 0.22f, 0.2f));
        }
    }

    public static void AddGlow(Transform parent, Vector3 localPosition, float range)
    {
        GameObject glow = new GameObject("LavaGlow");
        glow.transform.SetParent(parent, false);
        glow.transform.localPosition = localPosition;
        Light light = glow.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.45f, 0.15f);
        light.range = range;
        light.intensity = 1.8f;
    }

    // A gate on the inside face of a wall. Returns the interactable so callers can hook up travel.
    // `cutaway` gates sit on the south wall between the camera and the player, so they skip the lintel.
    public static Interactable BuildGate(Transform root, Vector3 wallPoint, float yaw, string title, bool locked, bool cutaway)
    {
        GameObject gate = WorldKit.Group("Gate", root, wallPoint, yaw);
        Transform t = gate.transform;
        Color stone = new Color(0.45f, 0.42f, 0.4f);
        float postHeight = cutaway ? 2.2f : 4.4f;

        WorldKit.Prim(PrimitiveType.Cube, t, new Vector3(-2.7f, postHeight * 0.5f, -0.4f), new Vector3(1.2f, postHeight, 1.2f), stone);
        WorldKit.Prim(PrimitiveType.Cube, t, new Vector3(2.7f, postHeight * 0.5f, -0.4f), new Vector3(1.2f, postHeight, 1.2f), stone);
        WorldKit.Prim(PrimitiveType.Sphere, t, new Vector3(-2.7f, postHeight + 0.4f, -0.4f), Vector3.one * 0.9f, locked ? new Color(0.8f, 0.2f, 0.15f) : new Color(0.3f, 0.95f, 0.5f), false);
        WorldKit.Prim(PrimitiveType.Sphere, t, new Vector3(2.7f, postHeight + 0.4f, -0.4f), Vector3.one * 0.9f, locked ? new Color(0.8f, 0.2f, 0.15f) : new Color(0.3f, 0.95f, 0.5f), false);

        if (!cutaway)
        {
            WorldKit.Prim(PrimitiveType.Cube, t, new Vector3(0f, 4.8f, -0.4f), new Vector3(6.6f, 0.9f, 1.3f), stone);
        }

        if (locked)
        {
            WorldKit.Prim(PrimitiveType.Cube, t, new Vector3(0f, 1.9f, -0.15f), new Vector3(4.2f, 3.8f, 0.2f), DoorColor, false);
            for (int i = 0; i < 5; i++)
            {
                WorldKit.Prim(PrimitiveType.Cube, t, new Vector3(-1.6f + i * 0.8f, 1.9f, -0.35f), new Vector3(0.18f, 3.8f, 0.18f), IronColor, false);
            }
            WorldKit.Prim(PrimitiveType.Cube, t, new Vector3(0f, 2.4f, -0.4f), new Vector3(4.2f, 0.25f, 0.2f), IronColor, false);
        }
        else
        {
            // An open gate shows a swirling portal.
            GameObject portal = WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 1.9f, -0.15f), new Vector3(3.8f, 0.05f, 3.6f), new Color(0.35f, 0.85f, 1f), false);
            portal.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            portal.AddComponent<Spinner>().degreesPerSecond = new Vector3(0f, 60f, 0f);
            WorldKit.Prim(PrimitiveType.Cube, t, new Vector3(0f, 0.02f, -1.6f), new Vector3(4.2f, 0.02f, 2.6f), new Color(0.3f, 0.75f, 0.9f), false);
        }

        WorldKit.Label(t, new Vector3(0f, postHeight + 1.6f, -0.6f), title, locked ? new Color(1f, 0.6f, 0.5f) : new Color(0.7f, 1f, 1f), 0.11f);

        return Interactable.Create(t, new Vector3(0f, 0f, -2.2f), 3.2f, null, null);
    }

    // Builds a building exterior and returns the interactable placed at its door.
    public static Interactable BuildBuilding(Transform root, BuildingDef b, bool cleared)
    {
        GameObject building = WorldKit.Group(b.name, root, WorldKit.Flat(b.position), b.facing);
        Transform t = building.transform;
        float labelHeight;

        switch (b.style)
        {
            case BuildingStyle.Tent: labelHeight = BuildTent(t, b); break;
            case BuildingStyle.Hut: labelHeight = BuildHut(t, b); break;
            case BuildingStyle.Mushroom: labelHeight = BuildMushroom(t, b); break;
            case BuildingStyle.Dome: labelHeight = BuildDome(t, b); break;
            case BuildingStyle.Tower: labelHeight = BuildTower(t, b); break;
            case BuildingStyle.Arena: labelHeight = BuildArena(t, b); break;
            case BuildingStyle.Stump: labelHeight = BuildStump(t, b); break;
            default: labelHeight = BuildHouse(t, b); break;
        }

        float doorDepth = b.DoorDepth;
        bool isShop = b.type == BuildingType.Shop;
        Vector3 doorFront = new Vector3(0f, 0f, -doorDepth);

        WorldKit.Prim(PrimitiveType.Cube, t, doorFront + new Vector3(0f, 1.5f, -0.1f), new Vector3(2.4f, 3f, 0.25f), DoorColor, false);
        WorldKit.Prim(PrimitiveType.Cube, t, doorFront + new Vector3(0f, 3.1f, -0.15f), new Vector3(2.9f, 0.3f, 0.3f), b.trimColor, false);
        WorldKit.Prim(PrimitiveType.Cube, t, doorFront + new Vector3(0f, 0.01f, -1.2f), new Vector3(2.6f, 0.02f, 2f), isShop ? new Color(0.75f, 0.6f, 0.2f) : new Color(0.6f, 0.15f, 0.12f), false);
        BuildDoorIcon(t, doorFront + new Vector3(0f, 3.9f, -0.3f), isShop);

        string kind;
        if (isShop)
        {
            kind = "<color=#ffd34d>SHOP</color>";
        }
        else
        {
            kind = cleared ? "<color=#7dff8a>BATTLE - CLEARED</color>" : "<color=#ff6a5a>BATTLE</color>";
        }
        WorldKit.Label(t, new Vector3(0f, labelHeight + 1.8f, 0f), b.name + "\n" + kind, Color.white, 0.14f);

        return Interactable.Create(t, doorFront + new Vector3(0f, 0f, -1.5f), 2.8f, null, null);
    }

    static void BuildDoorIcon(Transform parent, Vector3 position, bool isShop)
    {
        if (isShop)
        {
            GameObject coin = WorldKit.Prim(PrimitiveType.Cylinder, parent, position, new Vector3(0.9f, 0.06f, 0.9f), ShopLabelColor, false);
            coin.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            WorldKit.Prim(PrimitiveType.Cube, coin.transform, new Vector3(0f, -1f, 0f), new Vector3(0.18f, 0.5f, 0.55f), new Color(0.75f, 0.55f, 0.1f), false);
        }
        else
        {
            GameObject swords = WorldKit.Group("Swords", parent, position);
            Color blade = new Color(0.85f, 0.85f, 0.9f);
            GameObject a = WorldKit.Prim(PrimitiveType.Cube, swords.transform, Vector3.zero, new Vector3(0.14f, 1.3f, 0.06f), blade, false);
            a.transform.localRotation = Quaternion.Euler(0f, 0f, 40f);
            GameObject b = WorldKit.Prim(PrimitiveType.Cube, swords.transform, Vector3.zero, new Vector3(0.14f, 1.3f, 0.06f), blade, false);
            b.transform.localRotation = Quaternion.Euler(0f, 0f, -40f);
            WorldKit.Prim(PrimitiveType.Sphere, swords.transform, new Vector3(0f, 0f, -0.05f), Vector3.one * 0.3f, BattleLabelColor, false);
        }
    }

    static void AddWindows(Transform t, BuildingDef b, float frontZ, float y, float spread)
    {
        for (int side = -1; side <= 1; side += 2)
        {
            Vector3 position = new Vector3(side * spread, y, frontZ - 0.05f);
            WorldKit.Prim(PrimitiveType.Cube, t, position, new Vector3(1.7f, 1.7f, 0.12f), b.trimColor, false);
            WorldKit.Prim(PrimitiveType.Cube, t, position + new Vector3(0f, 0f, -0.05f), new Vector3(1.3f, 1.3f, 0.12f), WindowColor, false);
        }
    }

    static float BuildHouse(Transform t, BuildingDef b)
    {
        float w = b.size.x;
        float d = b.size.y;
        const float height = 5f;

        WorldKit.Prim(PrimitiveType.Cube, t, new Vector3(0f, height * 0.5f, 0f), new Vector3(w, height, d), b.bodyColor);

        // A cube turned 45 degrees makes both roof slopes and the gable ends.
        float side = w / Mathf.Sqrt(2f) + 0.5f;
        GameObject roof = WorldKit.Prim(PrimitiveType.Cube, t, new Vector3(0f, height, 0f), new Vector3(side, side, d + 0.8f), b.roofColor, false);
        roof.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

        WorldKit.Prim(PrimitiveType.Cube, t, new Vector3(w * 0.25f, height + w * 0.3f, d * 0.2f), new Vector3(1.2f, 3f, 1.2f), b.trimColor, false);
        WorldKit.Prim(PrimitiveType.Cube, t, new Vector3(0f, 0.25f, 0f), new Vector3(w + 0.3f, 0.5f, d + 0.3f), b.trimColor, false);
        AddWindows(t, b, -d * 0.5f, 2.9f, w * 0.32f);

        return height + w * 0.5f;
    }

    static float BuildTent(Transform t, BuildingDef b)
    {
        float w = b.size.x;
        float d = b.size.y;
        const float height = 3f;

        WorldKit.Prim(PrimitiveType.Cube, t, new Vector3(0f, height * 0.5f, 0f), new Vector3(w, height, d), b.bodyColor);

        float side = w / Mathf.Sqrt(2f) + 0.6f;
        GameObject roof = WorldKit.Prim(PrimitiveType.Cube, t, new Vector3(0f, height, 0f), new Vector3(side, side, d + 0.6f), b.roofColor, false);
        roof.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

        // Stripes on the front.
        for (int i = -2; i <= 2; i++)
        {
            if (i == 0) continue;
            WorldKit.Prim(PrimitiveType.Cube, t, new Vector3(i * w * 0.18f, height * 0.5f, -d * 0.5f - 0.03f), new Vector3(0.6f, height, 0.05f), b.roofColor, false);
        }

        // Awning over the entrance on two poles.
        GameObject awning = WorldKit.Prim(PrimitiveType.Cube, t, new Vector3(0f, height + 0.1f, -d * 0.5f - 1.2f), new Vector3(4f, 0.12f, 2.6f), b.roofColor, false);
        awning.transform.localRotation = Quaternion.Euler(-15f, 0f, 0f);
        WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(-1.9f, height * 0.5f, -d * 0.5f - 2.3f), new Vector3(0.15f, height * 0.5f, 0.15f), b.trimColor);
        WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(1.9f, height * 0.5f, -d * 0.5f - 2.3f), new Vector3(0.15f, height * 0.5f, 0.15f), b.trimColor);

        float top = height + w * 0.5f;
        WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, top + 1f, 0f), new Vector3(0.12f, 1f, 0.12f), b.trimColor, false);
        WorldKit.Prim(PrimitiveType.Cube, t, new Vector3(0.6f, top + 1.6f, 0f), new Vector3(1.1f, 0.7f, 0.05f), b.bodyColor, false);

        return top + 1f;
    }

    static float BuildHut(Transform t, BuildingDef b)
    {
        float w = b.size.x;
        const float height = 3.5f;

        WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, height * 0.5f, 0f), new Vector3(w, height * 0.5f, w), b.bodyColor);
        WorldKit.Prim(PrimitiveType.Sphere, t, new Vector3(0f, height, 0f), new Vector3(w * 1.25f, w * 0.55f, w * 1.25f), b.roofColor, false);
        WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, height + w * 0.3f, 0f), new Vector3(0.6f, 0.6f, 0.6f), b.trimColor, false);

        return height + w * 0.3f;
    }

    static float BuildMushroom(Transform t, BuildingDef b)
    {
        float w = b.size.x;
        float stemRadius = w * 0.35f;
        const float stemHeight = 4f;
        float capY = stemHeight + 0.6f;
        float capRadius = w * 0.7f;
        float capHeight = w * 0.24f;

        WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, stemHeight * 0.5f, 0f), new Vector3(stemRadius * 2f, stemHeight * 0.5f, stemRadius * 2f), b.bodyColor);
        WorldKit.Prim(PrimitiveType.Sphere, t, new Vector3(0f, capY, 0f), new Vector3(capRadius * 2f, capHeight * 2f, capRadius * 2f), b.roofColor, false);

        // White spots sit on the cap's surface.
        for (int ring = 0; ring < 2; ring++)
        {
            float elevation = (ring == 0 ? 30f : 60f) * Mathf.Deg2Rad;
            int spots = ring == 0 ? 8 : 4;
            for (int i = 0; i < spots; i++)
            {
                float angle = (i + ring * 0.5f) * Mathf.PI * 2f / spots;
                Vector3 position = new Vector3(
                    Mathf.Cos(elevation) * Mathf.Cos(angle) * capRadius,
                    capY + Mathf.Sin(elevation) * capHeight,
                    Mathf.Cos(elevation) * Mathf.Sin(angle) * capRadius);
                WorldKit.Prim(PrimitiveType.Sphere, t, position, new Vector3(1.1f, 0.5f, 1.1f), Color.white, false);
            }
        }

        // Round window above the door.
        WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 3.6f, -stemRadius + 0.05f), new Vector3(0.9f, 0.05f, 0.9f), WindowColor, false)
            .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        return capY + capHeight;
    }

    static float BuildDome(Transform t, BuildingDef b)
    {
        float w = b.size.x;
        float d = b.size.y;
        float height = w * 0.45f;

        WorldKit.Prim(PrimitiveType.Sphere, t, Vector3.zero, new Vector3(w, height * 2f, d), b.bodyColor);

        // Bands around the dome, sized to the dome's radius at that height.
        for (int i = 1; i <= 2; i++)
        {
            float y = height * i / 3f;
            float scale = Mathf.Sqrt(1f - (y / height) * (y / height));
            WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, y, 0f), new Vector3(w * scale + 0.12f, 0.12f, d * scale + 0.12f), b.roofColor, false);
        }

        // Entrance tunnel.
        WorldKit.Prim(PrimitiveType.Cube, t, new Vector3(0f, 1.6f, -d * 0.5f), new Vector3(3.4f, 3.2f, 3f), b.bodyColor);
        WorldKit.Prim(PrimitiveType.Cube, t, new Vector3(0f, 3.3f, -d * 0.5f), new Vector3(3.6f, 0.3f, 3.1f), b.roofColor, false);
        WorldKit.Prim(PrimitiveType.Sphere, t, new Vector3(0f, height, 0f), Vector3.one * 1.2f, b.roofColor, false);

        return height + 0.5f;
    }

    static float BuildTower(Transform t, BuildingDef b)
    {
        float w = b.size.x;
        float height = 6f + w * 0.35f;

        WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, height * 0.5f, 0f), new Vector3(w, height * 0.5f, w), b.bodyColor);
        WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 0.3f, 0f), new Vector3(w + 0.4f, 0.3f, w + 0.4f), b.trimColor, false);

        // Stepped conical roof.
        float[] tiers = { 1.1f, 0.75f, 0.42f, 0.15f };
        for (int i = 0; i < tiers.Length; i++)
        {
            WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, height + 0.5f + i * 1.1f, 0f), new Vector3(w * tiers[i], 0.55f, w * tiers[i]), b.roofColor, false);
        }

        float top = height + 0.5f + tiers.Length * 1.1f;
        WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, top + 1f, 0f), new Vector3(0.12f, 1.2f, 0.12f), b.trimColor, false);
        WorldKit.Prim(PrimitiveType.Cube, t, new Vector3(0.65f, top + 1.7f, 0f), new Vector3(1.2f, 0.75f, 0.05f), b.roofColor, false);

        // Narrow windows up the front.
        for (int i = 1; i <= 2; i++)
        {
            WorldKit.Prim(PrimitiveType.Cube, t, new Vector3(0f, 2.5f + i * (height - 3f) / 2.5f, -w * 0.5f + 0.02f), new Vector3(0.8f, 1.4f, 0.2f), WindowColor, false);
        }

        return top;
    }

    static float BuildArena(Transform t, BuildingDef b)
    {
        float w = b.size.x;
        float radius = w * 0.5f;
        const float height = 4f;

        WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, height * 0.5f, 0f), new Vector3(w, height * 0.5f, w), b.bodyColor);
        WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, height, 0f), new Vector3(w + 0.6f, 0.2f, w + 0.6f), b.trimColor, false);
        WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, height + 0.05f, 0f), new Vector3(w * 0.8f, 0.22f, w * 0.8f), b.interiorFloorColor, false);

        const int pillars = 12;
        for (int i = 0; i < pillars; i++)
        {
            float angle = (i + 0.5f) * Mathf.PI * 2f / pillars;
            Vector3 position = new Vector3(Mathf.Sin(angle) * (radius + 0.3f), 2.8f, -Mathf.Cos(angle) * (radius + 0.3f));
            WorldKit.Prim(PrimitiveType.Cylinder, t, position, new Vector3(0.9f, 2.8f, 0.9f), b.trimColor);

            // Every third pillar carries a pennant.
            if (i % 3 == 1)
            {
                WorldKit.Prim(PrimitiveType.Cube, t, position + new Vector3(0f, 3.4f, 0f), new Vector3(0.12f, 1.2f, 0.12f), b.trimColor, false);
                WorldKit.Prim(PrimitiveType.Cube, t, position + new Vector3(0.45f, 3.7f, 0f), new Vector3(0.8f, 0.5f, 0.05f), b.roofColor, false);
            }
            else if (Mathf.Abs(Mathf.DeltaAngle((angle + 0.26f) * Mathf.Rad2Deg, 0f)) > 20f)
            {
                // Dark arches between pillars, except where the door is.
                GameObject arch = WorldKit.Prim(PrimitiveType.Cube, t, new Vector3(Mathf.Sin(angle + 0.26f) * radius, 1.8f, -Mathf.Cos(angle + 0.26f) * radius), new Vector3(1.4f, 2.2f, 0.3f), new Color(0.15f, 0.1f, 0.08f), false);
                arch.transform.localRotation = Quaternion.Euler(0f, -(angle + 0.26f) * Mathf.Rad2Deg, 0f);
            }
        }

        return height + 3f;
    }

    static float BuildStump(Transform t, BuildingDef b)
    {
        float w = b.size.x;
        const float height = 4.5f;

        WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, height * 0.5f, 0f), new Vector3(w, height * 0.5f, w), b.bodyColor);

        // Tree rings on top.
        float[] rings = { 0.94f, 0.7f, 0.45f, 0.2f };
        for (int i = 0; i < rings.Length; i++)
        {
            Color color = i % 2 == 0 ? b.roofColor : Color.Lerp(b.roofColor, b.bodyColor, 0.5f);
            WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, height + 0.02f + i * 0.02f, 0f), new Vector3(w * rings[i], 0.03f, w * rings[i]), color, false);
        }

        // Roots spreading out from the base.
        for (int i = 0; i < 6; i++)
        {
            float angle = (i + 0.5f) * 60f;
            if (Mathf.Abs(Mathf.DeltaAngle(angle, 180f)) < 40f) continue;  // leave the door clear
            GameObject root = WorldKit.Group("Root", t, Vector3.zero, angle);
            GameObject piece = WorldKit.Prim(PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.5f, w * 0.5f + 0.8f), new Vector3(1f, 1.4f, 1f), b.bodyColor, false);
            piece.transform.localRotation = Quaternion.Euler(70f, 0f, 0f);
        }

        // A few shelf mushrooms on the side.
        for (int i = 0; i < 3; i++)
        {
            float angle = (60f + i * 50f) * Mathf.Deg2Rad;
            WorldKit.Prim(PrimitiveType.Sphere, t, new Vector3(Mathf.Sin(angle) * w * 0.5f, 1.5f + i * 0.9f, Mathf.Cos(angle) * w * 0.5f), new Vector3(1.4f, 0.35f, 1.4f), new Color(0.95f, 0.75f, 0.35f), false);
        }

        return height;
    }
}

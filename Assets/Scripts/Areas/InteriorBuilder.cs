using System.Collections.Generic;
using UnityEngine;

// Builds the inside of a building: a walled room with an exit on the south wall.
public static class InteriorBuilder
{
    public const float WallHeight = 4.5f;
    public static readonly Vector2 ShopSize = new Vector2(24f, 18f);

    public struct Room
    {
        public Vector2 size;
        public Interactable exit;
        public GameObject exitBars;
        public Vector3 spawn;
    }

    static Room BuildRoom(Transform root, BuildingDef b, Vector2 size)
    {
        if (string.IsNullOrEmpty(b.interiorFloorMaterial))
        {
            AreaBuilder.BuildGround(root, size, null, b.interiorFloorColor);
            // Plank lines so plain wooden floors don't look flat.
            for (float x = -size.x * 0.5f + 1.5f; x < size.x * 0.5f; x += 1.5f)
            {
                WorldKit.Prim(PrimitiveType.Cube, root, new Vector3(x, 0.005f, 0f), new Vector3(0.05f, 0.01f, size.y), Color.Lerp(b.interiorFloorColor, Color.black, 0.25f), false);
            }
        }
        else
        {
            AreaBuilder.BuildGround(root, size, b.interiorFloorMaterial, b.interiorFloorColor, 6f);
        }

        AreaBuilder.BuildWalls(root, size, WallHeight, null, b.interiorWallColor);

        // Baseboard and a trim line along the three visible walls.
        float hx = size.x * 0.5f;
        float hz = size.y * 0.5f;
        Color trim = Color.Lerp(b.interiorWallColor, Color.black, 0.35f);
        WorldKit.Prim(PrimitiveType.Cube, root, new Vector3(0f, 0.3f, hz - 0.05f), new Vector3(size.x, 0.6f, 0.1f), trim, false);
        WorldKit.Prim(PrimitiveType.Cube, root, new Vector3(hx - 0.05f, 0.3f, 0f), new Vector3(0.1f, 0.6f, size.y), trim, false);
        WorldKit.Prim(PrimitiveType.Cube, root, new Vector3(-hx + 0.05f, 0.3f, 0f), new Vector3(0.1f, 0.6f, size.y), trim, false);
        WorldKit.Prim(PrimitiveType.Cube, root, new Vector3(0f, WallHeight - 0.2f, hz - 0.05f), new Vector3(size.x, 0.25f, 0.1f), trim, false);

        // The exit is a doormat by the (see-through) south wall.
        Vector3 exitPosition = new Vector3(0f, 0f, -hz + 1.2f);
        WorldKit.Prim(PrimitiveType.Cube, root, exitPosition + Vector3.up * 0.02f, new Vector3(3.2f, 0.02f, 1.8f), new Color(0.55f, 0.15f, 0.12f), false);
        WorldKit.Label(root, exitPosition + Vector3.up * 1.4f, "EXIT", new Color(1f, 0.95f, 0.8f), 0.1f);

        GameObject bars = WorldKit.Group("ExitBars", root, exitPosition);
        for (int i = 0; i < 6; i++)
        {
            WorldKit.Prim(PrimitiveType.Cube, bars.transform, new Vector3(-1.5f + i * 0.6f, 1f, -0.4f), new Vector3(0.15f, 2f, 0.15f), new Color(0.6f, 0.15f, 0.12f), false);
        }
        bars.SetActive(false);

        Room room;
        room.size = size;
        room.exit = Interactable.Create(root, exitPosition, 2.6f, null, null);
        room.exitBars = bars;
        room.spawn = new Vector3(0f, 1.1f, -hz + 4.5f);
        return room;
    }

    static void AddLight(Transform root, Vector3 position, Color color, float range, float intensity)
    {
        GameObject obj = new GameObject("RoomLight");
        obj.transform.SetParent(root, false);
        obj.transform.localPosition = position;
        Light light = obj.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.range = range;
        light.intensity = intensity;
    }

    public static Room BuildShop(Transform root, BuildingDef b, List<ItemPedestal> pedestals)
    {
        Room room = BuildRoom(root, b, ShopSize);
        float hz = ShopSize.y * 0.5f;
        Color wood = new Color(0.45f, 0.3f, 0.18f);

        // Counter with the shopkeeper behind it.
        WorldKit.Prim(PrimitiveType.Cube, root, new Vector3(0f, 0.6f, 4.5f), new Vector3(10f, 1.2f, 1.4f), wood);
        WorldKit.Prim(PrimitiveType.Cube, root, new Vector3(0f, 1.25f, 4.5f), new Vector3(10.4f, 0.12f, 1.7f), b.roofColor, false);
        WorldKit.Prim(PrimitiveType.Cylinder, root, new Vector3(3.5f, 1.45f, 4.5f), new Vector3(0.5f, 0.15f, 0.5f), new Color(1f, 0.82f, 0.2f), false);

        // 3D shopkeeper made in Blender (Blender/build_shopkeepers.py), one per shop id.
        GameObject keeper = Resources.Load<GameObject>("Shopkeepers/" + b.id);
        if (keeper != null)
        {
            GameObject npc = Object.Instantiate(keeper, root, false);
            npc.name = "Shopkeeper";
            npc.transform.localPosition = new Vector3(0f, 0f, 6.2f);
            npc.transform.localRotation = Quaternion.identity;
            npc.AddComponent<ShopkeeperIdle>();
        }
        else
        {
            GameObject npc = WorldKit.Prim(PrimitiveType.Capsule, root, new Vector3(0f, 1.6f, 6.2f), new Vector3(1f, 1f, 1f), b.bodyColor, false);
            npc.AddComponent<Bobber>().amplitude = 0.06f;
        }

        // Shelves full of goods along the back wall.
        for (int side = -1; side <= 1; side += 2)
        {
            for (int shelf = 0; shelf < 3; shelf++)
            {
                float y = 1.1f + shelf * 1.05f;
                WorldKit.Prim(PrimitiveType.Cube, root, new Vector3(side * 7.5f, y, hz - 0.6f), new Vector3(6f, 0.15f, 0.9f), wood, false);
                for (int item = 0; item < 5; item++)
                {
                    Color color = Color.HSVToRGB(((item * 0.21f) + shelf * 0.13f + side * 0.07f + 1f) % 1f, 0.6f, 0.9f);
                    Vector3 position = new Vector3(side * 7.5f - 2.2f + item * 1.1f, y + 0.35f, hz - 0.6f);
                    PrimitiveType shape = (item + shelf) % 2 == 0 ? PrimitiveType.Sphere : PrimitiveType.Cube;
                    WorldKit.Prim(shape, root, position, Vector3.one * 0.5f, color, false);
                }
            }
        }

        WorldKit.Prim(PrimitiveType.Cube, root, new Vector3(0f, 0.01f, -1f), new Vector3(Mathf.Max(8f, b.stock.Length * 4.5f + 2f), 0.01f, 4.5f), Color.Lerp(b.roofColor, Color.white, 0.25f), false);

        for (int i = 0; i < b.stock.Length; i++)
        {
            float x = (i - (b.stock.Length - 1) * 0.5f) * 4.5f;
            pedestals.Add(ItemPedestal.Create(root, new Vector3(x, 0f, -1f), b.stock[i]));
        }

        AddLight(root, new Vector3(0f, 4f, 0f), new Color(1f, 0.92f, 0.78f), 24f, 1.3f);
        AddLight(root, new Vector3(0f, 3f, 6f), new Color(1f, 0.85f, 0.6f), 10f, 1f);
        return room;
    }

    public static Room BuildBattle(Transform root, BuildingDef b, AreaTheme theme)
    {
        BattleDef battle = b.battle;
        Room room = BuildRoom(root, b, battle.arenaSize);
        float hx = battle.arenaSize.x * 0.5f;
        float hz = battle.arenaSize.y * 0.5f;

        Color pillarColor = Color.Lerp(b.interiorWallColor, Color.black, 0.3f);
        foreach (Vector2 point in CoverLayout(battle.coverPillars, hx, hz))
        {
            BuildCover(root, new Vector3(point.x, 0f, point.y), theme, pillarColor);
        }

        // Braziers in the corners light the fight.
        Color fire = theme == AreaTheme.Snow ? new Color(0.5f, 0.8f, 1f) : new Color(1f, 0.55f, 0.15f);
        Vector2[] corners = { new Vector2(-hx + 1.5f, hz - 1.5f), new Vector2(hx - 1.5f, hz - 1.5f), new Vector2(-hx + 1.5f, -hz + 3f), new Vector2(hx - 1.5f, -hz + 3f) };
        foreach (Vector2 corner in corners)
        {
            BuildBrazier(root, new Vector3(corner.x, 0f, corner.y), fire);
        }

        // Title painted across the north wall, which always faces the camera.
        TextMesh title = WorldKit.Label(root, new Vector3(0f, WallHeight - 1.3f, hz - 0.12f), b.name.ToUpperInvariant(), new Color(1f, 0.9f, 0.7f), 0.16f, false);
        title.fontStyle = FontStyle.Bold;
        for (int side = -1; side <= 1; side += 2)
        {
            WorldKit.Prim(PrimitiveType.Cube, root, new Vector3(side * hx * 0.55f, WallHeight - 1.8f, hz - 0.1f), new Vector3(1.4f, 2.8f, 0.05f), b.roofColor, false);
        }

        AddLight(root, new Vector3(0f, 6f, 0f), Color.Lerp(fire, Color.white, 0.6f), Mathf.Max(hx, hz) * 2.2f, 1.1f);
        return room;
    }

    static IEnumerable<Vector2> CoverLayout(int count, float hx, float hz)
    {
        yield return new Vector2(-hx * 0.45f, hz * 0.3f);
        yield return new Vector2(hx * 0.45f, hz * 0.3f);
        yield return new Vector2(-hx * 0.45f, -hz * 0.25f);
        yield return new Vector2(hx * 0.45f, -hz * 0.25f);
        if (count >= 6)
        {
            yield return new Vector2(0f, hz * 0.55f);
            yield return new Vector2(0f, -hz * 0.05f);
        }
    }

    static void BuildCover(Transform root, Vector3 position, AreaTheme theme, Color color)
    {
        GameObject cover = WorldKit.Group("Cover", root, position);
        Transform t = cover.transform;

        switch (theme)
        {
            case AreaTheme.Forest:
                WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 0.9f, 0f), new Vector3(2.2f, 0.9f, 2.2f), new Color(0.42f, 0.28f, 0.16f));
                WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 1.82f, 0f), new Vector3(1.9f, 0.02f, 1.9f), new Color(0.75f, 0.6f, 0.4f), false);
                break;
            case AreaTheme.Desert:
                WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 2f, 0f), new Vector3(1.6f, 2f, 1.6f), new Color(0.85f, 0.72f, 0.5f));
                WorldKit.Prim(PrimitiveType.Cube, t, new Vector3(0f, 4.1f, 0f), new Vector3(2.2f, 0.4f, 2.2f), new Color(0.75f, 0.6f, 0.4f), false);
                break;
            case AreaTheme.Snow:
                GameObject ice = WorldKit.Prim(PrimitiveType.Cube, t, new Vector3(0f, 1.2f, 0f), new Vector3(2.2f, 2.4f, 2.2f), new Color(0.65f, 0.88f, 1f));
                ice.transform.localRotation = Quaternion.Euler(0f, 30f, 0f);
                break;
            case AreaTheme.Volcano:
                WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 1.5f, 0f), new Vector3(2f, 1.5f, 2f), new Color(0.12f, 0.1f, 0.12f));
                WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 3.02f, 0f), new Vector3(1.6f, 0.02f, 1.6f), new Color(1f, 0.45f, 0.1f), false);
                break;
            default:
                WorldKit.Prim(PrimitiveType.Cube, t, new Vector3(0f, 0.75f, 0f), new Vector3(2.4f, 1.5f, 1.4f), new Color(0.88f, 0.75f, 0.35f));
                WorldKit.Prim(PrimitiveType.Cube, t, new Vector3(0.3f, 1.9f, 0f), new Vector3(1.6f, 0.8f, 1.2f), new Color(0.85f, 0.72f, 0.32f));
                break;
        }
    }

    static void BuildBrazier(Transform root, Vector3 position, Color fire)
    {
        GameObject brazier = WorldKit.Group("Brazier", root, position);
        Transform t = brazier.transform;
        WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 0.6f, 0f), new Vector3(0.7f, 0.6f, 0.7f), new Color(0.3f, 0.28f, 0.28f));
        WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 1.25f, 0f), new Vector3(1.2f, 0.1f, 1.2f), new Color(0.25f, 0.23f, 0.23f), false);
        GameObject flame = WorldKit.Prim(PrimitiveType.Sphere, t, new Vector3(0f, 1.7f, 0f), new Vector3(0.7f, 1f, 0.7f), fire, false);

        GameObject lightObj = new GameObject("Light");
        lightObj.transform.SetParent(t, false);
        lightObj.transform.localPosition = new Vector3(0f, 2.2f, 0f);
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = fire;
        light.range = 10f;

        Flicker flicker = flame.AddComponent<Flicker>();
        flicker.targetLight = light;
        flicker.baseIntensity = 1.5f;
    }
}

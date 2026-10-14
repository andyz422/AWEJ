using System;
using System.Collections.Generic;
using UnityEngine;

// Theme scenery for an area. Props are scattered with a seeded random so every visit looks the same,
// and they keep clear of buildings, doors, paths, gates and hazards.
public class AreaDecor
{
    private readonly AreaDef area;
    private readonly Transform root;
    private readonly System.Random rng;
    private readonly List<Rect> blocked = new List<Rect>();

    public AreaDecor(AreaDef area, Transform root)
    {
        this.area = area;
        this.root = WorldKit.Group("Decor", root, Vector3.zero).transform;
        rng = new System.Random(area.seed);
        BlockLayout();
    }

    public void Build()
    {
        switch (area.theme)
        {
            case AreaTheme.Town: BuildTown(); break;
            case AreaTheme.Forest: BuildForest(); break;
            case AreaTheme.Desert: BuildDesert(); break;
            case AreaTheme.Snow: BuildSnow(); break;
            case AreaTheme.Volcano: BuildVolcano(); break;
        }
    }

    // ---------------------------------------------------------------- placement

    void BlockLayout()
    {
        foreach (BuildingDef building in area.buildings)
        {
            float radius = Mathf.Max(building.size.x, building.size.y) * 0.5f + 2.5f;
            Block(building.position, radius, radius);

            // Keep the approach to the door open.
            Vector3 near = WorldKit.Flat(building.position) + building.Front * building.DoorDepth;
            Vector3 far = near + building.Front * 8f;
            Block(new Vector2((near.x + far.x) * 0.5f, (near.z + far.z) * 0.5f), Mathf.Abs(far.x - near.x) * 0.5f + 3.5f, Mathf.Abs(far.z - near.z) * 0.5f + 3.5f);
        }

        foreach (PathDef path in area.paths)
        {
            Rect rect = AreaBuilder.PathRect(path);
            Block(rect.center, rect.width * 0.5f + 1.5f, rect.height * 0.5f + 1.5f);
        }

        foreach (HazardDef hazard in area.hazards)
        {
            if (hazard.radius > 0f)
            {
                Block(hazard.center, hazard.radius + 2f, hazard.radius + 2f);
            }
            else
            {
                Block(hazard.center, hazard.rectSize.x * 0.5f + 2f, hazard.rectSize.y * 0.5f + 2f);
            }
        }

        BlockGate(area.exitSide, area.exitOffset);
        BlockGate(area.entrySide, area.entryOffset);
        Block(area.startPosition, 5f, 5f);
    }

    void BlockGate(WallSide side, float offset)
    {
        Vector3 point = area.WallPoint(side, offset);
        Block(new Vector2(point.x, point.z), 8f, 8f);
    }

    void Block(Vector2 center, float halfX, float halfZ)
    {
        blocked.Add(new Rect(center.x - halfX, center.y - halfZ, halfX * 2f, halfZ * 2f));
    }

    float Range(float min, float max)
    {
        return min + (float)rng.NextDouble() * (max - min);
    }

    bool Chance(float probability)
    {
        return rng.NextDouble() < probability;
    }

    bool TryFind(float radius, out Vector2 point)
    {
        float hx = area.size.x * 0.5f - radius - 1.5f;
        float hz = area.size.y * 0.5f - radius - 1.5f;

        for (int attempt = 0; attempt < 40; attempt++)
        {
            Vector2 candidate = new Vector2(Range(-hx, hx), Range(-hz, hz));
            Rect footprint = new Rect(candidate.x - radius, candidate.y - radius, radius * 2f, radius * 2f);

            bool free = true;
            foreach (Rect rect in blocked)
            {
                if (rect.Overlaps(footprint))
                {
                    free = false;
                    break;
                }
            }

            if (free)
            {
                blocked.Add(footprint);
                point = candidate;
                return true;
            }
        }

        point = Vector2.zero;
        return false;
    }

    void Scatter(int count, float radius, Action<Vector3> make)
    {
        for (int i = 0; i < count; i++)
        {
            Vector2 point;
            if (TryFind(radius, out point))
            {
                make(WorldKit.Flat(point));
            }
        }
    }

    // ---------------------------------------------------------------- themes

    void BuildTown()
    {
        Material plaza = WorldKit.Textured("M_YFGM_GroundStones01", Vector2.one * 3f, new Color(0.55f, 0.55f, 0.55f));
        WorldKit.Prim(PrimitiveType.Cylinder, root, new Vector3(0f, 0.03f, 0f), new Vector3(24f, 0.02f, 24f), plaza, false);
        Block(Vector2.zero, 12f, 12f);
        Fountain(new Vector3(0f, 0f, 0f));

        float[] lampZ = { -42f, -28f, 28f, 42f };
        foreach (float z in lampZ)
        {
            Lamp(new Vector3(-5.5f, 0f, z));
            Lamp(new Vector3(5.5f, 0f, z));
        }

        Signpost(new Vector3(7f, 0f, -14f), "North Gate:\nWhispering Grove");
        Block(new Vector2(7f, -14f), 2.5f, 1.5f);

        Well(new Vector3(-12f, 0f, -38f));
        Block(new Vector2(-12f, -38f), 3f, 3f);

        Color[] leaves = { new Color(0.25f, 0.55f, 0.2f), new Color(0.35f, 0.6f, 0.25f), new Color(0.2f, 0.45f, 0.18f) };
        Scatter(28, 2.6f, p => Tree(p, leaves[rng.Next(leaves.Length)], 1f));
        Scatter(10, 1f, Crate);
        Scatter(8, 0.8f, Barrel);
        Scatter(6, 1.2f, HayBale);
        Scatter(18, 0.8f, Flowers);
    }

    void BuildForest()
    {
        Pond(new Vector3(-40f, 0f, -10f), 9f, new Color(0.2f, 0.45f, 0.5f), true);
        Block(new Vector2(-40f, -10f), 11f, 11f);

        AncientTree(new Vector3(-50f, 0f, 32f));
        Block(new Vector2(-50f, 32f), 8f, 8f);

        Color[] leaves = { new Color(0.12f, 0.4f, 0.15f), new Color(0.2f, 0.5f, 0.18f), new Color(0.1f, 0.32f, 0.12f), new Color(0.3f, 0.5f, 0.15f) };
        Scatter(70, 2.3f, p => Tree(p, leaves[rng.Next(leaves.Length)], Range(1f, 1.5f)));
        Scatter(25, 0.6f, SmallMushrooms);
        Scatter(14, 1.4f, p => Rock(p, new Color(0.4f, 0.42f, 0.38f), true));
        Scatter(10, 2f, Log);
        Scatter(20, 1.2f, p => Bush(p, leaves[rng.Next(leaves.Length)]));

        AmbientParticles fireflies = WorldKit.Group("Fireflies", root, Vector3.zero).AddComponent<AmbientParticles>();
        fireflies.count = 60;
        fireflies.color = new Color(1f, 0.95f, 0.45f);
        fireflies.drift = Vector3.zero;
        fireflies.wobble = 1.2f;
        fireflies.moteSize = 0.15f;
        fireflies.boxSize = new Vector3(40f, 5f, 40f);
    }

    void BuildDesert()
    {
        Ziggurat(new Vector3(28f, 0f, 26f));
        Block(new Vector2(28f, 26f), 9f, 9f);

        Pond(new Vector3(25f, 0f, -45f), 7f, new Color(0.2f, 0.6f, 0.7f), false);
        for (int i = 0; i < 6; i++)
        {
            float angle = i * Mathf.PI * 2f / 6f + 0.3f;
            Palm(new Vector3(25f + Mathf.Cos(angle) * 9.5f, 0f, -45f + Mathf.Sin(angle) * 9.5f));
        }
        Block(new Vector2(25f, -45f), 11f, 11f);

        Scatter(12, 6f, Dune);
        Scatter(35, 1.4f, Cactus);
        Scatter(5, 1f, Obelisk);
        Scatter(6, 0.8f, Skull);
        Scatter(10, 1.4f, p => Rock(p, new Color(0.7f, 0.5f, 0.35f), false));

        AmbientParticles dust = WorldKit.Group("Dust", root, Vector3.zero).AddComponent<AmbientParticles>();
        dust.count = 70;
        dust.color = new Color(0.95f, 0.85f, 0.6f);
        dust.drift = new Vector3(3f, 0f, 0.5f);
        dust.wobble = 0.4f;
        dust.moteSize = 0.07f;
        dust.boxSize = new Vector3(40f, 4f, 40f);
    }

    void BuildSnow()
    {
        FrozenLake(new Vector3(-32f, 0f, -40f), 10f);
        Block(new Vector2(-32f, -40f), 11f, 11f);

        Scatter(60, 2.3f, Pine);
        Scatter(6, 1.2f, Snowman);
        Scatter(10, 1.6f, IceSpikes);
        Scatter(12, 1.4f, SnowRock);

        AmbientParticles snow = WorldKit.Group("Snowfall", root, Vector3.zero).AddComponent<AmbientParticles>();
        snow.count = 160;
        snow.color = Color.white;
        snow.drift = new Vector3(0.6f, -2f, 0.2f);
        snow.wobble = 0.6f;
        snow.moteSize = 0.13f;
    }

    void BuildVolcano()
    {
        Volcano(new Vector3(-46f, 0f, 50f));
        Block(new Vector2(-46f, 50f), 11f, 11f);

        float[] brazierZ = { -15f, 8f, 30f, 50f };
        foreach (float z in brazierZ)
        {
            Brazier(new Vector3(-5f, 0f, z));
            Brazier(new Vector3(5f, 0f, z));
        }

        Scatter(14, 2f, Basalt);
        Scatter(16, 2f, DeadTree);
        Scatter(14, 1.4f, p => Rock(p, new Color(0.18f, 0.15f, 0.15f), true));

        AmbientParticles embers = WorldKit.Group("Embers", root, Vector3.zero).AddComponent<AmbientParticles>();
        embers.count = 100;
        embers.color = new Color(1f, 0.5f, 0.15f);
        embers.drift = new Vector3(0.2f, 1.5f, 0f);
        embers.wobble = 0.8f;
        embers.moteSize = 0.09f;
    }

    // ---------------------------------------------------------------- props

    GameObject Prop(string name, Vector3 position)
    {
        return WorldKit.Group(name, root, position, Range(0f, 360f));
    }

    void Tree(Vector3 position, Color leaf, float scale)
    {
        Transform t = Prop("Tree", position).transform;
        float trunk = Range(2.4f, 3.4f) * scale;
        WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, trunk * 0.5f, 0f), new Vector3(0.6f * scale, trunk * 0.5f, 0.6f * scale), new Color(0.4f, 0.27f, 0.15f));

        int blobs = rng.Next(1, 4);
        for (int i = 0; i < blobs; i++)
        {
            float size = Range(2.4f, 3.4f) * scale;
            Vector3 offset = new Vector3(Range(-0.8f, 0.8f), trunk + size * 0.3f + i * 0.6f, Range(-0.8f, 0.8f)) ;
            WorldKit.Prim(PrimitiveType.Sphere, t, offset, Vector3.one * size, Color.Lerp(leaf, Color.black, i * 0.08f), false);
        }
    }

    void AncientTree(Vector3 position)
    {
        Transform t = Prop("AncientTree", position).transform;
        Color bark = new Color(0.32f, 0.22f, 0.13f);
        WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 6f, 0f), new Vector3(3.5f, 6f, 3.5f), bark);
        for (int i = 0; i < 5; i++)
        {
            GameObject arm = WorldKit.Group("Root", t, Vector3.zero, i * 72f);
            WorldKit.Prim(PrimitiveType.Cylinder, arm.transform, new Vector3(0f, 0.6f, 2.4f), new Vector3(1.2f, 1.5f, 1.2f), bark, false).transform.localRotation = Quaternion.Euler(65f, 0f, 0f);
        }

        Color[] leaves = { new Color(0.1f, 0.35f, 0.15f), new Color(0.18f, 0.45f, 0.2f) };
        for (int i = 0; i < 6; i++)
        {
            float angle = i * 60f * Mathf.Deg2Rad;
            WorldKit.Prim(PrimitiveType.Sphere, t, new Vector3(Mathf.Cos(angle) * 3.5f, 13f + (i % 2) * 1.5f, Mathf.Sin(angle) * 3.5f), Vector3.one * 7f, leaves[i % 2], false);
        }
        WorldKit.Prim(PrimitiveType.Sphere, t, new Vector3(0f, 16f, 0f), Vector3.one * 7.5f, leaves[0], false);

        GameObject glow = WorldKit.Prim(PrimitiveType.Sphere, t, new Vector3(0f, 3f, -1.8f), new Vector3(1.2f, 1.6f, 0.4f), new Color(0.6f, 1f, 0.7f), false);
        glow.AddComponent<Bobber>().amplitude = 0.1f;
    }

    void Pine(Vector3 position)
    {
        Transform t = Prop("Pine", position).transform;
        float scale = Range(0.8f, 1.4f);
        WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 1f * scale, 0f), new Vector3(0.5f, 1f, 0.5f) * scale, new Color(0.35f, 0.24f, 0.15f));

        Color needles = new Color(0.12f, 0.32f, 0.22f);
        float[] tiers = { 4.2f, 3.3f, 2.4f, 1.4f };
        for (int i = 0; i < tiers.Length; i++)
        {
            float y = (1.8f + i * 1.1f) * scale;
            float diameter = tiers[i] * scale;
            WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, y, 0f), new Vector3(diameter, 0.5f * scale, diameter), needles, false);
            WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, y + 0.5f * scale, 0f), new Vector3(diameter * 0.8f, 0.1f, diameter * 0.8f), Color.white, false);
        }
        WorldKit.Prim(PrimitiveType.Sphere, t, new Vector3(0f, (1.8f + tiers.Length * 1.1f) * scale, 0f), Vector3.one * 0.6f * scale, Color.white, false);
    }

    void Palm(Vector3 position)
    {
        Transform t = Prop("Palm", position).transform;
        Color bark = new Color(0.55f, 0.4f, 0.25f);
        Vector3 top = Vector3.zero;
        for (int i = 0; i < 4; i++)
        {
            GameObject segment = WorldKit.Prim(PrimitiveType.Cylinder, t, top + new Vector3(0.12f, 0.8f, 0f), new Vector3(0.55f - i * 0.05f, 0.8f, 0.55f - i * 0.05f), bark, i == 0);
            segment.transform.localRotation = Quaternion.Euler(0f, 0f, -4f * i);
            top += new Vector3(0.25f, 1.55f, 0f);
        }

        for (int i = 0; i < 6; i++)
        {
            GameObject frond = WorldKit.Group("Frond", t, top, i * 60f);
            GameObject leaf = WorldKit.Prim(PrimitiveType.Cube, frond.transform, new Vector3(0f, -0.3f, 1.4f), new Vector3(0.8f, 0.08f, 3f), new Color(0.2f, 0.55f, 0.2f), false);
            leaf.transform.localRotation = Quaternion.Euler(25f, 0f, 0f);
        }
        WorldKit.Prim(PrimitiveType.Sphere, t, top + new Vector3(0.3f, -0.4f, 0f), Vector3.one * 0.45f, new Color(0.4f, 0.25f, 0.1f), false);
        WorldKit.Prim(PrimitiveType.Sphere, t, top + new Vector3(-0.2f, -0.45f, 0.3f), Vector3.one * 0.45f, new Color(0.4f, 0.25f, 0.1f), false);
    }

    void DeadTree(Vector3 position)
    {
        Transform t = Prop("DeadTree", position).transform;
        Color bark = new Color(0.12f, 0.1f, 0.1f);
        float height = Range(3f, 5f);
        WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, height * 0.5f, 0f), new Vector3(0.6f, height * 0.5f, 0.6f), bark);

        for (int i = 0; i < 3; i++)
        {
            GameObject branch = WorldKit.Group("Branch", t, new Vector3(0f, height * (0.5f + i * 0.18f), 0f), i * 120f + Range(-20f, 20f));
            WorldKit.Prim(PrimitiveType.Cylinder, branch.transform, new Vector3(0f, 0.5f, 0.5f), new Vector3(0.22f, 0.8f, 0.22f), bark, false).transform.localRotation = Quaternion.Euler(Range(35f, 60f), 0f, 0f);
        }
    }

    void Cactus(Vector3 position)
    {
        Transform t = Prop("Cactus", position).transform;
        Color green = new Color(0.25f, 0.55f, 0.3f);
        float height = Range(2.6f, 4.4f);
        WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, height * 0.5f, 0f), new Vector3(0.9f, height * 0.5f, 0.9f), green);
        WorldKit.Prim(PrimitiveType.Sphere, t, new Vector3(0f, height, 0f), Vector3.one * 0.9f, green, false);

        for (int side = -1; side <= 1; side += 2)
        {
            if (!Chance(0.7f)) continue;
            float y = Range(height * 0.35f, height * 0.65f);
            GameObject arm = WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(side * 0.75f, y, 0f), new Vector3(0.55f, 0.45f, 0.55f), green, false);
            arm.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(side * 1.15f, y + 0.6f, 0f), new Vector3(0.55f, 0.6f, 0.55f), green, false);
            WorldKit.Prim(PrimitiveType.Sphere, t, new Vector3(side * 1.15f, y + 1.2f, 0f), Vector3.one * 0.55f, green, false);
        }

        if (Chance(0.4f))
        {
            WorldKit.Prim(PrimitiveType.Sphere, t, new Vector3(0f, height + 0.45f, 0f), Vector3.one * 0.4f, new Color(1f, 0.45f, 0.65f), false);
        }
    }

    void Rock(Vector3 position, Color color, bool mossy)
    {
        Transform t = Prop("Rock", position).transform;
        Vector3 scale = new Vector3(Range(1.4f, 2.8f), Range(0.9f, 1.8f), Range(1.4f, 2.6f));
        WorldKit.Prim(PrimitiveType.Sphere, t, new Vector3(0f, scale.y * 0.3f, 0f), scale, color);
        if (mossy && Chance(0.5f))
        {
            WorldKit.Prim(PrimitiveType.Sphere, t, new Vector3(0f, scale.y * 0.65f, 0f), new Vector3(scale.x * 0.7f, 0.3f, scale.z * 0.7f), new Color(0.3f, 0.5f, 0.2f), false);
        }
    }

    void SnowRock(Vector3 position)
    {
        Transform t = Prop("SnowRock", position).transform;
        Vector3 scale = new Vector3(Range(1.6f, 3f), Range(1f, 1.8f), Range(1.6f, 2.8f));
        WorldKit.Prim(PrimitiveType.Sphere, t, new Vector3(0f, scale.y * 0.3f, 0f), scale, new Color(0.45f, 0.48f, 0.55f));
        WorldKit.Prim(PrimitiveType.Sphere, t, new Vector3(0f, scale.y * 0.68f, 0f), new Vector3(scale.x * 0.8f, 0.4f, scale.z * 0.8f), Color.white, false);
    }

    void SmallMushrooms(Vector3 position)
    {
        Transform t = Prop("Mushrooms", position).transform;
        Color[] caps = { new Color(0.9f, 0.2f, 0.15f), new Color(0.65f, 0.35f, 0.85f), new Color(0.95f, 0.6f, 0.2f) };
        Color cap = caps[rng.Next(caps.Length)];
        int count = rng.Next(2, 5);
        for (int i = 0; i < count; i++)
        {
            Vector3 offset = new Vector3(Range(-0.5f, 0.5f), 0f, Range(-0.5f, 0.5f));
            float size = Range(0.5f, 1f);
            WorldKit.Prim(PrimitiveType.Cylinder, t, offset + new Vector3(0f, 0.25f * size, 0f), new Vector3(0.18f, 0.25f, 0.18f) * size, new Color(0.95f, 0.92f, 0.85f), false);
            WorldKit.Prim(PrimitiveType.Sphere, t, offset + new Vector3(0f, 0.5f * size, 0f), new Vector3(0.7f, 0.35f, 0.7f) * size, cap, false);
        }
    }

    void Log(Vector3 position)
    {
        Transform t = Prop("Log", position).transform;
        GameObject log = WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 0.45f, 0f), new Vector3(0.9f, Range(1.4f, 2f), 0.9f), new Color(0.38f, 0.26f, 0.15f));
        log.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        WorldKit.Prim(PrimitiveType.Sphere, t, new Vector3(Range(-0.8f, 0.8f), 0.85f, 0f), new Vector3(1f, 0.3f, 0.7f), new Color(0.3f, 0.5f, 0.2f), false);
    }

    void Bush(Vector3 position, Color leaf)
    {
        Transform t = Prop("Bush", position).transform;
        int blobs = rng.Next(2, 4);
        for (int i = 0; i < blobs; i++)
        {
            float size = Range(0.9f, 1.5f);
            WorldKit.Prim(PrimitiveType.Sphere, t, new Vector3(Range(-0.5f, 0.5f), size * 0.35f, Range(-0.5f, 0.5f)), new Vector3(size, size * 0.75f, size), leaf, false);
        }
    }

    void Crate(Vector3 position)
    {
        Transform t = Prop("Crate", position).transform;
        float size = Range(0.9f, 1.2f);
        WorldKit.Prim(PrimitiveType.Cube, t, new Vector3(0f, size * 0.5f, 0f), Vector3.one * size, new Color(0.6f, 0.42f, 0.22f));
        WorldKit.Prim(PrimitiveType.Cube, t, new Vector3(0f, size * 0.5f, 0f), new Vector3(size * 1.02f, size * 0.15f, size * 1.02f), new Color(0.4f, 0.27f, 0.14f), false);
        if (Chance(0.4f))
        {
            WorldKit.Prim(PrimitiveType.Cube, t, new Vector3(0f, size * 1.35f, 0f), Vector3.one * size * 0.7f, new Color(0.62f, 0.45f, 0.25f));
        }
    }

    void Barrel(Vector3 position)
    {
        Transform t = Prop("Barrel", position).transform;
        WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 0.6f, 0f), new Vector3(0.9f, 0.6f, 0.9f), new Color(0.5f, 0.32f, 0.18f));
        WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 0.3f, 0f), new Vector3(0.95f, 0.05f, 0.95f), new Color(0.25f, 0.25f, 0.27f), false);
        WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 0.9f, 0f), new Vector3(0.95f, 0.05f, 0.95f), new Color(0.25f, 0.25f, 0.27f), false);
    }

    void HayBale(Vector3 position)
    {
        Transform t = Prop("HayBale", position).transform;
        GameObject bale = WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 0.7f, 0f), new Vector3(1.4f, 0.8f, 1.4f), new Color(0.88f, 0.75f, 0.35f));
        bale.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
    }

    void Flowers(Vector3 position)
    {
        Transform t = Prop("Flowers", position).transform;
        Color[] petals = { new Color(1f, 0.35f, 0.4f), new Color(1f, 0.85f, 0.3f), new Color(0.7f, 0.45f, 1f), Color.white };
        int count = rng.Next(4, 8);
        for (int i = 0; i < count; i++)
        {
            WorldKit.Prim(PrimitiveType.Sphere, t, new Vector3(Range(-0.6f, 0.6f), 0.2f, Range(-0.6f, 0.6f)), Vector3.one * 0.28f, petals[rng.Next(petals.Length)], false);
        }
    }

    void Lamp(Vector3 position)
    {
        Transform t = WorldKit.Group("Lamp", root, position).transform;
        WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 1.8f, 0f), new Vector3(0.25f, 1.8f, 0.25f), new Color(0.15f, 0.15f, 0.17f));
        WorldKit.Prim(PrimitiveType.Sphere, t, new Vector3(0f, 3.85f, 0f), Vector3.one * 0.7f, new Color(1f, 0.92f, 0.6f), false);
        WorldKit.Prim(PrimitiveType.Cube, t, new Vector3(0f, 4.25f, 0f), new Vector3(0.8f, 0.12f, 0.8f), new Color(0.15f, 0.15f, 0.17f), false);
    }

    void Signpost(Vector3 position, string text)
    {
        Transform t = WorldKit.Group("Signpost", root, position).transform;
        Color wood = new Color(0.5f, 0.35f, 0.2f);
        WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 1.4f, 0f), new Vector3(0.3f, 1.4f, 0.3f), wood);
        WorldKit.Prim(PrimitiveType.Cube, t, new Vector3(0f, 2.6f, 0f), new Vector3(3.8f, 1.2f, 0.15f), new Color(0.65f, 0.48f, 0.28f), false);
        TextMesh label = WorldKit.Label(t, new Vector3(0f, 2.6f, -0.1f), text, new Color(0.2f, 0.12f, 0.05f), 0.055f, false);
        label.fontStyle = FontStyle.Bold;
    }

    void Well(Vector3 position)
    {
        Transform t = WorldKit.Group("Well", root, position).transform;
        Color stone = new Color(0.55f, 0.52f, 0.5f);
        Color wood = new Color(0.45f, 0.3f, 0.18f);
        WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 0.5f, 0f), new Vector3(3f, 0.5f, 3f), stone);
        WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 1.01f, 0f), new Vector3(2.3f, 0.01f, 2.3f), new Color(0.1f, 0.2f, 0.3f), false);
        WorldKit.Prim(PrimitiveType.Cube, t, new Vector3(-1.3f, 1.8f, 0f), new Vector3(0.25f, 1.6f, 0.25f), wood, false);
        WorldKit.Prim(PrimitiveType.Cube, t, new Vector3(1.3f, 1.8f, 0f), new Vector3(0.25f, 1.6f, 0.25f), wood, false);
        GameObject roof = WorldKit.Prim(PrimitiveType.Cube, t, new Vector3(0f, 2.8f, 0f), new Vector3(2.4f, 2.4f, 1.6f), new Color(0.6f, 0.25f, 0.2f), false);
        roof.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
    }

    void Fountain(Vector3 position)
    {
        Transform t = WorldKit.Group("Fountain", root, position).transform;
        Color stone = new Color(0.7f, 0.68f, 0.65f);
        Color water = new Color(0.35f, 0.6f, 0.9f);
        WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 0.45f, 0f), new Vector3(7f, 0.45f, 7f), stone);
        WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 0.91f, 0f), new Vector3(6.2f, 0.01f, 6.2f), water, false);
        WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 1.6f, 0f), new Vector3(0.8f, 1f, 0.8f), stone, false);
        WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 2.6f, 0f), new Vector3(2.6f, 0.15f, 2.6f), stone, false);
        WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 2.76f, 0f), new Vector3(2.2f, 0.01f, 2.2f), water, false);
        GameObject spout = WorldKit.Prim(PrimitiveType.Sphere, t, new Vector3(0f, 3.3f, 0f), new Vector3(0.6f, 0.9f, 0.6f), new Color(0.6f, 0.8f, 1f), false);
        spout.AddComponent<Bobber>().amplitude = 0.15f;
    }

    void Pond(Vector3 position, float radius, Color water, bool lilies)
    {
        Transform t = WorldKit.Group("Pond", root, position).transform;
        WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 0.04f, 0f), new Vector3(radius * 2f, 0.02f, radius * 2f), water, false);

        // Invisible blocker so the player can't wade in.
        GameObject blocker = WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 1f, 0f), new Vector3(radius * 2f, 1f, radius * 2f), water);
        blocker.GetComponent<Renderer>().enabled = false;

        int rocks = Mathf.RoundToInt(radius * 2.2f);
        for (int i = 0; i < rocks; i++)
        {
            float angle = i * Mathf.PI * 2f / rocks;
            Vector3 offset = new Vector3(Mathf.Cos(angle) * (radius + 0.3f), 0.2f, Mathf.Sin(angle) * (radius + 0.3f));
            WorldKit.Prim(PrimitiveType.Sphere, t, offset, new Vector3(Range(1f, 1.6f), Range(0.5f, 0.9f), Range(1f, 1.6f)), new Color(0.5f, 0.5f, 0.48f), false);
        }

        if (lilies)
        {
            for (int i = 0; i < 7; i++)
            {
                float angle = Range(0f, Mathf.PI * 2f);
                float distance = Range(1f, radius - 1.5f);
                Vector3 padPosition = new Vector3(Mathf.Cos(angle) * distance, 0.07f, Mathf.Sin(angle) * distance);
                WorldKit.Prim(PrimitiveType.Cylinder, t, padPosition, new Vector3(1.2f, 0.01f, 1.2f), new Color(0.25f, 0.55f, 0.2f), false);
                if (Chance(0.5f))
                {
                    WorldKit.Prim(PrimitiveType.Sphere, t, padPosition + new Vector3(0.1f, 0.12f, 0f), Vector3.one * 0.32f, new Color(1f, 0.7f, 0.85f), false);
                }
            }
        }
    }

    void FrozenLake(Vector3 position, float radius)
    {
        Transform t = WorldKit.Group("FrozenLake", root, position).transform;
        // Walkable ice.
        WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 0.03f, 0f), new Vector3(radius * 2f, 0.01f, radius * 2f), new Color(0.7f, 0.88f, 0.95f), false);
        for (int i = 0; i < 5; i++)
        {
            GameObject crack = WorldKit.Prim(PrimitiveType.Cube, t, new Vector3(Range(-radius * 0.5f, radius * 0.5f), 0.05f, Range(-radius * 0.5f, radius * 0.5f)), new Vector3(Range(2f, 5f), 0.01f, 0.08f), Color.white, false);
            crack.transform.localRotation = Quaternion.Euler(0f, Range(0f, 180f), 0f);
        }
        int rocks = Mathf.RoundToInt(radius * 2f);
        for (int i = 0; i < rocks; i++)
        {
            float angle = i * Mathf.PI * 2f / rocks;
            WorldKit.Prim(PrimitiveType.Sphere, t, new Vector3(Mathf.Cos(angle) * (radius + 0.4f), 0.2f, Mathf.Sin(angle) * (radius + 0.4f)), new Vector3(1.3f, 0.6f, 1.2f), Color.white, false);
        }
    }

    void Ziggurat(Vector3 position)
    {
        Transform t = WorldKit.Group("Ziggurat", root, position).transform;
        Color[] stones = { new Color(0.85f, 0.7f, 0.48f), new Color(0.8f, 0.64f, 0.42f) };
        float size = 14f;
        for (int i = 0; i < 4; i++)
        {
            WorldKit.Prim(PrimitiveType.Cube, t, new Vector3(0f, 1f + i * 2f, 0f), new Vector3(size, 2f, size), stones[i % 2]);
            size -= 3.5f;
        }
        GameObject cap = WorldKit.Prim(PrimitiveType.Cube, t, new Vector3(0f, 8.6f, 0f), Vector3.one * 1.4f, new Color(1f, 0.82f, 0.25f), false);
        cap.transform.localRotation = Quaternion.Euler(45f, 45f, 0f);
        cap.AddComponent<Spinner>().degreesPerSecond = new Vector3(0f, 40f, 0f);
    }

    void Dune(Vector3 position)
    {
        Transform t = Prop("Dune", position).transform;
        WorldKit.Prim(PrimitiveType.Sphere, t, Vector3.zero, new Vector3(Range(8f, 12f), Range(1.4f, 2.2f), Range(6f, 9f)), new Color(0.88f, 0.74f, 0.48f), false);
    }

    void Obelisk(Vector3 position)
    {
        Transform t = WorldKit.Group("Obelisk", root, position).transform;
        WorldKit.Prim(PrimitiveType.Cube, t, new Vector3(0f, 0.3f, 0f), new Vector3(2f, 0.6f, 2f), new Color(0.75f, 0.6f, 0.4f));
        WorldKit.Prim(PrimitiveType.Cube, t, new Vector3(0f, 3.8f, 0f), new Vector3(1.2f, 6.4f, 1.2f), new Color(0.85f, 0.72f, 0.5f));
        GameObject tip = WorldKit.Prim(PrimitiveType.Cube, t, new Vector3(0f, 7.2f, 0f), Vector3.one * 0.85f, new Color(1f, 0.82f, 0.25f), false);
        tip.transform.localRotation = Quaternion.Euler(45f, 0f, 45f);
    }

    void Skull(Vector3 position)
    {
        Transform t = Prop("Skull", position).transform;
        Color bone = new Color(0.95f, 0.92f, 0.82f);
        WorldKit.Prim(PrimitiveType.Sphere, t, new Vector3(0f, 0.45f, 0f), new Vector3(0.9f, 0.8f, 1f), bone, false);
        WorldKit.Prim(PrimitiveType.Sphere, t, new Vector3(-0.2f, 0.5f, -0.4f), Vector3.one * 0.22f, Color.black, false);
        WorldKit.Prim(PrimitiveType.Sphere, t, new Vector3(0.2f, 0.5f, -0.4f), Vector3.one * 0.22f, Color.black, false);
        WorldKit.Prim(PrimitiveType.Cube, t, new Vector3(0f, 0.12f, -0.25f), new Vector3(0.5f, 0.18f, 0.4f), bone, false);
    }

    void Snowman(Vector3 position)
    {
        Transform t = WorldKit.Group("Snowman", root, position).transform;
        WorldKit.Prim(PrimitiveType.Sphere, t, new Vector3(0f, 0.75f, 0f), Vector3.one * 1.6f, Color.white);
        WorldKit.Prim(PrimitiveType.Sphere, t, new Vector3(0f, 2f, 0f), Vector3.one * 1.15f, Color.white, false);
        WorldKit.Prim(PrimitiveType.Sphere, t, new Vector3(0f, 2.95f, 0f), Vector3.one * 0.8f, Color.white, false);
        WorldKit.Prim(PrimitiveType.Sphere, t, new Vector3(-0.15f, 3.05f, -0.35f), Vector3.one * 0.12f, Color.black, false);
        WorldKit.Prim(PrimitiveType.Sphere, t, new Vector3(0.15f, 3.05f, -0.35f), Vector3.one * 0.12f, Color.black, false);
        GameObject nose = WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 2.92f, -0.55f), new Vector3(0.1f, 0.2f, 0.1f), new Color(1f, 0.5f, 0.1f), false);
        nose.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 3.33f, 0f), new Vector3(0.9f, 0.03f, 0.9f), new Color(0.1f, 0.1f, 0.12f), false);
        WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 3.6f, 0f), new Vector3(0.6f, 0.28f, 0.6f), new Color(0.1f, 0.1f, 0.12f), false);
        WorldKit.Prim(PrimitiveType.Cube, t, new Vector3(0f, 2.5f, -0.05f), new Vector3(1.1f, 0.2f, 1f), new Color(0.85f, 0.2f, 0.2f), false);
    }

    void IceSpikes(Vector3 position)
    {
        Transform t = Prop("IceSpikes", position).transform;
        int count = rng.Next(3, 6);
        for (int i = 0; i < count; i++)
        {
            float height = Range(2f, 5f);
            float width = Range(0.5f, 0.9f);
            GameObject spike = WorldKit.Prim(PrimitiveType.Cube, t, new Vector3(Range(-0.8f, 0.8f), height * 0.45f, Range(-0.8f, 0.8f)), new Vector3(width, height, width), new Color(0.65f, 0.88f, 1f));
            spike.transform.localRotation = Quaternion.Euler(Range(-15f, 15f), Range(0f, 90f), Range(-15f, 15f));
        }
    }

    void Volcano(Vector3 position)
    {
        Transform t = WorldKit.Group("Volcano", root, position).transform;
        Color rock = new Color(0.2f, 0.16f, 0.15f);
        float[] diameters = { 20f, 15f, 10f, 6f };
        for (int i = 0; i < diameters.Length; i++)
        {
            WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 1.5f + i * 3f, 0f), new Vector3(diameters[i], 1.5f, diameters[i]), Color.Lerp(rock, Color.black, i * 0.1f));
        }

        Material lava = WorldKit.Textured("M_YFGM_Lava", Vector2.one, new Color(1f, 0.4f, 0.05f));
        WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 12.02f, 0f), new Vector3(4.5f, 0.02f, 4.5f), lava, false);
        AreaBuilder.AddGlow(t, new Vector3(0f, 14f, 0f), 25f);

        for (int i = 0; i < 4; i++)
        {
            GameObject puff = WorldKit.Prim(PrimitiveType.Sphere, t, new Vector3(Range(-1.5f, 1.5f), 14f + i * 2.2f, Range(-1.5f, 1.5f)), Vector3.one * (2f + i * 0.8f), new Color(0.3f, 0.28f, 0.28f), false);
            Bobber bob = puff.AddComponent<Bobber>();
            bob.amplitude = 0.8f;
            bob.speed = 0.6f + i * 0.2f;
        }
    }

    void Brazier(Vector3 position)
    {
        Transform t = WorldKit.Group("Brazier", root, position).transform;
        Color stone = new Color(0.3f, 0.28f, 0.28f);
        WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 0.6f, 0f), new Vector3(0.8f, 0.6f, 0.8f), stone);
        WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 1.3f, 0f), new Vector3(1.4f, 0.12f, 1.4f), stone, false);
        GameObject flame = WorldKit.Prim(PrimitiveType.Sphere, t, new Vector3(0f, 1.85f, 0f), new Vector3(0.8f, 1.1f, 0.8f), new Color(1f, 0.55f, 0.1f), false);

        GameObject lightObj = new GameObject("Light");
        lightObj.transform.SetParent(t, false);
        lightObj.transform.localPosition = new Vector3(0f, 2.4f, 0f);
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.55f, 0.2f);
        light.range = 9f;

        Flicker flicker = flame.AddComponent<Flicker>();
        flicker.targetLight = light;
        flicker.baseIntensity = 1.8f;
    }

    void Basalt(Vector3 position)
    {
        Transform t = Prop("Basalt", position).transform;
        int count = rng.Next(3, 7);
        for (int i = 0; i < count; i++)
        {
            float height = Range(1f, 4f);
            float width = Range(0.9f, 1.4f);
            WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(Range(-1.2f, 1.2f), height * 0.5f, Range(-1.2f, 1.2f)), new Vector3(width, height * 0.5f, width), new Color(0.17f, 0.16f, 0.18f));
        }
    }
}

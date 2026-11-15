using System.Collections.Generic;
using UnityEngine;

// Small helpers for building areas out of primitives with cached materials.
public static class WorldKit
{
    private static Material baseMaterial;
    private static Font font;
    private static readonly Dictionary<Color, Material> colorCache = new Dictionary<Color, Material>();
    private static readonly Dictionary<string, Material> textureCache = new Dictionary<string, Material>();

    static Material BaseMaterial
    {
        get
        {
            if (baseMaterial == null)
            {
                GameObject probe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                baseMaterial = probe.GetComponent<Renderer>().sharedMaterial;
                Kill(probe);
            }
            return baseMaterial;
        }
    }

    public static Font Font
    {
        get
        {
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            return font;
        }
    }

    public static Material Colored(Color color)
    {
        Material material;
        if (!colorCache.TryGetValue(color, out material) || material == null)
        {
            material = new Material(BaseMaterial);
            material.color = color;
            // URP Lit calls it _Smoothness; the built-in Standard shader called it _Glossiness.
            material.SetFloat(material.HasProperty("_Smoothness") ? "_Smoothness" : "_Glossiness", 0.15f);
            colorCache[color] = material;
        }
        return material;
    }

    // A material from AreaMaterialLibrary, tiled so the texture is not stretched.
    // Falls back to a flat color when the material is missing.
    public static Material Textured(string materialName, Vector2 tiling, Color fallback)
    {
        if (string.IsNullOrEmpty(materialName))
        {
            return Colored(fallback);
        }

        string key = materialName + "|" + tiling.x.ToString("F2") + "|" + tiling.y.ToString("F2");
        Material material;
        if (textureCache.TryGetValue(key, out material) && material != null)
        {
            return material;
        }

        Material source = AreaMaterialLibrary.Find(materialName);
        if (source == null)
        {
            return Colored(fallback);
        }

        material = new Material(source);
        material.mainTextureScale = tiling;
        textureCache[key] = material;
        return material;
    }

    public static GameObject Prim(PrimitiveType type, Transform parent, Vector3 localPosition, Vector3 scale, Material material, bool solid = true)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = localPosition;
        obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().sharedMaterial = material;

        if (!solid)
        {
            Kill(obj.GetComponent<Collider>());
        }
        else if (type == PrimitiveType.Cylinder || type == PrimitiveType.Sphere || type == PrimitiveType.Capsule)
        {
            // Primitive colliders distort badly on non-uniform scales; use the real mesh.
            Kill(obj.GetComponent<Collider>());
            obj.AddComponent<MeshCollider>().sharedMesh = obj.GetComponent<MeshFilter>().sharedMesh;
        }

        return obj;
    }

    public static GameObject Prim(PrimitiveType type, Transform parent, Vector3 localPosition, Vector3 scale, Color color, bool solid = true)
    {
        return Prim(type, parent, localPosition, scale, Colored(color), solid);
    }

    public static GameObject Group(string name, Transform parent, Vector3 localPosition, float yaw = 0f)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = localPosition;
        obj.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        return obj;
    }

    public static TextMesh Label(Transform parent, Vector3 localPosition, string text, Color color, float size = 0.12f, bool billboard = true)
    {
        GameObject obj = new GameObject("Label");
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = localPosition;

        TextMesh mesh = obj.AddComponent<TextMesh>();
        mesh.font = Font;
        mesh.text = text;
        mesh.fontSize = 64;
        mesh.characterSize = size;
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;
        mesh.color = color;
        obj.GetComponent<MeshRenderer>().sharedMaterial = Font.material;

        if (billboard)
        {
            obj.AddComponent<Billboard>();
        }

        return mesh;
    }

    public static Vector3 Flat(Vector2 point, float y = 0f)
    {
        return new Vector3(point.x, y, point.y);
    }

    public static void Kill(Object obj)
    {
        if (obj == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Object.Destroy(obj);
        }
        else
        {
            Object.DestroyImmediate(obj);
        }
    }
}

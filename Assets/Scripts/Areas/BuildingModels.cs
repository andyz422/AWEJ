using System.Collections.Generic;
using UnityEngine;

// Building exteriors modelled in Blender (Blender/export_unity_buildings.py) and stored at
// Resources/Buildings/<building id>. When a model exists, AreaBuilder hides its primitive
// stand-in but keeps the primitive colliders, so gameplay collision is unchanged.
public static class BuildingModels
{
    const string Folder = "Buildings/";

    // Turn off to fall back to the primitive buildings everywhere.
    public static bool Enabled = true;

    static readonly Dictionary<string, GameObject> cache = new Dictionary<string, GameObject>();

    public static GameObject Find(string id)
    {
        if (!Enabled || string.IsNullOrEmpty(id))
        {
            return null;
        }

        GameObject prefab;
        if (!cache.TryGetValue(id, out prefab))
        {
            prefab = Resources.Load<GameObject>(Folder + id);
            cache[id] = prefab;
        }
        return prefab;
    }

    // Hides the primitives already built under the building, then places the model there.
    // Returns null (and leaves the primitives visible) when the building has no model.
    public static GameObject Replace(Transform building, string id)
    {
        GameObject prefab = Find(id);
        if (prefab == null)
        {
            return null;
        }

        foreach (Renderer r in building.GetComponentsInChildren<Renderer>(true))
        {
            r.enabled = false;
        }

        GameObject model = Object.Instantiate(prefab, building, false);
        model.name = "Model";
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one;

        foreach (Collider c in model.GetComponentsInChildren<Collider>(true))
        {
            WorldKit.Kill(c);
        }

        return model;
    }

    // Height of the model's top above the building origin.
    public static float TopHeight(GameObject model, Transform building)
    {
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return 0f;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
        return bounds.max.y - building.position.y;
    }
}

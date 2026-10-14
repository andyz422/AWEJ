using UnityEngine;

// A shop pedestal: shows an item model with a live name/price label and sells it on interact.
public class ItemPedestal : MonoBehaviour
{
    public string itemId;
    public TextMesh label;

    private float refreshTimer;

    void Update()
    {
        refreshTimer -= Time.deltaTime;
        if (refreshTimer <= 0f && label != null)
        {
            label.text = ShopCatalog.GetLabel(itemId);
            refreshTimer = 0.25f;
        }
    }

    public static ItemPedestal Create(Transform parent, Vector3 localPosition, string itemId)
    {
        GameObject root = WorldKit.Group("Pedestal_" + itemId, parent, localPosition);
        Transform t = root.transform;

        WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 0.5f, 0f), new Vector3(1.3f, 0.5f, 1.3f), new Color(0.55f, 0.52f, 0.5f));
        WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 1.02f, 0f), new Vector3(1.5f, 0.04f, 1.5f), new Color(0.75f, 0.6f, 0.3f), false);

        GameObject model = WorldKit.Group("Model", t, new Vector3(0f, 1.75f, 0f));
        BuildModel(model.transform, itemId);
        model.AddComponent<Spinner>().degreesPerSecond = new Vector3(0f, 70f, 0f);
        model.AddComponent<Bobber>().amplitude = 0.12f;

        ItemPedestal pedestal = root.AddComponent<ItemPedestal>();
        pedestal.itemId = itemId;
        pedestal.label = WorldKit.Label(t, new Vector3(0f, 3f, 0f), ShopCatalog.GetLabel(itemId), Color.white, 0.075f);
        return pedestal;
    }

    static void BuildModel(Transform t, string itemId)
    {
        Color color = ShopCatalog.GetItemColor(itemId);
        WeaponDef weapon = WeaponCatalog.Get(itemId);

        if (weapon != null)
        {
            Color metal = new Color(0.25f, 0.25f, 0.28f);
            WorldKit.Prim(PrimitiveType.Cube, t, Vector3.zero, new Vector3(0.35f, 0.32f, 0.8f), metal, false);
            WorldKit.Prim(PrimitiveType.Cube, t, new Vector3(0f, -0.3f, -0.25f), new Vector3(0.2f, 0.4f, 0.22f), metal, false);

            int barrels = Mathf.Clamp(weapon.projectileCount, 1, 3);
            float barrelWidth = weapon.projectileScale > 0.5f ? 0.32f : 0.14f;
            for (int i = 0; i < barrels; i++)
            {
                float x = barrels > 1 ? Mathf.Lerp(-0.12f, 0.12f, i / (float)(barrels - 1)) : 0f;
                GameObject barrel = WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(x, 0.05f, 0.6f), new Vector3(barrelWidth, 0.3f, barrelWidth), color, false);
                barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
            return;
        }

        switch (itemId)
        {
            case "potion":
                WorldKit.Prim(PrimitiveType.Sphere, t, Vector3.zero, Vector3.one * 0.55f, color, false);
                WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 0.35f, 0f), new Vector3(0.18f, 0.12f, 0.18f), new Color(0.85f, 0.95f, 1f), false);
                WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 0.5f, 0f), new Vector3(0.2f, 0.05f, 0.2f), new Color(0.6f, 0.4f, 0.2f), false);
                break;
            case "hp":
                WorldKit.Prim(PrimitiveType.Sphere, t, new Vector3(-0.16f, 0.1f, 0f), Vector3.one * 0.4f, color, false);
                WorldKit.Prim(PrimitiveType.Sphere, t, new Vector3(0.16f, 0.1f, 0f), Vector3.one * 0.4f, color, false);
                WorldKit.Prim(PrimitiveType.Cube, t, new Vector3(0f, -0.1f, 0f), new Vector3(0.38f, 0.38f, 0.38f), color, false)
                    .transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                break;
            case "dmg":
                WorldKit.Prim(PrimitiveType.Cube, t, Vector3.zero, Vector3.one * 0.45f, color, false)
                    .transform.localRotation = Quaternion.Euler(45f, 0f, 45f);
                WorldKit.Prim(PrimitiveType.Sphere, t, Vector3.zero, Vector3.one * 0.5f, new Color(1f, 0.9f, 0.6f), false);
                break;
            case "rate":
                for (int i = 0; i < 5; i++)
                {
                    WorldKit.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, -0.3f + i * 0.15f, 0f), new Vector3(0.45f, 0.03f, 0.45f), color, false);
                }
                WorldKit.Prim(PrimitiveType.Cylinder, t, Vector3.zero, new Vector3(0.12f, 0.35f, 0.12f), new Color(0.3f, 0.3f, 0.3f), false);
                break;
            case "boots":
                for (int side = -1; side <= 1; side += 2)
                {
                    WorldKit.Prim(PrimitiveType.Cube, t, new Vector3(side * 0.18f, 0.05f, 0f), new Vector3(0.22f, 0.45f, 0.25f), color, false);
                    WorldKit.Prim(PrimitiveType.Cube, t, new Vector3(side * 0.18f, -0.12f, 0.15f), new Vector3(0.22f, 0.12f, 0.35f), color, false);
                    WorldKit.Prim(PrimitiveType.Cube, t, new Vector3(side * 0.3f, 0.1f, -0.05f), new Vector3(0.04f, 0.2f, 0.3f), Color.white, false);
                }
                break;
            default:
                WorldKit.Prim(PrimitiveType.Cube, t, Vector3.zero, Vector3.one * 0.45f, color, false);
                break;
        }
    }
}

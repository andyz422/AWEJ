using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Builds each area in the editor with its URP atmosphere and renders it to Logs/AreaPreviews/:
// a ground-level view from the player's start, an overview, and one shop interior.
// Menu: AWEJ > Render Area Previews. Batch: -executeMethod AreaPreview.RunBatch
public static class AreaPreview
{
    const string OutDir = "Logs/AreaPreviews";

    [MenuItem("AWEJ/Render Area Previews")]
    public static void RunMenu()
    {
        Run();
        EditorUtility.RevealInFinder(OutDir);
    }

    public static void RunBatch()
    {
        Run();
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/MenuScene.unity");
        EditorApplication.Exit(0);
    }

    static void Run()
    {
        Directory.CreateDirectory(OutDir);
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/TownScene.unity");

        Camera cam = Camera.main;
        if (cam == null)
        {
            cam = new GameObject("PreviewCamera").AddComponent<Camera>();
            cam.tag = "MainCamera";
        }
        cam.fieldOfView = 55f;
        cam.farClipPlane = 800f;
        RenderTexture rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        bool warmedUp = false;

        for (int a = 0; a < AreaDatabase.Count; a++)
        {
            AreaDef area = AreaDatabase.Get(a);
            GameObject root = new GameObject("Preview_" + area.name);
            try
            {
                AreaBuilder.BuildGround(root.transform, area.size, area.groundMaterial, area.groundColor);
                AreaBuilder.BuildWalls(root.transform, area.size, area.wallHeight, area.wallMaterial, area.wallColor);
                AreaBuilder.BuildPaths(root.transform, area.paths, area.pathMaterial, area.pathColor);
                AreaBuilder.BuildHazards(root.transform, area.hazards);
                foreach (BuildingDef b in area.buildings)
                {
                    AreaBuilder.BuildBuilding(root.transform, b, false);
                }
                new AreaDecor(area, root.transform).Build();
                AreaAtmosphere.Apply(area, false, cam);

                // Ground-level view from the start position toward the area centre.
                Vector3 start = WorldKit.Flat(area.startPosition);
                Vector3 toCentre = (Vector3.zero - start);
                toCentre.y = 0f;
                Vector3 dir = toCentre.sqrMagnitude > 1f ? toCentre.normalized : Vector3.forward;
                cam.transform.position = start - dir * 6f + Vector3.up * 4.5f;
                cam.transform.LookAt(start + dir * 30f + Vector3.up * 1.5f);
                if (!warmedUp)
                {
                    // The first URP frame after a fresh import still has shaders compiling; throw it away.
                    for (int i = 0; i < 3; i++)
                    {
                        Save(cam, rt, null);
                    }
                    warmedUp = true;
                }
                Save(cam, rt, Path.Combine(OutDir, "area" + a + "_ground.png"));

                // Overview from above the south edge.
                float span = Mathf.Max(area.size.x, area.size.y);
                cam.transform.position = new Vector3(area.size.x * 0.2f, span * 0.55f, -area.size.y * 0.5f - span * 0.35f);
                cam.transform.LookAt(new Vector3(0f, 0f, area.size.y * 0.05f));
                Save(cam, rt, Path.Combine(OutDir, "area" + a + "_overview.png"));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }

            // One shop interior per area.
            foreach (BuildingDef b in area.buildings)
            {
                if (b.type != BuildingType.Shop)
                {
                    continue;
                }
                GameObject shop = new GameObject("PreviewShop_" + b.id);
                try
                {
                    InteriorBuilder.BuildShop(shop.transform, b, new List<ItemPedestal>());
                    AreaAtmosphere.Apply(area, true, cam);
                    cam.transform.position = new Vector3(0f, 3.2f, -6.5f);
                    cam.transform.LookAt(new Vector3(0f, 1.5f, 5f));
                    Save(cam, rt, Path.Combine(OutDir, "area" + a + "_shop_" + b.id + ".png"));
                }
                finally
                {
                    Object.DestroyImmediate(shop);
                }
                break;
            }
        }
        Object.DestroyImmediate(rt);
        Debug.Log("[AreaPreview] Rendered previews to " + OutDir);
    }

    static void Save(Camera cam, RenderTexture rt, string path)
    {
        RenderPipeline.StandardRequest request = new RenderPipeline.StandardRequest();
        if (RenderPipeline.SupportsRenderRequest(cam, request))
        {
            request.destination = rt;
            RenderPipeline.SubmitRenderRequest(cam, request);
        }
        else
        {
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = null;
        }
        if (path == null)
        {
            return;
        }
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        RenderTexture.active = previous;
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }
}

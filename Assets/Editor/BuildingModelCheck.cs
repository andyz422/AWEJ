using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

// Builds every area's buildings in the editor and checks the Blender models:
// each one must load, face its door toward local -Z and roughly cover its collider footprint.
// Writes a report and pictures to <project>/Temp/BuildingModelCheck/.
// Batch use: Unity -batchmode -projectPath . -executeMethod BuildingModelCheck.RunBatch -quit
public static class BuildingModelCheck
{
    // Logs/ survives editor shutdown (Temp/ is wiped) and is git-ignored.
    const string OutDir = "Logs/BuildingModelCheck";

    [MenuItem("AWEJ/Check Building Models")]
    public static void RunMenu()
    {
        int failures = Run();
        EditorUtility.DisplayDialog("Building models", failures == 0 ? "All building models OK." :
            failures + " problem(s). See " + OutDir + "/report.txt", "OK");
    }

    public static void RunBatch()
    {
        int failures = Run();
        EditorApplication.Exit(failures == 0 ? 0 : 1);
    }

    static int Run()
    {
        Directory.CreateDirectory(OutDir);
        StringBuilder report = new StringBuilder();
        int failures = 0;

        GameObject lightObj = new GameObject("CheckSun");
        Light sun = lightObj.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.1f;
        sun.shadows = LightShadows.Soft;
        lightObj.transform.rotation = Quaternion.Euler(45f, -40f, 0f);

        GameObject camObj = new GameObject("CheckCamera");
        Camera cam = camObj.AddComponent<Camera>();
        cam.fieldOfView = 50f;
        cam.farClipPlane = 600f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        RenderTexture rt = new RenderTexture(960, 540, 24);
        cam.targetTexture = rt;

        try
        {
            for (int a = 0; a < AreaDatabase.Count; a++)
            {
                AreaDef area = AreaDatabase.Get(a);
                cam.backgroundColor = area.skyColor;
                report.AppendLine("== " + area.name);
                GameObject root = new GameObject("Check_" + area.name);
                try
                {
                    AreaBuilder.BuildGround(root.transform, area.size, area.groundMaterial, area.groundColor);
                    foreach (BuildingDef b in area.buildings)
                    {
                        AreaBuilder.BuildBuilding(root.transform, b, false);
                        Transform t = root.transform.Find(b.name);
                        Transform model = t != null ? t.Find("Model") : null;
                        if (model == null)
                        {
                            failures++;
                            report.AppendLine("FAIL " + b.id + ": no model loaded");
                            continue;
                        }

                        Physics.SyncTransforms();
                        Bounds mb = LocalBounds(t, model.GetComponentsInChildren<MeshFilter>());
                        Bounds cb = ColliderBounds(t);
                        int doorVerts = CountVerts(t, model, b.DoorDepth, -1f);
                        int backVerts = CountVerts(t, model, b.DoorDepth, 1f);
                        int tris = 0, mats = 0;
                        foreach (MeshFilter mf in model.GetComponentsInChildren<MeshFilter>())
                        {
                            tris += mf.sharedMesh.triangles.Length / 3;
                        }
                        foreach (Renderer r in model.GetComponentsInChildren<Renderer>())
                        {
                            mats += r.sharedMaterials.Length;
                        }

                        // Door geometry must sit where the game puts the door (local -Z), not on the back.
                        bool doorFront = doorVerts > 20 && doorVerts > backVerts * 2;
                        // Some parts (tunnels, drifts, roots) are sunk on purpose; the bulk must start at the ground.
                        bool grounded = mb.min.y > -3.5f && mb.min.y < 0.3f;
                        bool covers = mb.size.x > cb.size.x * 0.6f && mb.size.z > cb.size.z * 0.6f;
                        bool ok = doorFront && grounded && covers;
                        if (!ok)
                        {
                            failures++;
                        }
                        report.AppendLine(string.Format(
                            "{0} {1}: tris={2} mats={3} model={4} colliders={5} doorVerts={6}/{7} minY={8:F1} covers={9}",
                            ok ? "OK  " : "FAIL", b.id, tris, mats, Fmt(mb.size), Fmt(cb.size), doorVerts, backVerts, mb.min.y, covers));

                        // Picture from the door side.
                        Vector3 front = t.rotation * Vector3.back;
                        Vector3 right = t.rotation * Vector3.right;
                        float dist = b.DoorDepth + 10f + Mathf.Max(mb.size.x, mb.size.y) * 0.6f;
                        cam.transform.position = t.position + front * dist + right * 4f + Vector3.up * (3f + mb.size.y * 0.35f);
                        cam.transform.LookAt(t.position + Vector3.up * mb.size.y * 0.4f);
                        Save(cam, rt, Path.Combine(OutDir, b.id + ".png"));
                    }

                    // Overview of the whole area.
                    float span = Mathf.Max(area.size.x, area.size.y);
                    cam.transform.position = new Vector3(area.size.x * 0.3f, span * 0.65f, -area.size.y * 0.5f - span * 0.6f);
                    cam.transform.LookAt(Vector3.zero);
                    Save(cam, rt, Path.Combine(OutDir, "area_" + a + ".png"));
                }
                finally
                {
                    Object.DestroyImmediate(root);
                }
            }
        }
        finally
        {
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(camObj);
            Object.DestroyImmediate(lightObj);
        }

        report.AppendLine(failures == 0 ? "RESULT: all OK" : "RESULT: " + failures + " failure(s)");
        File.WriteAllText(Path.Combine(OutDir, "report.txt"), report.ToString());
        Debug.Log("[BuildingModelCheck]\n" + report);
        return failures;
    }

    static Bounds LocalBounds(Transform space, MeshFilter[] filters)
    {
        bool first = true;
        Bounds result = new Bounds();
        foreach (MeshFilter mf in filters)
        {
            Bounds mb = mf.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = mb.center + Vector3.Scale(mb.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 p = space.InverseTransformPoint(mf.transform.TransformPoint(corner));
                if (first)
                {
                    result = new Bounds(p, Vector3.zero);
                    first = false;
                }
                else
                {
                    result.Encapsulate(p);
                }
            }
        }
        return result;
    }

    // Vertices inside the door's box (2.6 wide, 0-3.6 high, a slab at the door face); side -1 = front, +1 = back.
    static int CountVerts(Transform building, Transform model, float depth, float side)
    {
        int count = 0;
        foreach (MeshFilter mf in model.GetComponentsInChildren<MeshFilter>())
        {
            Matrix4x4 toBuilding = building.worldToLocalMatrix * mf.transform.localToWorldMatrix;
            foreach (Vector3 v in mf.sharedMesh.vertices)
            {
                Vector3 p = toBuilding.MultiplyPoint3x4(v);
                float z = p.z * side;
                if (Mathf.Abs(p.x) < 1.3f && p.y > 0.2f && p.y < 3.6f && z > depth - 0.2f && z < depth + 0.6f)
                {
                    count++;
                }
            }
        }
        return count;
    }

    static Bounds ColliderBounds(Transform building)
    {
        bool first = true;
        Bounds result = new Bounds();
        foreach (Collider c in building.GetComponentsInChildren<Collider>())
        {
            if (c.isTrigger)
            {
                continue;
            }
            Bounds wb = c.bounds;
            Vector3 lo = building.InverseTransformPoint(wb.min), hi = building.InverseTransformPoint(wb.max);
            Bounds lb = new Bounds((lo + hi) * 0.5f, new Vector3(Mathf.Abs(hi.x - lo.x), Mathf.Abs(hi.y - lo.y), Mathf.Abs(hi.z - lo.z)));
            if (first)
            {
                result = lb;
                first = false;
            }
            else
            {
                result.Encapsulate(lb);
            }
        }
        return result;
    }

    static void Save(Camera cam, RenderTexture rt, string path)
    {
        cam.Render();
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        RenderTexture.active = previous;
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    static string Fmt(Vector3 v)
    {
        return string.Format("{0:F1}x{1:F1}x{2:F1}", v.x, v.y, v.z);
    }
}

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Rendering.Universal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// One-time switch from the built-in renderer to URP, tuned for iPhone.
// Safe to run again: it reuses the assets it created.
// Menu: AWEJ > Setup URP. Batch: Unity -batchmode -projectPath . -executeMethod URPSetup.RunBatch -quit
public static class URPSetup
{
    const string Folder = "Assets/Settings/URP";
    const string AssetPath = Folder + "/AWEJ_URP.asset";
    const string RendererPath = Folder + "/AWEJ_URP_Renderer.asset";
    const string SkyPath = "Assets/Resources/Atmosphere/ProceduralSky.mat";

    [MenuItem("AWEJ/Setup URP")]
    public static void RunMenu()
    {
        Run();
        EditorUtility.DisplayDialog("URP", "URP is set up. See the Console for details.", "OK");
    }

    public static void RunBatch()
    {
        Run();
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/MenuScene.unity");
        EditorApplication.Exit(0);
    }

    static void Run()
    {
        Directory.CreateDirectory(Folder);
        Directory.CreateDirectory(Path.GetDirectoryName(SkyPath));

        // 1. Pipeline asset + Forward+ renderer (many small point lights: lanterns, lava, gate orbs).
        UniversalRenderPipelineAsset asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetPath);
        if (asset == null)
        {
            UniversalRendererData rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(rendererData, RendererPath);
            asset = UniversalRenderPipelineAsset.Create(rendererData);
            AssetDatabase.CreateAsset(asset, AssetPath);
        }
        UniversalRendererData renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
        if (renderer != null)
        {
            renderer.renderingMode = RenderingMode.ForwardPlus;
            EditorUtility.SetDirty(renderer);
        }

        // 2. iPhone-friendly quality: HDR for bloom, 2x MSAA, one soft-shadowed sun, no extra shadow maps.
        SerializedObject so = new SerializedObject(asset);
        Set(so, "m_SupportsHDR", true);
        Set(so, "m_MSAA", 2);
        Set(so, "m_RenderScale", 1f);
        Set(so, "m_ShadowDistance", 70f);
        Set(so, "m_ShadowCascadeCount", 2);
        Set(so, "m_MainLightShadowsSupported", true);
        Set(so, "m_MainLightShadowmapResolution", 2048);
        Set(so, "m_SoftShadowsSupported", true);
        Set(so, "m_AdditionalLightsRenderingMode", 1);
        Set(so, "m_AdditionalLightShadowsSupported", false);
        Set(so, "m_RequireDepthTexture", false);
        Set(so, "m_RequireOpaqueTexture", false);
        Set(so, "m_ColorGradingMode", 1);
        Set(so, "m_ColorGradingLutSize", 32);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);

        // 3. Use it everywhere: default + every quality level.
        GraphicsSettings.defaultRenderPipeline = asset;
        int current = QualitySettings.GetQualityLevel();
        for (int i = 0; i < QualitySettings.names.Length; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.renderPipeline = null;
        }
        QualitySettings.SetQualityLevel(current, false);

        // 4. Linear colour space: what URP lighting, HDR and bloom are designed for. Metal supports it.
        PlayerSettings.colorSpace = ColorSpace.Linear;

        // 5. Convert built-in Standard materials (ground texture pack, built-in defaults) to URP Lit.
        Converters.RunInBatchMode(ConverterContainerId.BuiltInToURP,
            new List<ConverterId> { ConverterId.Material, ConverterId.ReadonlyMaterial }, ConverterFilter.Inclusive);

        // 6. Re-import the Blender models so their embedded materials are regenerated as URP Lit.
        foreach (string folder in new[] { "Assets/Resources/Buildings", "Assets/Resources/Shopkeepers" })
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { folder }))
            {
                AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);
            }
        }

        // 7. Prefabs and scenes that point at Unity's built-in Default-Material (player, projectiles, walls):
        //    the converter only rewrites material assets, so swap those references for URP's default Lit.
        FixBuiltInMaterialReferences(asset.defaultMaterial);

        // 8. Procedural sky material the game tints per area (in Resources so it ships in builds).
        if (AssetDatabase.LoadAssetAtPath<Material>(SkyPath) == null)
        {
            Material sky = new Material(Shader.Find("Skybox/Procedural"));
            AssetDatabase.CreateAsset(sky, SkyPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[URPSetup] Done. Pipeline: " + AssetPath + ", colour space: " + PlayerSettings.colorSpace);
    }

    static bool NeedsReplacing(Material m)
    {
        if (m == null)
        {
            return false;
        }
        string path = AssetDatabase.GetAssetPath(m);
        bool builtIn = path == "Resources/unity_builtin_extra" || path == "Library/unity default resources";
        return builtIn && (m.shader == null || !m.shader.name.StartsWith("Universal Render Pipeline") && !m.shader.name.StartsWith("Sprites") && !m.shader.name.StartsWith("UI/") && !m.shader.name.StartsWith("GUI/"));
    }

    static int FixRenderers(GameObject root, Material replacement)
    {
        int fixedCount = 0;
        foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
        {
            if (r is ParticleSystemRenderer || r is SpriteRenderer)
            {
                continue;
            }
            Material[] mats = r.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                if (NeedsReplacing(mats[i]))
                {
                    mats[i] = replacement;
                    changed = true;
                    fixedCount++;
                }
            }
            if (changed)
            {
                r.sharedMaterials = mats;
            }
        }
        return fixedCount;
    }

    static void FixBuiltInMaterialReferences(Material replacement)
    {
        if (replacement == null)
        {
            Debug.LogWarning("[URPSetup] No URP default material; skipping prefab fix.");
            return;
        }
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            int n = FixRenderers(contents, replacement);
            if (n > 0)
            {
                PrefabUtility.SaveAsPrefabAsset(contents, path);
                Debug.Log("[URPSetup] " + path + ": replaced " + n + " built-in material(s)");
            }
            PrefabUtility.UnloadPrefabContents(contents);
        }
        foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path);
            int n = 0;
            foreach (GameObject go in scene.GetRootGameObjects())
            {
                n += FixRenderers(go, replacement);
            }
            if (n > 0)
            {
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
                Debug.Log("[URPSetup] " + path + ": replaced " + n + " built-in material(s)");
            }
        }
    }

    static void Set(SerializedObject so, string name, object value)
    {
        SerializedProperty p = so.FindProperty(name);
        if (p == null)
        {
            Debug.LogWarning("[URPSetup] Setting not found: " + name);
            return;
        }
        if (value is bool b) p.boolValue = b;
        else if (value is int i) p.intValue = i;
        else if (value is float f) p.floatValue = f;
    }
}

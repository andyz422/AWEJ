using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Renders the menu portraits of the playable characters in Unity (so colours match the game exactly)
// into Assets/Resources/Players/Portraits/, then captures the character select screen and the player
// in town to Logs/CharacterCheck/ for review.
// Menu: AWEJ > Render Character Portraits. Batch: -executeMethod CharacterPortraits.RunBatch
public static class CharacterPortraits
{
    const string PortraitDir = "Assets/Resources/Players/Portraits";
    const string CheckDir = "Logs/CharacterCheck";

    [MenuItem("AWEJ/Render Character Portraits")]
    public static void RunMenu()
    {
        Run();
        EditorSceneManager.OpenScene("Assets/Scenes/MenuScene.unity");
    }

    public static void RunBatch()
    {
        Run();
        EditorSceneManager.OpenScene("Assets/Scenes/MenuScene.unity");
        EditorApplication.Exit(0);
    }

    static void Run()
    {
        Directory.CreateDirectory(PortraitDir);
        Directory.CreateDirectory(CheckDir);
        RenderPortraits();
        AssetDatabase.Refresh();
        CaptureCharacterSelect();
        CapturePlayerInTown();
        Debug.Log("[CharacterPortraits] Done");
    }

    static Camera MakeCamera(Color clear)
    {
        Camera cam = new GameObject("CheckCamera").AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = clear;
        cam.allowHDR = false;
        UniversalAdditionalCameraData data = cam.GetUniversalAdditionalCameraData();
        data.renderPostProcessing = false;
        return cam;
    }

    static void RenderPortraits()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.75f, 0.8f, 0.9f);
        RenderSettings.ambientEquatorColor = new Color(0.6f, 0.62f, 0.68f);
        RenderSettings.ambientGroundColor = new Color(0.35f, 0.33f, 0.32f);

        Light key = new GameObject("Key").AddComponent<Light>();
        key.type = LightType.Directional;
        key.intensity = 1.25f;
        key.transform.rotation = Quaternion.Euler(28f, 155f, 0f); // from above-front-left onto the face (+Z)
        key.shadows = LightShadows.Soft;

        Camera cam = MakeCamera(new Color(0f, 0f, 0f, 0f));
        cam.fieldOfView = 26f;
        cam.transform.position = new Vector3(0.35f, 1.35f, 5.4f);
        cam.transform.LookAt(new Vector3(0f, 1.08f, 0f));
        RenderTexture rt = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);

        foreach (PlayerCharacters.Character c in PlayerCharacters.All)
        {
            GameObject prefab = Resources.Load<GameObject>("Players/" + c.id);
            if (prefab == null)
            {
                Debug.LogError("[CharacterPortraits] Missing model Players/" + c.id);
                continue;
            }
            GameObject model = Object.Instantiate(prefab);
            for (int i = 0; i < 3; i++)
            {
                Render(cam, rt, null); // warm up freshly imported shaders
            }
            Render(cam, rt, Path.Combine(PortraitDir, c.id + ".png"));
            Object.DestroyImmediate(model);
        }
        Object.DestroyImmediate(rt);
    }

    static void CaptureCharacterSelect()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/MenuScene.unity");
        MenuSceneSetup setup = new GameObject("MenuSetupPreview").AddComponent<MenuSceneSetup>();
        BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        typeof(MenuSceneSetup).GetMethod("SetupScene", flags).Invoke(setup, null);
        typeof(MenuSceneSetup).GetMethod("ShowCharacterSelect", flags).Invoke(setup, null);
        typeof(MenuSceneSetup).GetMethod("SelectCharacter", flags).Invoke(setup, new object[] { "mochi" });

        Camera cam = Camera.main != null ? Camera.main : MakeCamera(new Color(0.1f, 0.1f, 0.14f));
        Canvas canvas = GameObject.Find("MenuCanvas").GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 1f;
        RenderTexture rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        cam.targetTexture = rt;
        Canvas.ForceUpdateCanvases();
        Render(cam, rt, null);
        Render(cam, rt, Path.Combine(CheckDir, "character_select.png"));
        cam.targetTexture = null;
        Object.DestroyImmediate(rt);
    }

    static void CapturePlayerInTown()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/TownScene.unity");
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("[CharacterPortraits] No player in TownScene");
            return;
        }
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.transform.localScale = Vector3.one * 4f;
        ground.transform.position = new Vector3(player.transform.position.x, 0f, player.transform.position.z);

        GameObject model = PlayerCharacters.ApplyTo(player);
        Camera cam = Camera.main;
        RenderTexture rt = new RenderTexture(960, 540, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        Vector3 p = player.transform.position;
        // Behind (the gameplay view) and in front (to check the face points forward).
        foreach (var view in new[] { new { name = "behind", dir = -player.transform.forward }, new { name = "front", dir = player.transform.forward } })
        {
            cam.transform.position = p + view.dir * 6f + Vector3.up * 2.5f;
            cam.transform.LookAt(p + Vector3.up * 0.2f);
            Render(cam, rt, null);
            Render(cam, rt, Path.Combine(CheckDir, "player_" + view.name + ".png"));
        }
        Debug.Log(string.Format("[CharacterPortraits] Player model={0} footY={1:F2} capsuleHidden={2}",
            model != null ? model.name : "none", model != null ? model.transform.position.y - 0f : 0f,
            player.GetComponent<MeshRenderer>() != null && !player.GetComponent<MeshRenderer>().enabled));
        Object.DestroyImmediate(rt);
    }

    static void Render(Camera cam, RenderTexture rt, string path)
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
        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        RenderTexture.active = previous;
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }
}

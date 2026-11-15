using System.IO;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEngine;

// Creates the co-op network prefabs in Assets/Resources/Net (safe to run again):
//   NetPlayer - the solo TownPlayer plus networking (owner-driven movement)
//   NetEnemy  - an empty enemy shell the host fills in (host-driven movement)
//   NetParty  - the shared party state (travel, purse, votes, effects)
// Menu: AWEJ > Setup Multiplayer. Batch: -executeMethod NetworkSetup.RunBatch
public static class NetworkSetup
{
    const string Folder = "Assets/Resources/Net";

    [MenuItem("AWEJ/Setup Multiplayer")]
    public static void RunMenu()
    {
        Run();
        EditorUtility.DisplayDialog("Multiplayer", "Network prefabs created in " + Folder + ".", "OK");
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
        BuildPlayer();
        BuildEnemy();
        BuildParty();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[NetworkSetup] Done");
    }

    static void BuildPlayer()
    {
        GameObject contents = PrefabUtility.LoadPrefabContents("Assets/Prefabs/TownPlayer.prefab");
        contents.name = "NetPlayer";
        Ensure<PlayerAvatar>(contents);
        Ensure<NetworkObject>(contents);
        NetworkTransform nt = Ensure<NetworkTransform>(contents);
        nt.AuthorityMode = NetworkTransform.AuthorityModes.Owner;
        nt.SyncRotAngleX = false;
        nt.SyncRotAngleZ = false;
        nt.SyncScaleX = nt.SyncScaleY = nt.SyncScaleZ = false;
        nt.Interpolate = true;
        Ensure<NetPlayer>(contents);
        PrefabUtility.SaveAsPrefabAsset(contents, Folder + "/NetPlayer.prefab");
        PrefabUtility.UnloadPrefabContents(contents);
    }

    static void BuildEnemy()
    {
        GameObject go = new GameObject("NetEnemy");
        go.AddComponent<CharacterController>();
        go.AddComponent<Enemy>();
        go.AddComponent<NetworkObject>();
        NetworkTransform nt = go.AddComponent<NetworkTransform>();
        nt.AuthorityMode = NetworkTransform.AuthorityModes.Server;
        nt.SyncRotAngleX = false;
        nt.SyncRotAngleZ = false;
        nt.SyncScaleX = nt.SyncScaleY = nt.SyncScaleZ = false;
        nt.Interpolate = true;
        go.AddComponent<EnemyNet>();
        PrefabUtility.SaveAsPrefabAsset(go, Folder + "/NetEnemy.prefab");
        Object.DestroyImmediate(go);
    }

    static void BuildParty()
    {
        GameObject go = new GameObject("NetParty");
        go.AddComponent<NetworkObject>();
        go.AddComponent<PartyState>();
        PrefabUtility.SaveAsPrefabAsset(go, Folder + "/NetParty.prefab");
        Object.DestroyImmediate(go);
    }

    static T Ensure<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        return c != null ? c : go.AddComponent<T>();
    }
}

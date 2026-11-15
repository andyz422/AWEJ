using System;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

// Starts and stops co-op sessions. Solo play never touches this.
// Wi-Fi (LAN): the host listens on Port; others join with the host's IP address.
// Online room codes (Unity Relay) are layered on top in OnlineSession.
public class NetSession : MonoBehaviour
{
    public const ushort Port = 7777;
    public const int MaxPlayers = 4;
    public const string GameScene = "TownScene";
    public const string MenuScene = "MenuScene";

    public static NetSession Instance { get; private set; }

    public static GameObject PlayerPrefab { get; private set; }
    public static GameObject EnemyPrefab { get; private set; }
    public static GameObject PartyPrefab { get; private set; }

    public NetworkManager Manager { get; private set; }
    public UnityTransport Transport { get; private set; }

    // Raised on this device when the session ends (left, host quit, or connection lost).
    public static event Action<string> Ended;

    private bool leaving;

    public static NetSession Ensure()
    {
        if (Instance != null)
        {
            return Instance;
        }

        GameObject go = new GameObject("NetSession");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<NetSession>();
        Instance.Setup();
        return Instance;
    }

    void Setup()
    {
        PlayerPrefab = Resources.Load<GameObject>("Net/NetPlayer");
        EnemyPrefab = Resources.Load<GameObject>("Net/NetEnemy");
        PartyPrefab = Resources.Load<GameObject>("Net/NetParty");

        Transport = gameObject.AddComponent<UnityTransport>();
        Manager = gameObject.AddComponent<NetworkManager>();
        Manager.NetworkConfig = new NetworkConfig
        {
            NetworkTransport = Transport,
            EnableSceneManagement = true,
            ConnectionApproval = true,
            TickRate = 30,
        };
        foreach (GameObject prefab in new[] { PlayerPrefab, EnemyPrefab, PartyPrefab })
        {
            if (prefab != null)
            {
                Manager.AddNetworkPrefab(prefab);
            }
            else
            {
                Debug.LogError("[NetSession] Missing network prefab in Resources/Net. Run AWEJ > Setup Multiplayer.");
            }
        }

        Manager.ConnectionApprovalCallback = ApproveConnection;
        Manager.OnClientConnectedCallback += OnClientConnected;
        Manager.OnClientDisconnectCallback += OnClientDisconnected;
    }

    // At most four players, and only while the game is in the lobby or running.
    void ApproveConnection(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        bool full = Manager.ConnectedClientsIds.Count >= MaxPlayers;
        response.Approved = !full;
        response.Reason = full ? "The party is full (4 players)." : "";
        response.CreatePlayerObject = false;
    }

    // ---------------------------------------------------------------- start / stop

    // Before an online (Relay) session starts the host or client.
    public void PrepareOnline()
    {
        leaving = false;
    }

    public bool HostLan()
    {
        leaving = false;
        Transport.SetConnectionData("127.0.0.1", Port, "0.0.0.0");
        return Manager.StartHost();
    }

    public bool JoinLan(string address)
    {
        leaving = false;
        Transport.SetConnectionData(string.IsNullOrWhiteSpace(address) ? "127.0.0.1" : address.Trim(), Port);
        return Manager.StartClient();
    }

    // Host: everyone in the lobby goes to the first area together.
    public void StartGame()
    {
        if (!Manager.IsServer)
        {
            return;
        }
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetRun();
        }
        Manager.SceneManager.OnLoadEventCompleted -= OnSceneLoaded;
        Manager.SceneManager.OnLoadEventCompleted += OnSceneLoaded;
        Manager.SceneManager.LoadScene(GameScene, LoadSceneMode.Single);
    }

    public void Leave()
    {
        leaving = true;
        OnlineSession.Leave();
        if (Manager != null && Manager.IsListening)
        {
            Manager.Shutdown();
        }
        ReturnToMenu("");
    }

    void ReturnToMenu(string reason)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.isTransitioning = false;
            GameManager.Instance.isInDialogue = false;
        }
        Ended?.Invoke(reason);
        if (SceneManager.GetActiveScene().name != MenuScene)
        {
            SceneManager.LoadScene(MenuScene);
        }
    }

    // ---------------------------------------------------------------- spawning (host)

    void OnSceneLoaded(string sceneName, LoadSceneMode mode, System.Collections.Generic.List<ulong> completed, System.Collections.Generic.List<ulong> timedOut)
    {
        if (!Manager.IsServer || sceneName != GameScene)
        {
            return;
        }
        if (PartyState.Instance == null && PartyPrefab != null)
        {
            Instantiate(PartyPrefab).GetComponent<NetworkObject>().Spawn();
        }
        foreach (ulong clientId in Manager.ConnectedClientsIds)
        {
            SpawnPlayer(clientId);
        }
    }

    void OnClientConnected(ulong clientId)
    {
        // Late joiner: the scene sync brings them into the town; give them a player.
        if (Manager.IsServer && SceneManager.GetActiveScene().name == GameScene && PartyState.Instance != null)
        {
            SpawnPlayer(clientId);
        }
    }

    void SpawnPlayer(ulong clientId)
    {
        if (PlayerPrefab == null || Manager.ConnectedClients[clientId].PlayerObject != null)
        {
            return;
        }
        Vector3 at = Vector3.up * 1.1f;
        if (AreaManager.Instance != null)
        {
            at = AreaManager.Instance.SpawnPoint + Vector3.up * 1.1f;
        }
        GameObject player = Instantiate(PlayerPrefab, at, Quaternion.identity);
        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
    }

    void OnClientDisconnected(ulong clientId)
    {
        if (leaving)
        {
            return;
        }
        // On a guest this fires when we lost the host (or were refused).
        if (!Manager.IsServer && clientId == Manager.LocalClientId)
        {
            string reason = Manager.DisconnectReason;
            leaving = true;
            ReturnToMenu(string.IsNullOrEmpty(reason) ? "Disconnected from the host." : reason);
        }
    }
}

#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Automated two-player co-op test, only in development builds. Run one copy with
// "-awejtest host" and another with "-awejtest client"; both log lines starting with NETTEST.
// The host drives the scenario; the client votes yes, makes one purchase and reports what it sees.
public class NetAutoTest : MonoBehaviour
{
    string role;
    int failures;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "-awejtest")
            {
                GameObject go = new GameObject("NetAutoTest");
                DontDestroyOnLoad(go);
                go.AddComponent<NetAutoTest>().role = args[i + 1];
            }
        }
    }

    void Start()
    {
        Application.targetFrameRate = 60;
        Application.runInBackground = true;
        StartCoroutine(role == "host" ? Host() : Client());
    }

    static void Log(string message)
    {
        Debug.Log("NETTEST " + message);
    }

    void Check(bool ok, string what)
    {
        Log((ok ? "PASS " : "FAIL ") + what);
        if (!ok)
        {
            failures++;
        }
    }

    static IEnumerator WaitFor(System.Func<bool> condition, float timeout)
    {
        float end = Time.realtimeSinceStartup + timeout;
        while (!condition() && Time.realtimeSinceStartup < end)
        {
            yield return null;
        }
    }

    static GameManager GM
    {
        get { return GameManager.Instance; }
    }

    static NetPlayer FindPlayer(bool local)
    {
        foreach (PlayerAvatar p in PlayerRegistry.All)
        {
            if (p != null && p.Net != null && p.Net.IsSpawned && p.IsLocal == local)
            {
                return p.Net;
            }
        }
        return null;
    }

    // ---------------------------------------------------------------- host

    IEnumerator Host()
    {
        yield return WaitFor(() => GM != null, 10f);
        GM.playerCharacter = "pip";
        GM.ResetRun();
        Check(NetSession.Ensure().HostLan(), "host started");

        yield return WaitFor(() => NetworkManager.Singleton.ConnectedClientsIds.Count >= 2, 90f);
        Check(NetworkManager.Singleton.ConnectedClientsIds.Count >= 2, "client connected");

        NetSession.Instance.StartGame();
        yield return WaitFor(() => AreaManager.Instance != null && AreaManager.Instance.Area != null && PlayerRegistry.Count >= 2, 60f);
        Check(PlayerRegistry.Count == 2, "two players in town (count=" + PlayerRegistry.Count + ")");
        NetPlayer guest = FindPlayer(false);
        yield return WaitFor(() => guest != null && guest.CharacterId == "mochi", 5f);
        Check(guest != null && guest.CharacterId == "mochi", "guest shows their own character (" + (guest != null ? guest.CharacterId : "none") + ")");
        Check(PartyState.Instance != null && PartyState.Instance.PlayerCount.Value == 2, "party counts 2 players");
        Check(PlayerRegistry.Local != null && PlayerRegistry.Local.Net != null && PlayerRegistry.Local.Net.IsOwner,
            "host's local player is its own (camera/interactions follow the right player)");

        // Door vote into a battle building.
        yield return new WaitForSeconds(1f);
        PartyState.Instance.RequestTravel(0, "training_barn", AreaEntry.IntoBuilding, "", "", "Enter Training Barn?", GM.loopCount);
        yield return WaitFor(() => PartyState.Instance.Vote.Value.active, 5f);
        Check(PartyState.Instance.Vote.Value.active, "vote started");
        yield return WaitFor(() => GM.currentBuilding == "training_barn", 30f);
        Check(GM.currentBuilding == "training_barn", "vote passed and party entered the Training Barn");

        // Scaled waves.
        yield return WaitFor(() => Enemy.Alive.Count > 0, 20f);
        yield return new WaitForSeconds(1.5f);
        Check(Enemy.Alive.Count >= 5, "first wave scaled for 2 players (enemies=" + Enemy.Alive.Count + ", solo=3)");
        Check(PartyState.Instance.BattleInProgress.Value, "battle marked in progress for guests");

        int coinsBefore = GM.playerCoins;
        StartCoroutine(WatchForSpend(30)); // the guest buys a 30-coin Heart Container right after the win
        float end = Time.realtimeSinceStartup + 120f;
        while (PartyState.Instance.BattleInProgress.Value && Time.realtimeSinceStartup < end)
        {
            foreach (Enemy e in new List<Enemy>(Enemy.Alive))
            {
                e.TakeDamage(9999);
            }
            yield return new WaitForSeconds(0.4f);
        }
        Check(!PartyState.Instance.BattleInProgress.Value, "battle won");
        yield return new WaitForSeconds(2f);
        Check(peakCoins > coinsBefore, "shared purse grew (" + coinsBefore + " -> peak " + peakCoins + ")");
        Check(GM.IsBattleCleared("training_barn"), "battle recorded as cleared");

        // Guest bought an upgrade from the shared purse: the purse paid, only the guest got stronger.
        guest = FindPlayer(false);
        yield return WaitFor(() => spendSeen && guest.MaxHealth.Value == PlayerProfile.StartHealth + 1, 15f);
        Check(spendSeen, "guest's purchase was paid from the shared purse (-30)");
        Check(guest.MaxHealth.Value == PlayerProfile.StartHealth + 1, "guest got the Heart Container (max hp " + guest.MaxHealth.Value + ")");
        Check(GM.maxPlayerHealth == PlayerProfile.StartHealth, "host's own upgrades unchanged (max hp " + GM.maxPlayerHealth + ")");

        // Downed and revived.
        guest.HurtRpc(99);
        yield return WaitFor(() => guest.Downed.Value, 10f);
        Check(guest.Downed.Value, "guest went down at 0 HP (not defeated)");
        Check(PlayerRegistry.StandingCount == 1, "enemies/coins now ignore the downed guest");
        guest.ReviveRpc();
        yield return WaitFor(() => !guest.Downed.Value && guest.Health.Value > 0, 10f);
        Check(!guest.Downed.Value && guest.Health.Value > 0, "guest revived (hp=" + guest.Health.Value + "/" + guest.MaxHealth.Value + ")");

        // Vote back outside.
        PartyState.Instance.RequestTravel(0, "", AreaEntry.FromBuilding, "training_barn", "", "Leave Training Barn?", GM.loopCount);
        yield return WaitFor(() => GM.currentBuilding == "", 30f);
        Check(GM.currentBuilding == "", "vote passed and party left the building");

        // Whole party down: everyone is sent somewhere safe, healed.
        // (Wait out the guest's post-revive invincibility first.)
        yield return new WaitForSeconds(3.5f);
        int serial = PartyState.Instance.Travel.Value.serial;
        guest = FindPlayer(false);
        NetPlayer me = FindPlayer(true);
        guest.HurtRpc(99);
        me.HurtRpc(99);
        yield return WaitFor(() => PartyState.Instance.Travel.Value.serial != serial, 15f);
        Check(PartyState.Instance.Travel.Value.serial != serial, "party wipe triggered a respawn trip");
        yield return new WaitForSeconds(3f);
        NetPlayer guestAfter = FindPlayer(false);
        Check(!me.Downed.Value && guestAfter != null && !guestAfter.Downed.Value,
            "everyone back up after the wipe (host downed=" + me.Downed.Value + ", guest downed=" + (guestAfter != null && guestAfter.Downed.Value) + ")");
        Check(GM.playerHealth == GM.maxPlayerHealth, "host healed after the wipe");

        Log(failures == 0 ? "HOST DONE: all checks passed" : "HOST DONE: " + failures + " check(s) failed");
        yield return new WaitForSeconds(3f);
        Application.Quit(failures == 0 ? 0 : 1);
    }

    int peakCoins;
    bool spendSeen;

    // Watches the shared purse every frame for a drop of exactly `amount` (a purchase).
    IEnumerator WatchForSpend(int amount)
    {
        int previous = GM.playerCoins;
        float end = Time.realtimeSinceStartup + 150f;
        while (!spendSeen && Time.realtimeSinceStartup < end)
        {
            int current = GM.playerCoins;
            peakCoins = Mathf.Max(peakCoins, current);
            if (previous - current == amount)
            {
                spendSeen = true;
            }
            previous = current;
            yield return null;
        }
    }

    // ---------------------------------------------------------------- client

    IEnumerator Client()
    {
        yield return WaitFor(() => GM != null, 10f);
        GM.playerCharacter = "mochi";
        GM.ResetRun();
        yield return new WaitForSeconds(4f); // give the host time to start listening
        Check(NetSession.Ensure().JoinLan("127.0.0.1"), "client started");
        bool ended = false;
        NetSession.Ended += reason => ended = true;

        int votedFor = -1;
        bool bought = false;
        float nextReport = 0f;
        float end = Time.realtimeSinceStartup + 300f;
        while (!ended && Time.realtimeSinceStartup < end)
        {
            PartyState party = PartyState.Instance;
            if (party != null && party.IsSpawned)
            {
                if (party.Vote.Value.active && party.Vote.Value.id != votedFor)
                {
                    votedFor = party.Vote.Value.id;
                    party.SendVote(true);
                    Log("voted yes on '" + party.Vote.Value.title + "'");
                }
                if (!bought && party.Coins.Value >= 20 && GM.currentBuilding == "training_barn" && !party.BattleInProgress.Value)
                {
                    bought = true;
                    Log("buying hp upgrade: " + ShopCatalog.Purchase("hp"));
                }
            }
            if (Time.realtimeSinceStartup >= nextReport)
            {
                nextReport = Time.realtimeSinceStartup + 2f;
                NetPlayer me = FindPlayer(true);
                Log(string.Format("state area={0} building='{1}' coins={2} enemies={3} players={4} myHp={5}/{6} downed={7} battle={8} built={9}",
                    GM.currentArea, GM.currentBuilding, GM.playerCoins, Enemy.Alive.Count, PlayerRegistry.Count,
                    GM.playerHealth, GM.maxPlayerHealth, me != null && me.Downed.Value,
                    party != null && party.BattleInProgress.Value, AreaManager.Instance != null && AreaManager.Instance.Area != null));
            }
            yield return null;
        }
        Log("CLIENT DONE (session ended=" + ended + ")");
        Application.Quit(0);
    }
}
#endif

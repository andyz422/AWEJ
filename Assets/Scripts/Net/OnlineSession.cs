using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

// Online co-op through Unity Relay: the host gets a short room code, friends anywhere join with it.
// Uses the same NetSession/NetworkManager as Wi-Fi play; the Sessions package points it at Relay
// and starts the host or client.
//
// Needs the project linked to Unity Gaming Services (Edit > Project Settings > Services) so Relay
// knows which project this is. Until then HostAsync/JoinAsync fail with a readable message.
public static class OnlineSession
{
    public static ISession Current { get; private set; }

    // The code friends type to join, while hosting online.
    public static string Code
    {
        get { return Current != null ? Current.Code : ""; }
    }

    public static bool IsOnline
    {
        get { return Current != null; }
    }

    const string NotLinked = "Online play isn't set up yet: link the project to Unity Gaming Services " +
        "(Edit > Project Settings > Services), or play over Wi-Fi.";

    static async Task SignIn()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            await UnityServices.InitializeAsync();
        }
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    // Returns the room code, or throws with a message suitable for the menu.
    public static async Task<string> HostAsync()
    {
        NetSession.Ensure();
        try
        {
            await SignIn();
            SessionOptions options = new SessionOptions
            {
                MaxPlayers = NetSession.MaxPlayers,
                IsPrivate = true, // join by code only, never listed publicly
            }.WithRelayNetwork();
            Current = await MultiplayerService.Instance.CreateSessionAsync(options);
            return Current.Code;
        }
        catch (Exception e)
        {
            Current = null;
            throw new Exception(Explain(e), e);
        }
    }

    public static async Task JoinAsync(string code)
    {
        NetSession.Ensure();
        code = (code ?? "").Trim().ToUpperInvariant();
        if (code.Length == 0)
        {
            throw new Exception("Type the room code your friend sees on their screen.");
        }
        try
        {
            await SignIn();
            Current = await MultiplayerService.Instance.JoinSessionByCodeAsync(code);
        }
        catch (Exception e)
        {
            Current = null;
            throw new Exception(Explain(e), e);
        }
    }

    // Called by NetSession.Leave; safe when not online.
    public static async void Leave()
    {
        ISession session = Current;
        Current = null;
        if (session == null)
        {
            return;
        }
        try
        {
            await session.LeaveAsync();
        }
        catch (Exception e)
        {
            Debug.LogWarning("[OnlineSession] Leave: " + e.Message);
        }
    }

    static string Explain(Exception e)
    {
        string message = e.Message ?? "";
        Debug.LogWarning("[OnlineSession] " + e);
        if (string.IsNullOrEmpty(Application.cloudProjectId) || message.IndexOf("project", StringComparison.OrdinalIgnoreCase) >= 0
            && message.IndexOf("id", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return NotLinked;
        }
        if (message.IndexOf("not found", StringComparison.OrdinalIgnoreCase) >= 0
            || message.IndexOf("SessionNotFound", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "No game with that code. Check the code and try again.";
        }
        if (message.IndexOf("full", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "That game is full (4 players).";
        }
        return "Couldn't connect online: " + message;
    }
}

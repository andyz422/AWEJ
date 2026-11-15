#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

// iOS-only Info.plist additions, applied to the exported Xcode project after every iOS build
// (including Codemagic's). Compiles only when the iOS build target is active.
public static class IosBuildSettings
{
    [PostProcessBuild]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target != BuildTarget.iOS)
        {
            return;
        }

        string plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
        PlistDocument plist = new PlistDocument();
        plist.ReadFromFile(plistPath);
        PlistElementDict root = plist.root;

        // Wi-Fi co-op talks to other phones on the same network; iOS 14+ asks the player first and
        // blocks local connections without this explanation.
        root.SetString("NSLocalNetworkUsageDescription",
            "AWEJ connects to your friends' phones on the same Wi-Fi so you can play co-op together.");

        plist.WriteToFile(plistPath);
    }
}
#endif

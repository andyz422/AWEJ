# AWEJ

A 3D Unity game for iPhone (with a PC build for development). You explore areas, enter shops and battle buildings, and shoot enemies.

- **Unity:** 6000.1.13f1
- **Scenes (build order):** `MenuScene` → `TownScene` (hosts every area and building interior)
- **Input:** Input System package plus legacy input (Active Input Handling = Both)

## Controls

| Action | PC | iPhone |
| --- | --- | --- |
| Move | WASD / arrow keys | Joystick (bottom-left) |
| Fire (hold) | Left mouse button | **Fire** button (bottom-right) |
| Interact | E | **Use** button (shown when near a door, gate, NPC or pedestal) |
| Swap weapon | Q | **Swap** button (shown when you own 2+ weapons) |

Shots fire in the direction the player is facing, with aim assist toward the nearest enemy in front. The on-screen controls are built in code by `Assets/Scripts/UI/MobileControls.cs` and only appear on mobile.

## Testing in the editor

1. Open `Assets/Scenes/MenuScene`.
2. **Window → General → Device Simulator**, pick an iPhone, rotate to landscape.
3. Press Play and click **New Game**.

The mouse acts as a single finger, so you can't move and fire at the same time in the simulator. Use a TestFlight build for real multi-touch.

## TODO: iOS builds on Codemagic

No Mac is needed: Codemagic's macOS machines run Unity, export the Xcode project, then build, sign and upload it.

### Apple / App Store Connect

- [ ] Choose a real bundle ID (reverse domain you own) and set it in **Player Settings → iOS → Bundle Identifier**. It's currently the placeholder `com.AWEJ.AWEJ`.
- [ ] Register that bundle ID in the Apple Developer portal and create the app record in App Store Connect.
- [ ] Create an App Store Connect API key (Users and Access → Integrations) if you don't already have one from the previous game, and add it to Codemagic as a team integration.
- [ ] Set the version and build number in Player Settings (`bundleVersion` is `1.0`). Use a new build number for every upload, or have the build script set it from `$BUILD_NUMBER`.

### Unity licensing on Codemagic

- [ ] Check Codemagic's current Unity docs for which license types can be activated on their build machines. Plus/Pro serials have historically been required; Personal licenses may be restricted.
- [ ] Add the license credentials as secure environment variables in a Codemagic group (e.g. `unity_credentials`): `UNITY_EMAIL`, `UNITY_PASSWORD`, and `UNITY_SERIAL` if your license uses one.
- [ ] Return the license at the end of each build (`-returnlicense`) so it doesn't use up activation seats.
- [ ] Confirm Unity **6000.1.13f1** with iOS Build Support is available on the Codemagic machine image. If it isn't, install it in a build step with the Unity Hub CLI, or upgrade the project to a version they ship.

### Project changes

- [ ] Add an editor build script (e.g. `Assets/Editor/BuildScript.cs`) with a static method like `BuildScript.BuildIos()` that calls `BuildPipeline.BuildPlayer` with the enabled scenes, `BuildTarget.iOS`, and an output folder such as `ios/`.
- [ ] Add `ios/` (the exported Xcode project) to `.gitignore`.
- [ ] Set **Player Settings → iOS → Target minimum iOS version** (currently `13.0`) to whatever you want to support.
- [ ] Add app icons and a launch screen in Player Settings → iOS.
- [ ] Set the export-compliance flag (`ITSAppUsesNonExemptEncryption` = `false` if you only use standard HTTPS) so TestFlight doesn't ask on every upload. You can set it via a post-process build script that edits `Info.plist`.

### `codemagic.yaml`

- [ ] Add `codemagic.yaml` at the repo root with a workflow that:
  1. Uses a `mac_mini_m2` (or current macOS) instance and the `unity_credentials` env group.
  2. Activates the Unity license.
  3. Runs Unity in batch mode: `-batchmode -quit -nographics -projectPath . -executeMethod BuildScript.BuildIos -logFile -`.
  4. Fetches signing files: `app-store-connect fetch-signing-files "<bundle id>" --type IOS_APP_STORE --create`, then `keychain initialize`, `keychain add-certificates` and `xcode-project use-profiles --project ios/Unity-iPhone.xcodeproj`.
  5. Builds the IPA: `xcode-project build-ipa --project ios/Unity-iPhone.xcodeproj --scheme Unity-iPhone`.
  6. Returns the Unity license.
  7. Publishes to App Store Connect with `submit_to_testflight: true`.
- [ ] Run the first build manually and check the Unity log for build errors. Signing problems usually show up on the `UnityFramework` target or with a bundle ID mismatch.
- [ ] Install from TestFlight and test on a real iPhone: multi-touch (move + fire together), safe-area layout around the notch, frame rate, and the full menu → town → shop/battle flow.

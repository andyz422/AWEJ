# AWEJ TODO

Open work, roughly in priority order. Done items are removed rather than ticked.

## Co-op (multiplayer)

Co-op for up to 4 players works over Wi-Fi and passes the automated two-player test (`Builds/NetTest`, see README). Still to do:

- [ ] **Link the project to Unity Gaming Services** (Edit → Project Settings → Services, from your Unity account) so **Host Online / Join Online** room codes work. Until then those buttons show a "not set up yet" message.
- [ ] Test online room codes once linked: host on one device, join with the code from another network.
- [ ] Test co-op on real iPhones (needs the Codemagic/TestFlight pipeline below): lag, joining over Wi-Fi, and what happens when iOS pauses the app in the background (a guest switching apps currently drops out).
- [ ] Reconnecting: a player who drops can rejoin but starts with a fresh profile (upgrades bought earlier in the run are lost).
- [ ] The Local Network permission prompt on iOS (Wi-Fi play) needs an `NSLocalNetworkUsageDescription` string in Info.plist.
- [ ] Revive feedback: a progress ring over the downed player, so teammates can see someone is being revived.
- [ ] Save/load (the **Load Game** button) is still unimplemented for both solo and co-op.

## Graphics (iPhone-friendly upgrade path)

Step 1 (URP + per-area lighting and post-processing) is done. Next:

- [ ] Step 2: bake textures for the 20 buildings and 10 shopkeepers in Blender, one material per model (fixes 15–20 draw calls per building).
- [ ] Step 3: replace code-built ground, walls and paths with textured terrain, modular wall/gate pieces, game-ready trees with LODs, and short-range GPU grass.
- [ ] Step 4: models with skeletons and simple animations for the player characters and enemies.
- [ ] Step 5: effects polish (animated lava/water, particles, hit flashes).
- [ ] Measure frame rate on a real iPhone after each step.

## Touch controls and HUD

- [ ] Floating joystick that appears wherever the left thumb touches.
- [ ] Bigger **Swap** button with more space above **Fire** (about 42 pt on an iPhone SE, under Apple's 44 pt guideline).
- [ ] Keep the top-left / top-right info panels and the bottom prompt inside the notch-safe area, like the controls.

## iOS builds on Codemagic

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

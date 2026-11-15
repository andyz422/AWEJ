# AWEJ TODO

Open work, roughly in priority order. Done items are removed rather than ticked.

## Pick up here (last updated 2026-11-15, commit `19bff30`)

### Where things stand

- **Co-op (up to 4 players)** is built: Wi-Fi host/join, online room codes (code only, see below), door votes, shared purse with personal upgrades, downed + 4 s revive with a progress ring, party wipe respawn, rejoining keeps upgrades, enemy scaling, party health HUD. The automated two-player test passes (30 host checks + guest checks).
- **Graphics step 1** is done: URP, Linear colour, per-area sky/lighting/fog/post-processing.
- **Character select**: four pill characters (Bean, Mochi, Sprout, Pip) after **New Game** / **Co-op**.
- **Blender assets**: 20 building models and 10 shopkeepers in `Assets/Resources/`, built by scripts in `Blender/`.

### First play session (started 2026-11-15)

Found and fixed in the first session: the old scene menu showed through the co-op hosting screen, and the camera didn't follow the player in solo (both fixed in `19bff30`, confirmed in the editor).

Known, not a bug: in Multiplayer Play Mode the extra player's window has no joystick/Fire buttons, because only a Device Simulator Game view counts as a phone. That window uses PC controls (WASD, mouse, E, Q). An editor-only "force touch controls" switch could be added if needed.

Still to try by hand:

- [ ] Solo: walk around town, enter a shop and a battle. Check the URP look, the character model and its bounce, the shopkeepers.
- [ ] Co-op in the editor (**Window → Multiplayer → Multiplayer Play Mode**, 1 extra player; Host on Wi-Fi in one window, **Join** `127.0.0.1` in the other): a door vote (on-screen Yes/No), a battle, buying something, going down and reviving (hold E next to the teammate, watch the ring), leaving and rejoining.
- [ ] Note anything that looks or feels off; the HUD layout and vote panel have only been seen in code until now.

### Next steps, in order

1. [ ] **Link Unity Gaming Services**: sign in to Unity in the editor → **Edit → Project Settings → Services** → create or choose a project ID → check **Relay** is enabled on cloud.unity.com → commit the changed `ProjectSettings/ProjectSettings.asset`.
2. [ ] **Test online room codes**: build for Windows, host online on one machine, join with the code from another network (a phone hotspot works).
3. [ ] **Set up iOS builds for this repo**: Codemagic/TestFlight already works in another project, so reuse its `codemagic.yaml`, signing and Unity licence setup (checklist at the bottom of this file). iOS Build Support isn't installed on this PC; Codemagic's machines build iOS.
4. [ ] **Test on iPhones** (TestFlight): touch controls, frame rate with URP, Wi-Fi co-op (the Local Network prompt should appear on first join), online co-op over mobile data, switching apps mid-game.
5. [ ] Then continue with the graphics steps and touch-control fixes below.

### Handy tools (AWEJ menu in the Unity editor)

- **Render Area Previews**: renders every area and a shop with the URP look to `Logs/AreaPreviews/`.
- **Check Building Models**: verifies building models and shopkeepers load and face the right way.
- **Build Co-op Test Player**: builds `Builds/NetTest/AWEJ.exe` for the automated two-player test (commands in the README).
- **Setup Multiplayer**: recreates the network prefabs; run after changing `NetPlayer`/`EnemyNet`/`PartyState` components.
- **Setup URP**, **Render Character Portraits**: one-off setup/regeneration tools.

### Good to know

- Only one Unity instance can open the project: close the editor before running any headless/batch command.
- Pushing goes straight to `master` (single-branch repo).
- Blender scripts (`Blender/*.py`) rebuild the scenes and re-export models; `Blender/AWEJ_World.blend` holds all the Blender scenes.

## Co-op (multiplayer)

Co-op for up to 4 players works over Wi-Fi and passes the automated two-player test (`Builds/NetTest`, see README). Still to do:

- [ ] Link Unity Gaming Services and test online room codes (steps 1–2 above). Until linked, **Host Online / Join Online** show a "not set up yet" message.
- [ ] Test co-op on real iPhones once iOS builds are set up for this repo (see the Codemagic checklist below): lag, joining over Wi-Fi (the Local Network permission prompt should appear on first Wi-Fi join), and what happens when iOS pauses the app in the background (a guest switching apps currently drops out; they can rejoin and keep their upgrades).
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

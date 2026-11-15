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

## Co-op (up to 4 players)

From the main menu choose **Co-op**, pick a character, then:

- **Same Wi-Fi:** one player taps **Host on Wi-Fi**; the others type the IP address shown in the host's lobby and tap **Join**.
- **Anywhere (online):** one player taps **Host Online** and shares the room code; the others type it and tap **Join Online**. Needs the project linked to Unity Gaming Services (see `TODO.md`).

The host presses **Start Game** when everyone is in. Rules:

- **Doors and gates are voted on.** Using one asks the whole party; everyone must say yes within 20 seconds (Y/N or the on-screen buttons).
- **Money is shared, upgrades are personal.** Every coin and battle reward goes into one purse; each player buys their own weapons and upgrades from it.
- **Downed, not dead.** At 0 HP you go down; a teammate holds **Use** next to you for 4 seconds to revive you at half health. A ring over the downed player fills as they're revived (everyone sees it). If everyone is down, the party wakes up somewhere safe, fully healed.
- **Dropping out is fine.** A player who disconnects can rejoin the same game and gets their weapons and upgrades back (the host remembers each device for the run).
- **Harder with friends.** Enemy health +60% and waves +50% per extra player.

Solo **New Game** never uses the network.

How it works: the host runs the shared world (enemies, battles, hits, coins, the purse, votes) and every device controls its own player. Code lives in `Assets/Scripts/Net/` (`NetSession`, `PartyState`, `NetPlayer`, `EnemyNet`, `OnlineSession`).

### Testing co-op

- In the editor: **Window → Multiplayer → Multiplayer Play Mode** runs up to 4 players in one editor.
- Automated: **AWEJ → Build Co-op Test Player**, then run two copies from `Builds/NetTest`:
  `AWEJ.exe -batchmode -nographics -awejtest host -awejid host1 -logFile host.log` and `AWEJ.exe -batchmode -nographics -awejtest client -awejid guest1 -logFile client.log` (`-awejid` gives each copy its own device identity). The host log ends with `NETTEST HOST DONE`.
- After changing the network prefabs' scripts, run **AWEJ → Setup Multiplayer**.

## TODO

See [TODO.md](TODO.md).

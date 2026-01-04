# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Deploy

```bash
# Build and deploy to Old World mods folder (macOS)
./deploy.sh

# Or build manually
export OldWorldPath="$OLDWORLD_PATH"
dotnet build -c Release
```

The deploy script copies `ModInfo.xml`, the built DLL, and `Newtonsoft.Json.dll` to:
`~/Library/Application Support/OldWorld/Mods/OldWorldAPIEndpoint/`

## Testing

### GUI Mode
1. Launch Old World and enable the mod in Mod Manager
2. Start or load a game
3. Connect: `nc localhost 9876`
4. End a turn to see JSON output

### Headless Mode (Automated)
```bash
# Run 5 turns with TCP capture and pretty-printed JSON output
./test-headless.sh /tmp/APITestSave.zip 5
```

The script builds, deploys, starts a TCP listener, runs Old World headless, and captures all JSON output.

## Architecture

This is an Old World game mod that broadcasts game state over TCP for companion apps.

### Key Constraint: No Assembly-CSharp Reference

Old World explicitly blocks mods from referencing `Assembly-CSharp.dll`. To access game state (`AppMain.gApp.Client.Game`), we use runtime reflection to find and invoke methods in Assembly-CSharp. The reflection is cached in `InitializeReflection()`.

Types in `TenCrowns.GameCore.dll` (Game, Player, Infos, etc.) can be referenced directly.

### Components

- **APIEndpoint.cs** - Mod entry point extending `ModEntryPointAdapter`. Hooks `OnNewTurnServer` and `OnGameServerReady` to broadcast game state. Uses reflection to access Game instance.

- **TcpBroadcastServer.cs** - TCP server on port 9876. Broadcasts newline-delimited JSON to all connected clients. Background thread accepts connections.

### Data Flow

```
Game Event (OnNewTurnServer) → GetGame() via reflection → Build JSON (players, cities) → TcpBroadcastServer.Broadcast()
```

Uses Newtonsoft.Json with `DefaultContractResolver` to preserve exact game type strings.

### Important Behaviors

- Server persists across game sessions (not stopped on `Shutdown()`) so clients stay connected
- Game instance may be null during menu screens - handle gracefully

### Headless Mode

Use `./test-headless.sh` for automated testing (see Testing section above).

Key points:
- Save file is a **positional argument** (not a flag)
- All mod hooks fire normally (`Initialize`, `OnNewTurnServer`, etc.)
- Use `GetLocalGameServer().LocalGame` to access Game (requires `BindingFlags.NonPublic`)
- `Client.Game` returns null in headless mode

See `docs/headless-mode-investigation.md` for details.

## Game API Patterns

Access player data via `Game` and `Infos`:
```csharp
Player[] players = game.getPlayers();
Infos infos = game.infos();
string nationName = infos.nation(player.getNation()).mzType;  // "NATION_ROME"
string yieldName = infos.yield(yieldType).mzType;             // "YIELD_FOOD"
int stockpile = player.getYieldStockpileWhole(yieldType);
```

Use `mzType` field on Info objects to get string identifiers (enum `.ToString()` returns numeric values).

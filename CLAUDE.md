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

The deploy script copies `ModInfo.xml` and the built DLL to:
`~/Library/Application Support/OldWorld/Mods/OldWorldAPIEndpoint/`

## Testing

1. Launch Old World and enable the mod in Mod Manager
2. Start or load a game
3. Connect: `nc localhost 9876`
4. End a turn to see JSON output

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
Game Event (OnNewTurnServer) → GetGame() via reflection → BuildPlayersJson() → TcpBroadcastServer.Broadcast()
```

### Important Behaviors

- Server persists across game sessions (not stopped on `Shutdown()`) so clients stay connected
- Game instance may be null during menu screens - handle gracefully

### Headless Mode Testing

Old World supports headless/autorunturns mode for automated testing:

```bash
# macOS
arch -x86_64 "/path/to/OldWorld.app/Contents/MacOS/OldWorld" \
    /path/to/save.zip -batchmode -headless -autorunturns 5
```

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

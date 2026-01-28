# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Deploy

```bash
# First-time setup: copy .env.example to .env and configure paths
cp .env.example .env
# Edit .env with your Old World installation path

# Build and deploy (preferred - handles everything)
./deploy.sh            # macOS/Linux
.\deploy.ps1           # Windows (PowerShell)

# Build only (if needed separately)
source .env && export OldWorldPath="$OLDWORLD_PATH" && dotnet build -c Release
```

**Important:** Never run bare `dotnet build` - it will fail because `OldWorldPath` won't be set. Always use `./deploy.sh` or the full command above.

The deploy scripts copy `ModInfo.xml`, the built DLL, and `Newtonsoft.Json.dll` to the mods directory configured in `.env`.

**Windows notes:** The PowerShell script supports `%APPDATA%` expansion in paths. See `.env.example` for Windows path examples.

## Versioning

Version is managed in `ModInfo.xml` (single source of truth) using semantic versioning.

```bash
# Bump version
./bump-version.sh patch    # 0.0.2 -> 0.0.3
./bump-version.sh minor    # 0.0.2 -> 0.1.0
./bump-version.sh major    # 0.0.2 -> 1.0.0
./bump-version.sh 1.2.3    # Set explicit version
```

**Release workflow:**
1. `./bump-version.sh <type>` - Bump version in ModInfo.xml
2. Update `CHANGELOG.md` with changes for the new version
3. `./workshop-upload.sh` - Upload to Steam Workshop (reads version + changelog automatically)
4. `./modio-upload.sh` - Upload to mod.io (reads version + changelog automatically)

Both upload scripts extract the changelog for the current version from `CHANGELOG.md` automatically. You can override with a custom message: `./workshop-upload.sh "Custom changelog"`.

## Testing

**Always use headless testing to verify API changes.** This is the primary testing method.

```bash
# Test with 2 turns (enough to verify data and see changes between turns)
./test-headless.sh /tmp/APITestSave.zip 2
```

The script builds, deploys, starts a TCP listener, runs Old World headless, and captures all JSON output with pretty-printing. Use this to verify:
- New fields appear in the JSON output
- Values are correct format (whole numbers, proper signs, correct keys)
- No errors in the game log

### GUI Mode (Manual)
Only use GUI mode when you need to:
- Debug issues that don't reproduce in headless mode
- Test with a specific game state that requires manual setup
- Verify behavior during active gameplay

Steps:
1. Launch Old World and enable the mod in Mod Manager
2. Start or load a game
3. Connect: `nc localhost 9876`
4. End a turn to see JSON output

## Architecture

This is an Old World game mod that broadcasts game state over TCP for companion apps.

### Key Constraint: No Assembly-CSharp Reference

Old World explicitly blocks mods from referencing `Assembly-CSharp.dll`. To access game state (`AppMain.gApp.Client.Game`), we use runtime reflection to find and invoke methods in Assembly-CSharp. The reflection is cached in `InitializeReflection()`.

Types in `TenCrowns.GameCore.dll` (Game, Player, Infos, etc.) can be referenced directly.

### Game Source Reference

Decompiled game source code is available at `$OLDWORLD_PATH/Reference/` (where `$OLDWORLD_PATH` is configured in `.env`).

Use this to understand game internals, discover available methods, and find the correct property/method signatures.

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

## API Design Principles

When adding new data to the API, follow these principles (see `docs/api-design-principles.md` for full details):

1. **Mirror Game Data Structures** - Model entities as the game does (e.g., cities at top level with `ownerId`, not nested under players)
2. **Expose All Available Data** - Don't assume what clients need; expose complete field sets
3. **Use Game Type Strings** - Use exact game identifiers like `NATION_ROME`, `YIELD_FOOD`, `IMPROVEMENT_FARM` (from `mzType` fields)

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

## Null Checking Patterns

For new code, follow these conventions:

1. **Control flow** (early return/continue): Use traditional checks
   ```csharp
   if (player == null) return;
   if (unit == null) continue;
   ```

2. **Property chain access**: Use null-conditional
   ```csharp
   infos.nation(city.getNation())?.mzType
   ```

3. **Default values**: Use null-coalescing
   ```csharp
   value?.ToString() ?? "default"
   ```

Existing code uses these patterns consistently within their contexts.
Do not refactor working null checks without clear benefit.

## Post-Implementation Checklist

After making code changes, always complete these steps:

1. **Build & Deploy:** `./deploy.sh`
2. **Test:** `./test-headless.sh "/Users/jeff/Library/Application Support/OldWorld/Saves/APITestSave.zip" 2`
3. **Verify:** Check the test output for:
   - No errors in game log
   - New fields appear in JSON output
   - Expected event counts in log messages
4. **Update Documentation** (if API changed) - see below

## Documentation

API documentation lives in `docs/` and is served via GitHub Pages (Docsify, no build step).

**Documentation site:** https://becked.github.io/OldWorldAPIEndpoint/

### When to Update Docs

Update documentation when:
- Adding/removing/renaming fields in any data model
- Adding/removing REST endpoints
- Adding new event types
- Changing response structure

### Files to Update

| Change Type | Files to Update |
|-------------|-----------------|
| New/changed fields | `docs/schemas/{entity}.schema.json`, `docs/schemas/{entity}.md` |
| New endpoint | `docs/openapi.yaml`, `docs/api-reference.md` |
| New event type | `docs/schemas/events.schema.json`, `docs/schemas/events.md` |
| New entity type | Create new schema files, update `docs/_sidebar.md` |

### Key Documentation Files

- `docs/openapi.yaml` - OpenAPI 3.0 spec (16 endpoints)
- `docs/schemas/*.schema.json` - JSON Schema definitions
- `docs/schemas/*.md` - Human-readable schema docs
- `docs/api-reference.md` - Endpoint reference with examples
- `docs/_sidebar.md` - Navigation structure

### Local Preview

```bash
npx docsify-cli serve docs
# Opens at http://localhost:3000
```

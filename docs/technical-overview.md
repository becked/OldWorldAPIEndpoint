# Old World API Endpoint - Technical Documentation

## Overview

Old World API Endpoint is a mod for the strategy game Old World that exposes game state and commands via TCP and HTTP APIs, enabling companion apps, overlays, AI agents, and external tools to both read game data and control the game.

## Architecture

```
┌──────────────────────────────────────────────────────────────────────┐
│                         Old World Game                                │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │                   OldWorldAPIEndpoint.dll                       │  │
│  │  ┌───────────────────┐                                          │  │
│  │  │   APIEndpoint     │                                          │  │
│  │  │ (ModEntryPoint    │                                          │  │
│  │  │    Adapter)       │                                          │  │
│  │  └────────┬──────────┘                                          │  │
│  │           │                                                      │  │
│  │  ┌────────┴────────┬─────────────────────┐                      │  │
│  │  │                 │                     │                      │  │
│  │  ▼                 ▼                     ▼                      │  │
│  │  ┌─────────────┐  ┌─────────────────┐  ┌──────────────────┐    │  │
│  │  │ TCP Server  │  │ HTTP Server     │  │ Command Queue    │    │  │
│  │  │ Port 9876   │  │ Port 9877       │  │ (Main Thread)    │    │  │
│  │  │ Push events │  │ GET: Query      │  │ Execute actions  │    │  │
│  │  │             │  │ POST: Commands  │  │                  │    │  │
│  │  └─────────────┘  └─────────────────┘  └──────────────────┘    │  │
│  └────────────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────────┘
         │                      │
         │ TCP                  │ HTTP
         │ Push notifications   │ Request/Response
         ▼                      ▼
┌─────────────────────────────────────────┐
│              Client Apps                 │
│  - Companion apps    - AI agents         │
│  - Stream overlays   - Automation tools  │
│  - Custom dashboards                     │
└─────────────────────────────────────────┘
```

## Key Components

### APIEndpoint.cs

Main entry point extending `ModEntryPointAdapter`. Responsibilities:
- Initialize TCP and HTTP servers on mod load
- Hook into game lifecycle events (`OnNewTurnServer`, `OnGameServerReady`, `OnClientUpdate`)
- Access game state via reflection (required because mods cannot reference Assembly-CSharp.dll)
- Build JSON payloads and broadcast to connected clients
- Process command queue on main thread via `OnClientUpdate()`

### TcpBroadcastServer.cs

TCP push notification server:
- Listens on `localhost:9876`
- Accepts multiple concurrent client connections
- Background thread for connection acceptance
- Broadcasts newline-delimited JSON to all connected clients on game events
- Automatically removes disconnected clients

### HttpRestServer.cs

HTTP REST API server:
- Listens on `localhost:9877`
- Handles GET requests for querying game state (players, cities, units, tiles, etc.)
- Handles POST requests for executing game commands
- Thread-safe command queuing for main thread execution
- Returns JSON responses with proper error codes

## Game Data Access

### The Assembly-CSharp Restriction

Old World explicitly blocks mods from referencing `Assembly-CSharp.dll`:
```
"Mod dlls cannot reference Assembly-CSharp. Disabling Mod"
```

This means we cannot directly access `AppMain.gApp.Client.Game`. Instead, we use reflection:

```csharp
// Find Assembly-CSharp at runtime
foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
{
    if (assembly.GetName().Name == "Assembly-CSharp")
    {
        _appMainType = assembly.GetType("AppMain");
        break;
    }
}

// Cache reflection info
_gAppField = _appMainType.GetField("gApp", BindingFlags.Public | BindingFlags.Static);
_clientProperty = _appMainType.GetProperty("Client", BindingFlags.Public | BindingFlags.Instance);
// ...

// Access game instance (GUI mode)
var appMain = _gAppField.GetValue(null);        // AppMain.gApp
var client = _clientProperty.GetValue(appMain); // gApp.Client
var game = _gameProperty.GetValue(client);      // Client.Game

// Access game instance (Headless mode - also works in GUI)
var gameServer = _getLocalGameServerMethod.Invoke(appMain, null);
var localGameProp = gameServer.GetType().GetProperty("LocalGame",
    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
var game = localGameProp.GetValue(gameServer);  // Works in both modes
```

**Important:** `LocalGame` is a non-public property, requiring `BindingFlags.NonPublic`.

### Accessible via TenCrowns.GameCore.dll

These types can be referenced directly (no reflection needed):
- `Game` - Core game state
- `Player` - Player data and stockpiles
- `City` - City data, yields, production queues
- `Infos` - Game metadata (nation names, yield types, etc.)
- `InfoYield`, `InfoNation` - Type information with `mzType` string identifiers

### JSON Serialization

Uses Newtonsoft.Json with `DefaultContractResolver` to preserve exact game type strings (e.g., `YIELD_GROWTH`, `IMPROVEMENT_FARM`). See `docs/api-design-principles.md` for design philosophy.

## Connection Options

### TCP (Port 9876) - Push Notifications

Best for: Real-time updates, streaming overlays, persistent connections.

- **Host:** `localhost` (127.0.0.1)
- **Port:** `9876`
- **Protocol:** TCP
- **Format:** Newline-delimited JSON (each message is a complete JSON object followed by `\n`)
- **Direction:** Server → Client (push only)

```bash
nc localhost 9876
```

### HTTP (Port 9877) - REST API

Best for: On-demand queries, command execution, request/response patterns.

- **Host:** `localhost` (127.0.0.1)
- **Port:** `9877`
- **Protocol:** HTTP/1.1
- **Format:** JSON
- **Direction:** Bidirectional (GET for queries, POST for commands)

```bash
# Query game state
curl localhost:9877/state | jq

# Execute a command
curl -X POST localhost:9877/command \
  -H "Content-Type: application/json" \
  -d '{"action": "endTurn", "params": {}}'
```

## JSON Message Format

### Event Types

#### `gameReady`
Broadcast when the game server is ready (game loaded or started).

#### `newTurn`
Broadcast at the start of each new turn.

### Message Structure

```json
{
  "event": "newTurn",
  "turn": 3,
  "year": 3,
  "currentPlayer": 0,
  "players": [
    {
      "index": 0,
      "nation": "NATION_ROME",
      "cities": 2,
      "units": 5,
      "legitimacy": 100,
      "stockpiles": {
        "YIELD_FOOD": 150,
        "YIELD_CIVICS": 200,
        ...
      }
    }
  ],
  "cities": [
    {
      "id": 0,
      "name": "Rome",
      "ownerId": 0,
      "nation": "NATION_ROME",
      "isCapital": true,
      "citizens": 3,
      "yields": {
        "YIELD_FOOD": {"perTurn": 11, "progress": 64, "threshold": 200, ...},
        ...
      },
      "improvements": {"IMPROVEMENT_FARM": 2, ...},
      "currentBuild": {"buildType": "UNIT", "itemType": "UNIT_WARRIOR", ...},
      ...
    }
  ]
}
```

### Field Reference

**Top-level fields:**

| Field | Type | Description |
|-------|------|-------------|
| `event` | string | Event type (`newTurn`, `gameReady`) |
| `turn` | int | Current turn number (1-based) |
| `year` | int | Current game year (1-based, not calendar year) |
| `currentPlayer` | int | Index of player whose turn it is |
| `players` | array | Array of player objects |
| `cities` | array | Array of city objects (all cities in game) |

**Player fields:**

| Field | Type | Description |
|-------|------|-------------|
| `index` | int | Player index in game |
| `nation` | string | Nation type identifier (e.g., `NATION_ROME`) |
| `cities` | int | Number of cities owned |
| `units` | int | Number of units owned |
| `legitimacy` | int | Current legitimacy score |
| `stockpiles` | object | Resource stockpiles keyed by yield type |

**City fields (abbreviated - ~80 fields total):**

| Field | Type | Description |
|-------|------|-------------|
| `id` | int | Unique city ID |
| `name` | string | City name |
| `ownerId` | int | Player index who owns this city |
| `nation` | string | Nation type (e.g., `NATION_ROME`) |
| `isCapital` | bool | Whether this is the player's capital |
| `citizens` | int | Current citizen count |
| `yields` | object | Per-yield data (perTurn, progress, threshold, etc.) |
| `improvements` | object | Improvement counts by type |
| `currentBuild` | object | Current production (buildType, itemType, progress, etc.) |
| ... | ... | See schema documentation for complete field list |

### Yield Types

| Yield | Description |
|-------|-------------|
| `YIELD_FOOD` | Food stockpile |
| `YIELD_WOOD` | Wood stockpile |
| `YIELD_STONE` | Stone stockpile |
| `YIELD_IRON` | Iron stockpile |
| `YIELD_CIVICS` | Civics (laws, legitimacy actions) |
| `YIELD_TRAINING` | Training (unit production) |
| `YIELD_MONEY` | Gold/money |
| `YIELD_ORDERS` | Orders remaining this turn |
| `YIELD_GROWTH` | City growth (typically 0 in stockpile) |
| `YIELD_CULTURE` | Culture (typically 0 in stockpile) |
| `YIELD_SCIENCE` | Science (typically 0 in stockpile) |
| `YIELD_HAPPINESS` | Happiness modifier |
| `YIELD_DISCONTENT` | Discontent modifier |
| `YIELD_MAINTENANCE` | Maintenance costs |

## Build & Deploy

### Prerequisites
- .NET SDK 6.0+
- Old World installed via Steam

### Build
```bash
# First-time setup: copy .env.example to .env and configure paths
cp .env.example .env
# Edit .env with your Old World installation path

# Build
source .env
export OldWorldPath="$OLDWORLD_PATH"
dotnet build -c Release
```

### Deploy
```bash
./deploy.sh
```

This copies files to:
```
~/Library/Application Support/OldWorld/Mods/OldWorldAPIEndpoint/
├── ModInfo.xml
├── OldWorldAPIEndpoint.dll
└── Newtonsoft.Json.dll
```

### Enable Mod
1. Launch Old World
2. Go to Mods menu
3. Enable "Old World API Endpoint"
4. Restart game if prompted

## Constraints & Limitations

### Threading
- TCP server runs on background thread
- Game hooks (`OnNewTurnServer`, etc.) run on main thread
- `Debug.Log` calls are thread-safe in Unity

### Server Lifecycle
- Server starts on mod `Initialize()`
- Server persists across game sessions (not stopped on `Shutdown()`)
- This allows clients to stay connected when loading saves or starting new games

### Headless Mode
- All mod hooks (`OnNewTurnServer`, `Initialize`, etc.) fire normally in headless mode
- Game access requires `GetLocalGameServer().LocalGame` path (see below)
- `Client.Game` returns null in headless mode
- Use `test-headless.sh` for automated headless testing with TCP capture

### Data Availability
- Game instance may be null during menu screens
- Error responses include `"error":"game not available"` when game is not loaded

## File Structure

```
OldWorldAPIEndpoint/
├── OldWorldAPIEndpoint.csproj  # Project file
├── ModInfo.xml                 # Mod metadata for Old World
├── deploy.sh                   # Build and deploy script
├── test-headless.sh            # Automated headless testing with TCP capture
├── CLAUDE.md                   # Claude Code project instructions
├── Source/
│   ├── APIEndpoint.cs          # Main entry point
│   ├── TcpBroadcastServer.cs   # TCP server implementation
│   └── HttpRestServer.cs       # HTTP REST API server
├── bin/                        # Build output (gitignored)
│   └── OldWorldAPIEndpoint.dll
└── docs/                       # Documentation
    ├── technical-overview.md   # This file
    ├── api-reference.md        # REST API endpoint reference
    ├── api-design-principles.md # API design philosophy
    └── schemas/                # JSON Schema definitions
```

## Testing

### GUI Mode
```bash
./deploy.sh
# Launch Old World, enable mod, start game
nc localhost 9876
# End a turn to see JSON output
```

### Headless Mode
```bash
./test-headless.sh /path/to/save.zip 5
```

This script:
1. Builds and deploys the mod
2. Starts a TCP client with retry loop
3. Runs Old World in headless mode for N turns
4. Captures and pretty-prints all JSON output

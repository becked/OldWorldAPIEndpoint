# Old World API Endpoint - Technical Documentation

## Overview

Old World API Endpoint is a mod for the strategy game Old World that broadcasts game state over TCP, enabling companion apps, overlays, and external tools to receive real-time game data.

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                      Old World Game                          │
│  ┌─────────────────────────────────────────────────────┐    │
│  │              OldWorldAPIEndpoint.dll                 │    │
│  │  ┌───────────────────┐  ┌───────────────────────┐   │    │
│  │  │   APIEndpoint     │  │  TcpBroadcastServer   │   │    │
│  │  │ (ModEntryPoint    │  │  - Port 9876          │   │    │
│  │  │    Adapter)       │──│  - Accepts clients    │   │    │
│  │  │                   │  │  - Broadcasts JSON    │   │    │
│  │  └───────────────────┘  └───────────────────────┘   │    │
│  └─────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────┘
                              │
                              │ TCP (localhost:9876)
                              │ Newline-delimited JSON
                              ▼
                    ┌─────────────────┐
                    │  Client Apps    │
                    │  - nc/telnet    │
                    │  - Custom apps  │
                    │  - Overlays     │
                    └─────────────────┘
```

## Key Components

### APIEndpoint.cs

Main entry point extending `ModEntryPointAdapter`. Responsibilities:
- Initialize TCP server on mod load
- Hook into game lifecycle events (`OnNewTurnServer`, `OnGameServerReady`)
- Access game state via reflection (required because mods cannot reference Assembly-CSharp.dll)
- Build JSON payloads and broadcast to connected clients

### TcpBroadcastServer.cs

Minimal TCP server implementation:
- Listens on `localhost:9876`
- Accepts multiple concurrent client connections
- Background thread for connection acceptance
- Broadcasts newline-delimited JSON to all connected clients
- Automatically removes disconnected clients

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

// Access game instance
var appMain = _gAppField.GetValue(null);        // AppMain.gApp
var client = _clientProperty.GetValue(appMain); // gApp.Client
var game = _gameProperty.GetValue(client);      // Client.Game
```

### Accessible via TenCrowns.GameCore.dll

These types can be referenced directly (no reflection needed):
- `Game` - Core game state
- `Player` - Player data and stockpiles
- `Infos` - Game metadata (nation names, yield types, etc.)
- `InfoYield`, `InfoNation` - Type information with `mzType` string identifiers

## TCP Protocol

### Connection
- **Host:** `localhost` (127.0.0.1)
- **Port:** `9876`
- **Protocol:** TCP
- **Format:** Newline-delimited JSON (each message is a complete JSON object followed by `\n`)

### Testing Connection
```bash
nc localhost 9876
```

Or with telnet:
```bash
telnet localhost 9876
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
        "YIELD_WOOD": 80,
        "YIELD_STONE": 45,
        "YIELD_IRON": 20,
        "YIELD_CIVICS": 200,
        "YIELD_TRAINING": 50,
        "YIELD_MONEY": 100,
        "YIELD_ORDERS": 5,
        "YIELD_GROWTH": 0,
        "YIELD_CULTURE": 0,
        "YIELD_HAPPINESS": 0,
        "YIELD_DISCONTENT": 0,
        "YIELD_SCIENCE": 0,
        "YIELD_MAINTENANCE": 0
      }
    }
  ]
}
```

### Field Reference

| Field | Type | Description |
|-------|------|-------------|
| `event` | string | Event type (`newTurn`, `gameReady`) |
| `turn` | int | Current turn number (1-based) |
| `year` | int | Current game year (1-based, not calendar year) |
| `currentPlayer` | int | Index of player whose turn it is |
| `players` | array | Array of player objects |
| `players[].index` | int | Player index in game |
| `players[].nation` | string | Nation type identifier (e.g., `NATION_ROME`) |
| `players[].cities` | int | Number of cities owned |
| `players[].units` | int | Number of units owned |
| `players[].legitimacy` | int | Current legitimacy score |
| `players[].stockpiles` | object | Resource stockpiles keyed by yield type |

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
export OldWorldPath="$OLDWORLD_PATH"
cd /path/to/OldWorldAPIEndpoint
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
└── OldWorldAPIEndpoint.dll
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
- `OnNewTurnServer` does NOT fire during headless `-autorunturns` batch mode
- This is a game limitation, not a mod issue

### Data Availability
- Game instance may be null during menu screens
- Error responses include `"error":"game not available"` when game is not loaded

## File Structure

```
OldWorldAPIEndpoint/
├── OldWorldAPIEndpoint.csproj  # Project file
├── ModInfo.xml                 # Mod metadata for Old World
├── deploy.sh                   # Build and deploy script
├── Source/
│   ├── APIEndpoint.cs          # Main entry point
│   └── TcpBroadcastServer.cs   # TCP server implementation
├── bin/                        # Build output
│   └── OldWorldAPIEndpoint.dll
└── docs/                       # Documentation
    ├── technical-overview.md   # This file
    └── roadmap.md              # Future development ideas
```

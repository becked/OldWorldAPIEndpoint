# Architecture Overview

This document provides a high-level overview of how the Old World API Endpoint mod works.

## What This Mod Does

The Old World API Endpoint is a mod for the Old World turn-based strategy game that exposes game state to external applications through two communication channels:

- **TCP Broadcast Server** (port 9876): Push-based - automatically broadcasts game state at each turn end
- **HTTP REST Server** (port 9877): Pull-based - on-demand queries for game state and command execution

This enables companion apps, overlays, analytics tools, and external automation to interact with the game in real-time.

## Project Structure

```
OldWorldAPIEndpoint/
├── Source/                          # Main mod source code
│   ├── APIEndpoint.cs              # Mod entry point
│   ├── APIEndpoint.Reflection.cs   # Assembly-CSharp access via reflection
│   ├── APIEndpoint.Events.cs       # Event detection (state change diffing)
│   ├── TcpBroadcastServer.cs       # TCP broadcast implementation
│   ├── HttpRestServer.cs           # HTTP REST API implementation
│   ├── CommandExecutor.cs          # Command execution dispatcher
│   ├── CommandExecutor.Generated.cs # Auto-generated command handlers
│   └── DataBuilders.Generated.cs   # Auto-generated entity builders
│
├── tools/OldWorldCodeGen/          # Roslyn-based code generator
├── docs/                           # Documentation (GitHub Pages)
└── test-headless.sh               # Automated testing
```

## How The Mod Hooks Into The Game

The mod extends `ModEntryPointAdapter` and implements three key game hooks:

### 1. Initialize (Mod Startup)

When the game loads the mod:
- Initializes reflection system to access game internals
- Starts TCP server on port 9876
- Starts HTTP server on port 9877

### 2. OnGameServerReady (Game Start)

When a game session becomes playable:
- Takes initial snapshots of all entities for event detection
- Broadcasts a `gameReady` event with full game state

### 3. OnNewTurnServer (Turn End)

At the end of each turn:
- Detects changes by comparing current state to previous snapshots
- Emits events for character/unit/city/wonder changes
- Broadcasts a `newTurn` event with full state and detected events
- Updates snapshots for next turn's comparison

## The Reflection System

Old World explicitly blocks mods from referencing `Assembly-CSharp.dll`. To access game state, the mod uses runtime reflection:

```
AppMain.gApp → Client → Game
         └─→ Server → Game  (multiplayer)
         └─→ GetLocalGameServer().LocalGame  (headless)
```

All reflection info is cached at startup for performance. Three fallback paths ensure compatibility across GUI, headless, and multiplayer modes.

## Data Flow

### TCP Broadcast (Push Model)

```
Turn End Event
    ↓
Build game state objects (players, cities, units, characters)
    ↓
Serialize to JSON
    ↓
Broadcast to all connected TCP clients
```

Clients connect to port 9876 and receive newline-delimited JSON for each turn.

### HTTP REST (Pull Model)

```
HTTP GET /state
    ↓
Build requested data objects
    ↓
Serialize to JSON
    ↓
Return HTTP response
```

Clients can query specific endpoints like `/players`, `/cities`, `/units`, etc.

### Command Execution

```
HTTP POST /command (with JSON body)
    ↓
Queue command for main thread
    ↓
OnClientUpdate() dequeues and executes
    ↓
ClientManager.sendXxx() invoked
    ↓
Result returned to HTTP client
```

Commands must execute on the main game thread, so HTTP requests queue commands and wait for completion.

## Code Generation

The mod uses a Roslyn-based code generator that parses decompiled game source to automatically generate:

- **209 command handlers** from `ClientManager.sendXxx()` methods
- **880+ property builders** from entity getter methods
- **OpenAPI spec** for API documentation

This means when the game updates with new commands or properties, regenerating the code automatically exposes them.

### Generated Property Patterns

| Pattern | Example | Output |
|---------|---------|--------|
| Simple | `getAge()` | `"age": 42` |
| Enum-Indexed | `getRating(RatingType)` | `"ratings": {"RATING_COURAGE": 7, ...}` |
| Collection | `getTraits()` | `"traits": ["TRAIT_WARRIOR", ...]` |

## Event Detection

The mod detects game state changes by diffing against previous turn's snapshot:

- **Character events**: births, deaths, marriages, leader changes, trait changes
- **Unit events**: creation, death, promotion
- **City events**: founding, capture, destruction
- **Wonder events**: construction, destruction

Events are included in the `newTurn` broadcast and queryable via `/turn-summary` endpoints.

## Threading Model

| Thread | Responsibility |
|--------|---------------|
| Main (Game) | Mod hooks, command execution, state building |
| TCP Broadcast | Accepts connections, sends broadcasts |
| HTTP Server | Handles requests, queues commands |

Commands from HTTP must execute on the main thread. The mod uses a concurrent queue and signaling to coordinate:

1. HTTP thread queues command and waits
2. Main thread dequeues in `OnClientUpdate()`, executes, signals completion
3. HTTP thread receives signal and returns response

## API Statistics

| Entity | Properties | Notes |
|--------|------------|-------|
| Player | 211 | Yields, tech progress, diplomacy |
| Character | 204 | Stats, traits, relations |
| City | 172 | Production, population, improvements |
| Unit | 168 | Stats, equipment, location |
| Tile | 121 | Terrain, resources, visibility |

**Endpoints**: 40+ GET endpoints, 2 POST endpoints for commands

## Key Design Decisions

1. **Reflection over direct references**: Works around game's assembly blocking
2. **Push + Pull models**: TCP for real-time updates, HTTP for on-demand queries
3. **Code generation**: Single source of truth (game source) generates API code
4. **Null filtering**: Omits empty/null values to reduce payload size ~70%
5. **Snapshot diffing**: Detects events without game providing change hooks
6. **Main thread execution**: Commands queued and executed on game thread

## Testing

The mod includes automated headless testing:

```bash
./test-headless.sh /path/to/save.zip 2
```

This builds the mod, runs Old World in headless mode for 2 turns, captures all TCP/HTTP output, and validates the results.

## Further Reading

- [API Reference](api-reference.md) - Endpoint documentation
- [API Design Principles](api-design-principles.md) - Design philosophy
- [OpenAPI Spec](openapi.yaml) - Machine-readable API specification
- [Schemas](schemas/) - JSON Schema definitions for all entities

# Old World API Endpoint - Roadmap

## Current State

The mod provides a functional API with:
- **TCP Broadcast** (port 9876) - Newline-delimited JSON on turn end and game ready events
- **HTTP REST** (port 9877) - 14 endpoints for on-demand queries
- **Data Coverage**: Players, cities, characters, tribes, team/tribe diplomacy and alliances
- **Character Events**: Birth, death, marriage, leader/heir changes (via state diffing)
- **Unit Events**: Unit creation and death (via state diffing)
- **City Events**: City founding and capture (via state diffing)
- **Wonder Events**: Wonder completion (via state diffing)

See `technical-overview.md` for full details on current implementation.

---

## Planned Features

### Slice 7: Configurable Settings
Allow port and other settings via mod options.

**Features:**
- Custom TCP port (default 9876)
- Custom HTTP port (default 9877)
- Enable/disable specific event types
- Verbosity levels for logging

**Implementation:**
- Use Old World's mod settings system
- Read from ModSettings in `Initialize()`

**Complexity:** Low

---

### Slice 8: WebSocket Support
Add WebSocket server alongside TCP for browser-based clients.

**Benefits:**
- Direct browser connectivity (no CORS issues)
- Built-in message framing
- Enables web-based overlays and companion apps

**Implementation considerations:**
- WebSocket handshake and frame encoding
- May need external library or manual implementation
- Run alongside existing TCP server

**Complexity:** High

---

### Slice 9: Connection Status Events
Notify clients of server state changes.

**Events:**
```json
{"eventType": "connected", "version": "1.0.0", "serverTime": "2024-01-15T10:30:00Z"}
{"eventType": "gameLoaded", "saveName": "AutoSave-Turn-50"}
{"eventType": "gameUnloaded"}
```

**Benefits:**
- Clients know when game is loaded/unloaded
- Version handshake for compatibility checking
- Better client experience during game transitions

**Complexity:** Low

---

### Slice 10: Historical Data
Store and query game state history across turns.

**New HTTP endpoints:**
```
GET /history                      All recorded turn snapshots
GET /history/{turn}               State at specific turn
GET /history/player/{index}       Player data across all turns
GET /history/player/{index}/rates Rate trends for a player
```

**Example response:**
```json
// GET /history/player/0/rates
{
  "nation": "NATION_ROME",
  "turns": [
    {"turn": 1, "YIELD_SCIENCE": 5, "YIELD_MONEY": 12},
    {"turn": 2, "YIELD_SCIENCE": 7, "YIELD_MONEY": 15},
    {"turn": 3, "YIELD_SCIENCE": 11, "YIELD_MONEY": 18}
  ]
}
```

**Implementation approach:**
- Store snapshots in memory at each turn end (data already built)
- Configurable retention (last N turns, or all)
- Optional: persist to file for cross-session history

**Use cases:**
- Track science/economy trends over time
- Analyze diplomacy changes
- Power graphs in companion apps

**Complexity:** Medium

---

## Technical Debt

### TCP Framing: Switch to Length-Prefixed
Currently using newline-delimited JSON. Switch to 4-byte big-endian length prefix.

**Current:** `{"event":"newTurn",...}\n`

**Target:**
```
┌─────────────────────────────────────────────┐
│  4 bytes (BE)  │  N bytes (UTF-8 JSON)      │
│  Length = N    │  Payload                   │
└─────────────────────────────────────────────┘
```

**Benefits:**
- Binary-safe (handles JSON with embedded newlines)
- Deterministic parsing
- Matches design doc for Tauri client compatibility

**Include:** CLI test script (Python) for ad-hoc TCP testing since `nc` won't display length-prefixed messages cleanly.

---

### Error Handling
- More granular error messages in JSON responses
- Separate error event type for TCP broadcast
- Consistent error structure across HTTP and TCP

---

### Logging
- Configurable log levels (Debug, Info, Warning, Error)
- Option to log to file instead of Unity console
- Log rotation for long sessions

---

### Testing
- Unit tests for JSON generation
- Integration test harness for TCP/HTTP endpoints
- Automated regression tests for headless mode

---

## Priority Recommendations

**High value, low effort:**
1. Slice 9 (Connection status) - Simple, improves client experience
2. Slice 7 (Configurable settings) - Makes mod more flexible

**Medium effort, good value:**
3. Slice 10 (Historical data) - Enables trend analysis

**Lower priority:**
4. Slice 8 (WebSocket) - Nice for browser clients, but HTTP REST covers most needs
5. TCP length-prefixed framing - Current approach works, only needed for strict binary safety

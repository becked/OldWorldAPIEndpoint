# Old World API Endpoint - Roadmap & Future Ideas

## Completed Slices

### Slice 1: Hello World
- Mod loads correctly in Old World
- TCP server accepts connections on port 9876
- Broadcasts on turn end events

### Slice 2: Basic Game Data
- Turn number and year
- Real game state (not hardcoded)

### Slice 3: Player Data & Stockpiles
- All players in game
- Nation identifiers
- City and unit counts
- Legitimacy scores
- Full resource stockpiles (Food, Wood, Stone, Iron, Civics, Training, Money, Orders, etc.)

### Slice 4a: City Data
Comprehensive city information at top level with ~80 fields per city.

**Key data groups:**
- Identity & Location (id, name, ownerId, tileId, x, y, nation)
- Status Flags (isCapital, isConnected, isTribe, isIdle)
- Population & Growth (citizens, citizensTotal, growthCount)
- Military & Defense (hp, hpMax, strength, damage)
- Governor (governorId, hasGovernor, isGoverned)
- Family & Faction (family, familyClass, isFamilySeat, familyOpinion)
- Culture (culture, cultureStep)
- Religion (religions[], religionCount, holyCity[], hasStateReligion)
- Production & Build Queue (currentBuild, buildQueue[])
- Yields per type (perTurn, progress, threshold, overflow, modifier)
- Improvements & Projects (counts by type)
- Trade & Happiness (tradeNetwork, luxuryCount, happinessLevel)

**Implementation notes:**
- Cities at top level (mirrors game data structure)
- Uses Newtonsoft.Json for serialization
- Preserves exact game type strings (YIELD_GROWTH, IMPROVEMENT_FARM, etc.)

### Slice 4b: Character Data
Comprehensive character information at top level with ~40 fields per character.

**Key data groups:**
- Identity (id, name, suffix, gender, age, characterType)
- Player & Nation (playerId, nation, tribe)
- Status (isAlive, isDead, isRoyal, isAdult, isTemporary)
- Leadership & Succession (isLeader, isHeir, isSuccessor, isLeaderSpouse, isHeirSpouse, isRegent)
- Jobs & Positions (job, council, courtier)
- Governor/Agent (isCityGovernor, cityGovernorId, isCityAgent, cityAgentId)
- Military (hasUnit, unitId, isGeneral)
- Family (family, familyClass, isFamilyHead, fatherId, motherId)
- Religion (religion, isReligionHead)
- Traits (archetype, traits[])
- Ratings (RATING_WISDOM, RATING_CHARISMA, RATING_COURAGE, RATING_DISCIPLINE)
- XP & Level (xp, level)

**Player additions:**
- `leaderId` - References the player's current leader character

**Implementation notes:**
- Characters at top level (mirrors game data structure)
- Includes both living and dead characters
- Uses `mzType` for all game type strings (TRAIT_COMMANDER_ARCHETYPE, etc.)

### Slice 4c: Per-Turn Rates
Net income/expense rates per yield for each player.

**New fields:**
```json
{
  "players": [{
    "rates": {
      "YIELD_FOOD": 15,
      "YIELD_CIVICS": -5,
      "YIELD_MONEY": 10,
      ...
    }
  }]
}
```

**Implementation notes:**
- Uses `player.calculateYieldAfterUnits(yieldType, false)` for canonical rate (matches game history)
- Values divided by 10 (YIELDS_MULTIPLIER) to return whole numbers
- All 14 yield types included (YIELD_GROWTH, YIELD_CIVICS, YIELD_TRAINING, YIELD_CULTURE, YIELD_HAPPINESS, YIELD_DISCONTENT, YIELD_SCIENCE, YIELD_MONEY, YIELD_MAINTENANCE, YIELD_ORDERS, YIELD_FOOD, YIELD_IRON, YIELD_STONE, YIELD_WOOD)
- City-only yields (GROWTH, CULTURE, HAPPINESS) return 0 at player level

### Slice 4d: Team Diplomacy
Team-to-team diplomacy state with comprehensive data.

**New arrays:**
```json
{
  "teamDiplomacy": [
    {
      "fromTeam": 0,
      "toTeam": 1,
      "diplomacy": "DIPLOMACY_WAR",
      "isHostile": true,
      "isPeace": false,
      "hasContact": true,
      "warScore": 15,
      "warState": "WARSTATE_WINNING",
      "conflictTurn": 42,
      "conflictNumTurns": 8,
      "diplomacyTurn": 42,
      "diplomacyNumTurns": 8,
      "diplomacyBlockTurn": 52,
      "diplomacyBlockTurns": 2
    }
  ],
  "teamAlliances": [
    { "team": 2, "allyTeam": 3 }
  ]
}
```

**Player enhancement:**
- `team` field added to each player object to link players to teams

**Implementation notes:**
- Diplomacy is tracked between **teams**, not individual players
- One entry per directed relationship (fromTeam → toTeam)
- War score is **asymmetric** - each direction has its own value
- Contact (`hasContact`) is unidirectional
- Alliances are symmetric (stored per-team)
- Uses game type strings (DIPLOMACY_WAR, WARSTATE_WINNING, etc.)

### Slice 4e: Tribe Diplomacy
Tribe-to-team diplomacy state and tribe entity data (separate system from team-to-team diplomacy).

**New arrays:**
```json
{
  "tribes": [
    {
      "tribeType": "TRIBE_GAULS",
      "isAlive": true,
      "isDead": false,
      "hasDiplomacy": true,
      "leaderId": 19,
      "hasLeader": true,
      "religion": null,
      "hasReligion": false,
      "allyPlayerId": null,
      "allyTeam": null,
      "hasPlayerAlly": false,
      "numUnits": 3,
      "numCities": 0,
      "strength": 90,
      "cityIds": [],
      "settlementTileIds": [3160, 3525, 3740],
      "numTribeImprovements": 3
    }
  ],
  "tribeDiplomacy": [
    {
      "tribe": "TRIBE_GAULS",
      "toTeam": 0,
      "diplomacy": "DIPLOMACY_TRUCE",
      "isHostile": false,
      "isPeace": false,
      "hasContact": false,
      "warScore": 0,
      "warState": "WARSTATE_NEUTRAL",
      "conflictTurn": 0,
      "conflictNumTurns": 3,
      "diplomacyTurn": 0,
      "diplomacyNumTurns": 3,
      "diplomacyBlockTurn": 0,
      "diplomacyBlockTurns": 0
    }
  ],
  "tribeAlliances": [
    { "tribe": "TRIBE_BLEMMYES", "allyPlayerId": 2, "allyTeam": 2 }
  ]
}
```

**Implementation notes:**
- `tribes` array includes all tribes (alive and dead, diplomacy and non-diplomacy like TRIBE_BARBARIANS)
- `tribeDiplomacy` filtered to tribes with `isDiplomacyTribeAlive()` (excludes raiders/barbarians)
- `tribeAlliances` only includes tribes with active player alliances
- Tribe diplomacy is asymmetric (tribe → team only, no team → tribe)
- Uses `mzType` for all game type strings (TRIBE_GAULS, DIPLOMACY_TRUCE, etc.)

### Slice 6: HTTP REST Endpoint
HTTP server for on-demand queries via curl/scripts.

**Port:** 9877 (separate from TCP 9876)

**Endpoints:**
```
GET /state              Full game state (mirrors TCP broadcast)
GET /players            All players array
GET /player/{index}     Specific player by index
GET /cities             All cities array
GET /city/{id}          Specific city by ID
GET /characters         All characters array
GET /character/{id}     Specific character by ID
GET /tribes             All tribes array
GET /tribe/{type}       Specific tribe by type string (e.g., TRIBE_GAULS)
GET /team-diplomacy     All team diplomacy relationships
GET /team-alliances     All team alliances
GET /tribe-diplomacy    All tribe diplomacy relationships
GET /tribe-alliances    All tribe alliances
```

**Error handling:**
- `200 OK` - Success with JSON data
- `400 Bad Request` - Invalid ID format
- `404 Not Found` - Entity not found or unknown endpoint
- `503 Service Unavailable` - Game not loaded

**Implementation notes:**
- Uses .NET's `HttpListener` class
- CORS headers included for browser clients
- Reuses same data builders as TCP broadcast
- Game reference cached from main thread for HTTP thread safety
- Headless testing uses TCP sync (wait for TCP data before testing HTTP)

---

## Potential Future Slices

### Event Expansion

#### Slice 5a: Character Events
Broadcast events when characters are born, die, marry, etc.

**New event types:**
```json
{"event": "characterBorn", "character": {...}, "parents": [...]}
{"event": "characterDied", "character": {...}, "cause": "OLD_AGE"}
{"event": "characterMarried", "character1": {...}, "character2": {...}}
{"event": "leaderChanged", "player": 0, "oldLeader": {...}, "newLeader": {...}}
```

**Implementation approach:**
- Track character state between turns
- Diff to detect births, deaths, marriages
- Or hook into game event system if available

#### Slice 5b: Military Events
Broadcast battle outcomes and unit losses.

**New event types:**
```json
{"event": "battle", "attacker": {...}, "defender": {...}, "winner": "attacker", "location": {...}}
{"event": "unitKilled", "unit": {...}, "killedBy": {...}}
{"event": "cityCapture", "city": {...}, "oldOwner": 0, "newOwner": 1}
```

**Implementation approach:**
- May require different hooks (combat callbacks)
- Track unit/city ownership between turns

#### Slice 5c: City Events
Broadcast city founding, conquest, wonder completion.

**New event types:**
```json
{"event": "cityFounded", "city": {...}, "player": 0}
{"event": "cityConquered", "city": {...}, "oldOwner": 0, "newOwner": 1}
{"event": "wonderCompleted", "wonder": "WONDER_PYRAMIDS", "city": {...}}
```

---

### API Features

#### Slice 6: HTTP REST Endpoint
Add HTTP server for on-demand queries via curl/scripts.

**Port:** 9877 (separate from TCP 9876)

**Endpoints:**
```bash
curl localhost:9877/state          # Full game state
curl localhost:9877/players        # All players
curl localhost:9877/player/0       # Specific player
curl localhost:9877/cities         # All cities
curl localhost:9877/city/123       # Specific city
```

**Benefits:**
- Works with curl, wget, any HTTP client
- Easy debugging and scripting
- No persistent connection needed
- Pipe to `jq` for formatting

**Implementation:**
- Use .NET's built-in `HttpListener` class
- Simple routing for GET endpoints
- Return JSON responses

**Implementation complexity:** Medium
- HttpListener handles HTTP parsing
- Just need routing and JSON serialization

#### Slice 7: Configurable Settings
Allow port and other settings via mod options.

**Features:**
- Custom port (default 9876)
- Enable/disable specific events
- Verbosity levels

**Implementation:**
- Use Old World's mod settings system
- Read from ModSettings in Initialize()

---

### Quality of Life

#### Slice 8: WebSocket Support
Add WebSocket server alongside TCP for browser-based clients.

**Benefits:**
- Direct browser connectivity
- Built-in framing (no newline parsing)
- Enables web-based overlays

**Implementation complexity:** High
- WebSocket handshake
- Frame encoding/decoding
- May need external library

#### Slice 9: Connection Status Event
Notify clients of server state.

**Events:**
```json
{"event": "connected", "version": "0.2.0", "serverTime": "2024-01-15T10:30:00Z"}
{"event": "gameLoaded", "saveName": "AutoSave-Turn-50"}
{"event": "gameUnloaded"}
```

---

## Technical Debt & Improvements

### TCP Framing: Switch to Length-Prefixed
Currently using newline-delimited JSON. Switch to 4-byte big-endian length prefix per the design doc.

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

### JSON Serialization ✓
~~Currently using string interpolation.~~ Now using Newtonsoft.Json with:
- Anonymous objects for type-safe serialization
- `DefaultContractResolver` to preserve exact game type strings
- Proper escaping and null handling

### Error Handling
- More granular error messages
- Separate error event type

### Logging
- Configurable log levels
- Option to log to file instead of Unity console

### Testing
- Unit tests for JSON generation
- Integration test harness

---

## Priority Recommendations

**High value, low effort:**
1. ~~Slice 4c (Per-turn rates)~~ - Done
2. ~~Slice 4d (Diplomacy)~~ - Done
3. ~~Slice 4e (Tribe Diplomacy)~~ - Done
4. Slice 9 (Connection status) - Simple, improves client experience

**High value, medium effort:**
5. Slice 6 (HTTP REST) - Enables curl/scripting, great for debugging

**Lower priority:**
6. Slice 8 (WebSocket) - Nice to have for browser/web clients

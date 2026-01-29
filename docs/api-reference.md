# API Reference

Complete reference for the REST API on port 9877. Includes GET endpoints for querying game state and POST endpoints for executing commands.

## Base URL

```
http://localhost:9877
```

## Endpoints Overview

### Commands
| Endpoint | Description |
|----------|-------------|
| [`POST /command`](#post-command) | Execute single game command |
| [`POST /commands`](#post-commands) | Execute multiple commands in sequence |

### Core Data
| Endpoint | Description |
|----------|-------------|
| [`GET /state`](#get-state) | Full game state |
| [`GET /players`](#get-players) | All players |
| [`GET /player/{index}`](#get-playerindex) | Single player |
| [`GET /cities`](#get-cities) | All cities |
| [`GET /city/{id}`](#get-cityid) | Single city |
| [`GET /characters`](#get-characters) | All characters |
| [`GET /character/{id}`](#get-characterid) | Single character |

### Units
| Endpoint | Description |
|----------|-------------|
| [`GET /units`](#get-units) | All units |
| [`GET /unit/{id}`](#get-unitid) | Single unit |
| [`GET /player/{index}/units`](#get-playerindexunits) | Player's units |

### Player Extensions
| Endpoint | Description |
|----------|-------------|
| [`GET /player/{index}/techs`](#get-playerindextechs) | Technology state |
| [`GET /player/{index}/families`](#get-playerindexfamilies) | Family relationships |
| [`GET /player/{index}/religion`](#get-playerindexreligion) | Religion state |
| [`GET /player/{index}/goals`](#get-playerindexgoals) | Goals/ambitions |
| [`GET /player/{index}/decisions`](#get-playerindexdecisions) | Pending decisions |
| [`GET /player/{index}/laws`](#get-playerindexlaws) | Active laws |
| [`GET /player/{index}/missions`](#get-playerindexmissions) | Active missions |
| [`GET /player/{index}/resources`](#get-playerindexresources) | Resource counts |

### Map & Tiles
| Endpoint | Description |
|----------|-------------|
| [`GET /map`](#get-map) | Map metadata |
| [`GET /tiles`](#get-tiles) | Paginated tiles |
| [`GET /tile/{id}`](#get-tileid) | Tile by ID |
| [`GET /tile/{x}/{y}`](#get-tilexy) | Tile by coordinates |

### Events
| Endpoint | Description |
|----------|-------------|
| [`GET /character-events`](#get-character-events) | Character events |
| [`GET /unit-events`](#get-unit-events) | Unit events |
| [`GET /city-events`](#get-city-events) | City events |

### Tribes & Diplomacy
| Endpoint | Description |
|----------|-------------|
| [`GET /tribes`](#get-tribes) | All tribes |
| [`GET /tribe/{tribeType}`](#get-tribetribetype) | Single tribe |
| [`GET /team-diplomacy`](#get-team-diplomacy) | Team relationships |
| [`GET /team-alliances`](#get-team-alliances) | Team alliances |
| [`GET /tribe-diplomacy`](#get-tribe-diplomacy) | Tribe relationships |
| [`GET /tribe-alliances`](#get-tribe-alliances) | Tribe alliances |

### Global
| Endpoint | Description |
|----------|-------------|
| [`GET /religions`](#get-religions) | All religions |
| [`GET /config`](#get-config) | Game configuration |

---

## State

### GET /state

Returns complete game state with all entities.

**Response:** Full game state including turn, year, players, cities, characters, tribes, and diplomacy.

```bash
curl localhost:9877/state | jq '{turn, year, playerCount: (.players | length)}'
```

**Example Response:**
```json
{
  "turn": 5,
  "year": 5,
  "currentPlayer": 0,
  "players": [...],
  "characters": [...],
  "cities": [...],
  "teamDiplomacy": [...],
  "tribes": [...]
}
```

---

## Players

### GET /players

Returns array of all players.

```bash
curl localhost:9877/players | jq '.[] | {nation, cities, money: .stockpiles.YIELD_MONEY}'
```

### GET /player/{index}

Returns single player by 0-based index.

| Parameter | Type | Description |
|-----------|------|-------------|
| `index` | integer | Player index (0-based) |

```bash
curl localhost:9877/player/0 | jq
```

**Errors:**
- `400` - Invalid index format
- `404` - Player not found

---

## Cities

### GET /cities

Returns array of all cities (~80 fields each).

```bash
# All capitals
curl localhost:9877/cities | jq '.[] | select(.isCapital) | {name, nation}'

# Cities building something
curl localhost:9877/cities | jq '.[] | select(.currentBuild) | {name, building: .currentBuild.itemType}'
```

### GET /city/{id}

Returns single city by ID.

| Parameter | Type | Description |
|-----------|------|-------------|
| `id` | integer | City ID |

```bash
curl localhost:9877/city/0 | jq '{name, citizens, improvements}'
```

---

## Characters

### GET /characters

Returns array of all characters (~85 fields each).

```bash
# All living leaders
curl localhost:9877/characters | jq '.[] | select(.isLeader and .isAlive) | {name, nation, age}'

# Generals with ratings
curl localhost:9877/characters | jq '.[] | select(.isGeneral) | {name, ratings}'

# Characters with specific trait
curl localhost:9877/characters | jq '.[] | select(.traits | index("TRAIT_WARRIOR")) | .name'
```

### GET /character/{id}

Returns single character by ID.

| Parameter | Type | Description |
|-----------|------|-------------|
| `id` | integer | Character ID |

```bash
curl localhost:9877/character/42 | jq '{name, traits, spouseIds, childrenIds}'
```

---

## Units

### GET /units

Returns array of all living units.

```bash
curl localhost:9877/units | jq '.[] | {id, unitType, ownerId}'
```

### GET /unit/{id}

Returns single unit by ID.

| Parameter | Type | Description |
|-----------|------|-------------|
| `id` | integer | Unit ID |

```bash
curl localhost:9877/unit/1 | jq
```

### GET /player/{index}/units

Returns all units owned by a player.

```bash
curl localhost:9877/player/0/units | jq '.[] | {unitType, hp}'
```

---

## Player Extensions

### GET /player/{index}/techs

Returns technology research state.

```bash
curl localhost:9877/player/0/techs | jq
```

**Response:**
```json
{
  "researching": "TECH_STONECUTTING",
  "progress": { "TECH_STONECUTTING": 148 },
  "researched": ["TECH_TRAPPING", "TECH_ADMINISTRATION"],
  "available": ["TECH_IRONWORKING", "TECH_STONECUTTING"]
}
```

### GET /player/{index}/families

Returns family relationship data.

```bash
curl localhost:9877/player/0/families | jq
```

### GET /player/{index}/religion

Returns player religion state.

```bash
curl localhost:9877/player/0/religion | jq
```

### GET /player/{index}/goals

Returns goals and ambitions. *Note: Returns placeholder data due to API limitations.*

### GET /player/{index}/decisions

Returns pending decisions. *Note: Returns placeholder data due to API limitations.*

### GET /player/{index}/laws

Returns active laws. *Note: Returns placeholder data due to API limitations.*

### GET /player/{index}/missions

Returns active missions. *Note: Returns placeholder data due to API limitations.*

### GET /player/{index}/resources

Returns resource/luxury counts. *Note: Returns placeholder data due to API limitations.*

---

## Map & Tiles

### GET /map

Returns map metadata.

```bash
curl localhost:9877/map | jq
```

**Response:**
```json
{ "numTiles": 5476 }
```

### GET /tiles

Returns paginated tile list.

| Parameter | Default | Max | Description |
|-----------|---------|-----|-------------|
| `offset` | 0 | - | Starting index |
| `limit` | 100 | 1000 | Max tiles to return |

```bash
curl "localhost:9877/tiles?offset=0&limit=10" | jq '.pagination'
```

### GET /tile/{id}

Returns single tile by ID.

```bash
curl localhost:9877/tile/123 | jq
```

### GET /tile/{x}/{y}

Returns single tile by coordinates.

```bash
curl localhost:9877/tile/15/8 | jq
```

---

## Religion & Config

### GET /religions

Returns global religion state.

```bash
curl localhost:9877/religions | jq '.[] | select(.isFounded)'
```

### GET /config

Returns game configuration.

```bash
curl localhost:9877/config | jq
```

**Response:**
```json
{
  "numTiles": 5476,
  "numPlayers": 5,
  "numTeams": 5,
  "turn": 42,
  "year": 42
}
```

---

## Events

Events are detected by comparing game state between turns.

### GET /character-events

Returns character events from the last turn.

**Event Types:**
- `characterBorn` - New character born
- `characterDied` - Character died
- `leaderChanged` - Nation leader changed
- `heirChanged` - Nation heir changed
- `characterMarried` - Two characters married

```bash
curl localhost:9877/character-events | jq '.[] | select(.eventType == "characterDied")'
```

### GET /unit-events

Returns unit events from the last turn.

**Event Types:**
- `unitCreated` - New unit created
- `unitKilled` - Unit destroyed

```bash
curl localhost:9877/unit-events | jq
```

### GET /city-events

Returns city events from the last turn.

**Event Types:**
- `cityFounded` - New city founded
- `cityCapture` - City captured

```bash
curl localhost:9877/city-events | jq
```

---

## Tribes

### GET /tribes

Returns array of all tribes.

```bash
curl localhost:9877/tribes | jq '.[] | select(.isAlive) | {type: .tribeType, units: .numUnits}'
```

### GET /tribe/{tribeType}

Returns single tribe by type string (case-insensitive).

| Parameter | Type | Description |
|-----------|------|-------------|
| `tribeType` | string | Tribe type (e.g., `TRIBE_GAULS`) |

```bash
curl localhost:9877/tribe/TRIBE_GAULS | jq
```

---

## Diplomacy

### GET /team-diplomacy

Returns all directed relationships between teams.

```bash
# All wars
curl localhost:9877/team-diplomacy | jq '.[] | select(.diplomacy == "DIPLOMACY_WAR")'

# Who is at war
curl localhost:9877/team-diplomacy | jq -r '.[] | select(.isHostile) | "Team \(.fromTeam) vs Team \(.toTeam)"'
```

### GET /team-alliances

Returns all team alliance pairs.

```bash
curl localhost:9877/team-alliances | jq
```

### GET /tribe-diplomacy

Returns all directed relationships from tribes to teams.

```bash
curl localhost:9877/tribe-diplomacy | jq '.[] | select(.isHostile)'
```

### GET /tribe-alliances

Returns all tribe-to-player alliances.

```bash
curl localhost:9877/tribe-alliances | jq
```

---

## Error Responses

All errors return JSON with `error` and `code` fields.

| Code | Description |
|------|-------------|
| `400` | Bad Request - Invalid parameters |
| `404` | Not Found - Resource doesn't exist |
| `405` | Method Not Allowed - Only GET supported |
| `503` | Service Unavailable - Game not loaded |

**Example:**
```json
{
  "error": "Player not found: 99",
  "code": 404
}
```

---

## Commands

The API supports bidirectional communication. In addition to reading game state via GET endpoints, you can execute game commands via POST. **Commands only work in single-player games.**

For detailed command documentation, see [Commands](schemas/command.md).

### POST /command

Execute a single game command.

**Request:**
```json
{
  "action": "moveUnit",
  "requestId": "optional-correlation-id",
  "params": {
    "unitId": 42,
    "targetTileId": 661
  }
}
```

**Response:**
```json
{
  "requestId": "optional-correlation-id",
  "success": true,
  "error": null
}
```

```bash
curl -X POST http://localhost:9877/command \
  -H "Content-Type: application/json" \
  -d '{"action": "endTurn"}'
```

### POST /commands

Execute multiple commands in sequence.

**Request:**
```json
{
  "requestId": "batch-123",
  "stopOnError": true,
  "commands": [
    {"action": "moveUnit", "params": {"unitId": 42, "targetTileId": 661}},
    {"action": "attack", "params": {"unitId": 42, "targetTileId": 662}}
  ]
}
```

**Response:**
```json
{
  "requestId": "batch-123",
  "allSucceeded": true,
  "results": [
    {"index": 0, "action": "moveUnit", "success": true},
    {"index": 1, "action": "attack", "success": true}
  ]
}
```

### Available Actions

#### Unit Commands

| Action | Required Params | Optional Params |
|--------|-----------------|-----------------|
| `moveUnit` | `unitId`, `targetTileId` | `queue`, `force` |
| `attack` | `unitId`, `targetTileId` | - |
| `fortify` | `unitId` | - |
| `pass` / `skip` | `unitId` | - |
| `sleep` | `unitId` | - |
| `sentry` | `unitId` | - |
| `wake` | `unitId` | - |
| `disband` | `unitId` | `force` |
| `promote` | `unitId`, `promotion` | - |

#### City Commands

| Action | Required Params | Optional Params |
|--------|-----------------|-----------------|
| `build` / `buildUnit` / `buildProject` | `cityId`, `unitType` | `rush` |
| `hurry` | `cityId` | `yield` |

#### Research Commands

| Action | Required Params | Optional Params |
|--------|-----------------|-----------------|
| `research` | `tech` | - |

#### Turn Commands

| Action | Required Params | Optional Params |
|--------|-----------------|-----------------|
| `endTurn` | - | `force` |

#### Laws & Economy

| Action | Required Params | Optional Params |
|--------|-----------------|-----------------|
| `chooseLaw` | `lawType` | - |
| `cancelLaw` | `lawType` | - |
| `buyYield` | `yieldType`, `amount` | - |
| `sellYield` | `yieldType`, `amount` | - |
| `convertOrders` | - | - |
| `convertLegitimacy` | - | - |
| `convertOrdersToScience` | - | - |

#### Luxury Trading

| Action | Required Params | Optional Params |
|--------|-----------------|-----------------|
| `tradeCityLuxury` | `cityId`, `resourceType`, `enable` | - |
| `tradeFamilyLuxury` | `familyType`, `resourceType`, `enable` | - |
| `tradeTribeLuxury` | `tribeType`, `resourceType`, `enable` | - |
| `tradePlayerLuxury` | `targetPlayer`, `resourceType`, `enable` | - |
| `tribute` | `yieldType`, `amount` | `toPlayer`, `toTribe`, `fromPlayer` |

#### Unit Special Actions

| Action | Required Params | Optional Params |
|--------|-----------------|-----------------|
| `swap` | `unitId`, `targetTileId` | `forceMarch` |
| `doUnitQueue` | `unitId` | - |
| `cancelUnitQueue` | `unitId` | `clearAll` |
| `formation` | `unitId`, `effectUnitType` | - |
| `unlimber` | `unitId` | - |
| `anchor` | `unitId` | - |
| `repair` | `unitId` | `buyGoods`, `queue`, `tileId` |
| `cancelImprovement` | `unitId` | - |
| `removeVegetation` | `unitId` | - |
| `harvestResource` | `unitId` | `autoHarvest` |
| `unitAutomate` | `unitId` | - |
| `addUrban` | `unitId` | `buyGoods` |
| `roadTo` | `unitId`, `tileIds` | `buyGoods` |
| `buyTile` | `unitId`, `cityId`, `yieldType` | - |
| `recruitMercenary` | `unitId` | - |
| `hireMercenary` | `unitId` | - |
| `giftUnit` | `unitId`, `targetPlayer` | - |
| `launchOffensive` | `unitId` | - |
| `applyEffectUnit` | `unitId`, `effectUnitType` | - |
| `selectUnit` | `unitId` | - |

#### Agent & Caravan Units

| Action | Required Params | Optional Params |
|--------|-----------------|-----------------|
| `createAgentNetwork` | `unitId`, `cityId` | - |
| `createTradeOutpost` | `unitId`, `tileId` | - |
| `caravanMissionStart` | `unitId`, `targetPlayer` | - |
| `caravanMissionCancel` | `unitId` | - |

#### Religious Units

| Action | Required Params | Optional Params |
|--------|-----------------|-----------------|
| `purgeReligion` | `unitId`, `religionType` | - |
| `spreadReligionTribe` | `unitId`, `tribeType` | - |
| `establishTheology` | `unitId`, `theologyType` | - |

#### Character Management

| Action | Required Params | Optional Params |
|--------|-----------------|-----------------|
| `characterName` | `characterId`, `name` | - |
| `addCharacterTrait` | `characterId`, `traitType` | `remove` |
| `setCharacterRating` | `characterId`, `ratingType`, `value` | - |
| `setCharacterExperience` | `characterId`, `xp` | - |
| `setCharacterCognomen` | `characterId`, `cognomenType` | - |
| `setCharacterNation` | `characterId`, `nationType` | - |
| `setCharacterFamily` | `characterId`, `familyType` | - |
| `setCharacterReligion` | `characterId`, `religionType` | - |
| `setCharacterCourtier` | `characterId`, `courtierType` | - |
| `setCharacterCouncil` | `characterId`, `councilType` | - |
| `playerLeader` | `playerType`, `characterId` | - |
| `familyHead` | `playerType`, `familyType`, `characterId` | - |
| `pinCharacter` | `characterId` | - |

#### City Management

| Action | Required Params | Optional Params |
|--------|-----------------|-----------------|
| `cityRename` | `cityId`, `name` | - |
| `cityAutomate` | `cityId` | `enable` |
| `buildSpecialist` | `tileId`, `specialistType` | `buyGoods`, `addFirst` |
| `setSpecialist` | `tileId`, `specialistType` | - |
| `changeCitizens` | `cityId`, `delta` | - |
| `changeReligion` | `cityId`, `religionType` | `add` |
| `changeFamily` | `cityId`, `familyType` | - |
| `changeFamilySeat` | `cityId`, `familyType` | - |

#### Goals & Communication

| Action | Required Params | Optional Params |
|--------|-----------------|-----------------|
| `abandonAmbition` | `goalId` | - |
| `addPlayerGoal` | `playerType`, `goalType` | - |
| `removePlayerGoal` | `playerType`, `goalId` | - |
| `eventStory` | `playerType`, `eventStoryType` | - |
| `finishGoal` | `goalType` | `fail` |
| `chat` | `chatType`, `message` | `targetPlayer` |
| `ping` | `tileId`, `pingType` | `message`, `reminderTurn` |
| `customReminder` | `message` | - |
| `clearChat` | - | - |

#### Game State & Turn

| Action | Required Params | Optional Params |
|--------|-----------------|-----------------|
| `extendTime` | - | - |
| `pause` | - | - |
| `undo` | - | `turnUndo` |
| `redo` | - | - |
| `replayTurn` | - | `numTurns`, `step` |
| `aiFinishTurn` | - | `numTurns` |
| `toggleNoReplay` | - | - |

#### Diplomacy Extended

| Action | Required Params | Optional Params |
|--------|-----------------|-----------------|
| `teamAlliance` | `player1`, `player2` | - |
| `tribeInvasion` | `tribeType`, `targetPlayer` | - |
| `victoryTeam` | `victoryType`, `teamType`, `actionType` | - |

#### Editor/Debug - Unit Commands

| Action | Required Params | Optional Params |
|--------|-----------------|-----------------|
| `createUnit` | `tileId`, `unitType` | `playerType`, `tribeType` |
| `unitName` | `unitId`, `name` | - |
| `setUnitFamily` | `unitId`, `familyType` | - |
| `changeUnitOwner` | `unitId` | `playerType`, `tribeType` |
| `changeCooldown` | `unitId`, `delta` | - |
| `changeDamage` | `unitId`, `delta` | - |
| `unitIncrementLevel` | `unitId` | - |
| `unitChangePromotion` | `unitId`, `promotionType` | `delta` |

#### Editor/Debug - City Commands

| Action | Required Params | Optional Params |
|--------|-----------------|-----------------|
| `createCity` | `playerType`, `tileId`, `familyType` | `turn` |
| `removeCity` | `cityId` | - |
| `cityOwner` | `cityId`, `playerType` | - |
| `changeCityDamage` | `cityId`, `delta` | - |
| `changeCulture` | `cityId` | `grow` |
| `changeCityBuildTurns` | `cityId`, `delta` | - |
| `changeCityDiscontentLevel` | `cityId`, `delta` | - |
| `changeProject` | `cityId`, `projectType` | `delta` |

#### Editor/Debug - Tile Commands

| Action | Required Params | Optional Params |
|--------|-----------------|-----------------|
| `setTerrain` | `tileId`, `terrainType` | - |
| `setTerrainHeight` | `tileId`, `heightType` | - |
| `setVegetation` | `tileId`, `vegetationType` | - |
| `setResource` | `tileId`, `resourceType` | - |
| `setRoad` | `tileId` | `hasRoad` |
| `setImprovement` | `tileId`, `improvementType` | - |
| `setTileOwner` | `tileId` | `playerType`, `tribeType` |
| `setCitySite` | `tileId`, `citySiteType` | - |
| `improvementBuildTurns` | `tileId`, `turns` | - |

#### Editor/Debug - Map & Player Commands

| Action | Required Params | Optional Params |
|--------|-----------------|-----------------|
| `mapReveal` | - | `teamType` |
| `mapUnreveal` | - | `teamType` |
| `addTech` | `playerType`, `techType` | - |
| `addYield` | `playerType`, `yieldType`, `amount` | - |
| `addMoney` | `playerType`, `amount` | - |
| `cheat` | `hotkeyType` | - |

#### Editor/Debug - Character Commands

| Action | Required Params | Optional Params |
|--------|-----------------|-----------------|
| `makeCharacterDead` | `characterId` | - |
| `makeCharacterSafe` | `characterId` | `numTurns` |
| `newCharacter` | `playerType`, `familyType` | `age`, `fillValue` |
| `addCharacter` | `characterType`, `playerType`, `familyType` | - |
| `tribeLeader` | `tribeType`, `characterId` | - |

### Type String Format

Use game type strings exactly as they appear in GET responses:
- Units: `UNIT_WARRIOR`, `UNIT_SETTLER`, `UNIT_WORKER`, etc.
- Techs: `TECH_FORESTRY`, `TECH_STONECUTTING`, etc.
- Promotions: `PROMOTION_FIERCE`, `PROMOTION_SHIELDBEARER`, etc.
- Yields: `YIELD_CIVICS`, `YIELD_TRAINING`, etc.

### Security Notes

- Commands only work in single-player mode
- Server binds to localhost only
- All commands go through game validation

---

## CORS Headers

All responses include:
```
Access-Control-Allow-Origin: *
Access-Control-Allow-Methods: GET, POST, OPTIONS
```

---

## Interactive API

Try the API interactively with [Swagger UI](swagger.html).

## OpenAPI Specification

Download the [OpenAPI 3.0 spec](openapi.yaml) for code generation.

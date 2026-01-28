# City Schema

Cities are the primary economic and production units in Old World. Each city has ~80 fields organized into 20 categories.

## Field Categories

### 1. Identity & Location

| Field | Type | Description |
|-------|------|-------------|
| `id` | integer | Unique city identifier |
| `name` | string | City name |
| `ownerId` | integer | Player index of owner |
| `tileId` | integer | Tile ID of city center |
| `x` | integer | X coordinate |
| `y` | integer | Y coordinate |
| `nation` | string \| null | Nation type (e.g., `NATION_ROME`) |
| `team` | integer | Team number |

### 2. Status Flags

| Field | Type | Description |
|-------|------|-------------|
| `isCapital` | boolean | Is nation's capital |
| `isTribe` | boolean | Is a tribal city |
| `isConnected` | boolean | Connected to trade network |

### 3. Founding & History

| Field | Type | Description |
|-------|------|-------------|
| `foundedTurn` | integer | Turn when founded |

### 4. Population

| Field | Type | Description |
|-------|------|-------------|
| `citizens` | integer | Number of citizens |

### 5. Military & Defense

| Field | Type | Description |
|-------|------|-------------|
| `hp` | integer | Current hit points |
| `hpMax` | integer | Maximum hit points |
| `damage` | integer | Current damage |
| `strength` | integer | Defensive strength |

### 6. Capture & Assimilation

| Field | Type | Description |
|-------|------|-------------|
| `captureTurns` | integer | Turns until captured (0 if not) |
| `hasCapturePlayer` | boolean | Being captured by player |
| `hasCaptureTribe` | boolean | Being captured by tribe |
| `assimilateTurns` | integer | Turns until assimilated |

### 7. Governor

| Field | Type | Description |
|-------|------|-------------|
| `governorId` | integer \| null | Governor character ID |
| `hasGovernor` | boolean | Has governor assigned |

### 8. Family & Faction

| Field | Type | Description |
|-------|------|-------------|
| `family` | string \| null | Family type (e.g., `FAMILY_CLAUDIUS`) |
| `hasFamily` | boolean | Family controls city |
| `isFamilySeat` | boolean | Is family seat |

### 9. Culture

| Field | Type | Description |
|-------|------|-------------|
| `culture` | string \| null | Culture level (e.g., `CULTURE_DEVELOPING`) |
| `cultureStep` | integer | Progress in current level |

### 10. Religion

| Field | Type | Description |
|-------|------|-------------|
| `religions` | array | Religions present |
| `religionCount` | integer | Number of religions |
| `hasStateReligion` | boolean | State religion present |
| `holyCity` | array | Holy city for these religions |
| `isReligionHolyCityAny` | boolean | Is holy city for any religion |

### 11. Production & Build Queue

| Field | Type | Description |
|-------|------|-------------|
| `hasBuild` | boolean | Something being built |
| `buildCount` | integer | Items in queue |
| `currentBuild` | object \| null | Current build item |
| `buildQueue` | array | Full build queue |

**Build Queue Item:**
```json
{
  "buildType": "UNIT",
  "itemType": "UNIT_WARRIOR",
  "progress": 150,
  "threshold": 200,
  "turnsLeft": 1,
  "hurried": false
}
```

### 12. Yields

| Field | Type | Description |
|-------|------|-------------|
| `yields` | object | Per-yield production details |

**Yield Details:**
```json
{
  "YIELD_FOOD": {
    "perTurn": 11,
    "progress": 64,
    "threshold": 200,
    "overflow": 0
  }
}
```

### 13-20. Additional Fields

| Field | Type | Description |
|-------|------|-------------|
| `specialistCount` | integer | Number of specialists |
| `improvements` | object | Count by improvement type |
| `improvementClasses` | object | Count by class |
| `projects` | object | Count by project type |
| `tradeNetwork` | integer | Trade network ID |
| `luxuryCount` | integer | Luxury resources |
| `happinessLevel` | integer | Happiness level |
| `improvementCostModifier` | integer | Cost modifier % |
| `specialistCostModifier` | integer | Cost modifier % |
| `projectCostModifier` | integer | Cost modifier % |
| `urbanTiles` | integer | Urban tile count |
| `territoryTileCount` | integer | Total territory |
| `raidedTurn` | integer | Last raided turn |
| `buyTileCount` | integer | Purchasable tiles |

## Example

```json
{
  "id": 0,
  "name": "Rome",
  "ownerId": 0,
  "tileId": 42,
  "x": 10,
  "y": 15,
  "nation": "NATION_ROME",
  "team": 0,
  "isCapital": true,
  "isTribe": false,
  "isConnected": true,
  "citizens": 5,
  "hp": 100,
  "hpMax": 100,
  "currentBuild": {
    "buildType": "UNIT",
    "itemType": "UNIT_WARRIOR",
    "progress": 150,
    "threshold": 200,
    "turnsLeft": 1,
    "hurried": false
  },
  "improvements": {
    "IMPROVEMENT_FARM": 3,
    "IMPROVEMENT_GRANARY": 1
  }
}
```

## API Endpoints

- `GET /cities` - Returns array of all cities
- `GET /city/{id}` - Returns single city by ID

## Example Queries

```bash
# All capitals
curl -s localhost:9877/cities | jq '.[] | select(.isCapital) | {name, nation}'

# Cities with active production
curl -s localhost:9877/cities | jq '.[] | select(.currentBuild) | {name, building: .currentBuild.itemType}'

# First city's improvements
curl -s localhost:9877/city/0 | jq '.improvements'
```

## JSON Schema

See [city.schema.json](city.schema.json)

# Command API

The Command API allows you to execute game actions via HTTP POST requests. Commands are only available in single-player games.

## Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/command` | POST | Execute a single command |
| `/commands` | POST | Execute multiple commands in sequence |

## Single Command

**POST /command**

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
  "error": null,
  "data": null
}
```

## Bulk Commands

**POST /commands**

```json
{
  "requestId": "batch-123",
  "stopOnError": true,
  "commands": [
    { "action": "moveUnit", "params": { "unitId": 42, "targetTileId": 661 } },
    { "action": "attack", "params": { "unitId": 42, "targetTileId": 662 } }
  ]
}
```

**Response:**

```json
{
  "requestId": "batch-123",
  "allSucceeded": true,
  "results": [
    { "index": 0, "action": "moveUnit", "success": true },
    { "index": 1, "action": "attack", "success": true }
  ],
  "stoppedAtIndex": null
}
```

## Available Actions

### Unit Commands

| Action | Required Params | Optional Params | Description |
|--------|-----------------|-----------------|-------------|
| `moveUnit` | `unitId`, `targetTileId` | `queue`, `march`, `waypointTileId` | Move unit to tile |
| `attack` | `unitId`, `targetTileId` | | Attack target tile |
| `fortify` | `unitId` | | Fortify unit in place |
| `pass` / `skip` | `unitId` | | Skip unit's turn |
| `sleep` | `unitId` | | Put unit to sleep |
| `sentry` | `unitId` | | Set unit to sentry mode |
| `wake` | `unitId` | | Wake sleeping/sentry unit |
| `disband` | `unitId` | `force` | Disband unit |
| `promote` | `unitId`, `promotion` | | Apply promotion |

### City Commands

| Action | Required Params | Optional Params | Description |
|--------|-----------------|-----------------|-------------|
| `build` / `buildUnit` | `cityId`, `unitType` | `buyGoods`, `first` | Build unit in city |
| `buildProject` | `cityId`, `projectType` | `buyGoods`, `first`, `repeat` | Build project in city |
| `hurryCivics` / `hurry` | `cityId` | | Rush production with civics |
| `hurryTraining` | `cityId` | | Rush production with training |
| `hurryMoney` | `cityId` | | Rush production with money |
| `hurryPopulation` | `cityId` | | Rush production with population |
| `hurryOrders` | `cityId` | | Rush production with orders |

### Research Commands

| Action | Required Params | Optional Params | Description |
|--------|-----------------|-----------------|-------------|
| `research` | `tech` | | Set research target |

### Turn Commands

| Action | Required Params | Optional Params | Description |
|--------|-----------------|-----------------|-------------|
| `endTurn` | | `force` | End current turn |

## Parameter Details

### Unit IDs and Tile IDs

Get unit and tile IDs from the read endpoints:

```bash
# Get all units
curl http://localhost:9877/units

# Get specific player's units
curl http://localhost:9877/player/0/units

# Get tile info
curl http://localhost:9877/tile/661
```

### Game Type Strings

Parameters like `unitType`, `projectType`, `tech`, and `promotion` use game type strings:

- Units: `UNIT_WARRIOR`, `UNIT_WORKER`, `UNIT_SETTLER`, etc.
- Projects: `PROJECT_GRANARY`, `PROJECT_BARRACKS`, `PROJECT_SHRINE`, etc.
- Techs: `TECH_FORESTRY`, `TECH_MINING`, `TECH_STONECUTTING`, etc.
- Promotions: `PROMOTION_FIERCE`, `PROMOTION_SHIELDBEARER`, etc.

### Boolean Parameters

Boolean parameters default to `false` unless specified:

- `queue` - Queue the move instead of immediate
- `march` - Force march (spend extra fatigue for movement)
- `force` - Force action even with warnings
- `buyGoods` - Buy required goods for production
- `first` - Add to front of build queue
- `repeat` - Repeat project when complete
- `stopOnError` - Stop batch on first failure (default: `true`)

## Error Handling

Failed commands return `success: false` with an error message:

```json
{
  "requestId": "req-123",
  "success": false,
  "error": "Unit not found: 999"
}
```

Common errors:
- `"Game not available"` - No game loaded
- `"Commands not supported in multiplayer games"` - MP game detected
- `"Cannot perform actions (not player's turn or action blocked)"` - Not your turn
- `"Unit not found: {id}"` - Invalid unit ID
- `"Tile not found: {id}"` - Invalid tile ID
- `"City not found: {id}"` - Invalid city ID
- `"Unknown unit type: {type}"` - Invalid type string
- `"Unknown project type: {type}"` - Invalid project string
- `"Unknown tech type: {type}"` - Invalid tech string

## Examples

### Move and Attack Sequence

```bash
curl -X POST http://localhost:9877/commands \
  -H "Content-Type: application/json" \
  -d '{
    "commands": [
      {"action": "moveUnit", "params": {"unitId": 42, "targetTileId": 661}},
      {"action": "attack", "params": {"unitId": 42, "targetTileId": 662}}
    ]
  }'
```

### Move with Waypoint

```bash
curl -X POST http://localhost:9877/command \
  -H "Content-Type: application/json" \
  -d '{
    "action": "moveUnit",
    "params": {
      "unitId": 42,
      "targetTileId": 700,
      "waypointTileId": 661,
      "march": true
    }
  }'
```

### Build a Unit

```bash
curl -X POST http://localhost:9877/command \
  -H "Content-Type: application/json" \
  -d '{
    "action": "buildUnit",
    "params": {
      "cityId": 5,
      "unitType": "UNIT_WARRIOR"
    }
  }'
```

### Build a Project

```bash
curl -X POST http://localhost:9877/command \
  -H "Content-Type: application/json" \
  -d '{
    "action": "buildProject",
    "params": {
      "cityId": 5,
      "projectType": "PROJECT_GRANARY"
    }
  }'
```

### Rush Production with Money

```bash
curl -X POST http://localhost:9877/command \
  -H "Content-Type: application/json" \
  -d '{
    "action": "hurryMoney",
    "params": {"cityId": 5}
  }'
```

### Research a Technology

```bash
curl -X POST http://localhost:9877/command \
  -H "Content-Type: application/json" \
  -d '{
    "action": "research",
    "params": {"tech": "TECH_FORESTRY"}
  }'
```

### End Turn

```bash
curl -X POST http://localhost:9877/command \
  -H "Content-Type: application/json" \
  -d '{"action": "endTurn"}'
```

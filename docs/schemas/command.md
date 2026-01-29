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

## Available Actions (51 commands)

### Unit Movement & State

| Action | Required Params | Optional Params | Description |
|--------|-----------------|-----------------|-------------|
| `moveUnit` | `unitId`, `targetTileId` | `queue`, `march`, `waypointTileId` | Move unit to tile |
| `attack` | `unitId`, `targetTileId` | | Attack target tile |
| `fortify` | `unitId` | | Fortify unit in place |
| `pass` / `skip` | `unitId` | | Skip unit's turn |
| `sleep` | `unitId` | | Put unit to sleep |
| `sentry` | `unitId` | | Set unit to sentry mode |
| `wake` | `unitId` | | Wake sleeping/sentry unit |
| `heal` | `unitId` | `auto` | Heal unit |
| `march` | `unitId` | | Set unit to march mode |
| `lock` | `unitId` | | Lock unit in place |

### Unit Actions

| Action | Required Params | Optional Params | Description |
|--------|-----------------|-----------------|-------------|
| `disband` | `unitId` | `force` | Disband unit |
| `promote` | `unitId`, `promotion` | | Apply promotion |
| `pillage` | `unitId` | | Pillage with unit |
| `burn` | `unitId` | | Burn with unit |
| `upgrade` | `unitId`, `unitType` | `buyGoods` | Upgrade to new unit type |
| `spreadReligion` | `unitId`, `cityId` | | Spread religion to city |

### Worker Commands

| Action | Required Params | Optional Params | Description |
|--------|-----------------|-----------------|-------------|
| `buildImprovement` | `unitId`, `improvementType`, `tileId` | `buyGoods`, `queue` | Build improvement on tile |
| `upgradeImprovement` | `unitId` | `buyGoods` | Upgrade existing improvement |
| `addRoad` | `unitId`, `tileId` | `buyGoods`, `queue` | Add road to tile |

### City Foundation

| Action | Required Params | Optional Params | Description |
|--------|-----------------|-----------------|-------------|
| `foundCity` | `unitId`, `familyType` | `nationType` | Found city with settler |
| `joinCity` | `unitId` | | Join unit to city |

### City Production

| Action | Required Params | Optional Params | Description |
|--------|-----------------|-----------------|-------------|
| `build` / `buildUnit` | `cityId`, `unitType` | `buyGoods`, `first` | Build unit in city |
| `buildProject` | `cityId`, `projectType` | `buyGoods`, `first`, `repeat` | Build project in city |
| `buildQueue` | `cityId`, `oldSlot`, `newSlot` | | Reorder build queue |
| `hurryCivics` / `hurry` | `cityId` | | Rush production with civics |
| `hurryTraining` | `cityId` | | Rush production with training |
| `hurryMoney` | `cityId` | | Rush production with money |
| `hurryPopulation` | `cityId` | | Rush production with population |
| `hurryOrders` | `cityId` | | Rush production with orders |

### Research & Decisions

| Action | Required Params | Optional Params | Description |
|--------|-----------------|-----------------|-------------|
| `research` | `tech` | | Set research target |
| `redrawTech` | | | Redraw available technologies |
| `targetTech` | `techType` | | Set long-term tech target |
| `makeDecision` | `decisionId`, `choiceIndex` | `data` | Make a decision choice |
| `removeDecision` | `decisionId` | | Remove/dismiss a decision |

### Diplomacy - Players

| Action | Required Params | Optional Params | Description |
|--------|-----------------|-----------------|-------------|
| `declareWar` | `targetPlayer` | | Declare war on player |
| `makePeace` | `targetPlayer` | | Make peace with player |
| `declareTruce` | `targetPlayer` | | Declare truce with player |

### Diplomacy - Tribes

| Action | Required Params | Optional Params | Description |
|--------|-----------------|-----------------|-------------|
| `declareWarTribe` | `tribeType` | | Declare war on tribe |
| `makePeaceTribe` | `tribeType` | | Make peace with tribe |
| `declareTruceTribe` | `tribeType` | | Declare truce with tribe |
| `allyTribe` | `tribeType` | | Form alliance with tribe |

### Diplomacy - Gifts

| Action | Required Params | Optional Params | Description |
|--------|-----------------|-----------------|-------------|
| `giftCity` | `cityId`, `targetPlayer` | | Gift city to player |
| `giftYield` | `yieldType`, `targetPlayer` | `reverse` | Gift resources to player |

### Character Management

| Action | Required Params | Optional Params | Description |
|--------|-----------------|-----------------|-------------|
| `assignGovernor` | `cityId`, `characterId` | | Assign character as governor |
| `releaseGovernor` | `cityId` | | Release city's governor |
| `assignGeneral` | `unitId`, `characterId` | | Assign character as general |
| `releaseGeneral` | `unitId` | | Release unit's general |
| `assignAgent` | `cityId`, `characterId` | | Assign character as agent |
| `releaseAgent` | `cityId` | | Release city's agent |
| `startMission` | `missionType`, `characterId` | `target`, `cancel` | Start character mission |

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
- Improvements: `IMPROVEMENT_FARM`, `IMPROVEMENT_MINE`, `IMPROVEMENT_QUARRY`, etc.
- Families: `FAMILY_CHAMPIONS`, `FAMILY_ARTISANS`, `FAMILY_SAGES`, etc.
- Nations: `NATION_ROME`, `NATION_GREECE`, `NATION_EGYPT`, etc.
- Tribes: `TRIBE_GAULS`, `TRIBE_GOTHS`, `TRIBE_VANDALS`, etc.
- Yields: `YIELD_FOOD`, `YIELD_WOOD`, `YIELD_STONE`, `YIELD_IRON`, `YIELD_CIVICS`, etc.
- Missions: `MISSION_NETWORK`, `MISSION_INFLUENCE`, `MISSION_SLANDER`, etc.

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

### Build an Improvement

```bash
curl -X POST http://localhost:9877/command \
  -H "Content-Type: application/json" \
  -d '{
    "action": "buildImprovement",
    "params": {
      "unitId": 15,
      "improvementType": "IMPROVEMENT_FARM",
      "tileId": 500
    }
  }'
```

### Found a City

```bash
curl -X POST http://localhost:9877/command \
  -H "Content-Type: application/json" \
  -d '{
    "action": "foundCity",
    "params": {
      "unitId": 8,
      "familyType": "FAMILY_ARTISANS"
    }
  }'
```

### Declare War on Another Player

```bash
curl -X POST http://localhost:9877/command \
  -H "Content-Type: application/json" \
  -d '{
    "action": "declareWar",
    "params": {"targetPlayer": 1}
  }'
```

### Assign a Governor

```bash
curl -X POST http://localhost:9877/command \
  -H "Content-Type: application/json" \
  -d '{
    "action": "assignGovernor",
    "params": {
      "cityId": 2,
      "characterId": 15
    }
  }'
```

### Make a Decision

```bash
curl -X POST http://localhost:9877/command \
  -H "Content-Type: application/json" \
  -d '{
    "action": "makeDecision",
    "params": {
      "decisionId": 42,
      "choiceIndex": 0
    }
  }'
```

### Upgrade a Unit

```bash
curl -X POST http://localhost:9877/command \
  -H "Content-Type: application/json" \
  -d '{
    "action": "upgrade",
    "params": {
      "unitId": 12,
      "unitType": "UNIT_SWORDSMAN"
    }
  }'
```

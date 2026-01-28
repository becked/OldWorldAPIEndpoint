# Tribe Schema

Tribes represent barbarian factions and minor civilizations in Old World.

## Fields

| Field | Type | Description |
|-------|------|-------------|
| `tribeType` | string | Tribe identifier (e.g., `TRIBE_GAULS`, `TRIBE_BERBERS`) |
| `isAlive` | boolean | Tribe is active |
| `isDead` | boolean | Tribe has been eliminated |
| `hasDiplomacy` | boolean | Tribe type supports diplomacy |
| `leaderId` | integer \| null | Character ID of tribe leader |
| `hasLeader` | boolean | Has a named leader |
| `religion` | string \| null | Tribe's religion |
| `hasReligion` | boolean | Follows a religion |
| `allyPlayerId` | integer \| null | Allied player index |
| `allyTeam` | integer \| null | Allied team number |
| `hasPlayerAlly` | boolean | Has player alliance |
| `numUnits` | integer | Number of units |
| `numCities` | integer | Number of cities |
| `strength` | integer | Military strength |
| `cityIds` | array | City IDs controlled |
| `settlementTileIds` | array | Settlement tile IDs |
| `numTribeImprovements` | integer | Tribe improvements count |

## Example

```json
{
  "tribeType": "TRIBE_GAULS",
  "isAlive": true,
  "isDead": false,
  "hasDiplomacy": true,
  "leaderId": 150,
  "hasLeader": true,
  "religion": "RELIGION_DRUIDISM",
  "hasReligion": true,
  "allyPlayerId": null,
  "allyTeam": null,
  "hasPlayerAlly": false,
  "numUnits": 8,
  "numCities": 2,
  "strength": 45,
  "cityIds": [10, 12],
  "settlementTileIds": [500, 501, 502],
  "numTribeImprovements": 3
}
```

## API Endpoints

- `GET /tribes` - All tribes
- `GET /tribe/{tribeType}` - Single tribe by type string
- `GET /tribe-diplomacy` - Tribe diplomatic relationships
- `GET /tribe-alliances` - Tribe alliances

## Example Queries

```bash
# All living tribes
curl -s localhost:9877/tribes | jq '.[] | select(.isAlive) | {type: .tribeType, units: .numUnits}'

# Tribes with player alliances
curl -s localhost:9877/tribes | jq '.[] | select(.hasPlayerAlly) | {type: .tribeType, ally: .allyPlayerId}'

# Get specific tribe
curl -s localhost:9877/tribe/TRIBE_GAULS | jq
```

## JSON Schema

See [tribe.schema.json](tribe.schema.json)

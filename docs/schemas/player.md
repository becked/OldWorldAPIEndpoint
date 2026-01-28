# Player Schema

A player represents a nation in the game.

## Fields

| Field | Type | Description |
|-------|------|-------------|
| `index` | integer | Player index (0-based, used for lookups) |
| `team` | integer | Team number this player belongs to |
| `nation` | string | Nation type (e.g., `NATION_ROME`, `NATION_GREECE`) |
| `leaderId` | integer \| null | Character ID of the current leader |
| `cities` | integer | Number of cities owned |
| `units` | integer | Number of units controlled |
| `legitimacy` | integer | Current legitimacy value |
| `stockpiles` | object | Current resource stockpiles keyed by yield type |
| `rates` | object | Per-turn resource rates keyed by yield type |

## Stockpiles & Rates

Both `stockpiles` and `rates` are objects keyed by yield type strings:

- `YIELD_FOOD` - Food
- `YIELD_WOOD` - Wood
- `YIELD_STONE` - Stone
- `YIELD_IRON` - Iron
- `YIELD_MONEY` - Money (gold)
- `YIELD_CIVICS` - Civics
- `YIELD_TRAINING` - Training
- `YIELD_SCIENCE` - Science
- `YIELD_CULTURE` - Culture
- `YIELD_GROWTH` - Growth
- `YIELD_HAPPINESS` - Happiness
- `YIELD_DISCONTENT` - Discontent
- `YIELD_ORDERS` - Orders

## Example

```json
{
  "index": 0,
  "team": 0,
  "nation": "NATION_ROME",
  "leaderId": 42,
  "cities": 5,
  "units": 12,
  "legitimacy": 100,
  "stockpiles": {
    "YIELD_FOOD": 500,
    "YIELD_WOOD": 250,
    "YIELD_STONE": 100,
    "YIELD_IRON": 50,
    "YIELD_MONEY": 300,
    "YIELD_CIVICS": 200
  },
  "rates": {
    "YIELD_FOOD": 25,
    "YIELD_SCIENCE": 15,
    "YIELD_CIVICS": 10
  }
}
```

## API Endpoints

### Core
- `GET /players` - Returns array of all players
- `GET /player/{index}` - Returns single player by index

### Extensions
- `GET /player/{index}/units` - Player's units (see [Unit](unit.md))
- `GET /player/{index}/techs` - Technology research state
- `GET /player/{index}/families` - Family relationships
- `GET /player/{index}/religion` - Religion state (see [Religion](religion.md))
- `GET /player/{index}/goals` - Goals and ambitions*
- `GET /player/{index}/decisions` - Pending decisions*
- `GET /player/{index}/laws` - Active laws*
- `GET /player/{index}/missions` - Active missions*
- `GET /player/{index}/resources` - Resource/luxury counts*

*These endpoints return placeholder data due to game API limitations.

## Technology State

`GET /player/{index}/techs` returns:

```json
{
  "researching": "TECH_STONECUTTING",
  "progress": {
    "TECH_STONECUTTING": 148
  },
  "researched": [
    "TECH_TRAPPING",
    "TECH_ADMINISTRATION"
  ],
  "available": [
    "TECH_IRONWORKING",
    "TECH_STONECUTTING"
  ]
}
```

## Family Data

`GET /player/{index}/families` returns:

```json
{
  "families": [
    {
      "family": "FAMILY_SARGONID",
      "opinionRate": 0
    }
  ]
}
```

## Example Queries

```bash
# Get all player stockpiles
curl -s localhost:9877/players | jq '.[] | {nation, money: .stockpiles.YIELD_MONEY}'

# Get science rate by nation
curl -s localhost:9877/players | jq '.[] | {nation, science: .rates.YIELD_SCIENCE}'

# Get first player's data
curl -s localhost:9877/player/0 | jq
```

## JSON Schema

See [player.schema.json](player.schema.json)

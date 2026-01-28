# Config Schema

Game configuration and current state.

## Endpoint

| Endpoint | Description |
|----------|-------------|
| `GET /config` | Game configuration |

## Fields

| Field | Type | Description |
|-------|------|-------------|
| `numTiles` | integer | Total tiles on map |
| `numPlayers` | integer | Number of players |
| `numTeams` | integer | Number of teams |
| `turn` | integer | Current turn number |
| `year` | integer | Current year |

## Example

```json
{
  "numTiles": 5476,
  "numPlayers": 5,
  "numTeams": 5,
  "turn": 42,
  "year": 42
}
```

## API Limitations

Some configuration fields are not available due to game API restrictions:

- `mapWidth` / `mapHeight` - Map dimensions not directly accessible
- `mapClass` / `mapSize` - Map type info not accessible
- `difficulty` - Difficulty settings not accessible
- `gameOptions` - Game options not accessible

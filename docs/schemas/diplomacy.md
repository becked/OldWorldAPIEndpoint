# Diplomacy Schema

Diplomacy tracks relationships between teams (nations) and tribes.

## Team Diplomacy

Directed relationships between player teams.

| Field | Type | Description |
|-------|------|-------------|
| `fromTeam` | integer | Source team number |
| `toTeam` | integer | Target team number |
| `diplomacy` | string \| null | State (e.g., `DIPLOMACY_WAR`, `DIPLOMACY_PEACE`) |
| `isHostile` | boolean | In hostile state |
| `isPeace` | boolean | At peace |
| `hasContact` | boolean | Have made contact |
| `warScore` | integer | Current war score |
| `warState` | string \| null | War state type |
| `conflictTurn` | integer | Turn conflict began |
| `conflictNumTurns` | integer | Turns in conflict |
| `diplomacyTurn` | integer | Turn current state began |
| `diplomacyNumTurns` | integer | Turns in current state |
| `diplomacyBlockTurn` | integer | Turn diplomacy blocked |
| `diplomacyBlockTurns` | integer | Turns blocked remaining |

### Example

```json
{
  "fromTeam": 0,
  "toTeam": 1,
  "diplomacy": "DIPLOMACY_WAR",
  "isHostile": true,
  "isPeace": false,
  "hasContact": true,
  "warScore": 50,
  "warState": "WARSTATE_OFFENSIVE",
  "conflictTurn": 10,
  "conflictNumTurns": 5
}
```

## Team Alliances

Alliance pairs between teams.

| Field | Type | Description |
|-------|------|-------------|
| `team` | integer | Team number |
| `allyTeam` | integer | Allied team number |

### Example

```json
{
  "team": 0,
  "allyTeam": 2
}
```

## Tribe Diplomacy

Directed relationships from tribes to player teams.

| Field | Type | Description |
|-------|------|-------------|
| `tribe` | string | Tribe type (e.g., `TRIBE_GAULS`) |
| `toTeam` | integer | Target team number |
| `diplomacy` | string \| null | Diplomacy state |
| `isHostile` | boolean | Hostile state |
| `isPeace` | boolean | At peace |
| `hasContact` | boolean | Have contact |
| `warScore` | integer | War score |
| `warState` | string \| null | War state |
| `conflictTurn` | integer | Conflict start turn |
| `conflictNumTurns` | integer | Turns in conflict |

### Example

```json
{
  "tribe": "TRIBE_GAULS",
  "toTeam": 0,
  "diplomacy": "DIPLOMACY_WAR",
  "isHostile": true,
  "isPeace": false,
  "warScore": 25
}
```

## Tribe Alliances

Alliances between tribes and players.

| Field | Type | Description |
|-------|------|-------------|
| `tribe` | string | Tribe type |
| `allyPlayerId` | integer | Allied player index |
| `allyTeam` | integer | Allied team number |

### Example

```json
{
  "tribe": "TRIBE_GAULS",
  "allyPlayerId": 0,
  "allyTeam": 0
}
```

## API Endpoints

- `GET /team-diplomacy` - All team-to-team relationships
- `GET /team-alliances` - All team alliances
- `GET /tribe-diplomacy` - All tribe-to-team relationships
- `GET /tribe-alliances` - All tribe alliances

## Example Queries

```bash
# All active wars
curl -s localhost:9877/team-diplomacy | jq '.[] | select(.diplomacy == "DIPLOMACY_WAR")'

# Who is at war with whom
curl -s localhost:9877/team-diplomacy | jq -r '.[] | select(.isHostile) | "Team \(.fromTeam) vs Team \(.toTeam)"'

# Tribe alliances
curl -s localhost:9877/tribe-alliances | jq

# Hostile tribes
curl -s localhost:9877/tribe-diplomacy | jq '.[] | select(.isHostile) | {tribe, team: .toTeam}'
```

## JSON Schema

See [diplomacy.schema.json](diplomacy.schema.json)

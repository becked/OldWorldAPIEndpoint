# Getting Started

Get the Old World API running in under 2 minutes.

## Installation

### From Release

Copy the mod folder to your Old World mods directory:

| Platform | Path |
|----------|------|
| **macOS** | `~/Library/Application Support/OldWorld/Mods/OldWorldAPIEndpoint/` |
| **Windows** | `%APPDATA%\OldWorld\Mods\OldWorldAPIEndpoint\` |

### From Source

```bash
git clone https://github.com/becked/OldWorldAPIEndpoint
cd OldWorldAPIEndpoint
./deploy.sh
```

## Enable the Mod

1. Launch Old World
2. Go to **Mods** in the main menu
3. Enable **Old World API Endpoint**
4. Start or load a game

## Connect to the API

### Option 1: HTTP REST (Recommended)

Query game state on demand:

```bash
# Test connection
curl localhost:9877/state | jq '{turn, year}'

# Get all players
curl localhost:9877/players | jq

# Get specific city
curl localhost:9877/city/0 | jq
```

### Option 2: TCP Broadcast

Receive automatic updates at each turn end:

```bash
# Connect and wait for turn updates
nc localhost 9876
```

## Verify It's Working

After ending a turn in-game, you should see JSON output:

```bash
curl -s localhost:9877/state | jq '{turn, year, playerCount: (.players | length)}'
```

Expected output:
```json
{
  "turn": 5,
  "year": 5,
  "playerCount": 4
}
```

## Common Queries

### Player Resources

```bash
# All stockpiles
curl -s localhost:9877/players | jq '.[] | {nation, stockpiles}'

# Per-turn rates
curl -s localhost:9877/players | jq '.[] | {nation, science: .rates.YIELD_SCIENCE}'
```

### City Information

```bash
# All capitals
curl -s localhost:9877/cities | jq '.[] | select(.isCapital) | {name, nation}'

# Cities with active production
curl -s localhost:9877/cities | jq '.[] | select(.currentBuild) | {name, building: .currentBuild.itemType}'
```

### Character Data

```bash
# All leaders
curl -s localhost:9877/characters | jq '.[] | select(.isLeader and .isAlive) | {name, nation, age}'

# Character events (births, deaths, marriages)
curl -s localhost:9877/character-events | jq
```

### Diplomacy

```bash
# Active wars
curl -s localhost:9877/team-diplomacy | jq '.[] | select(.diplomacy == "DIPLOMACY_WAR")'

# Tribe alliances
curl -s localhost:9877/tribe-alliances | jq
```

## Next Steps

- [API Reference](api-reference.md) - Complete endpoint documentation
- [Player Schema](schemas/player.md) - Full player data model
- [City Schema](schemas/city.md) - All 80+ city fields
- [Character Schema](schemas/character.md) - All 85+ character fields

## Troubleshooting

### "Connection refused"

The mod only serves data when a game is loaded. Make sure you:
1. Have the mod enabled
2. Are in an active game (not main menu)
3. Have ended at least one turn

### No data in response

Try ending a turn in-game. The API initializes after the first turn end.

### Port already in use

The default ports are 9876 (TCP) and 9877 (HTTP). If another application is using these ports, you'll need to stop that application first.

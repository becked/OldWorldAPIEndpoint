# Old World API Endpoint

A mod for [Old World](https://store.steampowered.com/app/597180/Old_World/) that exposes game state via TCP and HTTP APIs, enabling companion apps, overlays, and external tools.

## Features

- **TCP Broadcast (port 9876)** - Push-based: automatically sends game state at each turn end
- **HTTP REST (port 9877)** - Pull-based: query game state on demand

## Installation

### From Release
Copy the mod folder to your Old World mods directory:
- **macOS**: `~/Library/Application Support/OldWorld/Mods/OldWorldAPIEndpoint/`
- **Windows**: `%APPDATA%\OldWorld\Mods\OldWorldAPIEndpoint\`

Enable the mod in Old World's Mod Manager.

### From Source
```bash
./deploy.sh
```

## API Overview

### Data Available
- **Game**: Turn number, year, current player
- **Players**: Nation, team, cities, units, legitimacy, resource stockpiles, per-turn rates
- **Cities**: ~80 fields including population, production, yields, improvements, religion
- **Characters**: ~40 fields including traits, ratings, family, jobs, military
- **Diplomacy**: Team-to-team and tribe-to-team relationships, alliances, war state
- **Tribes**: Barbarians, minor civs with their units, settlements, alliances

### TCP Broadcast (port 9876)

Connects and receives newline-delimited JSON at each turn end:

```bash
nc localhost 9876
```

### HTTP REST (port 9877)

#### Collection Endpoints
```
GET /state              Full game state
GET /players            All players
GET /cities             All cities
GET /characters         All characters
GET /tribes             All tribes
GET /team-diplomacy     Team diplomatic relationships
GET /team-alliances     Team alliances
GET /tribe-diplomacy    Tribe diplomatic relationships
GET /tribe-alliances    Tribe alliances
```

#### Single-Entity Endpoints
```
GET /player/{index}     Player by index (0-based)
GET /city/{id}          City by ID
GET /character/{id}     Character by ID
GET /tribe/{type}       Tribe by type (e.g., TRIBE_GAULS)
```

## Example Commands

### Basic Queries

```bash
# Full game state (pretty-printed)
curl -s localhost:9877/state | jq

# Current turn and year
curl -s localhost:9877/state | jq '{turn, year}'

# List all nations in game
curl -s localhost:9877/players | jq '.[].nation'
```

### Player Data

```bash
# All player stockpiles
curl -s localhost:9877/players | jq '.[] | {nation, money: .stockpiles.YIELD_MONEY, food: .stockpiles.YIELD_FOOD}'

# Per-turn rates for all players
curl -s localhost:9877/players | jq '.[] | {nation, rates}'

# Science rate by nation
curl -s localhost:9877/state | jq -r '.players[] | "\(.nation | ltrimstr("NATION_")): \(.rates.YIELD_SCIENCE)/turn"'
```

### City Data

```bash
# All cities with owner and population
curl -s localhost:9877/cities | jq '.[] | {name, nation, citizens}'

# Cities building something
curl -s localhost:9877/cities | jq '.[] | select(.currentBuild != null) | {name, building: .currentBuild}'

# Capital cities
curl -s localhost:9877/cities | jq '.[] | select(.isCapital) | {name, nation}'
```

### Character Data

```bash
# All living leaders
curl -s localhost:9877/characters | jq '.[] | select(.isLeader and .isAlive) | {name, nation, age}'

# Generals with their ratings
curl -s localhost:9877/characters | jq '.[] | select(.isGeneral) | {name, courage: .RATING_COURAGE, discipline: .RATING_DISCIPLINE}'

# Characters by trait
curl -s localhost:9877/characters | jq '.[] | select(.traits | index("TRAIT_WARRIOR")) | .name'
```

### Diplomacy

```bash
# All wars
curl -s localhost:9877/team-diplomacy | jq '.[] | select(.diplomacy == "DIPLOMACY_WAR")'

# Who is at war with whom
curl -s localhost:9877/team-diplomacy | jq -r '.[] | select(.diplomacy == "DIPLOMACY_WAR") | "Team \(.fromTeam) at war with Team \(.toTeam)"'

# Tribe alliances
curl -s localhost:9877/tribe-alliances | jq
```

### Recording History

The API provides snapshots, not historical data. To track changes over time:

```bash
# Record TCP stream (captures each turn)
nc localhost 9876 | tee game_history.jsonl

# Query recorded history
jq -r 'select(.players) | "Turn \(.turn): Science \([.players[] | select(.nation=="NATION_ROME")][0].rates.YIELD_SCIENCE)"' game_history.jsonl
```

### Watch Mode

```bash
# Live dashboard (updates every 2 seconds)
watch -n 2 'curl -s localhost:9877/state | jq "{turn, year, players: [.players[] | {n: .nation, s: .rates.YIELD_SCIENCE}]}"'
```

## Development

### Build & Deploy
```bash
./deploy.sh
```

### Testing
```bash
# Automated headless test
./test-headless.sh test/data/APITestSave.zip 2
```

### Project Structure
```
Source/
  APIEndpoint.cs       # Mod entry point, data builders
  TcpBroadcastServer.cs # TCP server (push)
  HttpRestServer.cs     # HTTP server (pull)
docs/
  roadmap.md           # Completed and planned features
  api-design-principles.md
```

## License

MIT

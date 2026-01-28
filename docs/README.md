# Old World API Documentation

A mod for [Old World](https://store.steampowered.com/app/597180/Old_World/) that exposes game state via TCP and HTTP APIs, enabling companion apps, overlays, and external tools.

## Features

- **TCP Broadcast (port 9876)**: Push-based updates sent automatically at each turn end
- **HTTP REST (port 9877)**: Pull-based queries for on-demand access to game state
- **Comprehensive Data**: Players, cities, characters, tribes, diplomacy, and events
- **Game Type Strings**: Uses exact game identifiers like `NATION_ROME`, `YIELD_FOOD`

## Available Data

| Entity | Fields | Description |
|--------|--------|-------------|
| **Players** | ~10 | Nation, team, stockpiles, per-turn rates |
| **Cities** | ~80 | Population, production, yields, improvements, religion |
| **Characters** | ~85 | Traits, ratings, family, jobs, relationships, opinions |
| **Tribes** | ~17 | Status, leader, religion, units, settlements |
| **Events** | 10 types | Births, deaths, marriages, captures, wonders |
| **Diplomacy** | 4 types | Team and tribe relationships, alliances |

## Quick Links

- [Quick Start Guide](getting-started.md) - Get connected in 2 minutes
- [API Reference](api-reference.md) - All 16 endpoints documented
- [Interactive API](swagger.html) - Try the API with Swagger UI
- [JSON Schemas](schemas/player.md) - Full data model documentation

## Example

```bash
# Get all player stockpiles
curl -s localhost:9877/players | jq '.[] | {nation, money: .stockpiles.YIELD_MONEY}'
```

```json
{
  "nation": "NATION_ROME",
  "money": 500
}
```

## Source Code

[View on GitHub](https://github.com/becked/OldWorldAPIEndpoint)

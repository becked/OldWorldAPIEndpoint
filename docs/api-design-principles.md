# API Design Principles

This document captures the design philosophy for the Old World API Endpoint.

## Core Philosophy

The API is a **data mirror** of the game state, not a curated interface for specific client use cases. We expose game data as-is and let clients decide what's relevant to them.

---

## Principle 1: Mirror Game Data Structures

**Entities live at the top level with references, not nested hierarchies.**

The game stores cities globally in `Game.getCities()`, not nested under players. Cities reference their owner via `city.getPlayer()`. Our API mirrors this:

```json
{
  "turn": 50,
  "players": [
    { "id": 0, "nation": "NATION_ROME", ... }
  ],
  "cities": [
    { "id": 123, "name": "Rome", "ownerId": 0, ... },
    { "id": 124, "name": "Memphis", "ownerId": 1, ... }
  ]
}
```

**Not** nested like this:
```json
{
  "players": [{
    "id": 0,
    "cities": [{ "id": 123, "name": "Rome", ... }]
  }]
}
```

### Rationale

- Honest to the source data model
- Scales consistently as we add units, characters, diplomacy, etc.
- Avoids data duplication
- Enables cross-entity queries (all cities, all units) without iteration
- Clients doing analysis want flexible access patterns anyway

---

## Principle 2: Expose All Available Data

**Be comprehensive, not selective. Don't assume what clients need.**

If the game exposes data on an entity, our API should include it. We don't curate fields based on guessed use cases.

### Rationale

- Future-proofs the API - no need to add fields as clients request them
- Clients can filter/ignore what they don't need
- Avoids second-guessing diverse client requirements
- One comprehensive schema is easier to maintain than evolving partial ones

### Example

For cities, include everything available:
- Basic: id, name, ownerId, coordinates
- Population: citizens, growth progress, growth rate
- Production: current build, progress, threshold
- Culture: level, step, progress
- Happiness level
- All per-turn yields

---

## Principle 3: Use Game Type Strings

**Use the game's internal type identifiers for enums and types.**

```json
{
  "nation": "NATION_ROME",
  "production": "UNIT_WARRIOR",
  "culture": "CULTURE_DEVELOPING"
}
```

**Not** prettified versions:
```json
{
  "nation": "Rome",
  "production": "Warrior",
  "culture": "Developing"
}
```

### Rationale

- Consistent with game's data model
- Unambiguous - no localization or display name confusion
- Clients can map to display names if needed
- Matches what modders and game files use

---

## Practical Implications

### Adding New Entity Types

When adding units, characters, diplomacy, etc., follow the same pattern:
1. Top-level array in the JSON root
2. Each entity has an `id` and references to related entities (e.g., `ownerId`, `cityId`)
3. Expose all available fields from the game API

### Schema Evolution

- Adding new fields is non-breaking
- Adding new entity types is non-breaking
- Removing fields should be avoided (deprecate instead)
- Structural changes (e.g., nesting) are breaking changes

### Client Expectations

Clients should expect:
- Stable field names matching game internals
- All entities at the root level
- References via IDs, not nested objects
- Comprehensive data - filter client-side as needed

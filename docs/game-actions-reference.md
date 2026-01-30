# Old World Game Actions Reference

This document provides a comprehensive reference for all game actions available in Old World's internal API. Each action is defined by an `ActionType` enum value and accepts parameters via an `ActionData` object.

## Action System Overview

### ActionData Structure

All game actions use the `ActionData` class:

```csharp
ActionData(ActionType eType, PlayerType ePlayer)
```

Parameters are added via `addValue(object value)` and retrieved via `getValue(int index)`.

### Type Conventions

| Type | Description | Example Values |
|------|-------------|----------------|
| `PlayerType` | Player index (0-based) | `0`, `1`, `2`, ... or `-1` for NONE |
| `TribeType` | Tribe enum value | `TRIBE_GAULS`, `TRIBE_SCYTHIANS` |
| `UnitType` | Unit type enum | `UNIT_WARRIOR`, `UNIT_SLINGER` |
| `TechType` | Technology enum | `TECH_SOVEREIGNTY`, `TECH_MINING` |
| `ImprovementType` | Improvement enum | `IMPROVEMENT_FARM`, `IMPROVEMENT_MINE` |
| `ResourceType` | Resource/luxury enum | `RESOURCE_IRON`, `RESOURCE_WINE` |
| `YieldType` | Yield type enum | `YIELD_FOOD`, `YIELD_GOLD`, `YIELD_CIVICS` |
| `int` (ID) | Entity ID | City ID, Character ID, Unit ID, Tile ID |
| `bool` | Boolean flag | `true`, `false` |
| `string` | Text string | Names, targets |

---

## Turn Management

### END_TURN
End the current player's turn.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Current turn number (validation) |
| 1 | `bool` | Force end turn |

```
ActionData(END_TURN, playerIndex)
  .addValue(currentTurn)    // int
  .addValue(force)          // bool
```

### EXTEND_TIME
Extend turn timer (multiplayer).

No parameters required.

---

## Technology & Research

### RESEARCH_TECH
Set active research target.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `TechType` | Technology to research |

```
ActionData(RESEARCH_TECH, playerIndex)
  .addValue(techType)       // TechType enum
```

### REDRAW_TECH
Redraw available tech cards (uses Civics).

No parameters required.

### TARGET_TECH
Set a future tech target (auto-path research).

| Index | Type | Description |
|-------|------|-------------|
| 0 | `TechType` | Target technology |

---

## Laws & Government

### SELECT_LAW
Adopt a new law.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `LawType` | Law to adopt |

```
ActionData(SELECT_LAW, playerIndex)
  .addValue(lawType)        // LawType enum
```

### CANCEL_LAW
Revoke an active law.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `LawType` | Law to cancel |

---

## Economy & Resources

### GIFT_YIELD
Gift resources to another player.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `YieldType` | Yield type to gift |
| 1 | `PlayerType` | Recipient player |
| 2 | `bool` | Reverse (receive instead of give) |

### BUY_YIELD
Purchase resources with gold.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `YieldType` | Yield type to buy |
| 1 | `int` | Quantity |

### SELL_YIELD
Sell resources for gold.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `YieldType` | Yield type to sell |
| 1 | `int` | Quantity |

### CONVERT_ORDERS
Convert orders to civics.

No parameters required.

### CONVERT_LEGITIMACY
Convert legitimacy to orders.

No parameters required.

### CONVERT_ORDERS_TO_SCIENCE
Convert orders to science.

No parameters required.

---

## Luxury Trading

### TRADE_CITY_LUXURY
Toggle luxury assignment to a city.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | City ID |
| 1 | `ResourceType` | Luxury resource |
| 2 | `bool` | Enable (true) or disable (false) |

### TRADE_FAMILY_LUXURY
Toggle luxury assignment to a family.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `FamilyType` | Family type |
| 1 | `ResourceType` | Luxury resource |
| 2 | `bool` | Enable/disable |

### TRADE_TRIBE_LUXURY
Toggle luxury gift to a tribe.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `TribeType` | Tribe type |
| 1 | `ResourceType` | Luxury resource |
| 2 | `bool` | Enable/disable |

### TRADE_PLAYER_LUXURY
Toggle luxury trade with another player.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `PlayerType` | Target player |
| 1 | `ResourceType` | Luxury resource |
| 2 | `bool` | Enable/disable |

---

## Diplomacy

### ALLY_TRIBE
Form alliance with a tribe.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `TribeType` | Tribe to ally |
| 1 | `PlayerType` | Player forming alliance |

### ALLY_TEAM
Form alliance with another player/team.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `PlayerType` | First player |
| 1 | `PlayerType` | Second player |

### DIPLOMACY_PEACE
Make peace with another player.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `PlayerType` | First player |
| 1 | `PlayerType` | Second player |

### DIPLOMACY_PEACE_TRIBE
Make peace with a tribe.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `TribeType` | Tribe |
| 1 | `PlayerType` | Player (team derived) |

### DIPLOMACY_TRUCE
Establish truce with another player.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `PlayerType` | First player |
| 1 | `PlayerType` | Second player |

### DIPLOMACY_TRUCE_TRIBE
Establish truce with a tribe.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `TribeType` | Tribe |
| 1 | `PlayerType` | Player |

### DIPLOMACY_HOSTILE
Declare war on another player.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `PlayerType` | First player |
| 1 | `PlayerType` | Second player |

### DIPLOMACY_HOSTILE_TRIBE
Declare war on a tribe.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `TribeType` | Tribe |
| 1 | `PlayerType` | Player |

### TRIBE_INVASION
Trigger tribe invasion against a player.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `TribeType` | Invading tribe |
| 1 | `PlayerType` | Target player |

---

## City Production

### BUILD_UNIT
Queue unit production.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | City ID |
| 1 | `UnitType` | Unit type to build |
| 2 | `bool` | Buy with goods (rush) |
| 3 | `bool` | Add to front of queue |
| 4 | `int` | Tile ID for spawn location (or -1) |

```
ActionData(BUILD_UNIT, playerIndex)
  .addValue(cityId)         // int
  .addValue(unitType)       // UnitType enum
  .addValue(buyGoods)       // bool
  .addValue(addFirst)       // bool
  .addValue(spawnTileId)    // int (-1 for default)
```

### BUILD_PROJECT
Queue project/wonder production.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | City ID |
| 1 | `ProjectType` | Project type |
| 2 | `bool` | Buy with goods |
| 3 | `bool` | Add to front of queue |
| 4 | `bool` | Repeat when complete |

### BUILD_SPECIALIST
Queue specialist training.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Tile ID (for placement) |
| 1 | `SpecialistType` | Specialist type |
| 2 | `bool` | Buy with goods |
| 3 | `bool` | Add to front of queue |

### BUILD_QUEUE
Reorder production queue.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | City ID |
| 1 | `int` | Old queue index |
| 2 | `int` | New queue index |

### HURRY_CIVICS
Rush production with Civics.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | City ID |

### HURRY_TRAINING
Rush production with Training.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | City ID |

### HURRY_MONEY
Rush production with Gold.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | City ID |

### HURRY_POPULATION
Rush production with Population (sacrifice citizen).

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | City ID |

### HURRY_ORDERS
Rush production with Orders.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | City ID |

### CITY_AUTOMATE
Toggle city automation.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | City ID |
| 1 | `bool` | Enable automation |

---

## City Management

### RENAME_CITY
Rename a city.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | City ID |
| 1 | `string` | New name |

### GIFT_CITY
Gift city to another player.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | City ID |
| 1 | `PlayerType` | Recipient player |

### CHANGE_CITIZENS
Modify city population (editor).

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | City ID |
| 1 | `int` | Change amount (+/-) |

### CHANGE_RELIGION
Add/remove religion from city.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | City ID |
| 1 | `ReligionType` | Religion |
| 2 | `bool` | Add (true) or remove (false) |

### SET_CITY_FAMILY
Change city's family assignment.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | City ID |
| 1 | `FamilyType` | New family |

---

## Unit Movement & Combat

### SELECT_UNIT
Select a unit (UI state).

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Unit ID |

### MOVE_UNIT
Move unit to destination.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Unit ID |
| 1 | `int` | From tile ID |
| 2 | `int` | To tile ID |
| 3 | `bool` | Force march |
| 4 | `bool` | Queue movement |
| 5 | `int` | Waypoint tile ID (-1 for none) |

```
ActionData(MOVE_UNIT, playerIndex)
  .addValue(unitId)         // int
  .addValue(fromTileId)     // int
  .addValue(toTileId)       // int
  .addValue(forceMarch)     // bool
  .addValue(queueMove)      // bool
  .addValue(waypointTileId) // int (-1 for none)
```

### ATTACK
Attack a target tile (unit or city).

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Attacking unit ID |
| 1 | `int` | Target tile ID |

```
ActionData(ATTACK, playerIndex)
  .addValue(unitId)         // int
  .addValue(targetTileId)   // int
```

### SWAP
Swap positions with another unit.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Unit ID |
| 1 | `int` | Target tile ID |
| 2 | `bool` | Force march |

### DO_UNIT_QUEUE
Execute queued unit orders.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Unit ID |

### CANCEL_UNIT_QUEUE
Clear unit's order queue.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Unit ID |
| 1 | `bool` | Single (pop one) or clear all |

---

## Unit States & Abilities

### HEAL
Heal/rest unit.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Unit ID |
| 1 | `bool` | Toggle auto-heal |

### FORTIFY
Enter fortified stance.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Unit ID |

### FORMATION
Set unit formation.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Unit ID |
| 1 | `EffectUnitType` | Formation effect |

### MARCH
Toggle forced march mode.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Unit ID |

### UNLIMBER
Toggle unlimbered state (siege).

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Unit ID |

### ANCHOR
Toggle anchored state (naval).

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Unit ID |

### PILLAGE
Pillage improvement on current tile.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Unit ID |

### BURN
Burn improvements (destroy permanently).

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Unit ID |

### WAKE
Wake unit from sleep/sentry.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Unit ID |

### PASS
Toggle pass (skip this turn).

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Unit ID |

### SLEEP
Toggle sleep mode.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Unit ID |

### SENTRY
Toggle sentry mode.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Unit ID |

### LOCK
Toggle unit lock (prevent accidental moves).

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Unit ID |

### UNIT_AUTOMATE
Toggle unit automation.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Unit ID |

### DISBAND
Disband/kill unit.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Unit ID |
| 1 | `bool` | Force kill (bypass checks) |

---

## Unit Advancement

### PROMOTE
Apply promotion to unit.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Unit ID |
| 1 | `PromotionType` | Promotion to apply |

```
ActionData(PROMOTE, playerIndex)
  .addValue(unitId)         // int
  .addValue(promotionType)  // PromotionType enum
```

### UPGRADE
Upgrade unit to new type.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Unit ID |
| 1 | `UnitType` | Target unit type |
| 2 | `bool` | Buy with goods |

### UNIT_NAME
Rename a unit.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Unit ID |
| 1 | `string` | New name |

---

## Settler & City Founding

### FOUND_CITY
Found a new city.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Settler unit ID |
| 1 | `FamilyType` | Family to assign |
| 2 | `NationType` | Nation (for first city) |

```
ActionData(FOUND_CITY, playerIndex)
  .addValue(settlerUnitId)  // int
  .addValue(familyType)     // FamilyType enum
  .addValue(nationtype)     // NationType enum
```

### CHOOSE_STARTING_SETUP
Choose nation/dynasty at game start.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Settler unit ID |
| 1 | `FamilyType` | Starting family |
| 2 | `NationType` | Nation |
| 3 | `CharacterPortraitType` | Leader portrait |
| 4 | `NameType` | Leader name |
| 5 | `GenderType` | Leader gender |
| 6 | `int` | Leader age |
| 7 | `TraitType` | Archetype trait |
| 8 | `TraitType` | Additional trait |
| 9 | `DynastyType` | Dynasty |

### JOIN_CITY
Add settler to city (gain citizen).

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Settler unit ID |

---

## Worker Actions

### BUILD_IMPROVEMENT
Build improvement on tile.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Worker unit ID |
| 1 | `ImprovementType` | Improvement to build |
| 2 | `bool` | Buy with goods |
| 3 | `bool` | Queue action |
| 4 | `int` | Target tile ID |

```
ActionData(BUILD_IMPROVEMENT, playerIndex)
  .addValue(workerUnitId)   // int
  .addValue(improvementType)// ImprovementType enum
  .addValue(buyGoods)       // bool
  .addValue(queueAction)    // bool
  .addValue(targetTileId)   // int
```

### UPGRADE_IMPROVEMENT
Upgrade existing improvement.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Worker unit ID |
| 1 | `bool` | Buy with goods |

### CANCEL_IMPROVEMENT
Cancel improvement in progress.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Worker unit ID |

### ADD_ROAD
Build road on tile.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Worker unit ID |
| 1 | `bool` | Buy with goods |
| 2 | `bool` | Queue action |
| 3 | `int` | Target tile ID |

### ROAD_TO
Build road along path.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Worker unit ID |
| 1 | `bool` | Buy with goods |
| 2 | `int[]` | Array of tile IDs (path) |

### ADD_URBAN
Convert tile to urban.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Worker unit ID |
| 1 | `bool` | Buy with goods |

### REPAIR
Repair pillaged improvement.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Worker unit ID |
| 1 | `bool` | Buy with goods |
| 2 | `bool` | Queue action |
| 3 | `int` | Target tile ID |

### REMOVE_VEGETATION
Clear vegetation (forest/scrub).

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Worker unit ID |

### HARVEST_RESOURCE
Harvest resource for immediate yield.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Unit ID |
| 1 | `bool` | Toggle auto-harvest |

### BUY_TILE
Acquire tile for city.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Unit ID (must be on tile) |
| 1 | `int` | City ID |
| 2 | `YieldType` | Payment yield (gold/civics) |

---

## Religious Units

### SPREAD_RELIGION
Spread religion to city.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Disciple unit ID |
| 1 | `int` | Target city ID |

### PURGE_RELIGION
Remove religion from city.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Disciple unit ID |
| 1 | `ReligionType` | Religion to remove |

### SPREAD_RELIGION_TRIBE
Spread religion to tribe.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Disciple unit ID |
| 1 | `TribeType` | Target tribe |

### ESTABLISH_THEOLOGY
Establish a theology doctrine.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Disciple unit ID |
| 1 | `TheologyType` | Theology to establish |

---

## Special Units

### RECRUIT_MERCENARY
Recruit a mercenary unit.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Mercenary unit ID |

### HIRE_MERCENARY
Hire mercenary permanently.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Mercenary unit ID |

### GIFT_UNIT
Gift unit to another player.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Unit ID |
| 1 | `PlayerType` | Recipient player |

### LAUNCH_OFFENSIVE
Launch military offensive (general ability).

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Unit ID |

### APPLY_EFFECTUNIT
Apply unit effect (ability).

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Unit ID |
| 1 | `EffectUnitType` | Effect to apply |

### CREATE_AGENT_NETWORK
Create spy network in city (agent).

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Agent unit ID |
| 1 | `int` | Target city ID |

### CREATE_TRADE_OUTPOST
Create trade outpost.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Caravan unit ID |
| 1 | `int` | Target tile ID |

### CARAVAN_MISSION_START
Start caravan trade mission.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Caravan unit ID |
| 1 | `PlayerType` | Target player |

### CARAVAN_MISSION_CANCEL
Cancel caravan mission.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Caravan unit ID |

---

## Character Assignment

### MAKE_UNIT_CHARACTER
Assign character to unit (general/explorer).

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Unit ID |
| 1 | `int` | Character ID |
| 2 | `bool` | As general (true) or explorer (false) |

```
ActionData(MAKE_UNIT_CHARACTER, playerIndex)
  .addValue(unitId)         // int
  .addValue(characterId)    // int
  .addValue(asGeneral)      // bool (true=general, false=explorer)
```

### RELEASE_UNIT_CHARACTER
Remove character from unit.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Unit ID |

### MAKE_GOVERNOR
Assign governor to city.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | City ID |
| 1 | `int` | Character ID |

### RELEASE_GOVERNOR
Remove governor from city.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | City ID |

### MAKE_AGENT
Assign agent to city.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | City ID |
| 1 | `int` | Character ID |

### RELEASE_AGENT
Remove agent from city.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | City ID |

### CHARACTER_COUNCIL
Assign character to council position.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Character ID |
| 1 | `CouncilType` | Council position |

### CHARACTER_COURTIER
Set character's courtier role.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Character ID |
| 1 | `CourtierType` | Courtier type |

---

## Missions (Agent Actions)

### START_MISSION
Start or cancel an agent mission.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `MissionType` | Mission type |
| 1 | `int` | Agent character ID |
| 2 | `string` | Target identifier (varies by mission) |
| 3 | `bool` | Cancel existing mission |

```
ActionData(START_MISSION, playerIndex)
  .addValue(missionType)    // MissionType enum
  .addValue(characterId)    // int
  .addValue(targetString)   // string (target encoding varies)
  .addValue(cancelMission)  // bool
```

---

## Decisions & Events

### MAKE_DECISION
Make an event/decision choice.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Decision ID |
| 1 | `int` | Choice index |
| 2 | `int` | Additional data (context-dependent) |

```
ActionData(MAKE_DECISION, playerIndex)
  .addValue(decisionId)     // int
  .addValue(choiceIndex)    // int (0-based option index)
  .addValue(extraData)      // int (often 0)
```

### REMOVE_DECISION
Dismiss a decision without choosing.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Decision ID |

### ABANDON_AMBITION
Abandon an active ambition/goal.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Goal ID |

---

## Character Management

### CHARACTER_NAME
Rename a character.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Character ID |
| 1 | `string` | New name |

### CHARACTER_TRAIT
Add or remove character trait.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Character ID |
| 1 | `TraitType` | Trait |
| 2 | `bool` | Remove (true) or add (false) |

### CHARACTER_RATING
Set character rating value.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Character ID |
| 1 | `RatingType` | Rating type (Courage, Discipline, etc.) |
| 2 | `int` | New value |

### CHARACTER_XP
Set character experience points.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Character ID |
| 1 | `int` | XP value |

### CHARACTER_COGNOMEN
Set character cognomen (epithet).

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Character ID |
| 1 | `CognomenType` | Cognomen |

### CHARACTER_RELIGION
Set character's religion.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `ReligionType` | Religion |
| 1 | `int` | Character ID |

Note: Parameter order is reversed from typical pattern.

### CHARACTER_FAMILY
Change character's family.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Character ID |
| 1 | `FamilyType` | New family |

### CHARACTER_NATION
Set character's nation.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Character ID |
| 1 | `NationType` | Nation |

### CHARACTER_RELIGIONHEAD
Set religion head.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `ReligionType` | Religion |
| 1 | `int` | Character ID |

### CHARACTER_KILL
Kill a character (editor/debug).

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Character ID |

### CHARACTER_SAFE
Make character safe from death.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Character ID |
| 1 | `int` | Number of turns |

### CHARACTER_ADD
Add predefined character (editor).

| Index | Type | Description |
|-------|------|-------------|
| 0 | `CharacterType` | Character preset |
| 1 | `PlayerType` | Owner player |
| 2 | `FamilyType` | Family |

### CHARACTER_CREATE
Create new character (editor).

| Index | Type | Description |
|-------|------|-------------|
| 0 | `PlayerType` | Owner player |
| 1 | `FamilyType` | Family |
| 2 | `int` | Age (-1 for random) |
| 3 | `int` | Fill value (stat boost) |

---

## Communication

### PING
Place map ping.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `PingType` | Ping type |
| 1 | `int` | Tile ID |
| 2 | `string` | Message |
| 3 | `int` | Reminder turn (-1 for none) |

### CHAT
Send chat message.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `ChatType` | Chat scope (all/team/player) |
| 1 | `string` | Message |
| 2 | `PlayerType` | Target player (for private) |

---

## Editor/Debug Actions

These actions are typically used in scenario editor or debug mode:

### SET_IMPROVEMENT
Place improvement on tile.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Tile ID |
| 1 | `ImprovementType` | Improvement |
| 2 | `bool` | Complete immediately |
| 3 | `PlayerType` | Owner |
| 4 | `TribeType` | Owner tribe |

### SET_SPECIALIST
Set tile specialist.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Tile ID |
| 1 | `SpecialistType` | Specialist (NONE to remove) |

### CREATE_UNIT
Spawn unit on map.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Tile ID |
| 1 | `UnitType` | Unit type |
| 2 | `PlayerType` | Owner |
| 3 | `TribeType` | Tribe owner |

### CREATE_CITY
Create city on tile.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Tile ID |
| 1 | `PlayerType` | Owner |
| 2 | `TribeType` | Tribe owner |
| 3 | `FamilyType` | Family |

### REMOVE_CITY
Remove city from map.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | City ID |

### CITY_OWNER
Change city ownership.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | City ID |
| 1 | `PlayerType` | New owner |

### TERRAIN
Change tile terrain.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Tile ID |
| 1 | `TerrainType` | Terrain |

### TERRAIN_HEIGHT
Change tile height.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Tile ID |
| 1 | `HeightType` | Height |

### RESOURCE
Set tile resource.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Tile ID |
| 1 | `ResourceType` | Resource (NONE to remove) |

### ROAD
Toggle road on tile.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Tile ID |
| 1 | `bool` | Has road |

### TILE_OWNER
Set tile ownership.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Tile ID |
| 1 | `PlayerType` | Owner |

### ADD_TECH
Grant technology to player.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `TechType` | Technology |
| 1 | `bool` | Show popup |

### ADD_YIELD
Add yield to player stockpile.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `YieldType` | Yield type |
| 1 | `int` | Amount |

### ADD_MONEY
Add gold to player.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Amount |

### SET_LEADER
Change player's leader.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `int` | Character ID |

### SET_FAMILYHEAD
Set family head.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `FamilyType` | Family |
| 1 | `int` | Character ID |

### EVENT_STORY
Trigger specific event.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `EventStoryType` | Event to trigger |

### VICTORY_TEAM
Declare victory for team.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `VictoryType` | Victory type |
| 1 | `TeamType` | Winning team |

### CHEAT
Execute cheat/debug command.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `HotkeyType` | Cheat hotkey type |

Available cheats:
- `HOTKEY_UNLOCK_NEXT_TECH` - Complete current research
- `HOTKEY_UNLOCK_ALL_TECHS` - Unlock all technologies
- `HOTKEY_GRANT_RESOURCES` - Add 1000 of each resource + 10000 gold
- `HOTKEY_FULLY_REVEAL_MAP` - Reveal entire map
- `HOTKEY_TOGGLE_AUTOPLAY` - AI plays one turn

---

## Save/Load Actions

### SAVE
Save game.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `string` | Save filename |
| 1 | `bool` | Auto-save |
| 2 | `bool` | Cloud save |

### LOAD
Load game.

| Index | Type | Description |
|-------|------|-------------|
| 0 | `string` | Save filename |
| 1 | `bool` | Cloud load |

### UNDO
Undo last action.

No parameters.

### REDO
Redo undone action.

No parameters.

---

## Notes for Implementation

### Validation
The game performs extensive validation before executing actions. Common checks:
- Player ownership verification
- Resource/cost requirements
- Prerequisite conditions (tech, improvement, etc.)
- Unit state requirements (not sleeping, has actions, etc.)

### Undo System
Most actions call `addUndoMark()` before execution, enabling undo functionality. The mod API should consider whether to support undo or execute actions as final.

### Error Handling
Actions that fail validation are silently ignored by the game. The mod API should ideally return validation results before attempting actions.

### Enum String Mapping
All enum types (TechType, UnitType, etc.) map to string identifiers in the XML data files. For example:
- `TECH_SOVEREIGNTY` maps to technology definition
- `UNIT_WARRIOR` maps to unit definition
- `IMPROVEMENT_FARM` maps to improvement definition

The `Infos` class provides methods like `getType<T>(string)` to convert between string IDs and enum values.

# Old World API - Missing Endpoints Reference

This document details game data not currently exposed by the API, with exact field specifications from the game's source code.

---

## Units

**Current State:** Only unit events (creation/destruction) are exposed. No unit data query endpoint exists.

### Suggested Endpoints
- `GET /units` - All units
- `GET /unit/{id}` - Single unit by ID
- `GET /player/{index}/units` - Units owned by player

### Unit Data Model

Source: `Unit.cs` NetworkData class

#### Identity & Type
| Field | Type | Description |
|-------|------|-------------|
| `id` | `int` | Unique unit identifier |
| `name` | `string` | Custom or default name |
| `unitType` | `UnitType` | Unit type enum (UNIT_WARRIOR, etc.) |
| `gender` | `GenderType` | Unit gender |

#### Ownership
| Field | Type | Description |
|-------|------|-------------|
| `ownerPlayer` | `PlayerType` | Owning player (-1 if tribe) |
| `ownerTribe` | `TribeType` | Owning tribe (-1 if player) |
| `originalPlayer` | `PlayerType` | Original owner (before capture) |
| `originalTribe` | `TribeType` | Original tribe |
| `rebelPlayer` | `PlayerType` | Player if unit rebels |
| `family` | `FamilyType[]` | Family per player (for family units) |

#### Position
| Field | Type | Description |
|-------|------|-------------|
| `tileId` | `int` | Current tile ID |
| `facing` | `DirectionType` | Facing direction (NW, NE, E, SE, SW, W) |

#### Characters
| Field | Type | Description |
|-------|------|-------------|
| `generalId` | `int` | Attached general character ID (-1 if none) |
| `explorerId` | `int` | Attached explorer character ID (-1 if none) |

#### Health & Combat
| Field | Type | Description |
|-------|------|-------------|
| `damage` | `int` | Current damage taken |
| `hp` | `int` | Current HP (derived: maxHP - damage) |
| `hpMax` | `int` | Maximum HP |
| `nextCriticalModifier` | `int` | Critical hit modifier |
| `routChain` | `int` | Rout chain counter |

#### Experience
| Field | Type | Description |
|-------|------|-------------|
| `xp` | `int` | Experience points |
| `level` | `int` | Current level |
| `levelPromotion` | `int` | Level at which last promoted |

#### Movement
| Field | Type | Description |
|-------|------|-------------|
| `turnSteps` | `int` | Movement used this turn |
| `freeActionsTaken` | `int` | Free actions used |
| `turnsSinceLastMove` | `int` | Turns idle |
| `roadMovementModifier` | `int` | Road movement bonus |
| `zocCount` | `int` | Zone of control modifiers |
| `ignoreZocCount` | `int` | ZOC ignore modifiers |
| `familyTerritoryOnlyCount` | `int` | Family territory restrictions |

#### Status Timers
| Field | Type | Description |
|-------|------|-------------|
| `cooldownTurns` | `int` | Cooldown remaining |
| `cooldownType` | `CooldownType` | Type of cooldown |
| `fortifyTurns` | `int` | Fortification buildup |
| `formationTurns` | `int` | Formation buildup |
| `tempHiddenTurns` | `int` | Hidden state turns |
| `createTurn` | `int` | Turn unit was created |

#### Status Flags
| Field | Type | Description |
|-------|------|-------------|
| `isAlive` | `bool` | Unit exists |
| `isPass` | `bool` | Passed this turn |
| `isSleep` | `bool` | Sleeping |
| `isSentry` | `bool` | On sentry duty |
| `isMarch` | `bool` | Force marching |
| `isUnlimbered` | `bool` | Siege unlimbered |
| `isAnchored` | `bool` | Naval anchored |
| `isAutoHarvest` | `bool` | Auto-harvest enabled |
| `isAutoHeal` | `bool` | Auto-heal enabled |
| `isLocked` | `bool` | Movement locked |
| `isTempHidden` | `bool` | Temporarily hidden |
| `isAutomated` | `bool` | Automated control |
| `wasCriticalHit` | `bool` | Last attack was critical |

#### Formation & Religion
| Field | Type | Description |
|-------|------|-------------|
| `currentFormation` | `EffectUnitType` | Active formation |
| `religion` | `ReligionType` | Unit religion (disciples) |
| `freeImprovementBuild` | `ImprovementType` | Free improvement to build |

#### Promotions
| Field | Type | Description |
|-------|------|-------------|
| `promotions` | `PromotionType[]` | Applied promotions |
| `promotionsAvailable` | `PromotionType[]` | Available promotion choices |

#### Effects
| Field | Type | Description |
|-------|------|-------------|
| `effectUnits` | `{EffectUnitType: count}` | Active unit effects |
| `adjacentEffectUnits` | `{EffectUnitType: count}` | Effects from adjacent units |
| `bonusEffectUnits` | `{EffectUnitType: count}` | Bonus effects |
| `effectUnitExpireTurns` | `{EffectUnitType: turn}[]` | Expiring effects |
| `unitTraitZocs` | `{UnitTraitType: count}` | ZOC from traits |

#### Queue
| Field | Type | Description |
|-------|------|-------------|
| `queueList` | `UnitQueueData[]` | Pending orders |

`UnitQueueData` structure:
```
{
  type: UnitQueueType,  // MOVETO, ADD_ROAD, REPAIR_IMPROVEMENT, BUILD_IMPROVEMENT
  tileId: int,
  data: int  // ImprovementType for BUILD_IMPROVEMENT
}
```

#### Caravan
| Field | Type | Description |
|-------|------|-------------|
| `caravanMissionTarget` | `PlayerType` | Active caravan destination |

#### Tracking
| Field | Type | Description |
|-------|------|-------------|
| `eventStoryTurn` | `{EventStoryType: turn}` | Last event turns |
| `raidTurn` | `{TeamType: turn}` | Raid tracking |
| `modVariables` | `{string: string}` | Mod-defined variables |

---

## Tiles / Map

**Current State:** No tile data exposed.

### Suggested Endpoints
- `GET /tiles` - All tiles (consider pagination for large maps)
- `GET /tile/{id}` - Single tile by ID
- `GET /tile/{x}/{y}` - Single tile by coordinates
- `GET /map` - Map metadata (dimensions, settings)

### Tile Data Model

Source: `Tile.cs` NetworkData class

#### Identity & Position
| Field | Type | Description |
|-------|------|-------------|
| `id` | `int` | Unique tile identifier |
| `x` | `int` | X coordinate |
| `y` | `int` | Y coordinate |
| `area` | `int` | Contiguous area ID |
| `landSection` | `int` | Land section (island) ID |

#### Geography
| Field | Type | Description |
|-------|------|-------------|
| `terrain` | `TerrainType` | Terrain type |
| `terrainStamp` | `TerrainStampType` | Terrain stamp overlay |
| `height` | `HeightType` | Elevation |
| `vegetation` | `VegetationType` | Vegetation type |
| `resource` | `ResourceType` | Resource (-1 if none) |
| `harvestTurn` | `int` | Turn resource was harvested (-1 if not) |
| `regrowthTurn` | `int` | Turn vegetation regrows |

#### Rivers
| Field | Type | Description |
|-------|------|-------------|
| `riverW` | `RiverType` | River on west edge |
| `riverSW` | `RiverType` | River on southwest edge |
| `riverSE` | `RiverType` | River on southeast edge |

#### Infrastructure
| Field | Type | Description |
|-------|------|-------------|
| `hasRoad` | `bool` | Has road |
| `isPillaged` | `bool` | Improvement is pillaged |

#### Improvement
| Field | Type | Description |
|-------|------|-------------|
| `improvement` | `ImprovementType` | Built improvement (-1 if none) |
| `improvementBuildTurnsOriginal` | `int` | Original build time |
| `improvementBuildTurnsLeft` | `int` | Remaining build turns |
| `improvementDevelopTurns` | `int` | Development progress |
| `improvementUnitTurns` | `int` | Unit production turns |
| `improvementPillageTurns` | `int` | Turns until unpillaged |
| `specialist` | `SpecialistType` | Assigned specialist |

#### Ownership
| Field | Type | Description |
|-------|------|-------------|
| `owner` | `PlayerType` | Owning player |
| `ownerTribe` | `TribeType` | Owning tribe |
| `origUrbanOwner` | `PlayerType` | Original urban tile owner |
| `cityId` | `int` | City on this tile (-1 if none) |
| `cityTerritory` | `int` | City territory ID |

#### Sites
| Field | Type | Description |
|-------|------|-------------|
| `citySite` | `CitySiteType` | City founding site status |
| `tribeSite` | `TribeType` | Tribe site type |
| `nationSite` | `NationType` | Nation site type |
| `isBoundary` | `bool` | Is boundary tile |

#### Visibility (per team)
| Field | Type | Description |
|-------|------|-------------|
| `visibleCount` | `int[]` | Visibility count per team |
| `visibleTime` | `int[]` | Last visible turn per team |
| `isRevealed` | `bool[]` | Revealed per team |
| `isAlwaysVisible` | `bool[]` | Always visible per team |
| `wasVisibleThisTurn` | `bool[]` | Seen this turn per team |

#### Revealed State (per team - fog of war memory)
| Field | Type | Description |
|-------|------|-------------|
| `revealedTerrain` | `TerrainType[]` | Last seen terrain |
| `revealedHeight` | `HeightType[]` | Last seen height |
| `revealedVegetation` | `VegetationType[]` | Last seen vegetation |
| `revealedImprovement` | `ImprovementType[]` | Last seen improvement |
| `revealedOwner` | `PlayerType[]` | Last seen owner |
| `revealedCitySite` | `CitySiteType[]` | Last seen city site |

#### Trade Networks (per team)
| Field | Type | Description |
|-------|------|-------------|
| `tradeNetworkW` | `int[]` | Trade network ID (west edge) |
| `tradeNetworkSW` | `int[]` | Trade network ID (southwest) |
| `tradeNetworkSE` | `int[]` | Trade network ID (southeast) |

#### Contents
| Field | Type | Description |
|-------|------|-------------|
| `units` | `int[]` | Unit IDs on this tile |
| `occurrences` | `OccurrenceType[]` | Active occurrences |

#### Religion
| Field | Type | Description |
|-------|------|-------------|
| `religions` | `bool[]` | Religion presence per type |

#### Naming
| Field | Type | Description |
|-------|------|-------------|
| `elementName` | `MapElementNamesType` | Named landmark type |
| `customElementName` | `string` | Custom landmark name |

#### Modifiers
| Field | Type | Description |
|-------|------|-------------|
| `movementCostExtra` | `int` | Extra movement cost |
| `impassableCount` | `int` | Impassable modifiers |
| `baseYieldModifier` | `int` | Base yield modifier |
| `extraBlockingVisibilityHeight` | `int` | Visibility blocking |

#### History
| Field | Type | Description |
|-------|------|-------------|
| `ownerHistory` | `{turn: PlayerType}` | Ownership history |
| `terrainHistory` | `{turn: TerrainType}` | Terrain changes |
| `heightHistory` | `{turn: HeightType}` | Height changes |
| `vegetationHistory` | `{turn: VegetationType}` | Vegetation changes |

---

## Technologies

**Current State:** Not exposed.

### Suggested Endpoints
- `GET /player/{index}/techs` - Player's tech state
- `GET /techs` - All tech definitions (static)

### Player Tech Data

Source: `Player.cs` NetworkData class

| Field | Type | Description |
|-------|------|-------------|
| `techResearching` | `TechType` | Currently researching |
| `techProgress` | `int[]` | Progress per tech |
| `techCount` | `int[]` | Number of times researched |
| `techAvailable` | `bool[]` | In current tech deck |
| `techPassed` | `bool[]` | Passed/skipped |
| `techTrashed` | `bool[]` | Discarded |
| `techLocked` | `bool[]` | Locked from selection |
| `techTarget` | `bool[]` | Targeted for auto-research |
| `popupTechDiscovered` | `TechType` | Pending discovery popup |
| `techRedraw` | `bool` | Redraw pending |
| `techsAvailableChange` | `int` | Deck size modifier |
| `techCostModifier` | `int` | Research cost modifier |

---

## Laws

**Current State:** Not exposed.

### Suggested Endpoint
- `GET /player/{index}/laws`

### Player Law Data

| Field | Type | Description |
|-------|------|-------------|
| `activeLaws` | `LawType[]` | Active law per category |
| `lawClassChangeCount` | `int[]` | Category changes used |
| `startLawModifier` | `int` | Law adoption cost modifier |
| `switchLawModifier` | `int` | Law switch cost modifier |

---

## Religion

**Current State:** City religion counts exposed. Player/global religion state not exposed.

### Suggested Endpoints
- `GET /religions` - All religion state
- `GET /player/{index}/religion`

### Player Religion Data

| Field | Type | Description |
|-------|------|-------------|
| `stateReligion` | `ReligionType` | State religion |
| `religionCount` | `int[]` | Religion presence count |
| `religionOpinionRate` | `int[]` | Religion opinion per type |
| `familyReligion` | `ReligionType[]` | Family religion per family |
| `theologyEstablishedCount` | `int[]` | Theologies per type |
| `stateReligionSpreadChange` | `int` | Spread modifier |
| `worldReligionSpreadChange` | `int` | World religion spread |
| `stateReligionChangeCount` | `int` | Religion changes made |

### Global Religion Data (Game class)

| Field | Type | Description |
|-------|------|-------------|
| `religionHeadId` | `int[]` | Religion head character per religion |
| `holyCityId` | `int[]` | Holy city per religion |
| `religionFounded` | `bool[]` | Religion founded status |

---

## Families

**Current State:** Not exposed.

### Suggested Endpoint
- `GET /player/{index}/families`

### Family Data

| Field | Type | Description |
|-------|------|-------------|
| `familySeatCityId` | `int[]` | Family seat city per family |
| `familyHeadId` | `int[]` | Family head character per family |
| `familyOpinionRate` | `int[]` | Opinion per family |
| `familyTurnsNoLeader` | `int[]` | Turns without head |
| `familyLawOpinion` | `int[][]` | Law opinion per family per law |
| `familyLuxuryTurn` | `int[][]` | Last luxury gift turn |
| `familySet` | `FamilyType[]` | Active families |
| `familySupremacy` | `FamilyType` | Dominant family |

---

## Goals & Ambitions

**Current State:** Not exposed.

### Suggested Endpoint
- `GET /player/{index}/goals`

### Goal Data

| Field | Type | Description |
|-------|------|-------------|
| `goalDataList` | `GoalData[]` | Active goals |
| `goalStartedCount` | `int[]` | Goals started per type |
| `ambitionDelay` | `int` | Turns until next ambition |

`GoalData` structure:
```
{
  goalType: GoalType,
  startTurn: int,
  progress: int,
  target: int,
  isAmbition: bool,
  isLegacy: bool
}
```

---

## Missions

**Current State:** Not exposed.

### Suggested Endpoint
- `GET /player/{index}/missions`

### Mission Data

| Field | Type | Description |
|-------|------|-------------|
| `missionList` | `MissionData[]` | Active missions |
| `missionStartedTurn` | `int[]` | Last started turn per type |
| `missionModifier` | `int` | Mission cost modifier |
| `missionYieldCostModifier` | `int[]` | Cost modifier per yield |

`MissionData` structure:
```
{
  missionType: MissionType,
  characterId: int,
  targetString: string,
  turnsRemaining: int,
  startTurn: int
}
```

---

## Decisions

**Current State:** Not exposed.

### Suggested Endpoint
- `GET /player/{index}/decisions`

### Decision Data

| Field | Type | Description |
|-------|------|-------------|
| `decisionList` | `DecisionData[]` | Pending decisions |

`DecisionData` structure:
```
{
  decisionId: int,
  decisionType: DecisionType,  // EVENT_STORY, CHOOSE_RESEARCH, CHOOSE_AMBITION, etc.
  eventStoryType: EventStoryType,  // For event decisions
  options: int[],  // Available choice indices
  subjectIds: int[],  // Related entity IDs
  data: varies  // Type-specific data
}
```

---

## Wonders

**Current State:** Exposed via city improvements. No dedicated endpoint.

### Suggested Endpoint
- `GET /wonders`

Wonder state can be derived from:
- City project completion
- Player improvement counts
- Specific project types marked as wonders

Key wonder data:
| Field | Type | Description |
|-------|------|-------------|
| `projectType` | `ProjectType` | Wonder project type |
| `cityId` | `int` | City that built it |
| `playerId` | `PlayerType` | Owner |
| `completeTurn` | `int` | Completion turn |

---

## Resources

**Current State:** City luxury access partially exposed.

### Suggested Endpoints
- `GET /resources` - Resource locations on map
- `GET /player/{index}/resources`

### Player Resource Data

| Field | Type | Description |
|-------|------|-------------|
| `luxuryCount` | `int[]` | Luxury access count per type |
| `resourceRevealed` | `int[]` | Revealed resources per type |
| `extraLuxuryCount` | `{ResourceType: int}` | Bonus luxury sources |

---

## Game Configuration

**Current State:** Minimal game metadata in `/state`.

### Suggested Endpoint
- `GET /config` or enhance `/state`

### Game Configuration Data

From `GameParameters` and `Game` class:

| Field | Type | Description |
|-------|------|-------------|
| `mapWidth` | `int` | Map width |
| `mapHeight` | `int` | Map height |
| `numPlayers` | `int` | Player count |
| `numTeams` | `int` | Team count |
| `mapClass` | `MapClassType` | Map type |
| `mapSize` | `MapSizeType` | Map size preset |
| `difficulty` | `DifficultyType` | Game difficulty |
| `difficultyMode` | `DifficultyModeType` | Difficulty mode |
| `turnScale` | `TurnScaleType` | Turn length |
| `turnStyle` | `TurnStyleType` | Turn style |
| `turnTimer` | `TurnTimerType` | Timer settings |
| `victoryTypes` | `VictoryType[]` | Enabled victories |
| `gameOptions` | `GameOptionType[]` | Enabled options |

---

## Improvements (Map-wide)

**Current State:** Only via city data.

### Suggested Endpoint
- `GET /improvements` - All improvements on map

### Improvement Query Data

| Field | Type | Description |
|-------|------|-------------|
| `tileId` | `int` | Tile location |
| `improvementType` | `ImprovementType` | Type |
| `owner` | `PlayerType` | Owner |
| `ownerTribe` | `TribeType` | Tribe owner |
| `isPillaged` | `bool` | Pillaged state |
| `buildTurnsLeft` | `int` | Construction remaining |
| `specialist` | `SpecialistType` | Assigned specialist |

---

## Historical Data

**Current State:** Planned for Slice 10.

### Player Historical Data

| Field | Type | Description |
|-------|------|-------------|
| `militaryPowerHistory` | `{turn: int}` | Military power per turn |
| `pointsHistory` | `{turn: int}` | Victory points per turn |
| `legitimacyHistory` | `{turn: int}` | Legitimacy per turn |
| `yieldRateHistory` | `{turn: {YieldType: int}}` | Yield rates per turn |
| `yieldTotalHistory` | `{turn: {YieldType: int}}` | Yield totals per turn |
| `familyOpinionHistory` | `{turn: {FamilyType: int}}` | Family opinions per turn |
| `religionOpinionHistory` | `{turn: {ReligionType: int}}` | Religion opinions per turn |

---

## Static Info Data

**Current State:** Not exposed.

### Suggested Endpoint
- `GET /infos/{type}` - Static game data definitions

The `Infos` class loads all XML definitions. Useful for clients to understand:

| Info Type | Description |
|-----------|-------------|
| `tech` | Technology definitions, prerequisites, costs |
| `unit` | Unit types, stats, abilities |
| `improvement` | Improvement definitions, yields |
| `resource` | Resource types, effects |
| `trait` | Character trait definitions |
| `law` | Law definitions, effects |
| `mission` | Mission types, requirements |
| `nation` | Nation bonuses, unique units |
| `family` | Family definitions |
| `familyClass` | Family class types |
| `religion` | Religion definitions |
| `theology` | Theology choices |
| `promotion` | Unit promotion definitions |
| `project` | Project/wonder definitions |
| `specialist` | Specialist definitions |
| `bonus` | Bonus effect definitions |
| `effectCity` | City effect definitions |
| `effectPlayer` | Player effect definitions |
| `effectUnit` | Unit effect definitions |
| `terrain` | Terrain type definitions |
| `vegetation` | Vegetation definitions |
| `yield` | Yield type definitions |

---

## Enum Reference

Key enums used throughout the API (string values in XML):

### UnitQueueType
```
MOVETO
ADD_ROAD
REPAIR_IMPROVEMENT
BUILD_IMPROVEMENT
```

### DecisionType
```
CHOOSE_NAME
EVENT_STORY
CHOOSE_RESEARCH
CHOOSE_ITEM
CHOOSE_AMBITION
CITY_FAMILY
OFFER_DIPLOMACY
UPGRADE_CHARACTER
CHOOSE_ARCHETYPE
PLACE_BONUS
```

### DirectionType
```
NW, NE, E, SE, SW, W
```

### CitySiteType
```
ACTIVE_START
ACTIVE_RESERVED
ACTIVE
USED
TEMP
```

### VisibilityType
```
HIDDEN
REVEALED
VISIBLE
```

---

## Implementation Notes

### Data Size Considerations
- **Tiles**: Large maps can have 10,000+ tiles. Consider:
  - Pagination (`/tiles?offset=0&limit=100`)
  - Viewport queries (`/tiles?x1=0&y1=0&x2=50&y2=50`)
  - Delta updates (only changed tiles since turn X)

- **Units**: Typically 100-500 units. Full list is manageable.

- **Historical Data**: Can grow large. Consider:
  - Limiting history depth
  - Sampling (every N turns)
  - On-demand queries

### Visibility/Fog of War
Many tile properties have "revealed" variants showing what each team last observed. The API should:
- Default to actual values for single-player/debug
- Support team-specific views for multiplayer fairness
- Consider a `?team=X` parameter for fog-of-war-correct responses

### Enum String Mapping
All enum values map to string identifiers in XML. For API responses:
- Option A: Return enum integer values (compact, requires client lookup)
- Option B: Return string identifiers (readable, larger payload)
- Option C: Return both (flexible, larger payload)

Recommendation: Return string identifiers for readability, with optional `?compact=true` for integers.

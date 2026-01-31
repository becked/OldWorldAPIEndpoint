# Changelog


## [3.3.0] - 2025-01-31

### Added
- **Field filtering for tile endpoints** - Request only specific fields to reduce payload size:
  - `GET /tiles?fields=x,y,terrain` - returns only requested fields per tile
  - `GET /tile/{id}?fields=id,terrain,height` - single tile with specific fields
  - `GET /tile/{x}/{y}?fields=x,y,resource` - tile by coords with specific fields
  - Field names are case-insensitive
  - Invalid field names return HTTP 400 with error message
  - Omitting `fields` parameter returns all fields (existing behavior)
- **Auto-generated field name registry** - `TileFieldNames` HashSet with all 119 valid tile field names, kept in sync with code generator


## [3.2.0] - 2025-01-31

### Changed
- **Tile payload dramatically reduced** - Tiles now omit empty/default values, reducing per-tile size by ~70%:
  - Removed `isImprovementBorderSpreads` and `isSpecialistCostCitizens` dictionaries (not useful for API consumers)
  - Filtered `"NONE"` enum string values (e.g., `owner`, `team`, `citySite`)
  - Filtered `null` values (e.g., `improvement`, `resource`, `vegetation`)
  - Filtered empty strings (e.g., `customMapElementName`, `mapElementName`)
  - Filtered `0` and `-1` integer values (except `id`, `x`, `y`, `index`)
  - Filtered `0.0` float/double values

- **All entities now filter noise values**:
  - String fields omit `null` and empty strings
  - Enum fields omit `null` and `"NONE"` values
  - Other fields omit `null` values


## [3.1.0] - 2025-01-31

### Changed
- **Zero values filtered from enum-indexed dictionaries** - Dictionary fields now omit entries with zero values, reducing JSON payload size by ~55% (4MB -> 1.8MB per turn). Zero values are semantically equivalent to "not present" for counts, modifiers, and rates.

  Filtered fields by entity:
  - **Player** (27 fields): counts (improvement, religion, unit, tech progress), family rates/controls, yield totals/upkeep, mission cooldowns, and more
  - **Character** (11 fields): job/council opinions, nation/tribe ethnicity, yield rates (courtier, leader, spouse, successor), cognomen thresholds
  - **City** (16 fields): unit/improvement costs and modifiers, yield progress/overflow, production counts, governor costs
  - **Unit** (2 fields): effectUnitCounts, harvestYieldModifiers
  - **Tile** (1 field): improvementCosts

- **Boolean fields already filtered** - Fields returning `false` are omitted (implemented in 3.0.3)
- **Sentinel values already filtered** - Integer fields returning `-1` are omitted (implemented in 3.0.3)


## [3.0.3] - 2025-01-30

### Fixed
- **Blocked large enum types from expansion** - EventStoryType (5000+ values), BonusType, MemoryType, etc. no longer generate massive dictionaries. Reduces JSON output by ~91% (77MB → 6.9MB per turn).

### Added
- **Character `ratings` object** - All four core stats now exposed automatically:
  - `RATING_COURAGE`, `RATING_WISDOM`, `RATING_CHARISMA`, `RATING_DISCIPLINE`
  - Example: `"ratings": {"RATING_COURAGE": 7, "RATING_WISDOM": 5, ...}`
- **Character `traits` array** - Full list of character traits:
  - Example: `"traits": ["TRAIT_COMMANDER_ARCHETYPE", "TRAIT_PIOUS"]`
- **Enum-indexed properties across all entities** - Automatic dictionary generation for getters like `getRating(RatingType)`, `getYieldStockpile(YieldType)`, `getTechProgress(TechType)`
- **Collection properties** - Automatic array generation for getters returning `ReadOnlyList<EnumType>`

### Changed
- **Expanded entity field counts** via new code generator patterns:
  | Entity | Simple | Enum-Indexed | Collections | Total |
  |--------|--------|--------------|-------------|-------|
  | Character | 180 | 23 | 5 | **208** |
  | Player | 122 | 88 | 1 | **211** |
  | City | 123 | 48 | 1 | **172** |
  | Unit | 151 | 16 | 1 | **168** |
  | Tile | 115 | 6 | 0 | **121** |
- **Code generator now detects three getter patterns**:
  1. Simple: `getAge()` → `"age": 42`
  2. Enum-indexed: `getRating(RatingType)` → `"ratings": {...}`
  3. Collections: `getTraits()` → `"traits": [...]`
- **bump-version.sh** now also updates `docs/openapi.yaml` version


## [3.0.2] - 2025-01-30

### Fixed
- **API unavailable after switching games** - Clear cached game and client manager references on shutdown so API works correctly when loading a different game and returning


## [3.0.1] - 2025-01-30

### Added
- **OpenAPI operationId** - All endpoints now have auto-generated `operationId` for CLI code generators:
  - Pattern: `{method}{Resource}` (e.g., `getPlayers`, `getPlayerUnits`, `getTileByCoords`)
  - Enables typed client generation in any language

### Fixed
- **Property naming in generated code** - Fixed camelCase conversion for consecutive uppercase letters:
  - `getID()` → `id` (was `iD`)
  - `getHP()` → `hp` (was `hP`)
  - `getHPMax()` → `hpMax` (was `hPMax`)
  - `getXP()` → `xp` (was `xP`)

### Changed
- **Entity builders now fully auto-generated** - City, Character, Unit, and Tile builders delegate to generated code
- **Null-safe code generation** - Each property access wrapped in try-catch, failed properties skipped gracefully
- **Expanded entity fields** - Generated builders expose 120-180 properties per entity (vs 20-80 hand-written)
- **Removed complex computed fields** - Build queue details, yield dictionaries, religion lists removed in favor of raw game data
- **Excluded non-serializable types** - Generator now filters out complex game objects (CitySite, CityQueueData, Vector3, etc.)


## [3.0.0] - 2025-01-30

### BREAKING CHANGES
- **All command parameter names changed** - Stripped Hungarian notation for cleaner API:
  - `pX` → `x_id` (e.g., `pUnit` → `unit_id`, `pCity` → `city_id`)
  - `eX` → `x_type` (e.g., `eUnit` → `unit_type`, `eYield` → `yield_type`)
  - `zX` → `x` (e.g., `zName` → `name`)
  - `bX` → `x` (e.g., `bForce` → `force`, `bQueue` → `queue`)
  - `iX` → context-dependent (e.g., `iCharacterID` → `character_id`, `iTurn` → `turn`)
- **Clients must update all command requests** to use the new parameter names
- Original game parameter names preserved in OpenAPI `x-game-param` extension for reference

### Added
- **`/commands` bulk endpoint** - Execute multiple commands in a single request
- **Roslyn-based code generator** (`tools/OldWorldCodeGen/`) - Parses game source and generates:
  - `CommandExecutor.Generated.cs` - All 209 command implementations
  - `DataBuilders.Generated.cs` - Entity builders for Player, City, Unit, Character, Tile
  - `docs/openapi.yaml` - Complete OpenAPI spec with all endpoints and schemas
- **Complete OpenAPI spec** - Now includes read endpoints (`/state`, `/players`, `/cities`, etc.) with full response schemas
- **Reference documentation** for decompiled game source

### Changed
- OpenAPI spec now auto-generated from game source (replaces hand-maintained `commands.yaml`)
- Version now read from `ModInfo.xml` during code generation


## [2.4.0] - 2025-01-30

### Fixed
- **40+ parameter discrepancies** in commands.yaml corrected against CommandExecutor.cs implementation
  - Renamed params: playerType→teamType (mapReveal/mapUnreveal), amount→delta (changeCitizens/changeCooldown/changeDamage), experience→xp, all→clearAll, buyGoods→autoHarvest, enable→hasRoad, cheatType→hotkeyType
  - Wrong params: buyTile now uses cityId (not unitId), caravanMissionStart uses targetPlayer int (not missionType string)
  - Missing required params: eventStory.playerType, chat.chatType, ping.pingType, teamAlliance.player1/player2, victoryTeam.victoryType/actionType, addCharacter.familyType
  - Type fixes: abandonAmbition/removePlayerGoal now use goalId (int) not goalType/ambitionType (string), finishGoal.success→fail (inverted logic), setCitySite.enable→citySiteType (string)
  - Removed non-existent params: extendTime.seconds, customReminder.turn, unitIncrementLevel.amount, pinCharacter.pin
  - Added missing optionals: undo.turnUndo, replayTurn.numTurns/step, aiFinishTurn.numTurns, createCity.turn, newCharacter.age/fillValue, setTileOwner.tribeType

### Changed
- Regenerated OpenAPI schema with corrected command parameter definitions

## [2.3.0] - 2025-01-29

### Added
- **115 new commands** bringing total to 166 commands (from 51)
  - Laws & Economy (7): chooseLaw, cancelLaw, buyYield, sellYield, convertOrders, convertLegitimacy, convertOrdersToScience
  - Luxury Trading (5): tradeCityLuxury, tradeFamilyLuxury, tradeTribeLuxury, tradePlayerLuxury, tribute
  - Unit Special Actions (20): swap, doUnitQueue, cancelUnitQueue, formation, unlimber, anchor, repair, cancelImprovement, removeVegetation, harvestResource, unitAutomate, addUrban, roadTo, buyTile, recruitMercenary, hireMercenary, giftUnit, launchOffensive, applyEffectUnit, selectUnit
  - Agent & Caravan (4): createAgentNetwork, createTradeOutpost, caravanMissionStart, caravanMissionCancel
  - Religious Units (3): purgeReligion, spreadReligionTribe, establishTheology
  - Character Management (13): characterName, addCharacterTrait, setCharacterRating, setCharacterExperience, setCharacterCognomen, setCharacterNation, setCharacterFamily, setCharacterReligion, setCharacterCourtier, setCharacterCouncil, playerLeader, familyHead, pinCharacter
  - City Management (8): cityRename, cityAutomate, buildSpecialist, setSpecialist, changeCitizens, changeReligion, changeFamily, changeFamilySeat
  - Goals & Communication (9): abandonAmbition, addPlayerGoal, removePlayerGoal, eventStory, finishGoal, chat, ping, customReminder, clearChat
  - Game State & Turn (7): extendTime, pause, undo, redo, replayTurn, aiFinishTurn, toggleNoReplay
  - Diplomacy Extended (3): teamAlliance, tribeInvasion, victoryTeam
  - Editor/Debug (36): createUnit, createCity, removeCity, cityOwner, setTerrain, setTerrainHeight, setVegetation, setResource, setRoad, setImprovement, setTileOwner, setCitySite, improvementBuildTurns, mapReveal, mapUnreveal, addTech, addYield, addMoney, cheat, makeCharacterDead, makeCharacterSafe, newCharacter, addCharacter, tribeLeader, unitName, setUnitFamily, changeUnitOwner, changeCooldown, changeDamage, unitIncrementLevel, unitChangePromotion, changeCityDamage, changeCulture, changeCityBuildTurns, changeCityDiscontentLevel, changeProject
- **21 new type resolvers** bringing total to 31: LawType, ResourceType, EffectUnitType, ReligionType, TheologyType, TraitType, RatingType, CognomenType, CouncilType, CourtierType, SpecialistType, GoalType, EventStoryType, PingType, VictoryType, TerrainType, HeightType, VegetationType, HotkeyType, CharacterType, CitySiteType

### Changed
- **Enhanced data endpoints** now return real game data instead of placeholders:
  - `/player/{index}/goals` - Active goals with id, type, turn, maxTurns, finished flags, target entities
  - `/player/{index}/decisions` - Pending decisions with id, type, sortOrder, modal flags
  - `/player/{index}/laws` - Active laws per law class with active law count
  - `/player/{index}/missions` - Active missions with type, turn, characterId, target; mission cooldowns
  - `/player/{index}/resources` - Luxury counts per resource type, revealed resource counts
  - `/player/{index}/families` - Filtered to player's families only, with opinionRate, seatCityId, headId, opinion level

## [2.2.0] - 2025-01-29

### Added
- **Complete Command API**: 33 new commands bringing total to 51
  - Unit commands: heal, march, lock, pillage, burn, upgrade, spreadReligion
  - Worker commands: buildImprovement, upgradeImprovement, addRoad
  - City foundation: foundCity, joinCity
  - City production: buildQueue
  - Research & decisions: redrawTech, targetTech, makeDecision, removeDecision
  - Diplomacy: declareWar, makePeace, declareTruce, tribe variants, giftCity, giftYield, allyTribe
  - Character management: assignGovernor, releaseGovernor, assignGeneral, releaseGeneral, assignAgent, releaseAgent, startMission
- New type resolvers: ImprovementType, FamilyType, NationType, TribeType, MissionType
- Automated API schema validation script (`scripts/validate-api.py`)

### Fixed
- Fixed hurry commands: separate handlers for hurryCivics, hurryTraining, hurryMoney, hurryPopulation, hurryOrders
- Fixed buildProject to correctly use sendBuildProject with projectType parameter
- Fixed moveUnit to support waypointTileId with correct march parameter
- Fixed character.gender to return "Male"/"Female" instead of "0"/"1"

### Changed
- Synced OpenAPI spec with JSON schemas (~100 missing fields added)
- Updated documentation with full command reference, parameters, and examples

## [2.1.0] - 2025-01-28

### Changed
- Refactored APIEndpoint.cs into partial classes for improved maintainability
- Optimized event detection lookups with secondary indexes and HashSet for better performance

### Fixed
- Fixed misleading error messages for invalid command parameters
- Fixed thread safety issues in HTTP request handling
- Added defense-in-depth exception handling for HTTP thread pool work

### Added
- Added TryGetPlayer helper method and documented null checking patterns

## [2.0.0] - 2025-01-28

### Added
- **Bidirectional command support**: POST endpoints for executing game commands (unit movement, city production, research, diplomacy, etc.)
- **Command validation**: `/validate` endpoint to check command validity before execution
- **Units endpoint**: `/units`, `/unit/{id}`, `/player/{index}/units` for querying unit data
- **Tiles endpoint**: `/tiles`, `/tile/{id}`, `/tile/{x}/{y}`, `/map` for map and tile data
- **Player extensions**: New endpoints for player-specific data:
  - `/player/{index}/techs` - Technology research state
  - `/player/{index}/laws` - Active laws
  - `/player/{index}/religion` - Religion state
  - `/player/{index}/families` - Family relationships
  - `/player/{index}/goals` - Goals and ambitions
  - `/player/{index}/missions` - Active missions
- **Game configuration**: `/config` endpoint for map settings, difficulty, victory conditions
- CLI DSL design document for building interactive command-line clients

### Changed
- API is now bidirectional (read/write) instead of read-only

## [1.0.0] - 2025-01-27

### Added
- Military events: battle outcomes, unit kills/deaths, siege events
- Wonder completion events
- Comprehensive API documentation site (https://becked.github.io/OldWorldAPIEndpoint/)
- OpenAPI 3.0 specification and JSON Schema definitions
- Steam Workshop upload support via SteamCMD
- mod.io upload support via REST API
- Version management with bump-version.sh

### Changed
- Documentation now served via GitHub Pages with Docsify

## [0.0.2] - 2025-01-06

### Added
- Character events: births, deaths, marriages, leader changes, heir changes
- Extended character data with 75+ fields
- HTTP REST API on port 9877 with endpoints: /state, /players, /cities, /characters, /character-events, /tribes
- Team diplomacy data with war states, war scores, alliances
- Per-turn yield rates for players
- Comprehensive city data with improvements, build queues, religion, culture
- Comprehensive character data with traits, ratings, jobs, relationships
- Tribe data with strength, cities, settlements

## [0.0.1] - 2024-12-20

### Added
- Initial release
- TCP broadcast server on port 9876
- Basic player data: nation, stockpiles, legitimacy
- Basic city data: yields, happiness

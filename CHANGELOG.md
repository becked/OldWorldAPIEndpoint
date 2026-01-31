# Changelog


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

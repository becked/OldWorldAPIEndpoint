# Command API Implementation Gap Analysis

This document compares the planned command support design (`docs/command-support-design.md`) against the actual implementation.

**Analysis Date:** January 2026 (Updated)
**Design Document:** `docs/command-support-design.md` (2288 lines)
**Implementation Files:**
- `Source/CommandExecutor.cs` (3100+ lines)
- `Source/CommandTypes.cs` (160 lines)
- `Source/HttpRestServer.cs` (command endpoints)
- `Source/APIEndpoint.cs` (command queue processing)

---

## Executive Summary

The command API is **fully implemented** with **166 total commands** - far exceeding the original 51-command design specification.

| Category | Original | Extended | Total |
|----------|----------|----------|-------|
| Unit Movement & Combat | 10 | 0 | 10 |
| Unit Special Actions | 11 | 20 | 31 |
| City Production | 8 | 0 | 8 |
| Research & Decisions | 5 | 0 | 5 |
| Diplomacy | 9 | 3 | 12 |
| Character Management | 7 | 13 | 20 |
| Turn Control | 1 | 0 | 1 |
| Laws & Economy | 0 | 7 | 7 |
| Luxury Trading | 0 | 5 | 5 |
| Agent & Caravan | 0 | 4 | 4 |
| Religious Units | 0 | 3 | 3 |
| City Management | 0 | 8 | 8 |
| Goals & Communication | 0 | 9 | 9 |
| Game State & Turn | 0 | 7 | 7 |
| Editor/Debug | 0 | 36 | 36 |
| **Total** | **51** | **115** | **166** |

---

## Infrastructure Status

### Fully Implemented

| Component | Status | Notes |
|-----------|--------|-------|
| Command queue | Complete | ConcurrentQueue with ManualResetEventSlim |
| Main thread processing | Complete | OnClientUpdate() processes up to 10 commands/frame |
| HTTP POST /command | Complete | Single command execution |
| HTTP POST /commands | Complete | Bulk command execution with StopOnError |
| Request/response types | Complete | GameCommand, CommandResult, BulkCommand, etc. |
| Type resolution helpers | Complete | 31 type resolvers (see below) |
| Parameter parsing | Complete | TryGetIntParam, TryGetStringParam with detailed error messages |
| Multiplayer check | Complete | Commands refused in MP games |
| canDoActions check | Complete | Validates player can act before executing |
| Reflection-based access | Complete | All ClientManager methods cached via reflection |

### Not Implemented

| Component | Status | Notes |
|-----------|--------|-------|
| GET /validate endpoint | Not implemented | Pre-validation without execution - design doc has full implementation |

---

## Command Implementation Status

### Unit Movement & Combat (10/10 - 100%)

| Command | Implemented | Notes |
|---------|-------------|-------|
| moveUnit | **Yes** | With waypoint support |
| attack | **Yes** | |
| fortify | **Yes** | |
| heal | **Yes** | auto param supported |
| march | **Yes** | |
| pass/skip | **Yes** | |
| sleep | **Yes** | |
| sentry | **Yes** | |
| wake | **Yes** | |
| lock | **Yes** | |

### Unit Special Actions (11/11 - 100%)

| Command | Implemented | Notes |
|---------|-------------|-------|
| foundCity | **Yes** | familyType required, nationType optional |
| joinCity | **Yes** | |
| buildImprovement | **Yes** | improvementType, tileId required |
| upgradeImprovement | **Yes** | |
| addRoad | **Yes** | tileId required |
| pillage | **Yes** | |
| burn | **Yes** | |
| spreadReligion | **Yes** | cityId required |
| promote | **Yes** | |
| upgrade | **Yes** | unitType required |
| disband | **Yes** | force optional |

### City Production (8/8 - 100%)

| Command | Implemented | Notes |
|---------|-------------|-------|
| buildUnit | **Yes** | |
| buildProject | **Yes** | |
| buildQueue | **Yes** | oldSlot, newSlot required |
| hurryCivics | **Yes** | |
| hurryTraining | **Yes** | |
| hurryMoney | **Yes** | |
| hurryPopulation | **Yes** | |
| hurryOrders | **Yes** | |

### Research & Decisions (5/5 - 100%)

| Command | Implemented | Notes |
|---------|-------------|-------|
| research | **Yes** | |
| redrawTech | **Yes** | No params |
| targetTech | **Yes** | techType required |
| makeDecision | **Yes** | decisionId, choiceIndex required |
| removeDecision | **Yes** | decisionId required |

### Diplomacy (9/9 - 100%)

| Command | Implemented | Notes |
|---------|-------------|-------|
| declareWar | **Yes** | targetPlayer required |
| makePeace | **Yes** | targetPlayer required |
| declareTruce | **Yes** | targetPlayer required |
| declareWarTribe | **Yes** | tribeType required |
| makePeaceTribe | **Yes** | tribeType required |
| declareTruceTribe | **Yes** | tribeType required |
| giftCity | **Yes** | cityId, targetPlayer required |
| giftYield | **Yes** | yieldType, targetPlayer required |
| allyTribe | **Yes** | tribeType required |

### Character Management (7/7 - 100%)

| Command | Implemented | Notes |
|---------|-------------|-------|
| assignGovernor | **Yes** | cityId, characterId required |
| releaseGovernor | **Yes** | cityId required |
| assignGeneral | **Yes** | unitId, characterId required |
| releaseGeneral | **Yes** | unitId required |
| assignAgent | **Yes** | cityId, characterId required |
| releaseAgent | **Yes** | cityId required |
| startMission | **Yes** | missionType, characterId required |

### Turn Control (1/1 - 100%)

| Command | Implemented | Notes |
|---------|-------------|-------|
| endTurn | **Yes** | force defaults to true |

---

## Type Resolver Coverage

All **31 type resolvers** are implemented in `CommandExecutor.cs`:

| Type | Resolver | Batch |
|------|----------|-------|
| UnitType | ResolveUnitType | Original |
| TechType | ResolveTechType | Original |
| ProjectType | ResolveProjectType | Original |
| PromotionType | ResolvePromotionType | Original |
| YieldType | ResolveYieldType | Original |
| ImprovementType | ResolveImprovementType | Original |
| FamilyType | ResolveFamilyType | Original |
| NationType | ResolveNationType | Original |
| TribeType | ResolveTribeType | Original |
| MissionType | ResolveMissionType | Original |
| LawType | ResolveLawType | Batch A |
| ResourceType | ResolveResourceType | Batch B |
| EffectUnitType | ResolveEffectUnitType | Batch C |
| ReligionType | ResolveReligionType | Batch E |
| TheologyType | ResolveTheologyType | Batch E |
| TraitType | ResolveTraitType | Batch F |
| RatingType | ResolveRatingType | Batch F |
| CognomenType | ResolveCognomenType | Batch F |
| CouncilType | ResolveCouncilType | Batch F |
| CourtierType | ResolveCourtierType | Batch F |
| SpecialistType | ResolveSpecialistType | Batch G |
| GoalType | ResolveGoalType | Batch H |
| EventStoryType | ResolveEventStoryType | Batch H |
| PingType | ResolvePingType | Batch H |
| VictoryType | ResolveVictoryType | Batch J |
| TerrainType | ResolveTerrainType | Batch K |
| HeightType | ResolveHeightType | Batch K |
| VegetationType | ResolveVegetationType | Batch K |
| HotkeyType | ResolveHotkeyType | Batch K |
| CharacterType | ResolveCharacterType | Batch K |
| CitySiteType | ResolveCitySiteType | Batch K |

---

## Data Endpoint Enhancements

The following player data endpoints now return real game data instead of placeholder structures:

| Endpoint | Data Returned |
|----------|---------------|
| `/player/{index}/goals` | Active goals with id, type, turn, maxTurns, finished flags, target entities |
| `/player/{index}/decisions` | Pending decisions with id, type, sortOrder, modal flags |
| `/player/{index}/laws` | Active laws per law class, active law count |
| `/player/{index}/missions` | Active missions with type, turn, characterId, target; mission cooldowns |
| `/player/{index}/resources` | Luxury counts, revealed resource counts |
| `/player/{index}/families` | Player's families with opinionRate, seatCityId, headId, opinion level |

---

## Implementation Notes

The game's `ClientManager.cs` has **212+ send* methods**. With 166 commands now implemented, most practical gameplay commands are exposed via the API.

### Not Implemented

The following remain unimplemented as they are less commonly needed:

**Validation:**
- `GET /validate` - Pre-validation without execution (useful for UI action building)

**Advanced Trading:**
- Some specialized luxury trading variants
- Complex multi-party negotiations

---

## Validation Endpoint Gap

The design document specifies a `GET /validate` endpoint for pre-validation:

```
GET /validate?action=moveUnit&unitId=42&targetTileId=156
```

This would return whether an action is valid without executing it, useful for:
- Building valid action lists in UI
- Avoiding wasted commands
- Understanding why an action would fail

**Status:** Not implemented. Consider for future versions.

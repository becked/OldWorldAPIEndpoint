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

The command API is **fully implemented**. All **51 commands** specified in the design document are now functional (100% coverage).

| Category | Planned | Implemented | Coverage |
|----------|---------|-------------|----------|
| Unit Movement & Combat | 10 | 10 | 100% |
| Unit Special Actions | 11 | 11 | 100% |
| City Production | 8 | 8 | 100% |
| Research & Decisions | 5 | 5 | 100% |
| Diplomacy | 9 | 9 | 100% |
| Character Management | 7 | 7 | 100% |
| Turn Control | 1 | 1 | 100% |
| **Total** | **51** | **51** | **100%** |

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
| Type resolution helpers | Complete | 10 type resolvers (see below) |
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

All required type resolvers are implemented in `CommandExecutor.cs`:

| Type | Resolver | Status |
|------|----------|--------|
| UnitType | ResolveUnitType | Implemented |
| TechType | ResolveTechType | Implemented |
| ProjectType | ResolveProjectType | Implemented |
| PromotionType | ResolvePromotionType | Implemented |
| YieldType | ResolveYieldType | Implemented |
| ImprovementType | ResolveImprovementType | Implemented |
| FamilyType | ResolveFamilyType | Implemented |
| NationType | ResolveNationType | Implemented |
| TribeType | ResolveTribeType | Implemented |
| MissionType | ResolveMissionType | Implemented |

---

## Additional Commands in Game API

The game's `ClientManager.cs` has **212+ send* methods**. Beyond the 51 implemented commands, these could potentially be exposed in future versions:

### Laws & Government
- `sendChooseLaw(LawType)`
- `sendCancelLaw(LawType)`

### Yield Trading
- `sendBuyYield(YieldType, int size)`
- `sendSellYield(YieldType, int size)`
- `sendConvertOrders()`
- `sendConvertLegitimacy()`
- `sendConvertOrdersToScience()`

### Trade & Luxury
- `sendTradeCityLuxury(City, ResourceType, bool)`
- `sendTradeFamilyLuxury(FamilyType, ResourceType, bool)`
- `sendTradeTribeLuxury(TribeType, ResourceType, bool)`

### Unit Special
- `sendFormation(Unit, EffectUnitType)` - Formation changes
- `sendUnlimber(Unit)` - Artillery unlimber
- `sendAnchor(Unit)` - Ship anchor
- `sendRepair(Unit)` - Unit repair
- `sendUnitAutomate(Unit)` - Auto-worker
- `sendRecruitMercenary(Unit)`
- `sendHireMercenary(Unit)`
- `sendGiftUnit(Unit, PlayerType)`

### City Special
- `sendCityAutomate(City, bool)` - Auto-build queue
- `sendRenameCity(City, string)`

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

# Command API Implementation Gap Analysis

This document compares the planned command support design (`docs/command-support-design.md`) against the actual implementation to identify gaps and missing features.

**Analysis Date:** January 2026
**Design Document:** `docs/command-support-design.md` (2288 lines)
**Implementation Files:**
- `Source/CommandExecutor.cs` (990 lines)
- `Source/CommandTypes.cs` (160 lines)
- `Source/HttpRestServer.cs` (command endpoints)
- `Source/APIEndpoint.cs` (command queue processing)

---

## Executive Summary

The command API infrastructure is fully in place. Of the **51 commands** specified in the design document, approximately **19 commands** are currently functional (37% coverage). Key missing areas include:

- **Diplomacy commands** (0% implemented)
- **Character management commands** (0% implemented)
- **Unit special actions** (0% implemented)
- **Pre-validation endpoint** (not implemented)

**Recent fixes (Jan 2026):** All hurry variants now working, buildProject implemented, waypoint support added to moveUnit.

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
| Type resolution helpers | Complete | ResolveUnitType, ResolveTechType, ResolvePromotionType, etc. |
| Parameter parsing | Complete | TryGetIntParam, TryGetStringParam with detailed error messages |
| Multiplayer check | Complete | Commands refused in MP games |
| canDoActions check | Complete | Validates player can act before executing |
| Reflection-based access | Complete | ClientManager methods cached via reflection |

### Not Implemented

| Component | Status | Notes |
|-----------|--------|-------|
| GET /validate endpoint | Not implemented | Pre-validation without execution |
| Waypoint support | Not implemented | moveUnit accepts waypoint in design but not used |

---

## Command Implementation Status

### Unit Movement & Combat

| Command | Design | Implemented | Notes |
|---------|--------|-------------|-------|
| moveUnit | Yes | **Yes** | Working, but waypoint param ignored |
| attack | Yes | **Yes** | Working |
| fortify | Yes | **Yes** | Working |
| heal | Yes | **No** | sendHeal(Unit, bool auto) |
| march | Yes | **No** | sendMarch(Unit) |
| pass/skip | Yes | **Yes** | Working |
| sleep | Yes | **Yes** | Working |
| sentry | Yes | **Yes** | Working |
| wake | Yes | **Yes** | Working |
| lock | Yes | **No** | sendLock(Unit) |

**Implemented:** 8 of 10 (80%)

### Unit Special Actions

| Command | Design | Implemented | Notes |
|---------|--------|-------------|-------|
| foundCity | Yes | **No** | sendFoundCity(Unit, FamilyType, NationType) |
| joinCity | Yes | **No** | sendJoinCity(Unit) |
| buildImprovement | Yes | **No** | sendBuildImprovement(Unit, ImprovementType, bool, bool, Tile) |
| upgradeImprovement | Yes | **No** | sendUpgradeImprovement(Unit, bool) |
| addRoad | Yes | **No** | sendAddRoad(Unit, bool, bool, Tile) |
| pillage | Yes | **No** | sendPillage(Unit) |
| burn | Yes | **No** | sendBurn(Unit) |
| spreadReligion | Yes | **No** | sendSpreadReligion(Unit, int cityId) |
| promote | Yes | **Yes** | Working |
| upgrade | Yes | **No** | sendUpgrade(Unit, UnitType, bool) - distinct from promote |
| disband | Yes | **Yes** | Working |

**Implemented:** 2 of 11 (18%)

### City Production

| Command | Design | Implemented | Notes |
|---------|--------|-------------|-------|
| buildUnit | Yes | **Yes** | Working |
| buildProject | Yes | **Yes** | Fixed Jan 2026 - now uses sendBuildProject |
| buildQueue | Yes | **No** | sendBuildQueue(City, int oldSlot, int newSlot) |
| hurryCivics | Yes | **Yes** | Fixed Jan 2026 |
| hurryTraining | Yes | **Yes** | Fixed Jan 2026 |
| hurryMoney | Yes | **Yes** | Fixed Jan 2026 |
| hurryPopulation | Yes | **Yes** | Fixed Jan 2026 |
| hurryOrders | Yes | **Yes** | Fixed Jan 2026 |

**Implemented:** 7 of 8 (88%)

### Research & Decisions

| Command | Design | Implemented | Notes |
|---------|--------|-------------|-------|
| research | Yes | **Yes** | Working |
| redrawTech | Yes | **No** | sendRedrawTech() |
| targetTech | Yes | **No** | sendTargetTech(TechType) |
| makeDecision | Yes | **No** | sendMakeDecision(int id, int choice, int data) |
| removeDecision | Yes | **No** | sendRemoveDecision(int id) |

**Implemented:** 1 of 5 (20%)

### Diplomacy

| Command | Design | Implemented | Notes |
|---------|--------|-------------|-------|
| declareWar | Yes | **No** | sendDiplomacyPlayer(..., DIPLOMACY_HOSTILE) |
| makePeace | Yes | **No** | sendDiplomacyPlayer(..., DIPLOMACY_PEACE) |
| declareTruce | Yes | **No** | sendDiplomacyPlayer(..., DIPLOMACY_TRUCE) |
| declareWarTribe | Yes | **No** | sendDiplomacyTribe(..., DIPLOMACY_HOSTILE_TRIBE) |
| makePeaceTribe | Yes | **No** | sendDiplomacyTribe(..., DIPLOMACY_PEACE_TRIBE) |
| declareTruceTribe | Yes | **No** | sendDiplomacyTribe(..., DIPLOMACY_TRUCE_TRIBE) |
| giftCity | Yes | **No** | sendGiftCity(City, PlayerType) |
| giftYield | Yes | **No** | sendGiftYield(YieldType, PlayerType, bool reverse) |
| allyTribe | Yes | **No** | sendAllyTribe(TribeType, PlayerType) |

**Implemented:** 0 of 9 (0%)

### Character Management

| Command | Design | Implemented | Notes |
|---------|--------|-------------|-------|
| assignGovernor | Yes | **No** | sendMakeGovernor(City, Character) |
| releaseGovernor | Yes | **No** | sendReleaseGovernor(City) |
| assignGeneral | Yes | **No** | sendMakeUnitCharacter(Unit, Character, true) |
| releaseGeneral | Yes | **No** | sendReleaseUnitCharacter(Unit) |
| assignAgent | Yes | **No** | sendMakeAgent(City, Character) |
| releaseAgent | Yes | **No** | sendReleaseAgent(City) |
| startMission | Yes | **No** | sendStartMission(MissionType, int charId, string target, bool cancel) |

**Implemented:** 0 of 7 (0%)

### Turn Control

| Command | Design | Implemented | Notes |
|---------|--------|-------------|-------|
| endTurn | Yes | **Yes** | Working |

**Implemented:** 1 of 1 (100%)

---

## Overall Summary

| Category | Planned | Implemented | Coverage |
|----------|---------|-------------|----------|
| Unit Movement & Combat | 10 | 8 | 80% |
| Unit Special Actions | 11 | 2 | 18% |
| City Production | 8 | 7 | 88% |
| Research & Decisions | 5 | 1 | 20% |
| Diplomacy | 9 | 0 | 0% |
| Character Management | 7 | 0 | 0% |
| Turn Control | 1 | 1 | 100% |
| **Total** | **51** | **19** | **37%** |

---

## Missing Reflection Method Cache

The design document specifies caching MethodInfo for many commands. Currently cached in `CommandExecutor.cs`:

```
Cached:
- canDoActions
- GameClient (property)
- sendMoveUnit
- sendAttack
- sendFortify
- sendPass
- sendSleep
- sendSentry
- sendWake
- sendDisband
- sendPromote
- sendBuildUnit
- sendBuildProject
- sendHurryCivics
- sendHurryTraining
- sendHurryMoney
- sendHurryPopulation
- sendHurryOrders
- sendResearchTech
- sendEndTurn

Missing (per design):
- sendHeal
- sendMarch
- sendLock
- sendFoundCity
- sendJoinCity
- sendBuildImprovement
- sendUpgradeImprovement
- sendAddRoad
- sendPillage
- sendBurn
- sendSpreadReligion
- sendUpgrade
- sendBuildProject
- sendBuildQueue
- sendHurryTraining
- sendHurryMoney
- sendHurryPopulation
- sendHurryOrders
- sendRedrawTech
- sendTargetTech
- sendMakeDecision
- sendRemoveDecision
- sendDiplomacyPlayer
- sendDiplomacyTribe
- sendGiftCity
- sendGiftYield
- sendAllyTribe
- sendMakeGovernor
- sendReleaseGovernor
- sendMakeUnitCharacter
- sendReleaseUnitCharacter
- sendMakeAgent
- sendReleaseAgent
- sendStartMission
- getActivePlayer
```

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

**Status:** Not implemented. No validation endpoint exists.

---

## Implementation Issues Found (All Fixed Jan 2026)

### 1. ~~Hurry Command Implementation Bug~~ FIXED

Separate handlers now exist for each hurry type: `ExecuteHurryCivics`, `ExecuteHurryTraining`, `ExecuteHurryMoney`, `ExecuteHurryPopulation`, `ExecuteHurryOrders`. Each correctly passes just the `City` object.

### 2. ~~BuildProject Not Implemented~~ FIXED

`buildProject` now routes to `ExecuteBuildProject()` which uses `sendBuildProject(City, ProjectType, bool buyGoods, bool first, bool repeat)`.

### 3. ~~Waypoint Parameter Ignored~~ FIXED

`ExecuteMoveUnit` now supports optional `waypointTileId` parameter and correctly uses `march` parameter (matching the actual method signature `sendMoveUnit(Unit, Tile, bool march, bool queue, Tile waypoint)`).

---

## Additional Commands in Game API

The game's `ClientManager.cs` has **212+ send* methods**. Beyond what's in the design document, these could potentially be exposed:

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

### Map/Tile (Editor/Debug)
- Various terrain, improvement, and tile manipulation commands

---

## Recommendations

### Priority 1: Complete Core Gameplay Commands

1. **Fix hurry command** - Implement separate handlers for each hurry type
2. **Implement buildProject** - Distinct from buildUnit
3. **Add missing unit movement** - heal, march, lock
4. **Add worker commands** - buildImprovement, addRoad, upgradeImprovement

### Priority 2: Strategic Commands

5. **Research commands** - redrawTech, targetTech
6. **Decision commands** - makeDecision, removeDecision
7. **City management** - buildQueue

### Priority 3: Advanced Features

8. **Diplomacy commands** - Full set for player and tribe relations
9. **Character management** - Governor, general, agent assignment
10. **Validation endpoint** - GET /validate for pre-flight checks

### Priority 4: Nice-to-Have

11. **Unit special actions** - foundCity, joinCity, pillage, burn, spreadReligion
12. **Unit upgrade** - Distinct from promotion
13. **Additional game commands** - Laws, yield trading, luxury trading

---

## Type Resolver Coverage

Current type resolvers in `CommandExecutor.cs`:

| Type | Resolver | Status |
|------|----------|--------|
| UnitType | ResolveUnitType | Implemented |
| TechType | ResolveTechType | Implemented |
| ProjectType | ResolveProjectType | Implemented |
| PromotionType | ResolvePromotionType | Implemented |
| YieldType | ResolveYieldType | Implemented |
| FamilyType | - | **Missing** |
| NationType | - | **Missing** |
| TribeType | - | **Missing** |
| ImprovementType | - | **Missing** |
| MissionType | - | **Missing** |
| LawType | - | **Missing** |
| ResourceType | - | **Missing** |
| PlayerType | - | **Missing** (design shows ResolvePlayerType) |

---

## Appendix: ClientManager Method Signatures

For reference, here are the exact signatures from the game source for unimplemented commands:

### Unit Commands
```csharp
sendHeal(Unit pUnit, bool bAuto)
sendMarch(Unit pUnit)
sendLock(Unit pUnit)
sendFoundCity(Unit pUnit, FamilyType eFamily, NationType eNation)
sendJoinCity(Unit pUnit)
sendBuildImprovement(Unit pUnit, ImprovementType eImprovement, bool bBuyGoods, bool bQueue, Tile pTile)
sendUpgradeImprovement(Unit pUnit, bool bBuyGoods)
sendAddRoad(Unit pUnit, bool bBuyGoods, bool bQueue, Tile pTile)
sendPillage(Unit pUnit)
sendBurn(Unit pUnit)
sendSpreadReligion(Unit pUnit, int iCityID)
sendUpgrade(Unit pUnit, UnitType eUnit, bool bBuyGoods)
```

### City Commands
```csharp
sendBuildProject(City pCity, ProjectType eProject, bool bBuyGoods, bool bFirst, bool bRepeat)
sendBuildQueue(City pCity, int iOldSlot, int iNewSlot)
sendHurryTraining(City pCity)
sendHurryMoney(City pCity)
sendHurryPopulation(City pCity)
sendHurryOrders(City pCity)
```

### Research & Decisions
```csharp
sendRedrawTech()
sendTargetTech(TechType eTech, bool bTarget = true)
sendMakeDecision(int iID, int iChoice, int iData)
sendRemoveDecision(int iID)
```

### Diplomacy
```csharp
sendDiplomacyPlayer(PlayerType ePlayer1, PlayerType ePlayer2, ActionType action)
sendDiplomacyTribe(TribeType eTribe, PlayerType ePlayer, ActionType action)
sendGiftCity(City pCity, PlayerType ePlayer)
sendGiftYield(YieldType eYield, PlayerType ePlayer, bool bReverse)
sendAllyTribe(TribeType eTribe, PlayerType ePlayer)
getActivePlayer()  // Needed to get current player for diplomacy
```

### Character Management
```csharp
sendMakeGovernor(City pCity, Character pCharacter)
sendReleaseGovernor(City pCity)
sendMakeUnitCharacter(Unit pUnit, Character pCharacter, bool bGeneral)
sendReleaseUnitCharacter(Unit pUnit)
sendMakeAgent(City pCity, Character pCharacter)
sendReleaseAgent(City pCity)
sendStartMission(MissionType eMission, int iCharacterID, string zTarget, bool bCancel)
```

# Command Support Design

This document outlines the design for adding bidirectional command support to the Old World API Endpoint mod, enabling external clients to send game commands (move units, build, research, etc.) in addition to reading game state.

## Architecture

```
┌─────────────────┐     TCP/HTTP      ┌──────────────────────────────────────────────┐
│  External       │◄────────────────► │  OldWorldAPIEndpoint Mod                     │
│  Client         │                   │                                              │
│  (AI/Tool)      │   JSON Commands   │  ┌─────────────┐    ┌───────────────────┐   │
└─────────────────┘   ─────────────►  │  │ TCP/HTTP    │───►│ Command Queue     │   │
                                      │  │ Server      │    │ (ConcurrentQueue) │   │
                      JSON State      │  └─────────────┘    └─────────┬─────────┘   │
                   ◄─────────────────  │                              │             │
                                      │                              ▼             │
                                      │  ┌─────────────────────────────────────┐   │
                                      │  │ OnClientUpdate() [Main Thread]      │   │
                                      │  │   - Dequeue commands                │   │
                                      │  │   - Validate via canDoActions()    │   │
                                      │  │   - Execute via ClientManager      │   │
                                      │  └─────────────────────────────────────┘   │
                                      └──────────────────────────────────────────────┘
```

## Implementation Plan

### 1. Add ClientCore Reference

Verify the project references `TenCrowns.ClientCore.dll`. Add the using directive:

```csharp
using TenCrowns.ClientCore;
```

### 2. Command Data Structures

Add to `APIEndpoint.cs`:

```csharp
/// <summary>
/// Represents a command received from an external client.
/// </summary>
public class GameCommand
{
    public string Action { get; set; }      // e.g., "moveUnit", "attack", "buildUnit"
    public string RequestId { get; set; }   // For correlating responses
    public Dictionary<string, object> Params { get; set; }
}

/// <summary>
/// Result of executing a command.
/// </summary>
public class CommandResult
{
    public string RequestId { get; set; }
    public bool Success { get; set; }
    public string Error { get; set; }
}
```

### 3. Thread-Safe Command Queue

Add to `APIEndpoint.cs`:

```csharp
private static readonly ConcurrentQueue<(GameCommand cmd, Action<CommandResult> callback)> _commandQueue = new();

/// <summary>
/// Queue a command for execution on the main thread.
/// </summary>
public static void QueueCommand(GameCommand cmd, Action<CommandResult> callback)
{
    _commandQueue.Enqueue((cmd, callback));
}
```

### 4. Main Thread Processing

Add `OnClientUpdate()` override to `APIEndpoint.cs`:

```csharp
public override void OnClientUpdate()
{
    // Process queued commands on main thread
    var manager = _modSettings?.App?.GetClientManager();
    if (manager == null) return;

    // Process up to 10 commands per frame to avoid blocking
    int processed = 0;
    while (processed < 10 && _commandQueue.TryDequeue(out var item))
    {
        var (cmd, callback) = item;
        var result = ExecuteCommand(manager, cmd);
        callback?.Invoke(result);
        processed++;
    }
}
```

### 5. Command Execution

Add command execution logic to `APIEndpoint.cs`:

```csharp
private static CommandResult ExecuteCommand(ClientManager manager, GameCommand cmd)
{
    var result = new CommandResult { RequestId = cmd.RequestId };

    try
    {
        // Check if player can act
        if (!manager.canDoActions())
        {
            result.Success = false;
            result.Error = "Cannot perform actions (not player's turn or observing)";
            return result;
        }

        var game = manager.GameClient;
        if (game == null)
        {
            result.Success = false;
            result.Error = "Game not available";
            return result;
        }

        switch (cmd.Action?.ToLowerInvariant())
        {
            // Unit Movement & Combat
            case "moveunit":
                result = ExecuteMoveUnit(manager, game, cmd);
                break;
            case "attack":
                result = ExecuteAttack(manager, game, cmd);
                break;
            case "fortify":
                result = ExecuteFortify(manager, game, cmd);
                break;
            case "heal":
                result = ExecuteHeal(manager, game, cmd);
                break;
            case "march":
                result = ExecuteMarch(manager, game, cmd);
                break;
            case "skip":
            case "pass":
                result = ExecutePass(manager, game, cmd);
                break;
            case "sleep":
                result = ExecuteSleep(manager, game, cmd);
                break;
            case "sentry":
                result = ExecuteSentry(manager, game, cmd);
                break;
            case "wake":
                result = ExecuteWake(manager, game, cmd);
                break;
            case "lock":
                result = ExecuteLock(manager, game, cmd);
                break;

            // Unit Special Actions
            case "foundcity":
                result = ExecuteFoundCity(manager, game, cmd);
                break;
            case "joincity":
                result = ExecuteJoinCity(manager, game, cmd);
                break;
            case "buildimprovement":
                result = ExecuteBuildImprovement(manager, game, cmd);
                break;
            case "upgradeimprovement":
                result = ExecuteUpgradeImprovement(manager, game, cmd);
                break;
            case "addroad":
                result = ExecuteAddRoad(manager, game, cmd);
                break;
            case "pillage":
                result = ExecutePillage(manager, game, cmd);
                break;
            case "burn":
                result = ExecuteBurn(manager, game, cmd);
                break;
            case "spreadreligion":
                result = ExecuteSpreadReligion(manager, game, cmd);
                break;
            case "promote":
                result = ExecutePromote(manager, game, cmd);
                break;
            case "upgrade":
                result = ExecuteUpgrade(manager, game, cmd);
                break;
            case "disband":
                result = ExecuteDisband(manager, game, cmd);
                break;

            // City Production
            case "buildunit":
                result = ExecuteBuildUnit(manager, game, cmd);
                break;
            case "buildproject":
                result = ExecuteBuildProject(manager, game, cmd);
                break;
            case "buildqueue":
                result = ExecuteBuildQueue(manager, game, cmd);
                break;
            case "hurrycivics":
                result = ExecuteHurryCivics(manager, game, cmd);
                break;
            case "hurrytraining":
                result = ExecuteHurryTraining(manager, game, cmd);
                break;
            case "hurrymoney":
                result = ExecuteHurryMoney(manager, game, cmd);
                break;
            case "hurrypopulation":
                result = ExecuteHurryPopulation(manager, game, cmd);
                break;
            case "hurryorders":
                result = ExecuteHurryOrders(manager, game, cmd);
                break;

            // Research & Decisions
            case "research":
                result = ExecuteResearch(manager, game, cmd);
                break;
            case "redrawtech":
                result = ExecuteRedrawTech(manager, game, cmd);
                break;
            case "targettech":
                result = ExecuteTargetTech(manager, game, cmd);
                break;
            case "makedecision":
                result = ExecuteMakeDecision(manager, game, cmd);
                break;
            case "removedecision":
                result = ExecuteRemoveDecision(manager, game, cmd);
                break;

            // Diplomacy
            case "declarewar":
                result = ExecuteDeclareWar(manager, game, cmd);
                break;
            case "makepeace":
                result = ExecuteMakePeace(manager, game, cmd);
                break;
            case "declaretruce":
                result = ExecuteDeclareTruce(manager, game, cmd);
                break;
            case "declarewartribe":
                result = ExecuteDeclareWarTribe(manager, game, cmd);
                break;
            case "makepeacetribe":
                result = ExecuteMakePeaceTribe(manager, game, cmd);
                break;
            case "declaretrucetribe":
                result = ExecuteDeclareTruceTribe(manager, game, cmd);
                break;
            case "giftcity":
                result = ExecuteGiftCity(manager, game, cmd);
                break;
            case "giftyield":
                result = ExecuteGiftYield(manager, game, cmd);
                break;
            case "allytribe":
                result = ExecuteAllyTribe(manager, game, cmd);
                break;

            // Character Management
            case "assigngovernor":
                result = ExecuteAssignGovernor(manager, game, cmd);
                break;
            case "releasegovernor":
                result = ExecuteReleaseGovernor(manager, game, cmd);
                break;
            case "assigngeneral":
                result = ExecuteAssignGeneral(manager, game, cmd);
                break;
            case "releasegeneral":
                result = ExecuteReleaseGeneral(manager, game, cmd);
                break;
            case "assignagent":
                result = ExecuteAssignAgent(manager, game, cmd);
                break;
            case "releaseagent":
                result = ExecuteReleaseAgent(manager, game, cmd);
                break;
            case "startmission":
                result = ExecuteStartMission(manager, game, cmd);
                break;

            // Turn Control
            case "endturn":
                result = ExecuteEndTurn(manager, game, cmd);
                break;

            default:
                result.Success = false;
                result.Error = $"Unknown action: {cmd.Action}";
                break;
        }
    }
    catch (Exception ex)
    {
        result.Success = false;
        result.Error = $"Exception: {ex.Message}";
        Debug.LogError($"[APIEndpoint] Command execution error: {ex}");
    }

    return result;
}

#region Command Implementations

private static CommandResult ExecuteMoveUnit(ClientManager manager, Game game, GameCommand cmd)
{
    var result = new CommandResult { RequestId = cmd.RequestId };

    if (!cmd.Params.TryGetValue("unitId", out var unitIdObj) ||
        !cmd.Params.TryGetValue("targetTileId", out var tileIdObj))
    {
        result.Error = "Missing required params: unitId, targetTileId";
        return result;
    }

    int unitId = Convert.ToInt32(unitIdObj);
    int tileId = Convert.ToInt32(tileIdObj);

    var unit = game.unit(unitId);
    var tile = game.tile(tileId);

    if (unit == null)
    {
        result.Error = $"Unit not found: {unitId}";
        return result;
    }

    if (tile == null)
    {
        result.Error = $"Tile not found: {tileId}";
        return result;
    }

    // Optional params
    bool march = cmd.Params.TryGetValue("march", out var marchObj) && Convert.ToBoolean(marchObj);
    bool queue = cmd.Params.TryGetValue("queue", out var queueObj) && Convert.ToBoolean(queueObj);

    manager.sendMoveUnit(unit, tile, march, queue);
    result.Success = true;
    return result;
}

private static CommandResult ExecuteAttack(ClientManager manager, Game game, GameCommand cmd)
{
    var result = new CommandResult { RequestId = cmd.RequestId };

    if (!cmd.Params.TryGetValue("unitId", out var unitIdObj) ||
        !cmd.Params.TryGetValue("targetTileId", out var tileIdObj))
    {
        result.Error = "Missing required params: unitId, targetTileId";
        return result;
    }

    int unitId = Convert.ToInt32(unitIdObj);
    int tileId = Convert.ToInt32(tileIdObj);

    var unit = game.unit(unitId);
    var tile = game.tile(tileId);

    if (unit == null)
    {
        result.Error = $"Unit not found: {unitId}";
        return result;
    }

    if (tile == null)
    {
        result.Error = $"Tile not found: {tileId}";
        return result;
    }

    manager.sendAttack(unit, tile);
    result.Success = true;
    return result;
}

private static CommandResult ExecuteFortify(ClientManager manager, Game game, GameCommand cmd)
{
    var result = new CommandResult { RequestId = cmd.RequestId };

    if (!cmd.Params.TryGetValue("unitId", out var unitIdObj))
    {
        result.Error = "Missing required param: unitId";
        return result;
    }

    int unitId = Convert.ToInt32(unitIdObj);
    var unit = game.unit(unitId);

    if (unit == null)
    {
        result.Error = $"Unit not found: {unitId}";
        return result;
    }

    manager.sendFortify(unit);
    result.Success = true;
    return result;
}

private static CommandResult ExecutePass(ClientManager manager, Game game, GameCommand cmd)
{
    var result = new CommandResult { RequestId = cmd.RequestId };

    if (!cmd.Params.TryGetValue("unitId", out var unitIdObj))
    {
        result.Error = "Missing required param: unitId";
        return result;
    }

    int unitId = Convert.ToInt32(unitIdObj);
    var unit = game.unit(unitId);

    if (unit == null)
    {
        result.Error = $"Unit not found: {unitId}";
        return result;
    }

    manager.sendPass(unit);
    result.Success = true;
    return result;
}

private static CommandResult ExecuteBuildUnit(ClientManager manager, Game game, GameCommand cmd)
{
    var result = new CommandResult { RequestId = cmd.RequestId };

    if (!cmd.Params.TryGetValue("cityId", out var cityIdObj) ||
        !cmd.Params.TryGetValue("unitType", out var unitTypeObj))
    {
        result.Error = "Missing required params: cityId, unitType";
        return result;
    }

    int cityId = Convert.ToInt32(cityIdObj);
    string unitTypeStr = unitTypeObj.ToString();

    var city = game.city(cityId);
    if (city == null)
    {
        result.Error = $"City not found: {cityId}";
        return result;
    }

    // Resolve unit type from string (e.g., "UNIT_WARRIOR")
    var infos = game.infos();
    UnitType unitType = UnitType.NONE;
    for (int i = 0; i < (int)infos.unitsNum(); i++)
    {
        if (infos.unit((UnitType)i).mzType.Equals(unitTypeStr, StringComparison.OrdinalIgnoreCase))
        {
            unitType = (UnitType)i;
            break;
        }
    }

    if (unitType == UnitType.NONE)
    {
        result.Error = $"Unknown unit type: {unitTypeStr}";
        return result;
    }

    bool buyGoods = cmd.Params.TryGetValue("buyGoods", out var buyObj) && Convert.ToBoolean(buyObj);
    bool first = cmd.Params.TryGetValue("first", out var firstObj) && Convert.ToBoolean(firstObj);

    manager.sendBuildUnit(city, unitType, buyGoods, city.tile(), first);
    result.Success = true;
    return result;
}

private static CommandResult ExecuteBuildProject(ClientManager manager, Game game, GameCommand cmd)
{
    var result = new CommandResult { RequestId = cmd.RequestId };

    if (!cmd.Params.TryGetValue("cityId", out var cityIdObj) ||
        !cmd.Params.TryGetValue("projectType", out var projectTypeObj))
    {
        result.Error = "Missing required params: cityId, projectType";
        return result;
    }

    int cityId = Convert.ToInt32(cityIdObj);
    string projectTypeStr = projectTypeObj.ToString();

    var city = game.city(cityId);
    if (city == null)
    {
        result.Error = $"City not found: {cityId}";
        return result;
    }

    // Resolve project type from string (e.g., "PROJECT_GRANARY")
    var infos = game.infos();
    ProjectType projectType = ProjectType.NONE;
    for (int i = 0; i < (int)infos.projectsNum(); i++)
    {
        if (infos.project((ProjectType)i).mzType.Equals(projectTypeStr, StringComparison.OrdinalIgnoreCase))
        {
            projectType = (ProjectType)i;
            break;
        }
    }

    if (projectType == ProjectType.NONE)
    {
        result.Error = $"Unknown project type: {projectTypeStr}";
        return result;
    }

    bool buyGoods = cmd.Params.TryGetValue("buyGoods", out var buyObj) && Convert.ToBoolean(buyObj);
    bool first = cmd.Params.TryGetValue("first", out var firstObj) && Convert.ToBoolean(firstObj);
    bool repeat = cmd.Params.TryGetValue("repeat", out var repeatObj) && Convert.ToBoolean(repeatObj);

    manager.sendBuildProject(city, projectType, buyGoods, first, repeat);
    result.Success = true;
    return result;
}

private static CommandResult ExecuteResearch(ClientManager manager, Game game, GameCommand cmd)
{
    var result = new CommandResult { RequestId = cmd.RequestId };

    if (!cmd.Params.TryGetValue("techType", out var techTypeObj))
    {
        result.Error = "Missing required param: techType";
        return result;
    }

    string techTypeStr = techTypeObj.ToString();

    // Resolve tech type from string (e.g., "TECH_TRAPPING")
    var infos = game.infos();
    TechType techType = TechType.NONE;
    for (int i = 0; i < (int)infos.techsNum(); i++)
    {
        if (infos.tech((TechType)i).mzType.Equals(techTypeStr, StringComparison.OrdinalIgnoreCase))
        {
            techType = (TechType)i;
            break;
        }
    }

    if (techType == TechType.NONE)
    {
        result.Error = $"Unknown tech type: {techTypeStr}";
        return result;
    }

    manager.sendResearchTech(techType);
    result.Success = true;
    return result;
}

private static CommandResult ExecuteMakeDecision(ClientManager manager, Game game, GameCommand cmd)
{
    var result = new CommandResult { RequestId = cmd.RequestId };

    if (!cmd.Params.TryGetValue("decisionId", out var decisionIdObj) ||
        !cmd.Params.TryGetValue("choiceIndex", out var choiceIndexObj))
    {
        result.Error = "Missing required params: decisionId, choiceIndex";
        return result;
    }

    int decisionId = Convert.ToInt32(decisionIdObj);
    int choiceIndex = Convert.ToInt32(choiceIndexObj);
    int data = cmd.Params.TryGetValue("data", out var dataObj) ? Convert.ToInt32(dataObj) : 0;

    manager.sendMakeDecision(decisionId, choiceIndex, data);
    result.Success = true;
    return result;
}

private static CommandResult ExecuteEndTurn(ClientManager manager, Game game, GameCommand cmd)
{
    var result = new CommandResult { RequestId = cmd.RequestId };
    manager.sendEndTurn();
    result.Success = true;
    return result;
}

// ============================================
// Additional Unit Movement & Combat Commands
// ============================================

private static CommandResult ExecuteHeal(ClientManager manager, Game game, GameCommand cmd)
{
    var result = new CommandResult { RequestId = cmd.RequestId };
    if (!cmd.Params.TryGetValue("unitId", out var unitIdObj))
    {
        result.Error = "Missing required param: unitId";
        return result;
    }
    var unit = game.unit(Convert.ToInt32(unitIdObj));
    if (unit == null) { result.Error = "Unit not found"; return result; }

    bool auto = cmd.Params.TryGetValue("auto", out var autoObj) && Convert.ToBoolean(autoObj);
    manager.sendHeal(unit, auto);
    result.Success = true;
    return result;
}

private static CommandResult ExecuteMarch(ClientManager manager, Game game, GameCommand cmd)
{
    var result = new CommandResult { RequestId = cmd.RequestId };
    if (!cmd.Params.TryGetValue("unitId", out var unitIdObj))
    {
        result.Error = "Missing required param: unitId";
        return result;
    }
    var unit = game.unit(Convert.ToInt32(unitIdObj));
    if (unit == null) { result.Error = "Unit not found"; return result; }

    manager.sendMarch(unit);
    result.Success = true;
    return result;
}

private static CommandResult ExecuteSleep(ClientManager manager, Game game, GameCommand cmd)
{
    var result = new CommandResult { RequestId = cmd.RequestId };
    if (!cmd.Params.TryGetValue("unitId", out var unitIdObj))
    {
        result.Error = "Missing required param: unitId";
        return result;
    }
    var unit = game.unit(Convert.ToInt32(unitIdObj));
    if (unit == null) { result.Error = "Unit not found"; return result; }

    manager.sendSleep(unit);
    result.Success = true;
    return result;
}

private static CommandResult ExecuteSentry(ClientManager manager, Game game, GameCommand cmd)
{
    var result = new CommandResult { RequestId = cmd.RequestId };
    if (!cmd.Params.TryGetValue("unitId", out var unitIdObj))
    {
        result.Error = "Missing required param: unitId";
        return result;
    }
    var unit = game.unit(Convert.ToInt32(unitIdObj));
    if (unit == null) { result.Error = "Unit not found"; return result; }

    manager.sendSentry(unit);
    result.Success = true;
    return result;
}

private static CommandResult ExecuteWake(ClientManager manager, Game game, GameCommand cmd)
{
    var result = new CommandResult { RequestId = cmd.RequestId };
    if (!cmd.Params.TryGetValue("unitId", out var unitIdObj))
    {
        result.Error = "Missing required param: unitId";
        return result;
    }
    var unit = game.unit(Convert.ToInt32(unitIdObj));
    if (unit == null) { result.Error = "Unit not found"; return result; }

    manager.sendWake(unit);
    result.Success = true;
    return result;
}

private static CommandResult ExecuteLock(ClientManager manager, Game game, GameCommand cmd)
{
    var result = new CommandResult { RequestId = cmd.RequestId };
    if (!cmd.Params.TryGetValue("unitId", out var unitIdObj))
    {
        result.Error = "Missing required param: unitId";
        return result;
    }
    var unit = game.unit(Convert.ToInt32(unitIdObj));
    if (unit == null) { result.Error = "Unit not found"; return result; }

    manager.sendLock(unit);
    result.Success = true;
    return result;
}

// ============================================
// Unit Special Actions
// ============================================

private static CommandResult ExecuteFoundCity(ClientManager manager, Game game, GameCommand cmd)
{
    var result = new CommandResult { RequestId = cmd.RequestId };
    if (!cmd.Params.TryGetValue("unitId", out var unitIdObj) ||
        !cmd.Params.TryGetValue("familyType", out var familyTypeObj))
    {
        result.Error = "Missing required params: unitId, familyType";
        return result;
    }

    var unit = game.unit(Convert.ToInt32(unitIdObj));
    if (unit == null) { result.Error = "Unit not found"; return result; }

    var infos = game.infos();
    string familyTypeStr = familyTypeObj.ToString();
    FamilyType familyType = FamilyType.NONE;
    for (int i = 0; i < (int)infos.familiesNum(); i++)
    {
        if (infos.family((FamilyType)i).mzType.Equals(familyTypeStr, StringComparison.OrdinalIgnoreCase))
        {
            familyType = (FamilyType)i;
            break;
        }
    }

    NationType nationType = NationType.NONE;
    if (cmd.Params.TryGetValue("nationType", out var nationTypeObj))
    {
        string nationTypeStr = nationTypeObj.ToString();
        for (int i = 0; i < (int)infos.nationsNum(); i++)
        {
            if (infos.nation((NationType)i).mzType.Equals(nationTypeStr, StringComparison.OrdinalIgnoreCase))
            {
                nationType = (NationType)i;
                break;
            }
        }
    }

    manager.sendFoundCity(unit, familyType, nationType);
    result.Success = true;
    return result;
}

private static CommandResult ExecuteJoinCity(ClientManager manager, Game game, GameCommand cmd)
{
    var result = new CommandResult { RequestId = cmd.RequestId };
    if (!cmd.Params.TryGetValue("unitId", out var unitIdObj))
    {
        result.Error = "Missing required param: unitId";
        return result;
    }
    var unit = game.unit(Convert.ToInt32(unitIdObj));
    if (unit == null) { result.Error = "Unit not found"; return result; }

    manager.sendJoinCity(unit);
    result.Success = true;
    return result;
}

private static CommandResult ExecuteBuildImprovement(ClientManager manager, Game game, GameCommand cmd)
{
    var result = new CommandResult { RequestId = cmd.RequestId };
    if (!cmd.Params.TryGetValue("unitId", out var unitIdObj) ||
        !cmd.Params.TryGetValue("improvementType", out var improvementTypeObj) ||
        !cmd.Params.TryGetValue("tileId", out var tileIdObj))
    {
        result.Error = "Missing required params: unitId, improvementType, tileId";
        return result;
    }

    var unit = game.unit(Convert.ToInt32(unitIdObj));
    if (unit == null) { result.Error = "Unit not found"; return result; }

    var tile = game.tile(Convert.ToInt32(tileIdObj));
    if (tile == null) { result.Error = "Tile not found"; return result; }

    var infos = game.infos();
    string improvementTypeStr = improvementTypeObj.ToString();
    ImprovementType improvementType = ImprovementType.NONE;
    for (int i = 0; i < (int)infos.improvementsNum(); i++)
    {
        if (infos.improvement((ImprovementType)i).mzType.Equals(improvementTypeStr, StringComparison.OrdinalIgnoreCase))
        {
            improvementType = (ImprovementType)i;
            break;
        }
    }
    if (improvementType == ImprovementType.NONE)
    {
        result.Error = $"Unknown improvement type: {improvementTypeStr}";
        return result;
    }

    bool buyGoods = cmd.Params.TryGetValue("buyGoods", out var buyObj) && Convert.ToBoolean(buyObj);
    bool queue = cmd.Params.TryGetValue("queue", out var queueObj) && Convert.ToBoolean(queueObj);

    manager.sendBuildImprovement(unit, improvementType, buyGoods, queue, tile);
    result.Success = true;
    return result;
}

private static CommandResult ExecuteUpgradeImprovement(ClientManager manager, Game game, GameCommand cmd)
{
    var result = new CommandResult { RequestId = cmd.RequestId };
    if (!cmd.Params.TryGetValue("unitId", out var unitIdObj))
    {
        result.Error = "Missing required param: unitId";
        return result;
    }
    var unit = game.unit(Convert.ToInt32(unitIdObj));
    if (unit == null) { result.Error = "Unit not found"; return result; }

    bool buyGoods = cmd.Params.TryGetValue("buyGoods", out var buyObj) && Convert.ToBoolean(buyObj);
    manager.sendUpgradeImprovement(unit, buyGoods);
    result.Success = true;
    return result;
}

private static CommandResult ExecuteAddRoad(ClientManager manager, Game game, GameCommand cmd)
{
    var result = new CommandResult { RequestId = cmd.RequestId };
    if (!cmd.Params.TryGetValue("unitId", out var unitIdObj) ||
        !cmd.Params.TryGetValue("tileId", out var tileIdObj))
    {
        result.Error = "Missing required params: unitId, tileId";
        return result;
    }

    var unit = game.unit(Convert.ToInt32(unitIdObj));
    if (unit == null) { result.Error = "Unit not found"; return result; }

    var tile = game.tile(Convert.ToInt32(tileIdObj));
    if (tile == null) { result.Error = "Tile not found"; return result; }

    bool buyGoods = cmd.Params.TryGetValue("buyGoods", out var buyObj) && Convert.ToBoolean(buyObj);
    bool queue = cmd.Params.TryGetValue("queue", out var queueObj) && Convert.ToBoolean(queueObj);

    manager.sendAddRoad(unit, buyGoods, queue, tile);
    result.Success = true;
    return result;
}

private static CommandResult ExecutePillage(ClientManager manager, Game game, GameCommand cmd)
{
    var result = new CommandResult { RequestId = cmd.RequestId };
    if (!cmd.Params.TryGetValue("unitId", out var unitIdObj))
    {
        result.Error = "Missing required param: unitId";
        return result;
    }
    var unit = game.unit(Convert.ToInt32(unitIdObj));
    if (unit == null) { result.Error = "Unit not found"; return result; }

    manager.sendPillage(unit);
    result.Success = true;
    return result;
}

private static CommandResult ExecuteBurn(ClientManager manager, Game game, GameCommand cmd)
{
    var result = new CommandResult { RequestId = cmd.RequestId };
    if (!cmd.Params.TryGetValue("unitId", out var unitIdObj))
    {
        result.Error = "Missing required param: unitId";
        return result;
    }
    var unit = game.unit(Convert.ToInt32(unitIdObj));
    if (unit == null) { result.Error = "Unit not found"; return result; }

    manager.sendBurn(unit);
    result.Success = true;
    return result;
}

private static CommandResult ExecuteSpreadReligion(ClientManager manager, Game game, GameCommand cmd)
{
    var result = new CommandResult { RequestId = cmd.RequestId };
    if (!cmd.Params.TryGetValue("unitId", out var unitIdObj) ||
        !cmd.Params.TryGetValue("cityId", out var cityIdObj))
    {
        result.Error = "Missing required params: unitId, cityId";
        return result;
    }

    var unit = game.unit(Convert.ToInt32(unitIdObj));
    if (unit == null) { result.Error = "Unit not found"; return result; }

    int cityId = Convert.ToInt32(cityIdObj);
    manager.sendSpreadReligion(unit, cityId);
    result.Success = true;
    return result;
}

private static CommandResult ExecutePromote(ClientManager manager, Game game, GameCommand cmd)
{
    var result = new CommandResult { RequestId = cmd.RequestId };
    if (!cmd.Params.TryGetValue("unitId", out var unitIdObj) ||
        !cmd.Params.TryGetValue("promotionType", out var promotionTypeObj))
    {
        result.Error = "Missing required params: unitId, promotionType";
        return result;
    }

    var unit = game.unit(Convert.ToInt32(unitIdObj));
    if (unit == null) { result.Error = "Unit not found"; return result; }

    var infos = game.infos();
    string promotionTypeStr = promotionTypeObj.ToString();
    PromotionType promotionType = PromotionType.NONE;
    for (int i = 0; i < (int)infos.promotionsNum(); i++)
    {
        if (infos.promotion((PromotionType)i).mzType.Equals(promotionTypeStr, StringComparison.OrdinalIgnoreCase))
        {
            promotionType = (PromotionType)i;
            break;
        }
    }
    if (promotionType == PromotionType.NONE)
    {
        result.Error = $"Unknown promotion type: {promotionTypeStr}";
        return result;
    }

    manager.sendPromote(unit, promotionType);
    result.Success = true;
    return result;
}

private static CommandResult ExecuteUpgrade(ClientManager manager, Game game, GameCommand cmd)
{
    var result = new CommandResult { RequestId = cmd.RequestId };
    if (!cmd.Params.TryGetValue("unitId", out var unitIdObj) ||
        !cmd.Params.TryGetValue("unitType", out var unitTypeObj))
    {
        result.Error = "Missing required params: unitId, unitType";
        return result;
    }

    var unit = game.unit(Convert.ToInt32(unitIdObj));
    if (unit == null) { result.Error = "Unit not found"; return result; }

    var infos = game.infos();
    string unitTypeStr = unitTypeObj.ToString();
    UnitType unitType = UnitType.NONE;
    for (int i = 0; i < (int)infos.unitsNum(); i++)
    {
        if (infos.unit((UnitType)i).mzType.Equals(unitTypeStr, StringComparison.OrdinalIgnoreCase))
        {
            unitType = (UnitType)i;
            break;
        }
    }
    if (unitType == UnitType.NONE)
    {
        result.Error = $"Unknown unit type: {unitTypeStr}";
        return result;
    }

    bool buyGoods = cmd.Params.TryGetValue("buyGoods", out var buyObj) && Convert.ToBoolean(buyObj);
    manager.sendUpgrade(unit, unitType, buyGoods);
    result.Success = true;
    return result;
}

private static CommandResult ExecuteDisband(ClientManager manager, Game game, GameCommand cmd)
{
    var result = new CommandResult { RequestId = cmd.RequestId };
    if (!cmd.Params.TryGetValue("unitId", out var unitIdObj))
    {
        result.Error = "Missing required param: unitId";
        return result;
    }
    var unit = game.unit(Convert.ToInt32(unitIdObj));
    if (unit == null) { result.Error = "Unit not found"; return result; }

    bool force = cmd.Params.TryGetValue("force", out var forceObj) && Convert.ToBoolean(forceObj);
    manager.sendDisband(unit, force);
    result.Success = true;
    return result;
}

// ============================================
// City Production Commands
// ============================================

private static CommandResult ExecuteBuildQueue(ClientManager manager, Game game, GameCommand cmd)
{
    var result = new CommandResult { RequestId = cmd.RequestId };
    if (!cmd.Params.TryGetValue("cityId", out var cityIdObj) ||
        !cmd.Params.TryGetValue("oldSlot", out var oldSlotObj) ||
        !cmd.Params.TryGetValue("newSlot", out var newSlotObj))
    {
        result.Error = "Missing required params: cityId, oldSlot, newSlot";
        return result;
    }

    var city = game.city(Convert.ToInt32(cityIdObj));
    if (city == null) { result.Error = "City not found"; return result; }

    manager.sendBuildQueue(city, Convert.ToInt32(oldSlotObj), Convert.ToInt32(newSlotObj));
    result.Success = true;
    return result;
}

private static CommandResult ExecuteHurryCivics(ClientManager manager, Game game, GameCommand cmd)
{
    var result = new CommandResult { RequestId = cmd.RequestId };
    if (!cmd.Params.TryGetValue("cityId", out var cityIdObj))
    {
        result.Error = "Missing required param: cityId";
        return result;
    }
    var city = game.city(Convert.ToInt32(cityIdObj));
    if (city == null) { result.Error = "City not found"; return result; }

    manager.sendHurryCivics(city);
    result.Success = true;
    return result;
}

private static CommandResult ExecuteHurryTraining(ClientManager manager, Game game, GameCommand cmd)
{
    var result = new CommandResult { RequestId = cmd.RequestId };
    if (!cmd.Params.TryGetValue("cityId", out var cityIdObj))
    {
        result.Error = "Missing required param: cityId";
        return result;
    }
    var city = game.city(Convert.ToInt32(cityIdObj));
    if (city == null) { result.Error = "City not found"; return result; }

    manager.sendHurryTraining(city);
    result.Success = true;
    return result;
}

private static CommandResult ExecuteHurryMoney(ClientManager manager, Game game, GameCommand cmd)
{
    var result = new CommandResult { RequestId = cmd.RequestId };
    if (!cmd.Params.TryGetValue("cityId", out var cityIdObj))
    {
        result.Error = "Missing required param: cityId";
        return result;
    }
    var city = game.city(Convert.ToInt32(cityIdObj));
    if (city == null) { result.Error = "City not found"; return result; }

    manager.sendHurryMoney(city);
    result.Success = true;
    return result;
}

private static CommandResult ExecuteHurryPopulation(ClientManager manager, Game game, GameCommand cmd)
{
    var result = new CommandResult { RequestId = cmd.RequestId };
    if (!cmd.Params.TryGetValue("cityId", out var cityIdObj))
    {
        result.Error = "Missing required param: cityId";
        return result;
    }
    var city = game.city(Convert.ToInt32(cityIdObj));
    if (city == null) { result.Error = "City not found"; return result; }

    manager.sendHurryPopulation(city);
    result.Success = true;
    return result;
}

private static CommandResult ExecuteHurryOrders(ClientManager manager, Game game, GameCommand cmd)
{
    var result = new CommandResult { RequestId = cmd.RequestId };
    if (!cmd.Params.TryGetValue("cityId", out var cityIdObj))
    {
        result.Error = "Missing required param: cityId";
        return result;
    }
    var city = game.city(Convert.ToInt32(cityIdObj));
    if (city == null) { result.Error = "City not found"; return result; }

    manager.sendHurryOrders(city);
    result.Success = true;
    return result;
}

// ============================================
// Research Commands
// ============================================

private static CommandResult ExecuteRedrawTech(ClientManager manager, Game game, GameCommand cmd)
{
    var result = new CommandResult { RequestId = cmd.RequestId };
    manager.sendRedrawTech();
    result.Success = true;
    return result;
}

private static CommandResult ExecuteTargetTech(ClientManager manager, Game game, GameCommand cmd)
{
    var result = new CommandResult { RequestId = cmd.RequestId };
    if (!cmd.Params.TryGetValue("techType", out var techTypeObj))
    {
        result.Error = "Missing required param: techType";
        return result;
    }

    var infos = game.infos();
    string techTypeStr = techTypeObj.ToString();
    TechType techType = TechType.NONE;
    for (int i = 0; i < (int)infos.techsNum(); i++)
    {
        if (infos.tech((TechType)i).mzType.Equals(techTypeStr, StringComparison.OrdinalIgnoreCase))
        {
            techType = (TechType)i;
            break;
        }
    }
    if (techType == TechType.NONE)
    {
        result.Error = $"Unknown tech type: {techTypeStr}";
        return result;
    }

    manager.sendTargetTech(techType);
    result.Success = true;
    return result;
}

private static CommandResult ExecuteRemoveDecision(ClientManager manager, Game game, GameCommand cmd)
{
    var result = new CommandResult { RequestId = cmd.RequestId };
    if (!cmd.Params.TryGetValue("decisionId", out var decisionIdObj))
    {
        result.Error = "Missing required param: decisionId";
        return result;
    }

    manager.sendRemoveDecision(Convert.ToInt32(decisionIdObj));
    result.Success = true;
    return result;
}

// ============================================
// Diplomacy Commands
// ============================================

private static PlayerType ResolvePlayerType(Game game, object playerObj)
{
    if (playerObj is int || int.TryParse(playerObj.ToString(), out _))
    {
        return (PlayerType)Convert.ToInt32(playerObj);
    }
    // Could also support player names here if needed
    return PlayerType.NONE;
}

private static TribeType ResolveTribeType(Game game, object tribeObj)
{
    var infos = game.infos();
    string tribeTypeStr = tribeObj.ToString();
    for (int i = 0; i < (int)infos.tribesNum(); i++)
    {
        if (infos.tribe((TribeType)i).mzType.Equals(tribeTypeStr, StringComparison.OrdinalIgnoreCase))
        {
            return (TribeType)i;
        }
    }
    return TribeType.NONE;
}

private static CommandResult ExecuteDeclareWar(ClientManager manager, Game game, GameCommand cmd)
{
    var result = new CommandResult { RequestId = cmd.RequestId };
    if (!cmd.Params.TryGetValue("targetPlayer", out var targetPlayerObj))
    {
        result.Error = "Missing required param: targetPlayer";
        return result;
    }

    PlayerType targetPlayer = ResolvePlayerType(game, targetPlayerObj);
    manager.sendDiplomacyPlayer(manager.getActivePlayer(), targetPlayer, ActionType.DIPLOMACY_HOSTILE);
    result.Success = true;
    return result;
}

private static CommandResult ExecuteMakePeace(ClientManager manager, Game game, GameCommand cmd)
{
    var result = new CommandResult { RequestId = cmd.RequestId };
    if (!cmd.Params.TryGetValue("targetPlayer", out var targetPlayerObj))
    {
        result.Error = "Missing required param: targetPlayer";
        return result;
    }

    PlayerType targetPlayer = ResolvePlayerType(game, targetPlayerObj);
    manager.sendDiplomacyPlayer(manager.getActivePlayer(), targetPlayer, ActionType.DIPLOMACY_PEACE);
    result.Success = true;
    return result;
}

private static CommandResult ExecuteDeclareTruce(ClientManager manager, Game game, GameCommand cmd)
{
    var result = new CommandResult { RequestId = cmd.RequestId };
    if (!cmd.Params.TryGetValue("targetPlayer", out var targetPlayerObj))
    {
        result.Error = "Missing required param: targetPlayer";
        return result;
    }

    PlayerType targetPlayer = ResolvePlayerType(game, targetPlayerObj);
    manager.sendDiplomacyPlayer(manager.getActivePlayer(), targetPlayer, ActionType.DIPLOMACY_TRUCE);
    result.Success = true;
    return result;
}

private static CommandResult ExecuteDeclareWarTribe(ClientManager manager, Game game, GameCommand cmd)
{
    var result = new CommandResult { RequestId = cmd.RequestId };
    if (!cmd.Params.TryGetValue("tribeType", out var tribeTypeObj))
    {
        result.Error = "Missing required param: tribeType";
        return result;
    }

    TribeType tribeType = ResolveTribeType(game, tribeTypeObj);
    if (tribeType == TribeType.NONE) { result.Error = "Unknown tribe type"; return result; }

    manager.sendDiplomacyTribe(tribeType, manager.getActivePlayer(), ActionType.DIPLOMACY_HOSTILE_TRIBE);
    result.Success = true;
    return result;
}

private static CommandResult ExecuteMakePeaceTribe(ClientManager manager, Game game, GameCommand cmd)
{
    var result = new CommandResult { RequestId = cmd.RequestId };
    if (!cmd.Params.TryGetValue("tribeType", out var tribeTypeObj))
    {
        result.Error = "Missing required param: tribeType";
        return result;
    }

    TribeType tribeType = ResolveTribeType(game, tribeTypeObj);
    if (tribeType == TribeType.NONE) { result.Error = "Unknown tribe type"; return result; }

    manager.sendDiplomacyTribe(tribeType, manager.getActivePlayer(), ActionType.DIPLOMACY_PEACE_TRIBE);
    result.Success = true;
    return result;
}

private static CommandResult ExecuteDeclareTruceTribe(ClientManager manager, Game game, GameCommand cmd)
{
    var result = new CommandResult { RequestId = cmd.RequestId };
    if (!cmd.Params.TryGetValue("tribeType", out var tribeTypeObj))
    {
        result.Error = "Missing required param: tribeType";
        return result;
    }

    TribeType tribeType = ResolveTribeType(game, tribeTypeObj);
    if (tribeType == TribeType.NONE) { result.Error = "Unknown tribe type"; return result; }

    manager.sendDiplomacyTribe(tribeType, manager.getActivePlayer(), ActionType.DIPLOMACY_TRUCE_TRIBE);
    result.Success = true;
    return result;
}

private static CommandResult ExecuteGiftCity(ClientManager manager, Game game, GameCommand cmd)
{
    var result = new CommandResult { RequestId = cmd.RequestId };
    if (!cmd.Params.TryGetValue("cityId", out var cityIdObj) ||
        !cmd.Params.TryGetValue("targetPlayer", out var targetPlayerObj))
    {
        result.Error = "Missing required params: cityId, targetPlayer";
        return result;
    }

    var city = game.city(Convert.ToInt32(cityIdObj));
    if (city == null) { result.Error = "City not found"; return result; }

    PlayerType targetPlayer = ResolvePlayerType(game, targetPlayerObj);
    manager.sendGiftCity(city, targetPlayer);
    result.Success = true;
    return result;
}

private static CommandResult ExecuteGiftYield(ClientManager manager, Game game, GameCommand cmd)
{
    var result = new CommandResult { RequestId = cmd.RequestId };
    if (!cmd.Params.TryGetValue("yieldType", out var yieldTypeObj) ||
        !cmd.Params.TryGetValue("targetPlayer", out var targetPlayerObj))
    {
        result.Error = "Missing required params: yieldType, targetPlayer";
        return result;
    }

    var infos = game.infos();
    string yieldTypeStr = yieldTypeObj.ToString();
    YieldType yieldType = YieldType.NONE;
    for (int i = 0; i < (int)infos.yieldsNum(); i++)
    {
        if (infos.yield((YieldType)i).mzType.Equals(yieldTypeStr, StringComparison.OrdinalIgnoreCase))
        {
            yieldType = (YieldType)i;
            break;
        }
    }
    if (yieldType == YieldType.NONE)
    {
        result.Error = $"Unknown yield type: {yieldTypeStr}";
        return result;
    }

    PlayerType targetPlayer = ResolvePlayerType(game, targetPlayerObj);
    bool reverse = cmd.Params.TryGetValue("reverse", out var reverseObj) && Convert.ToBoolean(reverseObj);

    manager.sendGiftYield(yieldType, targetPlayer, reverse);
    result.Success = true;
    return result;
}

private static CommandResult ExecuteAllyTribe(ClientManager manager, Game game, GameCommand cmd)
{
    var result = new CommandResult { RequestId = cmd.RequestId };
    if (!cmd.Params.TryGetValue("tribeType", out var tribeTypeObj))
    {
        result.Error = "Missing required param: tribeType";
        return result;
    }

    TribeType tribeType = ResolveTribeType(game, tribeTypeObj);
    if (tribeType == TribeType.NONE) { result.Error = "Unknown tribe type"; return result; }

    manager.sendAllyTribe(tribeType, manager.getActivePlayer());
    result.Success = true;
    return result;
}

// ============================================
// Character Management Commands
// ============================================

private static CommandResult ExecuteAssignGovernor(ClientManager manager, Game game, GameCommand cmd)
{
    var result = new CommandResult { RequestId = cmd.RequestId };
    if (!cmd.Params.TryGetValue("cityId", out var cityIdObj) ||
        !cmd.Params.TryGetValue("characterId", out var characterIdObj))
    {
        result.Error = "Missing required params: cityId, characterId";
        return result;
    }

    var city = game.city(Convert.ToInt32(cityIdObj));
    if (city == null) { result.Error = "City not found"; return result; }

    var character = game.character(Convert.ToInt32(characterIdObj));
    if (character == null) { result.Error = "Character not found"; return result; }

    manager.sendMakeGovernor(city, character);
    result.Success = true;
    return result;
}

private static CommandResult ExecuteReleaseGovernor(ClientManager manager, Game game, GameCommand cmd)
{
    var result = new CommandResult { RequestId = cmd.RequestId };
    if (!cmd.Params.TryGetValue("cityId", out var cityIdObj))
    {
        result.Error = "Missing required param: cityId";
        return result;
    }

    var city = game.city(Convert.ToInt32(cityIdObj));
    if (city == null) { result.Error = "City not found"; return result; }

    manager.sendReleaseGovernor(city);
    result.Success = true;
    return result;
}

private static CommandResult ExecuteAssignGeneral(ClientManager manager, Game game, GameCommand cmd)
{
    var result = new CommandResult { RequestId = cmd.RequestId };
    if (!cmd.Params.TryGetValue("unitId", out var unitIdObj) ||
        !cmd.Params.TryGetValue("characterId", out var characterIdObj))
    {
        result.Error = "Missing required params: unitId, characterId";
        return result;
    }

    var unit = game.unit(Convert.ToInt32(unitIdObj));
    if (unit == null) { result.Error = "Unit not found"; return result; }

    var character = game.character(Convert.ToInt32(characterIdObj));
    if (character == null) { result.Error = "Character not found"; return result; }

    manager.sendMakeUnitCharacter(unit, character, bGeneral: true);
    result.Success = true;
    return result;
}

private static CommandResult ExecuteReleaseGeneral(ClientManager manager, Game game, GameCommand cmd)
{
    var result = new CommandResult { RequestId = cmd.RequestId };
    if (!cmd.Params.TryGetValue("unitId", out var unitIdObj))
    {
        result.Error = "Missing required param: unitId";
        return result;
    }

    var unit = game.unit(Convert.ToInt32(unitIdObj));
    if (unit == null) { result.Error = "Unit not found"; return result; }

    manager.sendReleaseUnitCharacter(unit);
    result.Success = true;
    return result;
}

private static CommandResult ExecuteAssignAgent(ClientManager manager, Game game, GameCommand cmd)
{
    var result = new CommandResult { RequestId = cmd.RequestId };
    if (!cmd.Params.TryGetValue("cityId", out var cityIdObj) ||
        !cmd.Params.TryGetValue("characterId", out var characterIdObj))
    {
        result.Error = "Missing required params: cityId, characterId";
        return result;
    }

    var city = game.city(Convert.ToInt32(cityIdObj));
    if (city == null) { result.Error = "City not found"; return result; }

    var character = game.character(Convert.ToInt32(characterIdObj));
    if (character == null) { result.Error = "Character not found"; return result; }

    manager.sendMakeAgent(city, character);
    result.Success = true;
    return result;
}

private static CommandResult ExecuteReleaseAgent(ClientManager manager, Game game, GameCommand cmd)
{
    var result = new CommandResult { RequestId = cmd.RequestId };
    if (!cmd.Params.TryGetValue("cityId", out var cityIdObj))
    {
        result.Error = "Missing required param: cityId";
        return result;
    }

    var city = game.city(Convert.ToInt32(cityIdObj));
    if (city == null) { result.Error = "City not found"; return result; }

    manager.sendReleaseAgent(city);
    result.Success = true;
    return result;
}

private static CommandResult ExecuteStartMission(ClientManager manager, Game game, GameCommand cmd)
{
    var result = new CommandResult { RequestId = cmd.RequestId };
    if (!cmd.Params.TryGetValue("missionType", out var missionTypeObj) ||
        !cmd.Params.TryGetValue("characterId", out var characterIdObj) ||
        !cmd.Params.TryGetValue("target", out var targetObj))
    {
        result.Error = "Missing required params: missionType, characterId, target";
        return result;
    }

    var infos = game.infos();
    string missionTypeStr = missionTypeObj.ToString();
    MissionType missionType = MissionType.NONE;
    for (int i = 0; i < (int)infos.missionsNum(); i++)
    {
        if (infos.mission((MissionType)i).mzType.Equals(missionTypeStr, StringComparison.OrdinalIgnoreCase))
        {
            missionType = (MissionType)i;
            break;
        }
    }
    if (missionType == MissionType.NONE)
    {
        result.Error = $"Unknown mission type: {missionTypeStr}";
        return result;
    }

    int characterId = Convert.ToInt32(characterIdObj);
    string target = targetObj.ToString();
    bool cancel = cmd.Params.TryGetValue("cancel", out var cancelObj) && Convert.ToBoolean(cancelObj);

    manager.sendStartMission(missionType, characterId, target, cancel);
    result.Success = true;
    return result;
}

#endregion
```

### 6. HTTP POST Endpoint

Add to `HttpRestServer.cs` in `RouteRequest()`:

```csharp
// In RouteRequest(), add POST handling:
if (context.Request.HttpMethod == "POST")
{
    if (segments.Length > 0 && segments[0] == "command")
    {
        HandleCommandRequest(context);
        return;
    }
    SendErrorResponse(context.Response, "POST only supported for /command", 405);
    return;
}

// Add the handler:
private void HandleCommandRequest(HttpListenerContext context)
{
    try
    {
        using (var reader = new System.IO.StreamReader(context.Request.InputStream))
        {
            string body = reader.ReadToEnd();
            var cmd = JsonConvert.DeserializeObject<GameCommand>(body, _jsonSettings);

            if (cmd == null || string.IsNullOrEmpty(cmd.Action))
            {
                SendErrorResponse(context.Response, "Invalid command format", 400);
                return;
            }

            // Use ManualResetEvent to wait for main thread execution
            var waitHandle = new ManualResetEventSlim(false);
            CommandResult result = null;

            APIEndpoint.QueueCommand(cmd, r =>
            {
                result = r;
                waitHandle.Set();
            });

            // Wait up to 5 seconds for command execution
            if (waitHandle.Wait(5000))
            {
                SendJsonResponse(context.Response, result);
            }
            else
            {
                SendErrorResponse(context.Response, "Command execution timeout", 504);
            }
        }
    }
    catch (Exception ex)
    {
        SendErrorResponse(context.Response, $"Error processing command: {ex.Message}", 400);
    }
}
```

### 7. TCP Bidirectional Support (Optional)

For TCP, you could add command reading in a separate thread per client. However, HTTP POST is simpler and recommended for commands since:
- Request/response semantics match command execution
- Natural timeout handling
- Easier error reporting

## Command Protocol

### HTTP POST /command

**Request:**
```json
{
    "action": "moveUnit",
    "requestId": "abc123",
    "params": {
        "unitId": 42,
        "targetTileId": 156,
        "march": false,
        "queue": false
    }
}
```

**Response:**
```json
{
    "requestId": "abc123",
    "success": true,
    "error": null
}
```

### Supported Actions

#### Unit Movement & Combat
| Action | Required Params | Optional Params |
|--------|-----------------|-----------------|
| `moveUnit` | `unitId`, `targetTileId` | `march`, `queue`, `waypointTileId` |
| `attack` | `unitId`, `targetTileId` | - |
| `fortify` | `unitId` | - |
| `heal` | `unitId` | `auto` |
| `march` | `unitId` | - |
| `pass` / `skip` | `unitId` | - |
| `sleep` | `unitId` | - |
| `sentry` | `unitId` | - |
| `wake` | `unitId` | - |
| `lock` | `unitId` | - |

#### Unit Special Actions
| Action | Required Params | Optional Params |
|--------|-----------------|-----------------|
| `foundCity` | `unitId`, `familyType` | `nationType` |
| `joinCity` | `unitId` | - |
| `buildImprovement` | `unitId`, `improvementType`, `tileId` | `buyGoods`, `queue` |
| `upgradeImprovement` | `unitId` | `buyGoods` |
| `addRoad` | `unitId`, `tileId` | `buyGoods`, `queue` |
| `pillage` | `unitId` | - |
| `burn` | `unitId` | - |
| `spreadReligion` | `unitId`, `cityId` | - |
| `promote` | `unitId`, `promotionType` | - |
| `upgrade` | `unitId`, `unitType` | `buyGoods` |
| `disband` | `unitId` | `force` |

#### City Production
| Action | Required Params | Optional Params |
|--------|-----------------|-----------------|
| `buildUnit` | `cityId`, `unitType` | `buyGoods`, `first`, `tileId` |
| `buildProject` | `cityId`, `projectType` | `buyGoods`, `first`, `repeat` |
| `buildQueue` | `cityId`, `oldSlot`, `newSlot` | - |
| `hurryCivics` | `cityId` | - |
| `hurryTraining` | `cityId` | - |
| `hurryMoney` | `cityId` | - |
| `hurryPopulation` | `cityId` | - |
| `hurryOrders` | `cityId` | - |

#### Research & Decisions
| Action | Required Params | Optional Params |
|--------|-----------------|-----------------|
| `research` | `techType` | - |
| `redrawTech` | - | - |
| `targetTech` | `techType` | - |
| `makeDecision` | `decisionId`, `choiceIndex` | `data` |
| `removeDecision` | `decisionId` | - |

#### Diplomacy
| Action | Required Params | Optional Params |
|--------|-----------------|-----------------|
| `declareWar` | `targetPlayer` | - |
| `makePeace` | `targetPlayer` | - |
| `declareTruce` | `targetPlayer` | - |
| `declareWarTribe` | `tribeType` | - |
| `makePeaceTribe` | `tribeType` | - |
| `declareTruceTribe` | `tribeType` | - |
| `giftCity` | `cityId`, `targetPlayer` | - |
| `giftYield` | `yieldType`, `targetPlayer` | `reverse` |
| `allyTribe` | `tribeType` | - |

#### Character Management
| Action | Required Params | Optional Params |
|--------|-----------------|-----------------|
| `assignGovernor` | `cityId`, `characterId` | - |
| `releaseGovernor` | `cityId` | - |
| `assignGeneral` | `unitId`, `characterId` | - |
| `releaseGeneral` | `unitId` | - |
| `assignAgent` | `cityId`, `characterId` | - |
| `releaseAgent` | `cityId` | - |
| `startMission` | `missionType`, `characterId`, `target` | `cancel` |

#### Turn Control
| Action | Required Params | Optional Params |
|--------|-----------------|-----------------|
| `endTurn` | - | - |

### Type String Format

Use game type strings exactly as they appear in the API state output:
- Units: `UNIT_WARRIOR`, `UNIT_SETTLER`, etc.
- Projects: `PROJECT_GRANARY`, `PROJECT_BARRACKS`, etc.
- Techs: `TECH_TRAPPING`, `TECH_STONECUTTING`, etc.

## Available Commands Reference

The actions listed above are a **starter subset**. The full list of available commands can be found in the game source:

**Primary Source:** `Reference/Source/Base/Game/ClientCore/ClientManager.cs` (line 560+)

Search for methods starting with `send` to find all available commands. Here's the complete list as of the current game version:

### Unit Commands (ClientManager)
| Method | Signature |
|--------|-----------|
| `sendMoveUnit` | `(Unit pUnit, Tile pTile, bool bMarch, bool bQueue, Tile pWaypoint = null)` |
| `sendAttack` | `(Unit pUnit, Tile pTile)` |
| `sendFortify` | `(Unit pUnit)` |
| `sendHeal` | `(Unit pUnit, bool bAuto)` |
| `sendMarch` | `(Unit pUnit)` |
| `sendWake` | `(Unit pUnit)` |
| `sendPass` | `(Unit pUnit)` |
| `sendSleep` | `(Unit pUnit)` |
| `sendSentry` | `(Unit pUnit)` |
| `sendLock` | `(Unit pUnit)` |
| `sendPillage` | `(Unit pUnit)` |
| `sendBurn` | `(Unit pUnit)` |
| `sendDisband` | `(Unit pUnit, bool bForce)` |
| `sendFoundCity` | `(Unit pUnit, FamilyType eFamily, NationType eNation)` |
| `sendJoinCity` | `(Unit pUnit)` |
| `sendAddRoad` | `(Unit pUnit, bool bBuyGoods, bool bQueue, Tile pTile)` |
| `sendBuildImprovement` | `(Unit pUnit, ImprovementType eImprovement, bool bBuyGoods, bool bQueue, Tile pTile)` |
| `sendUpgradeImprovement` | `(Unit pUnit, bool bBuyGoods)` |
| `sendPromote` | `(Unit pUnit, PromotionType ePromotion)` |
| `sendUpgrade` | `(Unit pUnit, UnitType eUnit, bool bBuyGoods)` |
| `sendSpreadReligion` | `(Unit pUnit, int iCityID)` |

### City Commands (ClientManager)
| Method | Signature |
|--------|-----------|
| `sendBuildUnit` | `(City pCity, UnitType eUnit, bool bBuyGoods, Tile pTile, bool bFirst)` |
| `sendBuildProject` | `(City pCity, ProjectType eProject, bool bBuyGoods, bool bFirst, bool bRepeat)` |
| `sendBuildQueue` | `(City pCity, int iOldSlot, int iNewSlot)` |
| `sendHurryCivics` | `(City pCity)` |
| `sendHurryTraining` | `(City pCity)` |
| `sendHurryMoney` | `(City pCity)` |
| `sendHurryPopulation` | `(City pCity)` |
| `sendHurryOrders` | `(City pCity)` |
| `sendRenameCity` | `(City pCity, string zName)` |
| `sendGiftCity` | `(City pCity, PlayerType ePlayer)` |

### Player Commands (ClientManager)
| Method | Signature |
|--------|-----------|
| `sendResearchTech` | `(TechType eTech)` |
| `sendRedrawTech` | `()` |
| `sendTargetTech` | `(TechType eTech)` |
| `sendMakeDecision` | `(int iID, int iChoice, int iData)` |
| `sendRemoveDecision` | `(int iID)` |
| `sendDiplomacy` | `(PlayerType ePlayer, DiplomacyType eDiplomacy, ...)` |
| `sendEndTurn` | `()` |

### Character Commands (ClientManager)
| Method | Signature |
|--------|-----------|
| `sendPlayerLeader` | `(PlayerType ePlayer, int iCharacterId)` |
| `sendFamilyHead` | `(PlayerType ePlayerType, FamilyType efamilyType, int iCharacterId)` |
| `sendTribeLeader` | `(TribeType eTribe, int iCharacterId)` |
| `sendAddCharacter` | `(CharacterType eCharacter, PlayerType ePlayer, FamilyType eFamily)` |
| `sendNewCharacter` | `(PlayerType ePlayer, FamilyType eFamily, int iAge, int iFillValue)` |
| `sendMakeCharacterDead` | `(int iCharacterId)` |
| `sendAddCharacterTrait` | `(int iCharacterId, TraitType eTrait, bool bRemove)` |
| `sendSetCharacterRating` | `(int iCharacterId, RatingType eRating, int iRating)` |
| `sendSetCharacterFamily` | `(int iCharacterId, FamilyType eFamily)` |
| `sendSetCharacterReligion` | `(ReligionType eReligion, int iCharacterId)` |
| `sendSetCharacterCouncil` | `(int iCharacterId, CouncilType eCouncil)` |

### Low-Level ActionData Approach

For actions without convenience methods, you can construct `ActionData` directly using the factory:

```csharp
ActionData pActionData = manager.ModSettings.Factory.CreateActionData(ActionType.SOME_ACTION, manager.getActivePlayer());
pActionData.addValue(param1);
pActionData.addValue(param2);
manager.sendAction(pActionData);
```

See `Reference/Source/Base/Game/GameCore/Enums.cs` for the full `ActionType` enumeration (90+ action types).

## Security Considerations

### Single-Player Only (Recommended)

The implementation should check for multiplayer and refuse commands:

```csharp
if (game.isMultiplayer())
{
    result.Error = "Commands disabled in multiplayer";
    return result;
}
```

### Localhost Only

Both TCP and HTTP servers bind to `localhost`/`127.0.0.1` only, preventing remote access.

### Validation

All commands go through the game's normal validation via `ClientManager`. Invalid commands (wrong turn, insufficient resources, illegal moves) are rejected by the server-side game logic.

## Testing

### Manual Testing

```bash
# Start game with mod enabled

# Send a move command
curl -X POST http://localhost:9877/command \
  -H "Content-Type: application/json" \
  -d '{"action": "moveUnit", "requestId": "test1", "params": {"unitId": 1, "targetTileId": 100}}'

# Skip a unit
curl -X POST http://localhost:9877/command \
  -H "Content-Type: application/json" \
  -d '{"action": "pass", "requestId": "test2", "params": {"unitId": 1}}'

# End turn
curl -X POST http://localhost:9877/command \
  -H "Content-Type: application/json" \
  -d '{"action": "endTurn", "requestId": "test3", "params": {}}'
```

### Headless Testing

Commands can be tested in headless mode, but note that `canDoActions()` checks may behave differently. The game must be in an active turn state for commands to execute.

## Pre-Validation Endpoints

Pre-validation allows clients to check if an action is valid before sending it. This is useful for:
- Building valid action lists in UI
- Avoiding wasted commands
- Understanding why an action would fail

### HTTP GET /validate

**Request:**
```
GET /validate?action=moveUnit&unitId=42&targetTileId=156
```

**Response:**
```json
{
    "valid": true,
    "reason": null
}
```

Or if invalid:
```json
{
    "valid": false,
    "reason": "Unit has no movement points remaining"
}
```

### Implementation

Add validation endpoint to `HttpRestServer.cs`:

```csharp
case "validate":
    HandleValidateRequest(context, game);
    break;

private void HandleValidateRequest(HttpListenerContext context, Game game)
{
    var query = context.Request.QueryString;
    string action = query["action"]?.ToLowerInvariant();

    var validationResult = ValidateAction(game, action, query);
    SendJsonResponse(context.Response, validationResult);
}
```

Add validation logic to `APIEndpoint.cs`:

```csharp
public static ValidationResult ValidateAction(Game game, string action, NameValueCollection query)
{
    var result = new ValidationResult { Valid = false };
    var player = game.player(game.getPlayerTurn());

    switch (action)
    {
        case "moveunit":
            return ValidateMoveUnit(game, player, query);
        case "attack":
            return ValidateAttack(game, player, query);
        case "buildunit":
            return ValidateBuildUnit(game, player, query);
        case "buildproject":
            return ValidateBuildProject(game, player, query);
        // ... other validations
        default:
            result.Reason = $"Unknown action: {action}";
            return result;
    }
}

private static ValidationResult ValidateMoveUnit(Game game, Player player, NameValueCollection query)
{
    var result = new ValidationResult();

    if (!int.TryParse(query["unitId"], out int unitId))
    {
        result.Reason = "Missing or invalid unitId";
        return result;
    }

    var unit = game.unit(unitId);
    if (unit == null)
    {
        result.Reason = "Unit not found";
        return result;
    }

    if (!unit.canActMove(player))
    {
        result.Reason = "Unit cannot move (no movement points or cannot act)";
        return result;
    }

    if (!int.TryParse(query["targetTileId"], out int tileId))
    {
        result.Reason = "Missing or invalid targetTileId";
        return result;
    }

    var tile = game.tile(tileId);
    if (tile == null)
    {
        result.Reason = "Tile not found";
        return result;
    }

    if (!unit.canOccupyTile(tile, player.getTeam(), bTestTheirUnits: true, bTestOurUnits: true, bBump: false))
    {
        result.Reason = "Unit cannot occupy target tile";
        return result;
    }

    result.Valid = true;
    return result;
}

private static ValidationResult ValidateAttack(Game game, Player player, NameValueCollection query)
{
    var result = new ValidationResult();

    if (!int.TryParse(query["unitId"], out int unitId))
    {
        result.Reason = "Missing or invalid unitId";
        return result;
    }

    var unit = game.unit(unitId);
    if (unit == null)
    {
        result.Reason = "Unit not found";
        return result;
    }

    if (!int.TryParse(query["targetTileId"], out int tileId))
    {
        result.Reason = "Missing or invalid targetTileId";
        return result;
    }

    var tile = game.tile(tileId);
    if (tile == null)
    {
        result.Reason = "Tile not found";
        return result;
    }

    if (!unit.canDamageUnitOrCity(tile))
    {
        result.Reason = "Unit cannot attack target tile";
        return result;
    }

    result.Valid = true;
    return result;
}

private static ValidationResult ValidateBuildUnit(Game game, Player player, NameValueCollection query)
{
    var result = new ValidationResult();

    if (!int.TryParse(query["cityId"], out int cityId))
    {
        result.Reason = "Missing or invalid cityId";
        return result;
    }

    var city = game.city(cityId);
    if (city == null)
    {
        result.Reason = "City not found";
        return result;
    }

    string unitTypeStr = query["unitType"];
    if (string.IsNullOrEmpty(unitTypeStr))
    {
        result.Reason = "Missing unitType";
        return result;
    }

    var infos = game.infos();
    UnitType unitType = UnitType.NONE;
    for (int i = 0; i < (int)infos.unitsNum(); i++)
    {
        if (infos.unit((UnitType)i).mzType.Equals(unitTypeStr, StringComparison.OrdinalIgnoreCase))
        {
            unitType = (UnitType)i;
            break;
        }
    }

    if (unitType == UnitType.NONE)
    {
        result.Reason = $"Unknown unit type: {unitTypeStr}";
        return result;
    }

    if (!city.canBuildUnit(unitType, bBuyGoods: false, bTestEnabled: true))
    {
        result.Reason = "City cannot build this unit type";
        return result;
    }

    result.Valid = true;
    return result;
}

private static ValidationResult ValidateBuildProject(Game game, Player player, NameValueCollection query)
{
    var result = new ValidationResult();

    if (!int.TryParse(query["cityId"], out int cityId))
    {
        result.Reason = "Missing or invalid cityId";
        return result;
    }

    var city = game.city(cityId);
    if (city == null)
    {
        result.Reason = "City not found";
        return result;
    }

    string projectTypeStr = query["projectType"];
    if (string.IsNullOrEmpty(projectTypeStr))
    {
        result.Reason = "Missing projectType";
        return result;
    }

    var infos = game.infos();
    ProjectType projectType = ProjectType.NONE;
    for (int i = 0; i < (int)infos.projectsNum(); i++)
    {
        if (infos.project((ProjectType)i).mzType.Equals(projectTypeStr, StringComparison.OrdinalIgnoreCase))
        {
            projectType = (ProjectType)i;
            break;
        }
    }

    if (projectType == ProjectType.NONE)
    {
        result.Reason = $"Unknown project type: {projectTypeStr}";
        return result;
    }

    if (!city.canBuildProject(projectType, bBuyGoods: false, bTestEnabled: true))
    {
        result.Reason = "City cannot build this project";
        return result;
    }

    result.Valid = true;
    return result;
}

public class ValidationResult
{
    public bool Valid { get; set; }
    public string Reason { get; set; }
}
```

### Key Validation Methods by Entity

**Source:** These methods are defined on `Unit`, `City`, and `Player` classes.

| Entity | Method | Purpose |
|--------|--------|---------|
| Unit | `canActMove(Player, int moves)` | Can unit move? |
| Unit | `canAct(Player, int cost)` | Can unit perform actions? |
| Unit | `canAttackUnitOrCity(Tile, Player)` | Can attack target? |
| Unit | `canFortify(Player)` | Can fortify? |
| Unit | `canPillage(Tile, Player)` | Can pillage? |
| Unit | `canFoundCity(Tile, Player, FamilyType)` | Can found city? |
| Unit | `canBuildImprovement(Tile, ImprovementType, Player, ...)` | Can build improvement? |
| City | `canBuildUnit(UnitType, bool buyGoods)` | Can produce unit? |
| City | `canBuildProject(ProjectType, bool buyGoods)` | Can build project? |
| City | `canHurryCivics()` | Can rush with civics? |
| City | `canHurryMoney()` | Can rush with gold? |
| Player | `canResearch(TechType)` | Can research tech? |
| Player | `canEndTurn()` | Can end turn? |
| Player | `canGiftCity(City, PlayerType)` | Can gift city? |

---

## Bulk Commands

Bulk commands allow sending multiple commands in a single request, useful for:
- Executing a planned sequence atomically
- Reducing network round-trips
- Ensuring command ordering

### HTTP POST /commands (plural)

**Request:**
```json
{
    "commands": [
        {"action": "moveUnit", "params": {"unitId": 42, "targetTileId": 156}},
        {"action": "attack", "params": {"unitId": 42, "targetTileId": 157}},
        {"action": "fortify", "params": {"unitId": 42}}
    ],
    "requestId": "batch-001",
    "stopOnError": true
}
```

**Response:**
```json
{
    "requestId": "batch-001",
    "results": [
        {"index": 0, "success": true, "error": null},
        {"index": 1, "success": true, "error": null},
        {"index": 2, "success": true, "error": null}
    ],
    "allSucceeded": true
}
```

Or with `stopOnError: true` and a failure:
```json
{
    "requestId": "batch-001",
    "results": [
        {"index": 0, "success": true, "error": null},
        {"index": 1, "success": false, "error": "Unit has no attacks remaining"}
    ],
    "allSucceeded": false,
    "stoppedAtIndex": 1
}
```

### Implementation

Add bulk command handler to `HttpRestServer.cs`:

```csharp
case "commands":
    HandleBulkCommandRequest(context);
    break;

private void HandleBulkCommandRequest(HttpListenerContext context)
{
    try
    {
        using (var reader = new System.IO.StreamReader(context.Request.InputStream))
        {
            string body = reader.ReadToEnd();
            var bulkCmd = JsonConvert.DeserializeObject<BulkCommand>(body, _jsonSettings);

            if (bulkCmd?.Commands == null || bulkCmd.Commands.Count == 0)
            {
                SendErrorResponse(context.Response, "No commands provided", 400);
                return;
            }

            var waitHandle = new ManualResetEventSlim(false);
            BulkCommandResult result = null;

            APIEndpoint.QueueBulkCommand(bulkCmd, r =>
            {
                result = r;
                waitHandle.Set();
            });

            // Longer timeout for bulk commands
            if (waitHandle.Wait(30000))
            {
                SendJsonResponse(context.Response, result);
            }
            else
            {
                SendErrorResponse(context.Response, "Bulk command execution timeout", 504);
            }
        }
    }
    catch (Exception ex)
    {
        SendErrorResponse(context.Response, $"Error processing bulk command: {ex.Message}", 400);
    }
}
```

Add bulk command processing to `APIEndpoint.cs`:

```csharp
public class BulkCommand
{
    public List<GameCommand> Commands { get; set; }
    public string RequestId { get; set; }
    public bool StopOnError { get; set; } = true;
}

public class BulkCommandResult
{
    public string RequestId { get; set; }
    public List<BulkCommandItemResult> Results { get; set; } = new();
    public bool AllSucceeded { get; set; }
    public int? StoppedAtIndex { get; set; }
}

public class BulkCommandItemResult
{
    public int Index { get; set; }
    public bool Success { get; set; }
    public string Error { get; set; }
}

private static readonly ConcurrentQueue<(BulkCommand cmd, Action<BulkCommandResult> callback)> _bulkCommandQueue = new();

public static void QueueBulkCommand(BulkCommand cmd, Action<BulkCommandResult> callback)
{
    _bulkCommandQueue.Enqueue((cmd, callback));
}

// In OnClientUpdate(), add bulk command processing:
while (_bulkCommandQueue.TryDequeue(out var bulkItem))
{
    var (bulkCmd, callback) = bulkItem;
    var bulkResult = ExecuteBulkCommand(manager, bulkCmd);
    callback?.Invoke(bulkResult);
}

private static BulkCommandResult ExecuteBulkCommand(ClientManager manager, BulkCommand bulkCmd)
{
    var result = new BulkCommandResult
    {
        RequestId = bulkCmd.RequestId,
        AllSucceeded = true
    };

    var game = manager.GameClient;

    for (int i = 0; i < bulkCmd.Commands.Count; i++)
    {
        var cmd = bulkCmd.Commands[i];
        var cmdResult = ExecuteCommand(manager, game, cmd);

        result.Results.Add(new BulkCommandItemResult
        {
            Index = i,
            Success = cmdResult.Success,
            Error = cmdResult.Error
        });

        if (!cmdResult.Success)
        {
            result.AllSucceeded = false;
            if (bulkCmd.StopOnError)
            {
                result.StoppedAtIndex = i;
                break;
            }
        }
    }

    return result;
}
```

---

## References

- `Reference/Source/Base/Game/ClientCore/ClientManager.cs` - All send methods (line 560+)
- `Reference/Source/Base/Game/GameCore/ActionData.cs` - Action data structure
- `Reference/Source/Base/Game/GameCore/Enums.cs` - ActionType enumeration
- `docs/old-world-command-api-reference.md` - Full command API documentation

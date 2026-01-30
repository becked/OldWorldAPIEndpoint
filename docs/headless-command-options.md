# Headless Command Execution Options Analysis

This document analyzes three approaches for enabling command execution in headless mode, where `ClientManager` is not available.

## Background

Currently, commands work only in GUI mode because they go through `ClientManager`, which is client-side infrastructure:

```
HTTP POST → CommandExecutor → ClientManager.sendXxx() → Network → Server
```

In headless mode, there is no client - we have direct access to the server-side `Game` object via `LocalGameServer.LocalGame`.

---

## Option 1: Connect as Headless Client

**Concept:** Create a network client that connects to the game server without a UI, using the same protocol that GUI clients use.

### How It Would Work

```
Our Mod (Headless Client)
    ↓
Connect via TCP/Steam networking
    ↓
Serialize ActionData with BinaryFormatter
    ↓
Send to Game Server
    ↓
Server calls handleAction()
```

### Implementation Requirements

1. **Reverse-engineer network protocol** - Old World uses a custom binary protocol:
   - `BinaryFormatter` serializes `ActionData` objects
   - Sent as byte arrays over TCP
   - Session management (authentication, player claiming)

2. **Implement client connection**:
   ```csharp
   // Pseudocode - actual implementation unknown
   class HeadlessClient : IClientNetwork
   {
       void Connect(string host, int port);
       void sendToServer(byte[] bytes);
       void OnMessageReceived(byte[] bytes);
   }
   ```

3. **Handle authentication/session**:
   - Claim a player slot
   - Handle turn synchronization
   - Process server responses

### Pros

| Aspect | Rating | Details |
|--------|--------|---------|
| **Correctness** | Excellent | Uses exact same code path as real clients |
| **Multiplayer support** | Yes | Could work with networked games |
| **Validation** | Full | Server validates everything |
| **Undo/Replay** | Yes | Actions recorded in MessageStore |

### Cons

| Aspect | Rating | Details |
|--------|--------|---------|
| **Performance** | Poor | Serialization + network overhead per command |
| **Maintainability** | Poor | Network protocol undocumented, may change between versions |
| **Implementation effort** | Very High | Need to reverse-engineer protocol, handle sessions |
| **Code clarity** | Moderate | Clean separation but complex setup |
| **Testing** | Hard | Requires server to be running |

### Performance Estimate

```
Per command:
  - ActionData creation: ~0.1ms
  - BinaryFormatter serialize: ~0.5-1ms
  - Network send: ~1-5ms (localhost)
  - Server deserialize: ~0.5-1ms
  - Execution: varies
  - Response: ~1-5ms
Total: ~5-15ms per command
```

### Maintainability Risk

**HIGH RISK** - The network protocol is internal to Old World and undocumented. Any game update could:
- Change message format
- Add authentication requirements
- Change serialization approach
- Break compatibility silently

---

## Option 2: Direct Server-Side Execution

**Concept:** Call game state mutation methods directly on `Game`, `Unit`, `City`, `Player` objects without going through the network layer.

### How It Would Work

```
HTTP POST → CommandExecutor → Direct method calls
                                    ↓
                              game.unit(id).moveTo(tile, player, ...)
                              game.player(id).endTurn(force)
                              game.city(id).setBuildUnit(type, ...)
```

### Implementation

Looking at `Game.handleAction()`, we can see the exact code the server runs. We replicate it:

```csharp
// Server-side handleAction for MOVE_UNIT (from Game.cs:17300)
case ActionType.MOVE_UNIT:
{
    Unit pUnit = unit((int)pActionData.getValue(0));
    Tile pToTile = tile((int)pActionData.getValue(2));
    bool bMarch = (bool)pActionData.getValue(3);

    if (pUnit.canActMove(pPlayer, 1, bMarch))
    {
        pUnit.moveTo(pToTile, pPlayer, bMarch, pWaypoint, preMove);
    }
}

// Our direct equivalent:
public CommandResult ExecuteMoveUnit(Game game, GameCommand cmd)
{
    var unit = game.unit(GetInt(cmd, "unitId"));
    var toTile = game.tile(GetInt(cmd, "targetTileId"));
    var player = game.player(game.getPlayerTurn());
    bool march = GetBool(cmd, "march", false);

    if (unit != null && unit.canActMove(player, 1, march))
    {
        unit.moveTo(toTile, player, march, null, null);
        return Success();
    }
    return Error("Cannot move unit");
}
```

### Key Methods Available (from reference code)

| Entity | Method | Signature |
|--------|--------|-----------|
| `Unit` | `moveTo` | `(Tile pToTile, Player pActingPlayer, bool bFull, Tile pWaypoint, Action preMove)` |
| `Unit` | `attackUnitOrCity` | `(Tile pTile, Player pActingPlayer)` |
| `Unit` | `fortify` | `(Player pActingPlayer)` |
| `Unit` | `togglePass` | `()` |
| `Unit` | `toggleSleep` | `()` |
| `Unit` | `disband` | `(bool bVoluntary)` |
| `Player` | `endTurn` | `(bool bForce)` |
| `Player` | `setTechTarget` | `(TechType eTech, bool bTarget)` |
| `City` | `setBuildUnit` | via build queue methods |

### Pros

| Aspect | Rating | Details |
|--------|--------|---------|
| **Performance** | Excellent | Direct method calls, no serialization |
| **Implementation effort** | Moderate | Straightforward once patterns understood |
| **Code clarity** | Good | Clear mapping from command to method |
| **Testing** | Easy | Works in headless mode directly |

### Cons

| Aspect | Rating | Details |
|--------|--------|---------|
| **Maintainability** | Moderate | Method signatures may change, but game reference available |
| **Multiplayer support** | No | Only works for local game server |
| **Undo/Replay** | No | Actions not recorded in MessageStore |
| **Validation duplication** | Partial | Must replicate server validation logic |

### Performance Estimate

```
Per command:
  - Parameter extraction: ~0.01ms
  - Direct method call: ~0.1-1ms (depends on action)
Total: ~0.1-1ms per command (10-100x faster than Option 1)
```

### Maintainability Risk

**MODERATE RISK** - Method signatures are in `TenCrowns.GameCore.dll` which we can reference. The reference code shows:
- Methods are `virtual` (stable interfaces)
- Parameters are game types we can inspect
- Breaking changes would be visible in updated reference code

---

## Option 3: ActionData via handleAction()

**Concept:** Create `ActionData` objects and call `Game.handleAction()` directly, using the server's own dispatch mechanism but bypassing network serialization.

### How It Would Work

```
HTTP POST → CommandExecutor → Create ActionData
                                    ↓
                              game.handleAction(actionData, connectionId: -1)
```

### Implementation

```csharp
public CommandResult ExecuteViaActionData(Game game, GameCommand cmd)
{
    // Create ActionData exactly as ClientManager does
    var actionData = new ActionData(ActionType.MOVE_UNIT, game.getPlayerTurn());
    actionData.addValue(GetInt(cmd, "unitId"));           // 0: unit ID
    actionData.addValue(GetInt(cmd, "fromTileId"));       // 1: from tile
    actionData.addValue(GetInt(cmd, "targetTileId"));     // 2: to tile
    actionData.addValue(GetBool(cmd, "march", false));    // 3: march
    actionData.addValue(false);                           // 4: queue
    actionData.addValue(-1);                              // 5: waypoint (-1 = none)

    // Call server's handler directly
    // Note: handleAction is protected, needs reflection
    var method = typeof(Game).GetMethod("handleAction",
        BindingFlags.NonPublic | BindingFlags.Instance);
    method.Invoke(game, new object[] { actionData, -1 });

    return Success();
}
```

### ActionData Parameter Reference (from Game.handleAction)

| Action | Parameters |
|--------|------------|
| `MOVE_UNIT` | 0:unitId, 1:fromTileId, 2:toTileId, 3:march, 4:queue, 5:waypointId |
| `ATTACK` | 0:unitId, 1:targetTileId |
| `FORTIFY` | 0:unitId |
| `PASS` | 0:unitId |
| `END_TURN` | 0:turn, 1:force |
| `RESEARCH_TECH` | 0:techType |
| `BUILD_UNIT` | 0:cityId, 1:unitType, 2:hurry, 3:rallyTileId |

### Pros

| Aspect | Rating | Details |
|--------|--------|---------|
| **Correctness** | Excellent | Uses exact server dispatch logic |
| **Validation** | Full | Server's handleAction has all validation |
| **Undo/Replay** | Yes | `addUndoMark()` called by handleAction |
| **Code clarity** | Good | Mirrors how ClientManager works |
| **Single code path** | Yes | Both GUI and headless use same execution |

### Cons

| Aspect | Rating | Details |
|--------|--------|---------|
| **Performance** | Good | Small overhead vs Option 2 |
| **Maintainability** | Moderate | Parameter order may change between versions |
| **Reflection needed** | Yes | `handleAction` is protected |
| **Multiplayer** | No | Local server only |

### Performance Estimate

```
Per command:
  - ActionData creation: ~0.1ms
  - Reflection invoke: ~0.05ms
  - handleAction dispatch: ~0.1-1ms
Total: ~0.2-1.2ms per command
```

### Maintainability Risk

**MODERATE RISK** - ActionData parameter order is implicit (positional). Changes require:
- Checking `Game.handleAction()` switch cases in new versions
- Reference code makes this discoverable

---

## Comparison Matrix

| Criteria | Option 1: Network Client | Option 2: Direct Methods | Option 3: ActionData |
|----------|-------------------------|-------------------------|---------------------|
| **Performance** | Poor (5-15ms) | Excellent (0.1-1ms) | Good (0.2-1.2ms) |
| **Maintainability** | Poor (undocumented) | Moderate (method sigs) | Moderate (param order) |
| **Implementation effort** | Very High | Moderate | Moderate |
| **Code clarity** | Moderate | Good | Good |
| **Validation** | Full (server) | Manual | Full (server) |
| **Undo/Replay support** | Yes | No | Yes |
| **Multiplayer support** | Possible | No | No |
| **Error handling** | Complex (async) | Simple | Simple |

---

## Recommendation

### For Headless Testing: **Option 3 (ActionData via handleAction)**

**Rationale:**
1. **Same code path as GUI** - Reduces divergence between modes
2. **Full validation** - Server's built-in checks apply
3. **Undo marks** - Maintains game state consistency
4. **Good performance** - Minimal overhead
5. **Maintainable** - Parameter order discoverable from reference code

### Hybrid Approach (Best of Both Worlds)

```csharp
public class CommandExecutor
{
    public CommandResult Execute(Game game, object clientManager, GameCommand cmd)
    {
        if (clientManager != null)
        {
            // GUI mode: use ClientManager (current implementation)
            return ExecuteViaClientManager(clientManager, cmd);
        }
        else
        {
            // Headless mode: use ActionData + handleAction
            return ExecuteViaActionData(game, cmd);
        }
    }
}
```

This provides:
- **GUI mode**: Uses proper client-server flow for networked games
- **Headless mode**: Uses server dispatch for testing
- **Single API**: Same command format works in both modes
- **Testable**: Can verify commands work without launching GUI

---

## Implementation Estimate

| Option | Files to Modify | New Files | Complexity |
|--------|-----------------|-----------|------------|
| Option 1 | 2-3 | 4-5 | High |
| Option 2 | 1-2 | 0-1 | Low |
| Option 3 | 1-2 | 0-1 | Low-Medium |

### For Option 3 specifically:

1. **Modify `CommandExecutor.cs`**:
   - Add `ExecuteViaActionData()` method
   - Add parameter builders for each ActionType
   - Use reflection to call `handleAction()`

2. **Modify `APIEndpoint.cs`**:
   - Detect headless mode (ClientManager == null)
   - Route to appropriate execution path

3. **Test coverage**:
   - All existing commands work in headless mode
   - Use `./test-headless.sh` to verify

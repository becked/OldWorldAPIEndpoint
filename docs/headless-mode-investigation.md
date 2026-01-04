# Old World Headless Mode Reference

This document covers Old World's headless/batch mode for automated game simulation.

## Overview

Old World includes a headless AI feature that runs saved games in the background with AI-controlled turns. Use cases:

- **Mod debugging**: Test mods without playing manually
- **Challenge runs**: Let AI play N turns, then continue from that point
- **Automated testing**: Simulate game progression programmatically

## Command Line Syntax

```bash
OldWorld <savefile> -batchmode -headless -autorunturns <N>
```

| Argument | Description |
|----------|-------------|
| `<savefile>` | Path to save file (positional argument, not a flag) |
| `-batchmode` | Unity batch mode - required for headless operation |
| `-headless` | Disables graphics rendering |
| `-autorunturns <N>` | Run N turns with AI control, then exit |

### Examples

```bash
# macOS
arch -x86_64 "/path/to/OldWorld.app/Contents/MacOS/OldWorld" \
    /path/to/save.zip -batchmode -headless -autorunturns 5

# Windows (via OldWorldAutorun.bat)
OldWorld.exe MySave.zip -batchmode -headless -autorunturns 10
```

### Windows Batch File

Old World includes `OldWorldAutorun.bat` in the installation directory:

```batch
@echo off
set /p loadfile="Filename: "
set /p maxturns="Turns: "
START /B OldWorld.exe %loadfile% -batchmode -headless -autorunturns %maxturns%
```

## Behavior

When running in headless mode:

1. Game loads the specified save file
2. All players are controlled by AI
3. Turns advance automatically
4. Auto-saves are created in `Saves/Auto/` directory
5. Game exits after completing the specified number of turns

### Output

```
[App] Loaded game from /path/to/save.zip
```

Auto-saves appear as:
- `OW-Save-Auto-2.zip`
- `OW-Save-Auto-3.zip`
- etc.

## Mod Hook Behavior

| Hook | Fires in Headless? |
|------|-------------------|
| `Initialize()` | Yes |
| `OnClientUpdate()` | Yes |
| `OnNewTurnServer()` | Yes |
| `OnGameServerReady()` | Yes |
| `Shutdown()` | Yes |

### Game Access in Headless Mode

In headless mode, `AppMain.gApp.Client.Game` returns null. Instead, access the Game through:

```csharp
AppMain.gApp.GetLocalGameServer().LocalGame
```

Note: `LocalGame` is a **non-public property**, requiring `BindingFlags.NonPublic`:

```csharp
var gameServer = _getLocalGameServerMethod.Invoke(appMain, null);
var localGameProp = gameServer.GetType().GetProperty("LocalGame",
    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
var game = localGameProp.GetValue(gameServer) as Game;
```

This path also works in GUI mode, making it a universal solution.

## Technical Details

### Unity Flags

- `-batchmode`: Runs Unity without creating a window, disables interactive features
- `-headless`: Uses null graphics device, skips GPU initialization

### Game Architecture

```
AppMain (static gApp field)
├── Headless (bool) = true in headless mode
├── Client
│   └── Game          ← works in GUI mode only
└── GetLocalGameServer()
    └── GameServerBehaviour
        └── LocalGame ← works in both GUI and headless mode (non-public)
```

### Process Flow

1. Unity initializes with null graphics device
2. Game processes command line, identifies save file
3. Mods load, `Initialize()` called
4. Save file loads: `[App] Loaded game from...`
5. Game loop runs N turns
6. Auto-saves created each turn
7. `Shutdown()` called, process exits

## References

- [Steam Discussion: How to use headless AI feature?](https://steamcommunity.com/app/597180/discussions/0/591756872987518878/)
- [Mohawk Games Test Build Notes (2024.11.06)](https://github.com/MohawkGames/test_buildnotes/blob/main/Old%20World%20Test%20update%202024.11.06)
- `OldWorldAutorun.bat` - Windows batch file in game installation directory

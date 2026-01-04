# Old World API Endpoint - Technical Design

## Overview

An API endpoint mod that broadcasts live game state from Old World over TCP, enabling companion applications to provide real-time statistics and analysis.

**Components:**
1. **Old World API Endpoint Mod (C#)** - Extracts game state and broadcasts over TCP using Harmony patches
2. **Tauri App (Rust + TypeScript)** - Receives state, computes analytics, displays UI

**Platforms:** macOS, Linux, Windows

**Mod Compatibility:** Uses Harmony library for patching instead of GameFactory override, ensuring compatibility with other mods.

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                              OLD WORLD GAME                             │
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐  │
│  │                   OldWorldAPIEndpoint.dll                         │  │
│  │                                                                   │  │
│  │  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────────┐  │  │
│  │  │  EntryPoint     │  │  HarmonyPatches │  │  GameStateTracker│  │  │
│  │  │  - Initialize   │──│  - Game.doTurn  │──│  - BuildHistory  │  │  │
│  │  │  - ApplyPatches │  │  - finishBuild  │  │  - UnitOrigins   │  │  │
│  │  └─────────────────┘  └─────────────────┘  └────────┬─────────┘  │  │
│  │                                                     │            │  │
│  │  ┌─────────────────────────────────────────────────┐│            │  │
│  │  │              TcpBroadcastService                ││            │  │
│  │  │  - Per-client send queues                       │◄────────────┘  │
│  │  │  - Length-prefixed framing                      │             │  │
│  │  │  - Backpressure handling                        │             │  │
│  │  └───────────────────────┬─────────────────────────┘             │  │
│  └──────────────────────────┼────────────────────────────────────────┘  │
└─────────────────────────────┼───────────────────────────────────────────┘
                              │ TCP localhost:9876
                              ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                         TAURI COMPANION APP                             │
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐  │
│  │                      Rust Backend                                 │  │
│  │  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────────┐  │  │
│  │  │  TcpClient      │  │  StateManager   │  │  Analytics       │  │  │
│  │  │  - Connect      │──│  - Current      │──│  - Projections   │  │  │
│  │  │  - Reconnect    │  │  - History      │  │  - Trends        │  │  │
│  │  └─────────────────┘  └────────┬────────┘  └──────────────────┘  │  │
│  └────────────────────────────────┼──────────────────────────────────┘  │
│                                   │ Tauri Events                        │
│  ┌────────────────────────────────▼──────────────────────────────────┐  │
│  │                    TypeScript Frontend                            │  │
│  │  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────────┐  │  │
│  │  │  Dashboard      │  │  Player Stats   │  │  Charts          │  │  │
│  │  └─────────────────┘  └─────────────────┘  └──────────────────┘  │  │
│  └───────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Part 1: Old World Mod

### 1.1 File Structure

```
OldWorldAPIEndpoint/
├── ModInfo.xml
├── 0Harmony.dll
├── OldWorldAPIEndpoint.dll
└── Source/
    ├── APIEndpoint.cs
    ├── GameStateTracker.cs
    ├── Patches/
    │   ├── GamePatches.cs
    │   └── CityPatches.cs
    ├── Networking/
    │   ├── TcpBroadcastService.cs
    │   ├── ClientConnection.cs
    │   └── MessageFraming.cs
    ├── Serialization/
    │   └── JsonSerializer.cs
    ├── DTOs/
    │   ├── GameStateMessage.cs
    │   ├── Snapshots.cs
    │   └── BuildRecord.cs
    └── Collections/
        └── RingBuffer.cs
```

### 1.2 ModInfo.xml

```xml
<?xml version="1.0"?>
<ModInfo xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <displayName>Old World API Endpoint</displayName>
  <description>Broadcasts live game state over TCP for companion applications to provide real-time statistics and analysis. This mod uses Harmony for compatibility with other mods.</description>
  <modpicture>OldWorldAPIEndpoint.png</modpicture>
  <author>YourName</author>
  <modplatform>Workshop</modplatform>
  <modversion>1.0.0</modversion>
  <modbuild>1.0.80908</modbuild>
  <tags>Utility</tags>
  <singlePlayer>true</singlePlayer>
  <multiplayer>true</multiplayer>
  <scenario>false</scenario>
  <scenarioToggle>false</scenarioToggle>
  <blocksMods>false</blocksMods>
  <modDependencies />
  <modIncompatibilities />
  <modWhitelist />
  <gameContentRequired>NONE</gameContentRequired>
</ModInfo>
```

### 1.3 Entry Point

```csharp
using System;
using HarmonyLib;
using TenCrowns.AppCore;
using TenCrowns.GameCore;
using UnityEngine;

namespace OldWorldAPIEndpoint
{
    public class APIEndpoint : ModEntryPointAdapter
    {
        private static Harmony _harmony;
        private TcpBroadcastService _broadcastService;

        public override void Initialize(ModSettings modSettings)
        {
            base.Initialize(modSettings);

            try
            {
                // Initialize Harmony
                _harmony = new Harmony("com.oldworld.apiendpoint");
                _harmony.PatchAll();
                Debug.Log("[OldWorldAPIEndpoint] Harmony patches applied");

                // Initialize dispatcher for main thread callbacks
                MainThreadDispatcher.Initialize();

                // Start TCP service
                _broadcastService = TcpBroadcastService.Instance;
                _broadcastService.Start(port: 9876);
                _broadcastService.OnClientConnected += HandleClientConnected;

                Debug.Log("[OldWorldAPIEndpoint] Initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OldWorldAPIEndpoint] Initialization failed: {ex}");
            }
        }

        private void HandleClientConnected(int clientId)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                var tracker = GameStateTracker.Instance;
                tracker?.SendFullStateTo(clientId);
            });
        }

        public override void Shutdown()
        {
            _broadcastService?.Stop();
            _harmony?.UnpatchSelf();
            base.Shutdown();
        }

        public override void OnExitGame()
        {
            _broadcastService?.Broadcast(new GameStateMessage
            {
                Type = MessageType.GameEnded,
                Timestamp = DateTime.UtcNow
            });
        }
    }
}
```

### 1.4 Game State Tracker

Static state management since we can't subclass Game/City with Harmony.

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using TenCrowns.GameCore;
using UnityEngine;

namespace OldWorldAPIEndpoint
{
    public class GameStateTracker
    {
        private static GameStateTracker _instance;
        public static GameStateTracker Instance => _instance ??= new GameStateTracker();

        private const int MaxBuildHistory = 1000;

        private readonly RingBuffer<BuildRecord> _buildHistory = new(MaxBuildHistory);
        private readonly Dictionary<int, BuildRecord> _unitIdToOrigin = new();
        private string _sessionId = Guid.NewGuid().ToString("N")[..8];
        private long _sequence = 0;
        private Game _currentGame;

        public void SetCurrentGame(Game game)
        {
            _currentGame = game;
        }

        public void ResetSession()
        {
            _sessionId = Guid.NewGuid().ToString("N")[..8];
            _sequence = 0;
            _buildHistory.Clear();
            _unitIdToOrigin.Clear();
        }

        public void RecordBuild(BuildRecord record)
        {
            _buildHistory.Add(record);
            if (record.UnitId.HasValue)
            {
                _unitIdToOrigin[record.UnitId.Value] = record;
            }
        }

        public BuildRecord GetUnitOrigin(int unitId)
        {
            return _unitIdToOrigin.TryGetValue(unitId, out var record) ? record : null;
        }

        public void BroadcastTurnUpdate()
        {
            if (_currentGame == null) return;
            if (!TcpBroadcastService.Instance.HasConnections) return;

            try
            {
                var message = new GameStateMessage
                {
                    Version = ProtocolVersion.Current,
                    Type = MessageType.TurnUpdate,
                    SessionId = _sessionId,
                    Sequence = ++_sequence,
                    Timestamp = DateTime.UtcNow,
                    Payload = CreateTurnSnapshot()
                };

                TcpBroadcastService.Instance.Broadcast(message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OldWorldAPIEndpoint] Broadcast failed: {ex.Message}");
            }
        }

        public void SendFullStateTo(int clientId)
        {
            if (_currentGame == null) return;

            var message = new GameStateMessage
            {
                Version = ProtocolVersion.Current,
                Type = MessageType.FullState,
                SessionId = _sessionId,
                Sequence = ++_sequence,
                Timestamp = DateTime.UtcNow,
                Payload = CreateFullSnapshot()
            };

            TcpBroadcastService.Instance.SendTo(clientId, message);
        }

        private TurnSnapshot CreateTurnSnapshot()
        {
            return new TurnSnapshot
            {
                Turn = _currentGame.getTurn(),
                Year = _currentGame.getYear(),
                Players = ExtractPlayers(),
                Units = ExtractUnits(),
                Cities = ExtractCities(),
                BuildHistory = _buildHistory.ToList()
            };
        }

        private GameSnapshot CreateFullSnapshot()
        {
            return new GameSnapshot
            {
                GameInfo = new GameInfo
                {
                    Turn = _currentGame.getTurn(),
                    Year = _currentGame.getYear(),
                    MapWidth = _currentGame.getMapWidth(),
                    MapHeight = _currentGame.getMapHeight(),
                    NumPlayers = _currentGame.getNumPlayers()
                },
                Players = ExtractPlayers(),
                Units = ExtractUnits(),
                Cities = ExtractCities(),
                BuildHistory = _buildHistory.ToList()
            };
        }

        private List<PlayerData> ExtractPlayers()
        {
            var infos = _currentGame.infos();
            return _currentGame.getPlayers()
                .Where(p => p != null && p.isAlive())
                .Select(p => new PlayerData
                {
                    Id = (int)p.getPlayer(),
                    Name = p.getName(),
                    IsHuman = p.isHuman(),
                    // Yields use getYieldStockpileWhole() with yield type from infos().Globals
                    Money = p.getMoneyWhole(),
                    MoneyPerTurn = p.calculateYield(infos.Globals.MONEY_YIELD),
                    Civics = p.getYieldStockpileWhole(infos.Globals.CIVICS_YIELD),
                    CivicsPerTurn = p.calculateYield(infos.Globals.CIVICS_YIELD),
                    Training = p.getYieldStockpileWhole(infos.Globals.TRAINING_YIELD),
                    TrainingPerTurn = p.calculateYield(infos.Globals.TRAINING_YIELD),
                    Science = p.getYieldStockpileWhole(infos.Globals.SCIENCE_YIELD),
                    SciencePerTurn = p.calculateYield(infos.Globals.SCIENCE_YIELD),
                    Orders = p.getYieldStockpileWhole(infos.Globals.ORDERS_YIELD),
                    NumUnits = p.getNumUnits(),
                    NumCities = p.getNumCities()
                }).ToList();
        }

        private List<UnitData> ExtractUnits()
        {
            return _currentGame.getUnits()
                .Where(u => u != null && u.isAlive())
                .Select(u =>
                {
                    var origin = GetUnitOrigin(u.getID());
                    return new UnitData
                    {
                        Id = u.getID(),
                        Type = _currentGame.infos().unit(u.getType()).mzType,
                        PlayerId = (int)u.getPlayer(),
                        CreateTurn = u.getCreateTurn(),
                        OriginCityId = origin?.CityId,
                        OriginCityName = origin?.CityName,
                        TileX = u.tile()?.getX() ?? -1,
                        TileY = u.tile()?.getY() ?? -1,
                        Hp = u.getHP(),
                        HpMax = u.getHPMax(),
                        Strength = u.getStrengthRating(),
                        Xp = u.getXP(),
                        Level = u.getLevel(),
                        // Fatigue: getTurnSteps() = steps taken, getFatigueLimit() = max before fatigued
                        TurnSteps = u.getTurnSteps(),
                        FatigueLimit = u.getFatigueLimit(),
                        IsFatigued = u.isFatigued()
                    };
                }).ToList();
        }

        private List<CityData> ExtractCities()
        {
            var infos = _currentGame.infos();
            return _currentGame.getCities()
                .Where(c => c != null)
                .Select(c => new CityData
                {
                    Id = c.getID(),
                    Name = c.getName(),
                    PlayerId = (int)c.getPlayer(),
                    TileX = c.tile()?.getX() ?? -1,
                    TileY = c.tile()?.getY() ?? -1,
                    Citizens = c.getCitizens(),
                    // Growth yield tracks population growth (food equivalent)
                    GrowthProgress = c.getYieldProgress(infos.Globals.GROWTH_YIELD),
                    GrowthThreshold = c.getYieldThresholdWhole(infos.Globals.GROWTH_YIELD),
                    GrowthPerTurn = c.calculateCurrentYield(infos.Globals.GROWTH_YIELD),
                    CurrentBuild = c.getCurrentBuild()?.ToString(),
                    // getCulture() returns CultureType enum, getCultureStep() returns progress level
                    CultureLevel = (int)c.getCulture(),
                    CultureStep = c.getCultureStep(),
                    CultureProgress = c.getYieldProgress(infos.Globals.CULTURE_YIELD),
                    // getHappinessLevel() returns happiness as int (negative = unhappy)
                    HappinessLevel = c.getHappinessLevel()
                }).ToList();
        }
    }
}
```

### 1.5 Harmony Patches

#### Game Patches

```csharp
using HarmonyLib;
using TenCrowns.GameCore;

namespace OldWorldAPIEndpoint.Patches
{
    [HarmonyPatch(typeof(Game))]
    public static class GamePatches
    {
        /// <summary>
        /// Postfix patch on Game.doTurn to broadcast state after each turn.
        /// </summary>
        [HarmonyPatch(nameof(Game.doTurn))]
        [HarmonyPostfix]
        public static void DoTurn_Postfix(Game __instance)
        {
            var tracker = GameStateTracker.Instance;
            tracker.SetCurrentGame(__instance);
            tracker.BroadcastTurnUpdate();
        }

        /// <summary>
        /// Postfix patch on Game.setupNew to reset session on new game.
        /// </summary>
        [HarmonyPatch(nameof(Game.setupNew))]
        [HarmonyPostfix]
        public static void SetupNew_Postfix(Game __instance)
        {
            var tracker = GameStateTracker.Instance;
            tracker.SetCurrentGame(__instance);
            tracker.ResetSession();
        }

        /// <summary>
        /// Postfix patch on Game.loadGame to set game reference on load.
        /// </summary>
        [HarmonyPatch(nameof(Game.loadGame))]
        [HarmonyPostfix]
        public static void LoadGame_Postfix(Game __instance)
        {
            var tracker = GameStateTracker.Instance;
            tracker.SetCurrentGame(__instance);
        }
    }
}
```

#### City Patches - Build History Tracking

Old World doesn't track which city produced military units. We capture this via a Harmony patch on `City.finishBuild`.

```csharp
using System;
using HarmonyLib;
using TenCrowns.GameCore;

namespace OldWorldAPIEndpoint.Patches
{
    [HarmonyPatch(typeof(City))]
    public static class CityPatches
    {
        /// <summary>
        /// Prefix patch to capture build info before finishBuild executes.
        /// </summary>
        [HarmonyPatch(nameof(City.finishBuild))]
        [HarmonyPrefix]
        public static void FinishBuild_Prefix(City __instance, out BuildRecord __state)
        {
            __state = null;

            try
            {
                CityQueueData pCurrentBuild = __instance.getCurrentBuild();
                if (pCurrentBuild == null) return;

                var game = __instance.game();
                var infos = game.infos();

                if (pCurrentBuild.meBuild == infos.Globals.UNIT_BUILD)
                {
                    __state = new BuildRecord
                    {
                        BuildType = BuildRecordType.Unit,
                        ItemType = ((UnitType)pCurrentBuild.miType).ToString(),
                        ItemName = infos.unit((UnitType)pCurrentBuild.miType).mzType,
                        CityId = __instance.getID(),
                        CityName = __instance.getName(),
                        PlayerId = (int)__instance.getPlayer(),
                        Turn = game.getTurn(),
                        Year = game.getYear(),
                        WasHurried = pCurrentBuild.mbHurried
                    };
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[OldWorldAPIEndpoint] FinishBuild_Prefix error: {ex.Message}");
            }
        }

        /// <summary>
        /// Postfix patch to find the newly created unit and record the build.
        /// </summary>
        [HarmonyPatch(nameof(City.finishBuild))]
        [HarmonyPostfix]
        public static void FinishBuild_Postfix(City __instance, BuildRecord __state)
        {
            if (__state == null) return;

            try
            {
                Unit newUnit = FindNewlyCreatedUnit(__instance, __state);
                if (newUnit != null)
                {
                    __state.UnitId = newUnit.getID();
                }

                GameStateTracker.Instance.RecordBuild(__state);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[OldWorldAPIEndpoint] FinishBuild_Postfix error: {ex.Message}");
            }
        }

        private static Unit FindNewlyCreatedUnit(City city, BuildRecord record)
        {
            var game = city.game();
            int currentTurn = game.getTurn();
            UnitType expectedType = (UnitType)Enum.Parse(typeof(UnitType), record.ItemType);

            foreach (Unit unit in game.getUnits())
            {
                if (unit.getCreateTurn() == currentTurn &&
                    unit.getType() == expectedType &&
                    unit.getPlayer() == city.getPlayer() &&
                    IsNearCity(city, unit.tile()))
                {
                    return unit;
                }
            }
            return null;
        }

        private static bool IsNearCity(City city, Tile tile)
        {
            if (tile == null) return false;
            var cityTile = city.game().tile(city.getTileID());
            return tile.getID() == city.getTileID() ||
                   tile.isTileAdjacent(cityTile);
        }
    }
}
```

### 1.6 TCP Broadcast Service

Per-client queues with backpressure handling and length-prefixed message framing.

```csharp
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace OldWorldAPIEndpoint
{
    public class ClientConnection : IDisposable
    {
        private const int MaxQueueSize = 100;

        public int Id { get; }
        private readonly TcpClient _client;
        private readonly NetworkStream _stream;
        private readonly BlockingCollection<byte[]> _sendQueue;
        private readonly Thread _writerThread;
        private readonly CancellationTokenSource _cts;

        public event Action<int> OnDisconnected;

        public ClientConnection(int id, TcpClient client)
        {
            Id = id;
            _client = client;
            _stream = client.GetStream();
            _sendQueue = new BlockingCollection<byte[]>(MaxQueueSize);
            _cts = new CancellationTokenSource();

            _writerThread = new Thread(WriterLoop) { IsBackground = true };
            _writerThread.Start();
        }

        public bool Enqueue(byte[] data)
        {
            if (_sendQueue.IsAddingCompleted) return false;

            // Drop oldest on backpressure
            while (_sendQueue.Count >= MaxQueueSize - 1)
                _sendQueue.TryTake(out _);

            return _sendQueue.TryAdd(data);
        }

        private void WriterLoop()
        {
            try
            {
                foreach (var data in _sendQueue.GetConsumingEnumerable(_cts.Token))
                {
                    _stream.Write(data, 0, data.Length);
                    _stream.Flush();
                }
            }
            catch { }
            finally { OnDisconnected?.Invoke(Id); }
        }

        public void Dispose()
        {
            _sendQueue.CompleteAdding();
            _cts.Cancel();
            try { _client?.Close(); } catch { }
        }
    }

    public class TcpBroadcastService
    {
        private static TcpBroadcastService _instance;
        public static TcpBroadcastService Instance => _instance ??= new TcpBroadcastService();

        private TcpListener _listener;
        private Thread _listenerThread;
        private readonly ConcurrentDictionary<int, ClientConnection> _clients = new();
        private int _clientIdCounter = 0;
        private bool _running = false;

        private static readonly JsonSerializerSettings JsonSettings = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.None
        };

        public event Action<int> OnClientConnected;
        public bool HasConnections => !_clients.IsEmpty;

        public void Start(int port = 9876)
        {
            if (_running) return;
            _running = true;

            _listener = new TcpListener(IPAddress.Loopback, port);
            _listener.Start();

            _listenerThread = new Thread(AcceptLoop) { IsBackground = true };
            _listenerThread.Start();
        }

        public void Stop()
        {
            _running = false;
            _listener?.Stop();
            foreach (var c in _clients.Values) c.Dispose();
            _clients.Clear();
        }

        private void AcceptLoop()
        {
            while (_running)
            {
                try
                {
                    var tcp = _listener.AcceptTcpClient();
                    var id = Interlocked.Increment(ref _clientIdCounter);
                    var conn = new ClientConnection(id, tcp);
                    conn.OnDisconnected += cid => _clients.TryRemove(cid, out _);
                    _clients[id] = conn;
                    OnClientConnected?.Invoke(id);
                }
                catch (SocketException) when (!_running) { break; }
            }
        }

        public void Broadcast(GameStateMessage message)
        {
            if (_clients.IsEmpty) return;
            byte[] data = Frame(message);
            foreach (var c in _clients.Values) c.Enqueue(data);
        }

        public void SendTo(int clientId, GameStateMessage message)
        {
            if (_clients.TryGetValue(clientId, out var c))
                c.Enqueue(Frame(message));
        }

        private byte[] Frame(GameStateMessage message)
        {
            string json = JsonConvert.SerializeObject(message, JsonSettings);
            byte[] payload = Encoding.UTF8.GetBytes(json);
            byte[] framed = new byte[4 + payload.Length];

            // 4-byte big-endian length prefix
            framed[0] = (byte)(payload.Length >> 24);
            framed[1] = (byte)(payload.Length >> 16);
            framed[2] = (byte)(payload.Length >> 8);
            framed[3] = (byte)(payload.Length);

            Buffer.BlockCopy(payload, 0, framed, 4, payload.Length);
            return framed;
        }
    }
}
```

### 1.7 Data Transfer Objects

```csharp
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace OldWorldAPIEndpoint
{
    public static class ProtocolVersion
    {
        public const string Current = "1.0.0";
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum MessageType
    {
        FullState, TurnUpdate, BuildCompleted, GameStarted, GameEnded, GameLoaded, Ping, Error
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum BuildRecordType
    {
        Unit, Project, Specialist
    }

    public class GameStateMessage
    {
        public string Version { get; set; } = ProtocolVersion.Current;
        public MessageType Type { get; set; }
        public string SessionId { get; set; }
        public long Sequence { get; set; }
        public DateTime Timestamp { get; set; }
        public object Payload { get; set; }
    }

    public class GameSnapshot
    {
        public GameInfo GameInfo { get; set; }
        public List<PlayerData> Players { get; set; }
        public List<UnitData> Units { get; set; }
        public List<CityData> Cities { get; set; }
        public List<BuildRecord> BuildHistory { get; set; }
    }

    public class TurnSnapshot
    {
        public int Turn { get; set; }
        public int Year { get; set; }
        public List<PlayerData> Players { get; set; }
        public List<UnitData> Units { get; set; }
        public List<CityData> Cities { get; set; }
        public List<BuildRecord> BuildHistory { get; set; }
    }

    public class GameInfo
    {
        public int Turn { get; set; }
        public int Year { get; set; }
        public int MapWidth { get; set; }
        public int MapHeight { get; set; }
        public int NumPlayers { get; set; }
    }

    public class PlayerData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsHuman { get; set; }
        // Yields - stockpiles and per-turn rates
        public int Money { get; set; }
        public int MoneyPerTurn { get; set; }
        public int Civics { get; set; }
        public int CivicsPerTurn { get; set; }
        public int Training { get; set; }
        public int TrainingPerTurn { get; set; }
        public int Science { get; set; }
        public int SciencePerTurn { get; set; }
        public int Orders { get; set; }
        // Counts
        public int NumUnits { get; set; }
        public int NumCities { get; set; }
    }

    public class UnitData
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public int PlayerId { get; set; }
        public int CreateTurn { get; set; }
        public int? OriginCityId { get; set; }
        public string OriginCityName { get; set; }
        public int TileX { get; set; }
        public int TileY { get; set; }
        public int Hp { get; set; }
        public int HpMax { get; set; }
        public int Strength { get; set; }  // From getStrengthRating()
        public int Xp { get; set; }
        public int Level { get; set; }
        // Fatigue system: TurnSteps/FatigueLimit determines if fatigued
        public int TurnSteps { get; set; }
        public int FatigueLimit { get; set; }
        public bool IsFatigued { get; set; }
    }

    public class CityData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int PlayerId { get; set; }
        public int TileX { get; set; }
        public int TileY { get; set; }
        public int Citizens { get; set; }
        // Growth (population) - progress toward next citizen
        public int GrowthProgress { get; set; }
        public int GrowthThreshold { get; set; }
        public int GrowthPerTurn { get; set; }
        public string CurrentBuild { get; set; }
        // Culture - getCulture() returns enum, getCultureStep() returns level
        public int CultureLevel { get; set; }
        public int CultureStep { get; set; }
        public int CultureProgress { get; set; }
        // Happiness - getHappinessLevel() returns int (negative = unhappy)
        public int HappinessLevel { get; set; }
    }

    public class BuildRecord
    {
        public BuildRecordType BuildType { get; set; }
        public string ItemType { get; set; }
        public string ItemName { get; set; }
        public int CityId { get; set; }
        public string CityName { get; set; }
        public int PlayerId { get; set; }
        public int Turn { get; set; }
        public int Year { get; set; }
        public bool WasHurried { get; set; }
        public int? UnitId { get; set; }
    }
}
```

### 1.8 Ring Buffer

Bounded history to prevent unbounded memory growth.

```csharp
using System;
using System.Collections.Generic;

namespace OldWorldAPIEndpoint
{
    public class RingBuffer<T>
    {
        private readonly T[] _buffer;
        private readonly object _lock = new();
        private int _head = 0;
        private int _count = 0;

        public int Capacity { get; }

        public RingBuffer(int capacity)
        {
            Capacity = capacity;
            _buffer = new T[capacity];
        }

        public void Add(T item)
        {
            lock (_lock)
            {
                _buffer[_head] = item;
                _head = (_head + 1) % Capacity;
                if (_count < Capacity) _count++;
            }
        }

        public List<T> ToList()
        {
            lock (_lock)
            {
                var result = new List<T>(_count);
                int start = (_head - _count + Capacity) % Capacity;
                for (int i = 0; i < _count; i++)
                    result.Add(_buffer[(start + i) % Capacity]);
                return result;
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                Array.Clear(_buffer, 0, _buffer.Length);
                _head = 0;
                _count = 0;
            }
        }
    }
}
```

### 1.9 Main Thread Dispatcher

Unity APIs must be called from the main thread.

```csharp
using System;
using System.Collections.Concurrent;
using UnityEngine;

namespace OldWorldAPIEndpoint
{
    public class MainThreadDispatcher : MonoBehaviour
    {
        private static readonly ConcurrentQueue<Action> _queue = new();
        private static MainThreadDispatcher _instance;

        public static void Enqueue(Action action) => _queue.Enqueue(action);

        void Update()
        {
            while (_queue.TryDequeue(out var action))
            {
                try { action(); }
                catch (Exception ex) { Debug.LogError($"[Dispatcher] {ex}"); }
            }
        }

        public static void Initialize()
        {
            if (_instance == null)
            {
                var go = new GameObject("OldWorldAPIEndpoint_Dispatcher");
                _instance = go.AddComponent<MainThreadDispatcher>();
                DontDestroyOnLoad(go);
            }
        }
    }
}
```

---

## Part 2: Tauri App

### 2.1 Project Structure

```
old-world-companion/
├── src-tauri/
│   ├── Cargo.toml
│   └── src/
│       ├── main.rs
│       ├── tcp_client.rs
│       ├── state_manager.rs
│       └── commands.rs
├── src/
│   ├── App.tsx
│   ├── hooks/
│   │   └── useGameState.ts
│   ├── components/
│   │   ├── Dashboard.tsx
│   │   ├── PlayerStats.tsx
│   │   └── BuildHistory.tsx
│   └── types/
│       └── gameState.ts
└── package.json
```

### 2.2 Rust TCP Client

```rust
use std::io::Read;
use std::net::TcpStream;
use std::sync::{Arc, Mutex};
use std::thread;
use std::time::Duration;
use serde::{Deserialize, Serialize};
use tauri::{AppHandle, Emitter};

const MAX_MESSAGE_SIZE: usize = 10 * 1024 * 1024;

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct GameStateMessage {
    pub version: String,
    #[serde(rename = "type")]
    pub message_type: String,
    pub session_id: Option<String>,
    pub sequence: Option<i64>,
    pub timestamp: String,
    #[serde(default)]
    pub payload: serde_json::Value,
}

pub struct TcpClient {
    stop_signal: Arc<Mutex<bool>>,
}

impl TcpClient {
    pub fn new() -> Self {
        Self { stop_signal: Arc::new(Mutex::new(false)) }
    }

    pub fn start(&self, app_handle: AppHandle, port: u16) {
        let stop = Arc::clone(&self.stop_signal);
        *stop.lock().unwrap() = false;

        thread::spawn(move || {
            let mut backoff = 1000u64;

            loop {
                if *stop.lock().unwrap() { break; }

                let _ = app_handle.emit("connection-status", "connecting");

                match TcpStream::connect(format!("127.0.0.1:{}", port)) {
                    Ok(stream) => {
                        let _ = app_handle.emit("connection-status", "connected");
                        backoff = 1000;

                        if let Err(e) = Self::read_loop(&app_handle, stream, &stop) {
                            eprintln!("Read error: {}", e);
                        }

                        let _ = app_handle.emit("connection-status", "disconnected");
                    }
                    Err(_) => {
                        let _ = app_handle.emit("connection-status", "disconnected");
                    }
                }

                Self::sleep_interruptible(&stop, backoff);
                backoff = (backoff * 2).min(10000);
            }
        });
    }

    pub fn stop(&self) {
        *self.stop_signal.lock().unwrap() = true;
    }

    fn read_loop(
        app: &AppHandle,
        mut stream: TcpStream,
        stop: &Arc<Mutex<bool>>,
    ) -> Result<(), Box<dyn std::error::Error>> {
        stream.set_read_timeout(Some(Duration::from_secs(30)))?;

        loop {
            if *stop.lock().unwrap() { break; }

            // Read 4-byte length prefix
            let mut len_buf = [0u8; 4];
            match stream.read_exact(&mut len_buf) {
                Ok(()) => {}
                Err(ref e) if e.kind() == std::io::ErrorKind::TimedOut => continue,
                Err(e) => return Err(Box::new(e)),
            }

            let len = u32::from_be_bytes(len_buf) as usize;
            if len > MAX_MESSAGE_SIZE {
                return Err("Message too large".into());
            }

            let mut buf = vec![0u8; len];
            stream.read_exact(&mut buf)?;

            if let Ok(msg) = serde_json::from_slice::<GameStateMessage>(&buf) {
                let _ = app.emit("game-state", msg);
            }
        }
        Ok(())
    }

    fn sleep_interruptible(stop: &Arc<Mutex<bool>>, ms: u64) {
        for _ in 0..(ms / 100) {
            if *stop.lock().unwrap() { return; }
            thread::sleep(Duration::from_millis(100));
        }
    }
}
```

### 2.3 TypeScript Types

```typescript
export type MessageType =
  | 'FullState' | 'TurnUpdate' | 'BuildCompleted'
  | 'GameStarted' | 'GameEnded' | 'GameLoaded' | 'Ping' | 'Error';

export type BuildRecordType = 'Unit' | 'Project' | 'Specialist';

export interface GameStateMessage {
  version: string;
  type: MessageType;
  sessionId?: string;
  sequence?: number;
  timestamp: string;
  payload: TurnSnapshot | GameSnapshot | null;
}

export interface GameSnapshot {
  gameInfo: GameInfo;
  players: PlayerData[];
  units: UnitData[];
  cities: CityData[];
  buildHistory: BuildRecord[];
}

export interface TurnSnapshot {
  turn: number;
  year: number;
  players: PlayerData[];
  units: UnitData[];
  cities: CityData[];
  buildHistory: BuildRecord[];
}

export interface GameInfo {
  turn: number;
  year: number;
  mapWidth: number;
  mapHeight: number;
  numPlayers: number;
}

export interface PlayerData {
  id: number;
  name: string;
  isHuman: boolean;
  // Yields - stockpiles and per-turn rates
  money: number;
  moneyPerTurn: number;
  civics: number;
  civicsPerTurn: number;
  training: number;
  trainingPerTurn: number;
  science: number;
  sciencePerTurn: number;
  orders: number;
  // Counts
  numUnits: number;
  numCities: number;
}

export interface UnitData {
  id: number;
  type: string;
  playerId: number;
  createTurn: number;
  originCityId?: number;
  originCityName?: string;
  tileX: number;
  tileY: number;
  hp: number;
  hpMax: number;
  strength: number;  // From getStrengthRating()
  xp: number;
  level: number;
  // Fatigue system
  turnSteps: number;
  fatigueLimit: number;
  isFatigued: boolean;
}

export interface CityData {
  id: number;
  name: string;
  playerId: number;
  tileX: number;
  tileY: number;
  citizens: number;
  // Growth (population) - progress toward next citizen
  growthProgress: number;
  growthThreshold: number;
  growthPerTurn: number;
  currentBuild?: string;
  // Culture
  cultureLevel: number;
  cultureStep: number;
  cultureProgress: number;
  // Happiness - negative = unhappy
  happinessLevel: number;
}

export interface BuildRecord {
  buildType: BuildRecordType;
  itemType: string;
  itemName: string;
  cityId: number;
  cityName: string;
  playerId: number;
  turn: number;
  year: number;
  wasHurried: boolean;
  unitId?: number;
}

export type ConnectionStatus = 'disconnected' | 'connecting' | 'connected';
```

### 2.4 React Hook

```typescript
import { useEffect, useState } from 'react';
import { listen } from '@tauri-apps/api/event';
import { invoke } from '@tauri-apps/api/core';
import type { GameStateMessage, TurnSnapshot, ConnectionStatus } from '../types/gameState';

export function useGameState() {
  const [status, setStatus] = useState<ConnectionStatus>('disconnected');
  const [state, setState] = useState<TurnSnapshot | null>(null);
  const [history, setHistory] = useState<TurnSnapshot[]>([]);

  useEffect(() => {
    const unlistenStatus = listen<ConnectionStatus>('connection-status', (e) => {
      setStatus(e.payload);
    });

    const unlistenState = listen<GameStateMessage>('game-state', (e) => {
      const msg = e.payload;
      if (msg.type === 'TurnUpdate' || msg.type === 'FullState') {
        const snapshot = msg.payload as TurnSnapshot;
        setState(snapshot);
        setHistory(prev => [...prev, snapshot].slice(-200));
      } else if (msg.type === 'GameEnded') {
        setState(null);
      }
    });

    invoke('connect_to_game');

    return () => {
      unlistenStatus.then(fn => fn());
      unlistenState.then(fn => fn());
      invoke('disconnect_from_game');
    };
  }, []);

  return { status, state, history };
}
```

---

## Part 3: Protocol

### Message Framing

```
┌─────────────────────────────────────────────┐
│  4 bytes (BE)  │  N bytes (UTF-8 JSON)      │
│  Length = N    │  Payload                   │
└─────────────────────────────────────────────┘
```

### Message Envelope

```json
{
  "version": "1.0.0",
  "type": "TurnUpdate",
  "sessionId": "a1b2c3d4",
  "sequence": 42,
  "timestamp": "2024-01-15T10:30:00Z",
  "payload": { ... }
}
```

### Connection Flow

```
Client                                       Mod
  │                                           │
  │────────── TCP Connect ───────────────────►│
  │◄───────── FullState ─────────────────────│
  │                                           │
  │◄───────── TurnUpdate ────────────────────│
  │◄───────── TurnUpdate ────────────────────│
  │                                           │
  │           ... disconnect ...              │
  │                                           │
  │────────── TCP Reconnect ─────────────────►│
  │◄───────── FullState ─────────────────────│
```

---

## Part 4: Analytics Examples

```typescript
// Projected money in N turns
function projectMoney(player: PlayerData, turns: number): number {
  return player.money + player.moneyPerTurn * turns;
}

// Units built by city
function getUnitsBuiltByCity(history: BuildRecord[], cityId: number): BuildRecord[] {
  return history.filter(b => b.buildType === 'Unit' && b.cityId === cityId);
}

// City production breakdown
function getCityStats(history: BuildRecord[], cityId: number) {
  const builds = history.filter(b => b.cityId === cityId);
  return {
    totalUnits: builds.filter(b => b.buildType === 'Unit').length,
    totalProjects: builds.filter(b => b.buildType === 'Project').length,
    unitsByType: builds
      .filter(b => b.buildType === 'Unit')
      .reduce((acc, b) => {
        acc[b.itemName] = (acc[b.itemName] || 0) + 1;
        return acc;
      }, {} as Record<string, number>)
  };
}

// Find unit's origin city
function getUnitOrigin(unit: UnitData): string | null {
  return unit.originCityName ?? null;
}
```

---

## Appendix A: Building the Mod

### Prerequisites

- .NET Framework 4.x or Mono (matching Old World's Unity version)
- Newtonsoft.Json.dll (bundle with mod or reference game's copy)
- 0Harmony.dll (must be bundled with the mod)
- Reference assemblies: `Assembly-CSharp.dll`, `UnityEngine.dll` from game

### Obtaining Harmony

Download 0Harmony.dll from:
- https://github.com/pardeike/Harmony/releases
- Use version 2.x compatible with the game's .NET version

### Compilation

```bash
mcs -target:library \
    -reference:Assembly-CSharp.dll \
    -reference:UnityEngine.dll \
    -reference:Newtonsoft.Json.dll \
    -reference:0Harmony.dll \
    -out:OldWorldAPIEndpoint.dll \
    Source/*.cs Source/Patches/*.cs Source/Networking/*.cs Source/DTOs/*.cs Source/Collections/*.cs
```

### Mod Directory Structure

```
OldWorldAPIEndpoint/
├── ModInfo.xml               # Mod metadata
├── 0Harmony.dll              # Harmony library (REQUIRED)
├── OldWorldAPIEndpoint.dll   # Your compiled mod
├── Newtonsoft.Json.dll       # JSON library (if not using game's copy)
└── OldWorldAPIEndpoint.png   # Mod icon (optional)
```

### Installation Paths

| Platform | Path |
|----------|------|
| Windows  | `%APPDATA%\OldWorld\Mods\OldWorldAPIEndpoint\` |
| macOS    | `~/Library/Application Support/OldWorld/Mods/OldWorldAPIEndpoint/` |
| Linux    | `~/.config/OldWorld/Mods/OldWorldAPIEndpoint/` |

---

## Appendix B: Key Game APIs

### Game Object
| Method | Returns |
|--------|---------|
| `getTurn()` | Turn number |
| `getYear()` | Year |
| `getPlayers()` | `Player[]` - All players |
| `getUnits()` | `ReadOnlyList<Unit>` - All units |
| `getCities()` | `ReadOnlyList<City>` - All cities |
| `infos()` | `Infos` - Game info/type lookup |
| `getMapWidth()` / `getMapHeight()` | Map dimensions |

### Player Object
| Method | Returns |
|--------|---------|
| `getMoneyWhole()` | Money stockpile (whole number) |
| `getYieldStockpileWhole(YieldType)` | Stockpile for any yield type |
| `calculateYield(YieldType)` | Per-turn rate for any yield |
| `getNumUnits()` | Total unit count |
| `getNumCities()` | City count |
| `isHuman()` | Is human player |
| `isAlive()` | Is player alive |
| `getName()` | Player name |

**Yield Types** (via `infos().Globals`):
- `MONEY_YIELD`, `CIVICS_YIELD`, `TRAINING_YIELD`, `SCIENCE_YIELD`, `ORDERS_YIELD`

### City Object
| Method | Returns |
|--------|---------|
| `getCitizens()` | Population |
| `getYieldProgress(YieldType)` | Current progress toward threshold |
| `getYieldThresholdWhole(YieldType)` | Threshold for yield completion |
| `calculateCurrentYield(YieldType)` | Per-turn yield rate |
| `getCurrentBuild()` | `CityQueueData` - Current build |
| `finishBuild()` | Completes current build (protected) |
| `getCulture()` | `CultureType` enum |
| `getCultureStep()` | Culture progress level |
| `getHappinessLevel()` | Happiness (negative = unhappy) |

**City Yield Types** (via `infos().Globals`):
- `GROWTH_YIELD` - Population growth (food equivalent)
- `CULTURE_YIELD` - Culture progress
- `HAPPINESS_YIELD` - Happiness progress

### Unit Object
| Method | Returns |
|--------|---------|
| `getCreateTurn()` | Turn created |
| `getHP()` / `getHPMax()` | Current/max health |
| `getStrengthRating()` | Combat strength |
| `getXP()` / `getLevel()` | Experience and level |
| `getTurnSteps()` | Steps taken this turn |
| `getFatigueLimit()` | Max steps before fatigue |
| `isFatigued()` | Is unit fatigued |
| `getType()` | `UnitType` enum |
| `tile()` | Current tile |

---

## Appendix C: Harmony Quick Reference

### Patch Types

| Type | When | Use Case |
|------|------|----------|
| `[HarmonyPrefix]` | Before original | Capture pre-state, skip original |
| `[HarmonyPostfix]` | After original | React to results, modify output |
| `[HarmonyTranspiler]` | IL modification | Low-level patching |

### State Passing Between Prefix/Postfix

```csharp
[HarmonyPrefix]
public static void MyPrefix(out MyState __state) {
    __state = new MyState { /* capture data */ };
}

[HarmonyPostfix]
public static void MyPostfix(MyState __state) {
    // Use captured state
}
```

### Common Parameters

| Parameter | Description |
|-----------|-------------|
| `__instance` | The object the method is called on |
| `__result` | Return value (postfix only, can modify with `ref`) |
| `__state` | State passed from prefix to postfix |
| `___fieldName` | Access private field `fieldName` |

### Why Harmony Over GameFactory

**GameFactory limitation:** Only ONE mod can override GameFactory. If multiple mods try to set `modSettings.Factory`, they conflict.

**Harmony advantage:** Multiple mods can patch the same methods. Patches stack and execute in order. This allows:
- API endpoint mod to patch `Game.doTurn` for state broadcast
- Another mod to patch `Game.doTurn` for different functionality
- Both work simultaneously without conflicts

---

## Appendix D: Troubleshooting

### Mod Not Loading

1. Check `ModInfo.xml` is valid XML
2. Verify `0Harmony.dll` is in the mod folder
3. Check game logs for errors: `Player.log` / `Player-prev.log`

### Harmony Patches Not Working

1. Verify method signature matches exactly
2. Check for method overloads
3. Use `AccessTools.Method()` for explicit targeting

### TCP Connection Issues

1. Verify port 9876 is available
2. Check firewall rules (localhost should be allowed)
3. Ensure only one Old World instance is running

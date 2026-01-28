using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using TenCrowns.AppCore;
using TenCrowns.GameCore;
using UnityEngine;

namespace OldWorldAPIEndpoint
{
    /// <summary>
    /// Old World mod that broadcasts game state over TCP for companion apps.
    /// This is the main entry point; additional functionality is in partial classes:
    /// - APIEndpoint.Reflection.cs - Reflection initialization and game access
    /// - APIEndpoint.DataBuilders.cs - JSON object building methods
    /// - APIEndpoint.Events.cs - Event detection systems
    /// - APIEndpoint.Diplomacy.cs - Diplomacy methods
    /// - APIEndpoint.Lookups.cs - Single entity lookup methods
    /// </summary>
    public partial class APIEndpoint : ModEntryPointAdapter
    {
        private static TcpBroadcastServer _server;
        private static HttpRestServer _httpServer;
        private static ModSettings _modSettings;
        private static int _initCount = 0;

        // Cached game reference for HTTP access (updated from main thread, read by HTTP thread)
        private static volatile Game _cachedGame;

        // Cached ClientManager for command execution
        private static object _clientManager;

        // Command queue: HTTP thread enqueues, main thread (OnClientUpdate) dequeues and executes
        private static readonly ConcurrentQueue<(GameCommand cmd, ManualResetEventSlim signal, CommandResult result)> _commandQueue
            = new ConcurrentQueue<(GameCommand, ManualResetEventSlim, CommandResult)>();

        // JSON serializer settings
        // Note: We use DefaultContractResolver to preserve exact game type strings (e.g., YIELD_GROWTH)
        // Property names in anonymous objects are already camelCase as defined
        private static readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver(),
            NullValueHandling = NullValueHandling.Include,
            Formatting = Formatting.None
        };

        #region ModEntryPointAdapter Implementation

        public override void Initialize(ModSettings modSettings)
        {
            base.Initialize(modSettings);
            _modSettings = modSettings;
            _initCount++;

            Debug.Log($"[APIEndpoint] Initialize() called (count={_initCount})");

            // Initialize reflection FIRST so GetGame() works
            InitializeReflection();

            // Start TCP server if not already running
            if (_server == null)
            {
                _server = new TcpBroadcastServer(9876);
                _server.Start();
            }

            // Start HTTP server if not already running
            if (_httpServer == null)
            {
                _httpServer = new HttpRestServer(9877, GetGame, _jsonSettings);
                _httpServer.Start();
            }
        }

        public override void Shutdown()
        {
            Debug.Log("[APIEndpoint] Shutdown() called");
            base.Shutdown();
        }

        public override void OnNewTurnServer()
        {
            try
            {
                // Get game and cache it for HTTP thread access
                InitializeReflection();
                var appMain = GetAppMain();
                Game game = null;

                // Try to get game directly from main thread and cache it
                if (appMain != null && _getLocalGameServerMethod != null)
                {
                    var gameServer = _getLocalGameServerMethod.Invoke(appMain, null);
                    if (gameServer != null)
                    {
                        var localGameProp = gameServer.GetType().GetProperty("LocalGame",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (localGameProp != null)
                            game = localGameProp.GetValue(gameServer) as Game;
                    }
                }

                // Fallback to standard GetGame if above failed
                if (game == null) game = GetGame();

                // Cache for HTTP access
                _cachedGame = game;

                string json;

                if (game != null)
                {
                    int turn = game.getTurn();
                    int year = game.getYear();
                    Infos infos = game.infos();

                    // Detect events by diffing against previous turn's state
                    var characterEvents = DetectCharacterEvents(game, infos);
                    var unitEvents = DetectUnitEvents(game, infos);
                    var cityEvents = DetectCityEvents(game, infos);
                    var wonderEvents = DetectWonderEvents(game, infos);

                    var message = new
                    {
                        @event = "newTurn",
                        turn = turn,
                        year = year,
                        currentPlayer = (int)game.getPlayerTurn(),
                        characterEvents = characterEvents,
                        unitEvents = unitEvents,
                        cityEvents = cityEvents,
                        wonderEvents = wonderEvents,
                        players = BuildPlayersObject(game),
                        characters = BuildCharactersObject(game),
                        cities = BuildCitiesObject(game),
                        teamDiplomacy = BuildTeamDiplomacyObject(game),
                        teamAlliances = BuildTeamAlliancesObject(game),
                        tribes = BuildTribesObject(game),
                        tribeDiplomacy = BuildTribeDiplomacyObject(game),
                        tribeAlliances = BuildTribeAlliancesObject(game)
                    };
                    json = JsonConvert.SerializeObject(message, _jsonSettings);

                    Debug.Log($"[APIEndpoint] OnNewTurnServer: turn={turn}, year={year}, charEvents={characterEvents.Count}, unitEvents={unitEvents.Count}, cityEvents={cityEvents.Count}, wonderEvents={wonderEvents.Count}");
                }
                else
                {
                    Debug.Log("[APIEndpoint] OnNewTurnServer: game=null");
                    json = JsonConvert.SerializeObject(new { @event = "newTurn", error = "game not available" }, _jsonSettings);
                }

                _server?.Broadcast(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[APIEndpoint] OnNewTurnServer error: {ex.Message}");
            }
        }

        public override void OnGameServerReady()
        {
            try
            {
                // Get game and cache it for HTTP thread access
                InitializeReflection();
                var appMain = GetAppMain();
                Game game = null;

                // Try to get game directly from main thread and cache it
                if (appMain != null && _getLocalGameServerMethod != null)
                {
                    var gameServer = _getLocalGameServerMethod.Invoke(appMain, null);
                    if (gameServer != null)
                    {
                        var localGameProp = gameServer.GetType().GetProperty("LocalGame",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (localGameProp != null)
                            game = localGameProp.GetValue(gameServer) as Game;
                    }
                }

                // Fallback to standard GetGame if above failed
                if (game == null) game = GetGame();

                // Cache for HTTP access
                _cachedGame = game;

                string json;

                if (game != null)
                {
                    int turn = game.getTurn();
                    int year = game.getYear();

                    Infos infos = game.infos();

                    // Initialize snapshots for event detection
                    // (no events emitted on game start, just baseline state)
                    UpdateCharacterSnapshots(game);
                    UpdateUnitSnapshots(game, infos);
                    UpdateCitySnapshots(game);
                    UpdateWonderSnapshots(game, infos);
                    _previousTurn = turn;

                    var message = new
                    {
                        @event = "gameReady",
                        turn = turn,
                        year = year,
                        characterEvents = new List<object>(),  // Empty on game start
                        unitEvents = new List<object>(),       // Empty on game start
                        cityEvents = new List<object>(),       // Empty on game start
                        wonderEvents = new List<object>(),     // Empty on game start
                        players = BuildPlayersObject(game),
                        characters = BuildCharactersObject(game),
                        cities = BuildCitiesObject(game),
                        teamDiplomacy = BuildTeamDiplomacyObject(game),
                        teamAlliances = BuildTeamAlliancesObject(game),
                        tribes = BuildTribesObject(game),
                        tribeDiplomacy = BuildTribeDiplomacyObject(game),
                        tribeAlliances = BuildTribeAlliancesObject(game)
                    };
                    json = JsonConvert.SerializeObject(message, _jsonSettings);

                    Debug.Log($"[APIEndpoint] OnGameServerReady: turn={turn}, year={year}");
                }
                else
                {
                    Debug.Log("[APIEndpoint] OnGameServerReady: game=null");
                    json = JsonConvert.SerializeObject(new { @event = "gameReady", error = "game not available" }, _jsonSettings);
                }

                _server?.Broadcast(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[APIEndpoint] OnGameServerReady error: {ex.Message}");
            }
        }

        /// <summary>
        /// Called every frame on the main Unity thread.
        /// Process queued commands here so they execute on the correct thread.
        /// </summary>
        public override void OnClientUpdate()
        {
            // Ensure we have the game reference
            if (_cachedGame == null)
            {
                _cachedGame = GetGame();
            }

            // Process up to 10 commands per frame to avoid blocking
            int processed = 0;
            while (processed < 10 && _commandQueue.TryDequeue(out var item))
            {
                var (cmd, signal, result) = item;
                try
                {
                    // Get ClientManager on main thread
                    var clientManager = GetOrCreateClientManager();
                    if (clientManager == null)
                    {
                        result.Success = false;
                        result.Error = "ClientManager not available";
                    }
                    else if (_cachedGame == null)
                    {
                        result.Success = false;
                        result.Error = "Game not available";
                    }
                    else
                    {
                        var execResult = CommandExecutor.Execute(clientManager, _cachedGame, cmd);
                        result.RequestId = execResult.RequestId;
                        result.Success = execResult.Success;
                        result.Error = execResult.Error;
                        result.Data = execResult.Data;
                    }
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.Error = $"Exception: {ex.Message}";
                    Debug.LogError($"[APIEndpoint] Command error in OnClientUpdate: {ex}");
                }
                signal.Set(); // Signal the waiting HTTP thread
                processed++;
            }
        }

        public override bool CallOnGUI()
        {
            return false;
        }

        #endregion

        #region Command Execution

        /// <summary>
        /// Queue a command for execution on the main thread and wait for result.
        /// Called from HTTP thread.
        /// </summary>
        public static CommandResult ExecuteCommand(GameCommand cmd, int timeoutMs = 5000)
        {
            var result = new CommandResult { RequestId = cmd.RequestId };
            using var signal = new ManualResetEventSlim(false);

            // Queue the command for processing on the main thread
            _commandQueue.Enqueue((cmd, signal, result));

            // Wait for the main thread to process it
            if (!signal.Wait(timeoutMs))
            {
                result.Success = false;
                result.Error = "Command execution timed out (main thread may not be processing)";
            }

            return result;
        }

        /// <summary>
        /// Execute multiple commands in sequence.
        /// Each command is queued and executed on the main thread.
        /// </summary>
        public static BulkCommandResult ExecuteBulkCommand(BulkCommand bulkCmd)
        {
            var result = new BulkCommandResult
            {
                RequestId = bulkCmd.RequestId,
                Results = new List<BulkCommandItemResult>(),
                AllSucceeded = true
            };

            for (int i = 0; i < bulkCmd.Commands.Count; i++)
            {
                var cmd = bulkCmd.Commands[i];
                var itemResult = new BulkCommandItemResult
                {
                    Index = i,
                    Action = cmd.Action
                };

                var execResult = ExecuteCommand(cmd);
                itemResult.Success = execResult.Success;
                itemResult.Error = execResult.Error;

                result.Results.Add(itemResult);

                if (!itemResult.Success)
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

        /// <summary>
        /// Get or create the ClientManager instance via reflection.
        /// Path: AppMain.gApp.Client (GameClientBehaviour) -> ClientMgr (ClientManager)
        /// </summary>
        private static object GetOrCreateClientManager()
        {
            if (_clientManager != null) return _clientManager;

            try
            {
                InitializeReflection();
                var appMain = GetAppMain();
                if (appMain == null)
                {
                    Debug.Log("[APIEndpoint] GetOrCreateClientManager: appMain is null");
                    return null;
                }

                // Step 1: Get GameClientBehaviour via AppMain.gApp.Client
                object gameClientBehaviour = null;
                if (_clientProperty != null)
                {
                    gameClientBehaviour = _clientProperty.GetValue(appMain);
                }

                if (gameClientBehaviour == null)
                {
                    Debug.LogWarning("[APIEndpoint] Could not get GameClientBehaviour");
                    return null;
                }

                // Step 2: Get ClientManager via GameClientBehaviour.ClientMgr
                var clientMgrProp = gameClientBehaviour.GetType().GetProperty("ClientMgr",
                    BindingFlags.Public | BindingFlags.Instance);
                if (clientMgrProp != null)
                {
                    _clientManager = clientMgrProp.GetValue(gameClientBehaviour);
                    if (_clientManager != null)
                    {
                        Debug.Log($"[APIEndpoint] Got ClientManager: {_clientManager.GetType().Name}");
                        return _clientManager;
                    }
                }

                Debug.LogWarning("[APIEndpoint] Could not find ClientManager via ClientMgr property");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[APIEndpoint] Failed to get ClientManager: {ex.Message}");
            }

            return _clientManager;
        }

        /// <summary>
        /// Get the cached ClientManager for command execution.
        /// </summary>
        public static object GetClientManager()
        {
            return _clientManager;
        }

        /// <summary>
        /// Get the cached game instance.
        /// </summary>
        public static Game GetCachedGame()
        {
            return _cachedGame;
        }

        // Queue methods execute synchronously on the main thread
        public static CommandResult QueueAndWaitCommand(GameCommand cmd, int timeoutMs = 5000)
        {
            return ExecuteCommand(cmd);
        }

        public static BulkCommandResult QueueAndWaitBulkCommand(BulkCommand cmd, int timeoutMs = 30000)
        {
            return ExecuteBulkCommand(cmd);
        }

        #endregion
    }
}

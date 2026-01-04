using System;
using System.Reflection;
using System.Text;
using TenCrowns.AppCore;
using TenCrowns.GameCore;
using UnityEngine;

namespace OldWorldAPIEndpoint
{
    /// <summary>
    /// Old World mod that broadcasts game state over TCP for companion apps.
    /// </summary>
    public class APIEndpoint : ModEntryPointAdapter
    {
        private static TcpBroadcastServer _server;
        private static ModSettings _modSettings;
        private static int _initCount = 0;

        // Cached reflection info for AppMain.gApp.Client.Game
        private static Type _appMainType;
        private static FieldInfo _gAppField;
        private static PropertyInfo _clientProperty;
        private static PropertyInfo _gameProperty;
        private static bool _reflectionInitialized;

        // Alternative reflection paths for headless mode
        private static PropertyInfo _serverProperty;
        private static MethodInfo _getLocalGameServerMethod;
        private static PropertyInfo _localGameProperty;

        private static void InitializeReflection()
        {
            if (_reflectionInitialized) return;

            try
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (assembly.GetName().Name == "Assembly-CSharp")
                    {
                        _appMainType = assembly.GetType("AppMain");

                        // Get GameServerBehaviour and GameNetworkBehaviour for headless access
                        var gameServerBehaviourType = assembly.GetType("GameServerBehaviour");
                        var gameNetworkBehaviourType = assembly.GetType("GameNetworkBehaviour");

                        if (gameNetworkBehaviourType != null)
                        {
                            _localGameProperty = gameNetworkBehaviourType.GetProperty("LocalGame",
                                BindingFlags.Public | BindingFlags.Instance);
                        }

                        break;
                    }
                }

                if (_appMainType != null)
                {
                    _gAppField = _appMainType.GetField("gApp", BindingFlags.Public | BindingFlags.Static);
                    _clientProperty = _appMainType.GetProperty("Client", BindingFlags.Public | BindingFlags.Instance);
                    _serverProperty = _appMainType.GetProperty("Server", BindingFlags.Public | BindingFlags.Instance);
                    _getLocalGameServerMethod = _appMainType.GetMethod("GetLocalGameServer",
                        BindingFlags.Public | BindingFlags.Instance);

                    if (_clientProperty != null)
                    {
                        var clientType = _clientProperty.PropertyType;
                        _gameProperty = clientType.GetProperty("Game", BindingFlags.Public | BindingFlags.Instance);
                    }

                    Debug.Log($"[APIEndpoint] Reflection initialized");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[APIEndpoint] Reflection init failed: {ex.Message}");
            }

            _reflectionInitialized = true;
        }

        private static object GetAppMain()
        {
            InitializeReflection();
            return _gAppField?.GetValue(null);
        }

        /// <summary>
        /// Try multiple paths to get the Game instance.
        /// </summary>
        private static Game GetGame()
        {
            try
            {
                InitializeReflection();
                var appMain = GetAppMain();
                if (appMain == null) return null;

                // Path 1: AppMain.gApp.Client.Game (works in GUI mode)
                if (_clientProperty != null && _gameProperty != null)
                {
                    var client = _clientProperty.GetValue(appMain);
                    if (client != null)
                    {
                        var game = _gameProperty.GetValue(client) as Game;
                        if (game != null) return game;
                    }
                }

                // Path 2: AppMain.gApp.Server.Game (try server property)
                if (_serverProperty != null && _gameProperty != null)
                {
                    var server = _serverProperty.GetValue(appMain);
                    if (server != null)
                    {
                        // Server might have a Game property too
                        var serverGameProp = server.GetType().GetProperty("Game",
                            BindingFlags.Public | BindingFlags.Instance);
                        if (serverGameProp != null)
                        {
                            var game = serverGameProp.GetValue(server) as Game;
                            if (game != null) return game;
                        }
                    }
                }

                // Path 3: AppMain.GetLocalGameServer().LocalGame
                if (_getLocalGameServerMethod != null)
                {
                    var gameServer = _getLocalGameServerMethod.Invoke(appMain, null);
                    if (gameServer != null)
                    {
                        // Try to get LocalGame property dynamically from the gameServer instance
                        var localGameProp = gameServer.GetType().GetProperty("LocalGame",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (localGameProp != null)
                        {
                            var game = localGameProp.GetValue(gameServer) as Game;
                            if (game != null) return game;
                        }

                        // Also try Game property directly
                        var gameProp = gameServer.GetType().GetProperty("Game",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (gameProp != null)
                        {
                            var game = gameProp.GetValue(gameServer) as Game;
                            if (game != null) return game;
                        }
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Build JSON array of all players with their data and stockpiles.
        /// </summary>
        private static string BuildPlayersJson(Game game)
        {
            var sb = new StringBuilder();
            sb.Append("[");

            Player[] players = game.getPlayers();
            Infos infos = game.infos();
            int yieldCount = (int)infos.yieldsNum();

            bool first = true;
            for (int i = 0; i < players.Length; i++)
            {
                var player = players[i];
                if (player == null) continue;

                if (!first) sb.Append(",");
                first = false;

                string nationName = infos.nation(player.getNation()).mzType;

                sb.Append("{");
                sb.Append($"\"index\":{i},");
                sb.Append($"\"nation\":\"{nationName}\",");
                sb.Append($"\"cities\":{player.getNumCities()},");
                sb.Append($"\"units\":{player.getNumUnits()},");
                sb.Append($"\"legitimacy\":{player.getLegitimacy()},");

                sb.Append("\"stockpiles\":{");
                for (int y = 0; y < yieldCount; y++)
                {
                    if (y > 0) sb.Append(",");
                    var yieldType = (YieldType)y;
                    string yieldName = infos.yield(yieldType).mzType;
                    int amount = player.getYieldStockpileWhole(yieldType);
                    sb.Append($"\"{yieldName}\":{amount}");
                }
                sb.Append("}");

                sb.Append("}");
            }

            sb.Append("]");
            return sb.ToString();
        }

        public override void Initialize(ModSettings modSettings)
        {
            base.Initialize(modSettings);
            _modSettings = modSettings;
            _initCount++;

            Debug.Log($"[APIEndpoint] Initialize() called (count={_initCount})");

            // Start TCP server if not already running
            if (_server == null)
            {
                _server = new TcpBroadcastServer(9876);
                _server.Start();
            }

            InitializeReflection();
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
                var game = GetGame();
                string json;

                if (game != null)
                {
                    int turn = game.getTurn();
                    int year = game.getYear();
                    int currentPlayer = (int)game.getPlayerTurn();
                    string playersJson = BuildPlayersJson(game);

                    json = $"{{\"event\":\"newTurn\",\"turn\":{turn},\"year\":{year}," +
                           $"\"currentPlayer\":{currentPlayer},\"players\":{playersJson}}}";

                    Debug.Log($"[APIEndpoint] OnNewTurnServer: turn={turn}, year={year}");
                }
                else
                {
                    Debug.Log("[APIEndpoint] OnNewTurnServer: game=null");
                    json = "{\"event\":\"newTurn\",\"error\":\"game not available\"}";
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
                var game = GetGame();
                string json;

                if (game != null)
                {
                    int turn = game.getTurn();
                    int year = game.getYear();
                    string playersJson = BuildPlayersJson(game);

                    json = $"{{\"event\":\"gameReady\",\"turn\":{turn},\"year\":{year}," +
                           $"\"players\":{playersJson}}}";

                    Debug.Log($"[APIEndpoint] OnGameServerReady: turn={turn}, year={year}");
                }
                else
                {
                    Debug.Log("[APIEndpoint] OnGameServerReady: game=null");
                    json = "{\"event\":\"gameReady\",\"error\":\"game not available\"}";
                }

                _server?.Broadcast(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[APIEndpoint] OnGameServerReady error: {ex.Message}");
            }
        }

        public override bool CallOnGUI()
        {
            return false;
        }
    }
}

using System;
using System.Reflection;
using TenCrowns.GameCore;
using UnityEngine;

namespace OldWorldAPIEndpoint
{
    /// <summary>
    /// Reflection initialization and game access methods.
    /// Uses reflection to access Assembly-CSharp types (blocked from direct reference).
    /// </summary>
    public partial class APIEndpoint
    {
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
        /// Returns cached game for thread-safe access from HTTP requests.
        /// </summary>
        private static Game GetGame()
        {
            // Return cached game if available (for HTTP thread safety)
            if (_cachedGame != null) return _cachedGame;

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
    }
}

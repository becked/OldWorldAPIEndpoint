using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using TenCrowns.GameCore;
using UnityEngine;

namespace OldWorldAPIEndpoint
{
    /// <summary>
    /// HTTP REST server that provides on-demand game state queries.
    /// Complements the TCP broadcast server with pull-based access.
    /// </summary>
    public class HttpRestServer
    {
        private readonly int _port;
        private HttpListener _listener;
        private volatile bool _running;
        private Thread _listenerThread;
        private readonly Func<Game> _getGameFunc;
        private readonly JsonSerializerSettings _jsonSettings;

        public HttpRestServer(int port, Func<Game> getGameFunc, JsonSerializerSettings jsonSettings)
        {
            _port = port;
            _getGameFunc = getGameFunc;
            _jsonSettings = jsonSettings;
        }

        /// <summary>
        /// Start the HTTP server and begin accepting requests.
        /// </summary>
        public void Start()
        {
            if (_running) return;

            try
            {
                _listener = new HttpListener();
                // Use both localhost and 127.0.0.1 for compatibility
                _listener.Prefixes.Add($"http://localhost:{_port}/");
                _listener.Prefixes.Add($"http://127.0.0.1:{_port}/");
                _listener.Start();
                _running = true;

                _listenerThread = new Thread(ListenerLoop)
                {
                    IsBackground = true,
                    Name = "APIEndpoint-HTTP"
                };
                _listenerThread.Start();

                Debug.Log($"[APIEndpoint] HTTP server started on port {_port}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[APIEndpoint] Failed to start HTTP server: {ex.Message}");
                _running = false;
            }
        }

        /// <summary>
        /// Stop the HTTP server.
        /// </summary>
        public void Stop()
        {
            _running = false;

            try
            {
                _listener?.Stop();
                _listener?.Close();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[HttpRestServer] Error stopping listener: {ex.Message}");
            }

            Debug.Log("[APIEndpoint] HTTP server stopped");
        }

        /// <summary>
        /// Background thread that accepts incoming HTTP requests.
        /// </summary>
        private void ListenerLoop()
        {
            while (_running)
            {
                try
                {
                    var context = _listener.GetContext();
                    ThreadPool.QueueUserWorkItem(_ => HandleRequest(context));
                }
                catch (HttpListenerException) when (!_running)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (_running)
                    {
                        Debug.LogError($"[APIEndpoint] HTTP listener error: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Handle an individual HTTP request.
        /// </summary>
        private void HandleRequest(HttpListenerContext context)
        {
            try
            {
                string path = context.Request.Url.AbsolutePath;
                string method = context.Request.HttpMethod;
                Debug.Log($"[APIEndpoint] HTTP {method} {path}");

                // Handle OPTIONS for CORS preflight
                if (method == "OPTIONS")
                {
                    HandleCorsPreflightRequest(context);
                    return;
                }

                if (method == "POST")
                {
                    HandlePostRequest(context, path);
                    return;
                }

                if (method == "GET")
                {
                    RouteRequest(context, path);
                    return;
                }

                SendErrorResponse(context.Response, "Method not allowed. Use GET or POST.", 405);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[APIEndpoint] HTTP request error: {ex.Message}");
                try
                {
                    SendErrorResponse(context.Response, "Internal server error", 500);
                }
                catch (Exception responseEx)
                {
                    Debug.LogWarning($"[HttpRestServer] Failed to send error response: {responseEx.Message}");
                }
            }
        }

        /// <summary>
        /// Handle CORS preflight OPTIONS request.
        /// </summary>
        private void HandleCorsPreflightRequest(HttpListenerContext context)
        {
            var response = context.Response;
            response.StatusCode = 204; // No Content
            response.AddHeader("Access-Control-Allow-Origin", "*");
            response.AddHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            response.AddHeader("Access-Control-Allow-Headers", "Content-Type");
            response.AddHeader("Access-Control-Max-Age", "86400");
            response.Close();
        }

        /// <summary>
        /// Handle POST requests for command execution.
        /// </summary>
        private void HandlePostRequest(HttpListenerContext context, string path)
        {
            path = path.Trim('/').ToLowerInvariant();

            switch (path)
            {
                case "command":
                    HandleCommandRequest(context);
                    break;
                case "commands":
                    HandleBulkCommandRequest(context);
                    break;
                default:
                    SendErrorResponse(context.Response, $"POST not supported for /{path}. Use /command or /commands.", 405);
                    break;
            }
        }

        /// <summary>
        /// Handle single command execution request.
        /// </summary>
        private void HandleCommandRequest(HttpListenerContext context)
        {
            try
            {
                using var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8);
                string body = reader.ReadToEnd();

                var cmd = JsonConvert.DeserializeObject<GameCommand>(body, _jsonSettings);

                if (cmd == null)
                {
                    SendErrorResponse(context.Response, "Invalid JSON body", 400);
                    return;
                }

                if (string.IsNullOrEmpty(cmd.Action))
                {
                    SendErrorResponse(context.Response, "Missing required field: action", 400);
                    return;
                }

                var result = APIEndpoint.QueueAndWaitCommand(cmd);
                SendJsonResponse(context.Response, result, result.Success ? 200 : 400);
            }
            catch (JsonException ex)
            {
                SendErrorResponse(context.Response, $"Invalid JSON: {ex.Message}", 400);
            }
            catch (Exception ex)
            {
                SendErrorResponse(context.Response, $"Command execution error: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Handle bulk command execution request.
        /// </summary>
        private void HandleBulkCommandRequest(HttpListenerContext context)
        {
            try
            {
                using var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8);
                string body = reader.ReadToEnd();

                var bulkCmd = JsonConvert.DeserializeObject<BulkCommand>(body, _jsonSettings);

                if (bulkCmd == null)
                {
                    SendErrorResponse(context.Response, "Invalid JSON body", 400);
                    return;
                }

                if (bulkCmd.Commands == null || bulkCmd.Commands.Count == 0)
                {
                    SendErrorResponse(context.Response, "Missing or empty commands array", 400);
                    return;
                }

                var result = APIEndpoint.QueueAndWaitBulkCommand(bulkCmd);
                SendJsonResponse(context.Response, result, result.AllSucceeded ? 200 : 400);
            }
            catch (JsonException ex)
            {
                SendErrorResponse(context.Response, $"Invalid JSON: {ex.Message}", 400);
            }
            catch (Exception ex)
            {
                SendErrorResponse(context.Response, $"Bulk command execution error: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Route the request to the appropriate handler.
        /// </summary>
        private void RouteRequest(HttpListenerContext context, string path)
        {
            var game = _getGameFunc();

            if (game == null)
            {
                SendErrorResponse(context.Response, "Game not available", 503);
                return;
            }

            path = path.Trim('/').ToLowerInvariant();
            var segments = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length == 0)
            {
                SendErrorResponse(context.Response, "GET: /state, /players, /cities, /characters, /tribes, /team-diplomacy. POST: /command, /commands", 404);
                return;
            }

            switch (segments[0])
            {
                case "state":
                    HandleStateRequest(context, game);
                    break;

                case "players":
                    HandlePlayersRequest(context, game);
                    break;

                case "player":
                    if (segments.Length > 1 && int.TryParse(segments[1], out int playerIndex))
                    {
                        if (segments.Length == 2)
                        {
                            HandlePlayerRequest(context, game, playerIndex);
                        }
                        else if (segments.Length == 3)
                        {
                            // Handle player sub-resources: /player/{index}/{resource}
                            switch (segments[2])
                            {
                                case "units":
                                    HandlePlayerUnitsRequest(context, game, playerIndex);
                                    break;
                                case "techs":
                                    HandlePlayerTechsRequest(context, game, playerIndex);
                                    break;
                                case "families":
                                    HandlePlayerFamiliesRequest(context, game, playerIndex);
                                    break;
                                case "religion":
                                    HandlePlayerReligionRequest(context, game, playerIndex);
                                    break;
                                case "goals":
                                    HandlePlayerGoalsRequest(context, game, playerIndex);
                                    break;
                                case "decisions":
                                    HandlePlayerDecisionsRequest(context, game, playerIndex);
                                    break;
                                case "laws":
                                    HandlePlayerLawsRequest(context, game, playerIndex);
                                    break;
                                case "missions":
                                    HandlePlayerMissionsRequest(context, game, playerIndex);
                                    break;
                                case "resources":
                                    HandlePlayerResourcesRequest(context, game, playerIndex);
                                    break;
                                default:
                                    SendErrorResponse(context.Response, $"Unknown player resource: {segments[2]}", 404);
                                    break;
                            }
                        }
                        else
                        {
                            SendErrorResponse(context.Response, "Invalid player path", 400);
                        }
                    }
                    else
                        SendErrorResponse(context.Response, "Invalid player index. Use /player/{index}", 400);
                    break;

                case "units":
                    HandleUnitsRequest(context, game);
                    break;

                case "unit":
                    if (segments.Length > 1 && int.TryParse(segments[1], out int unitId))
                        HandleUnitRequest(context, game, unitId);
                    else
                        SendErrorResponse(context.Response, "Invalid unit ID. Use /unit/{id}", 400);
                    break;

                case "cities":
                    HandleCitiesRequest(context, game);
                    break;

                case "city":
                    if (segments.Length > 1 && int.TryParse(segments[1], out int cityId))
                        HandleCityRequest(context, game, cityId);
                    else
                        SendErrorResponse(context.Response, "Invalid city ID. Use /city/{id}", 400);
                    break;

                case "characters":
                    HandleCharactersRequest(context, game);
                    break;

                case "character":
                    if (segments.Length > 1 && int.TryParse(segments[1], out int charId))
                        HandleCharacterRequest(context, game, charId);
                    else
                        SendErrorResponse(context.Response, "Invalid character ID. Use /character/{id}", 400);
                    break;

                case "character-events":
                    HandleCharacterEventsRequest(context);
                    break;

                case "unit-events":
                    HandleUnitEventsRequest(context);
                    break;

                case "city-events":
                    HandleCityEventsRequest(context);
                    break;

                case "tribes":
                    HandleTribesRequest(context, game);
                    break;

                case "tribe":
                    if (segments.Length > 1)
                        HandleTribeRequest(context, game, segments[1]);
                    else
                        SendErrorResponse(context.Response, "Missing tribe type. Use /tribe/{TRIBE_TYPE}", 400);
                    break;

                case "team-diplomacy":
                    HandleTeamDiplomacyRequest(context, game);
                    break;

                case "team-alliances":
                    HandleTeamAlliancesRequest(context, game);
                    break;

                case "tribe-diplomacy":
                    HandleTribeDiplomacyRequest(context, game);
                    break;

                case "tribe-alliances":
                    HandleTribeAlliancesRequest(context, game);
                    break;

                case "religions":
                    HandleReligionsRequest(context, game);
                    break;

                case "map":
                    HandleMapRequest(context, game);
                    break;

                case "tiles":
                    HandleTilesRequest(context, game);
                    break;

                case "tile":
                    if (segments.Length == 2 && int.TryParse(segments[1], out int tileId))
                        HandleTileRequest(context, game, tileId);
                    else if (segments.Length == 3 && int.TryParse(segments[1], out int tileX) && int.TryParse(segments[2], out int tileY))
                        HandleTileByCoords(context, game, tileX, tileY);
                    else
                        SendErrorResponse(context.Response, "Invalid tile path. Use /tile/{id} or /tile/{x}/{y}", 400);
                    break;

                case "config":
                    HandleConfigRequest(context, game);
                    break;

                default:
                    SendErrorResponse(context.Response, $"Unknown endpoint: /{path}", 404);
                    break;
            }
        }

        #region Request Handlers

        private void HandleStateRequest(HttpListenerContext context, Game game)
        {
            var state = new
            {
                turn = game.getTurn(),
                year = game.getYear(),
                currentPlayer = (int)game.getPlayerTurn(),
                players = APIEndpoint.BuildPlayersObject(game),
                characters = APIEndpoint.BuildCharactersObject(game),
                cities = APIEndpoint.BuildCitiesObject(game),
                teamDiplomacy = APIEndpoint.BuildTeamDiplomacyObject(game),
                teamAlliances = APIEndpoint.BuildTeamAlliancesObject(game),
                tribes = APIEndpoint.BuildTribesObject(game),
                tribeDiplomacy = APIEndpoint.BuildTribeDiplomacyObject(game),
                tribeAlliances = APIEndpoint.BuildTribeAlliancesObject(game)
            };
            SendJsonResponse(context.Response, state);
        }

        private void HandlePlayersRequest(HttpListenerContext context, Game game)
        {
            var players = APIEndpoint.BuildPlayersObject(game);
            SendJsonResponse(context.Response, players);
        }

        private void HandlePlayerRequest(HttpListenerContext context, Game game, int index)
        {
            var player = APIEndpoint.GetPlayerByIndex(game, index);
            if (player != null)
                SendJsonResponse(context.Response, player);
            else
                SendErrorResponse(context.Response, $"Player not found: {index}", 404);
        }

        private void HandlePlayerUnitsRequest(HttpListenerContext context, Game game, int index)
        {
            var units = APIEndpoint.BuildPlayerUnitsObject(game, index);
            SendJsonResponse(context.Response, units);
        }

        private void HandlePlayerTechsRequest(HttpListenerContext context, Game game, int index)
        {
            if (!APIEndpoint.TryGetPlayer(game, index, out var player))
            {
                SendErrorResponse(context.Response, $"Player not found: {index}", 404);
                return;
            }
            var techs = APIEndpoint.BuildPlayerTechs(player, game, game.infos());
            SendJsonResponse(context.Response, techs);
        }

        private void HandlePlayerFamiliesRequest(HttpListenerContext context, Game game, int index)
        {
            if (!APIEndpoint.TryGetPlayer(game, index, out var player))
            {
                SendErrorResponse(context.Response, $"Player not found: {index}", 404);
                return;
            }
            var families = APIEndpoint.BuildPlayerFamilies(player, game, game.infos());
            SendJsonResponse(context.Response, families);
        }

        private void HandlePlayerReligionRequest(HttpListenerContext context, Game game, int index)
        {
            if (!APIEndpoint.TryGetPlayer(game, index, out var player))
            {
                SendErrorResponse(context.Response, $"Player not found: {index}", 404);
                return;
            }
            var religion = APIEndpoint.BuildPlayerReligion(player, game, game.infos());
            SendJsonResponse(context.Response, religion);
        }

        private void HandlePlayerGoalsRequest(HttpListenerContext context, Game game, int index)
        {
            if (!APIEndpoint.TryGetPlayer(game, index, out var player))
            {
                SendErrorResponse(context.Response, $"Player not found: {index}", 404);
                return;
            }
            var goals = APIEndpoint.BuildPlayerGoals(player, game, game.infos());
            SendJsonResponse(context.Response, goals);
        }

        private void HandlePlayerDecisionsRequest(HttpListenerContext context, Game game, int index)
        {
            if (!APIEndpoint.TryGetPlayer(game, index, out var player))
            {
                SendErrorResponse(context.Response, $"Player not found: {index}", 404);
                return;
            }
            var decisions = APIEndpoint.BuildPlayerDecisions(player, game, game.infos());
            SendJsonResponse(context.Response, decisions);
        }

        private void HandlePlayerLawsRequest(HttpListenerContext context, Game game, int index)
        {
            if (!APIEndpoint.TryGetPlayer(game, index, out var player))
            {
                SendErrorResponse(context.Response, $"Player not found: {index}", 404);
                return;
            }
            var laws = APIEndpoint.BuildPlayerLaws(player, game, game.infos());
            SendJsonResponse(context.Response, laws);
        }

        private void HandlePlayerMissionsRequest(HttpListenerContext context, Game game, int index)
        {
            if (!APIEndpoint.TryGetPlayer(game, index, out var player))
            {
                SendErrorResponse(context.Response, $"Player not found: {index}", 404);
                return;
            }
            var missions = APIEndpoint.BuildPlayerMissions(player, game, game.infos());
            SendJsonResponse(context.Response, missions);
        }

        private void HandlePlayerResourcesRequest(HttpListenerContext context, Game game, int index)
        {
            if (!APIEndpoint.TryGetPlayer(game, index, out var player))
            {
                SendErrorResponse(context.Response, $"Player not found: {index}", 404);
                return;
            }
            var resources = APIEndpoint.BuildPlayerResources(player, game, game.infos());
            SendJsonResponse(context.Response, resources);
        }

        private void HandleUnitsRequest(HttpListenerContext context, Game game)
        {
            var units = APIEndpoint.BuildUnitsObject(game);
            SendJsonResponse(context.Response, units);
        }

        private void HandleUnitRequest(HttpListenerContext context, Game game, int unitId)
        {
            var unit = APIEndpoint.GetUnitById(game, unitId);
            if (unit != null)
                SendJsonResponse(context.Response, unit);
            else
                SendErrorResponse(context.Response, $"Unit not found: {unitId}", 404);
        }

        private void HandleCitiesRequest(HttpListenerContext context, Game game)
        {
            var cities = APIEndpoint.BuildCitiesObject(game);
            SendJsonResponse(context.Response, cities);
        }

        private void HandleCityRequest(HttpListenerContext context, Game game, int cityId)
        {
            var city = APIEndpoint.GetCityById(game, cityId);
            if (city != null)
                SendJsonResponse(context.Response, city);
            else
                SendErrorResponse(context.Response, $"City not found: {cityId}", 404);
        }

        private void HandleCharactersRequest(HttpListenerContext context, Game game)
        {
            var characters = APIEndpoint.BuildCharactersObject(game);
            SendJsonResponse(context.Response, characters);
        }

        private void HandleCharacterRequest(HttpListenerContext context, Game game, int charId)
        {
            var character = APIEndpoint.GetCharacterById(game, charId);
            if (character != null)
                SendJsonResponse(context.Response, character);
            else
                SendErrorResponse(context.Response, $"Character not found: {charId}", 404);
        }

        private void HandleCharacterEventsRequest(HttpListenerContext context)
        {
            var events = APIEndpoint.GetLastCharacterEvents();
            SendJsonResponse(context.Response, events);
        }

        private void HandleUnitEventsRequest(HttpListenerContext context)
        {
            var events = APIEndpoint.GetLastUnitEvents();
            SendJsonResponse(context.Response, events);
        }

        private void HandleCityEventsRequest(HttpListenerContext context)
        {
            var events = APIEndpoint.GetLastCityEvents();
            SendJsonResponse(context.Response, events);
        }

        private void HandleTribesRequest(HttpListenerContext context, Game game)
        {
            var tribes = APIEndpoint.BuildTribesObject(game);
            SendJsonResponse(context.Response, tribes);
        }

        private void HandleTribeRequest(HttpListenerContext context, Game game, string tribeType)
        {
            var tribe = APIEndpoint.GetTribeByType(game, tribeType);
            if (tribe != null)
                SendJsonResponse(context.Response, tribe);
            else
                SendErrorResponse(context.Response, $"Tribe not found: {tribeType}", 404);
        }

        private void HandleTeamDiplomacyRequest(HttpListenerContext context, Game game)
        {
            var diplomacy = APIEndpoint.BuildTeamDiplomacyObject(game);
            SendJsonResponse(context.Response, diplomacy);
        }

        private void HandleTeamAlliancesRequest(HttpListenerContext context, Game game)
        {
            var alliances = APIEndpoint.BuildTeamAlliancesObject(game);
            SendJsonResponse(context.Response, alliances);
        }

        private void HandleTribeDiplomacyRequest(HttpListenerContext context, Game game)
        {
            var diplomacy = APIEndpoint.BuildTribeDiplomacyObject(game);
            SendJsonResponse(context.Response, diplomacy);
        }

        private void HandleTribeAlliancesRequest(HttpListenerContext context, Game game)
        {
            var alliances = APIEndpoint.BuildTribeAlliancesObject(game);
            SendJsonResponse(context.Response, alliances);
        }

        private void HandleReligionsRequest(HttpListenerContext context, Game game)
        {
            var religions = APIEndpoint.BuildReligionsObject(game);
            SendJsonResponse(context.Response, religions);
        }

        private void HandleMapRequest(HttpListenerContext context, Game game)
        {
            var map = APIEndpoint.BuildMapObject(game);
            SendJsonResponse(context.Response, map);
        }

        private void HandleTilesRequest(HttpListenerContext context, Game game)
        {
            // Parse pagination parameters from query string
            int offset = 0;
            int limit = 100; // Default limit

            var query = context.Request.QueryString;
            if (!string.IsNullOrEmpty(query["offset"]))
                int.TryParse(query["offset"], out offset);
            if (!string.IsNullOrEmpty(query["limit"]))
            {
                int.TryParse(query["limit"], out limit);
                // Cap at 1000 tiles per request to prevent huge responses
                limit = Math.Min(limit, 1000);
            }

            var tiles = APIEndpoint.BuildTilesObjectPaginated(game, offset, limit);
            SendJsonResponse(context.Response, tiles);
        }

        private void HandleTileRequest(HttpListenerContext context, Game game, int tileId)
        {
            var tile = APIEndpoint.GetTileById(game, tileId);
            if (tile != null)
                SendJsonResponse(context.Response, tile);
            else
                SendErrorResponse(context.Response, $"Tile not found: {tileId}", 404);
        }

        private void HandleTileByCoords(HttpListenerContext context, Game game, int x, int y)
        {
            var tile = APIEndpoint.GetTileByCoords(game, x, y);
            if (tile != null)
                SendJsonResponse(context.Response, tile);
            else
                SendErrorResponse(context.Response, $"Tile not found at ({x}, {y})", 404);
        }

        private void HandleConfigRequest(HttpListenerContext context, Game game)
        {
            var config = APIEndpoint.BuildConfigObject(game);
            SendJsonResponse(context.Response, config);
        }

        #endregion

        #region Response Helpers

        private void SendJsonResponse(HttpListenerResponse response, object data, int statusCode = 200)
        {
            string json = JsonConvert.SerializeObject(data, _jsonSettings);
            byte[] buffer = Encoding.UTF8.GetBytes(json);

            response.StatusCode = statusCode;
            response.ContentType = "application/json";
            response.ContentLength64 = buffer.Length;

            // CORS headers for browser clients
            response.AddHeader("Access-Control-Allow-Origin", "*");
            response.AddHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            response.AddHeader("Access-Control-Allow-Headers", "Content-Type");

            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.Close();
        }

        private void SendErrorResponse(HttpListenerResponse response, string error, int statusCode)
        {
            var errorObj = new
            {
                error = error,
                code = statusCode
            };

            string json = JsonConvert.SerializeObject(errorObj, _jsonSettings);
            byte[] buffer = Encoding.UTF8.GetBytes(json);

            response.StatusCode = statusCode;
            response.ContentType = "application/json";
            response.ContentLength64 = buffer.Length;

            // CORS headers for browser clients
            response.AddHeader("Access-Control-Allow-Origin", "*");
            response.AddHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            response.AddHeader("Access-Control-Allow-Headers", "Content-Type");

            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.Close();
        }

        #endregion
    }
}

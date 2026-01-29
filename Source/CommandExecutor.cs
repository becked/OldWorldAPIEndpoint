using System;
using System.Collections.Generic;
using System.Reflection;
using TenCrowns.GameCore;
using UnityEngine;

namespace OldWorldAPIEndpoint
{
    /// <summary>
    /// Executes game commands via ClientManager.
    /// Uses reflection to access ClientManager methods since Assembly-CSharp
    /// cannot be directly referenced by mods.
    /// </summary>
    public static class CommandExecutor
    {
        // Cached reflection info for ClientManager methods
        private static Type _clientManagerType;
        private static MethodInfo _canDoActionsMethod;
        private static MethodInfo _sendUnitMoveMethod;
        private static MethodInfo _sendUnitAttackMethod;
        private static MethodInfo _sendUnitFortifyMethod;
        private static MethodInfo _sendUnitPassMethod;
        private static MethodInfo _sendUnitSleepMethod;
        private static MethodInfo _sendUnitSentryMethod;
        private static MethodInfo _sendUnitWakeMethod;
        private static MethodInfo _sendUnitDisbandMethod;
        private static MethodInfo _sendUnitPromoteMethod;
        private static MethodInfo _sendCityBuildUnitMethod;
        private static MethodInfo _sendCityBuildProjectMethod;
        private static MethodInfo _sendHurryCivicsMethod;
        private static MethodInfo _sendHurryTrainingMethod;
        private static MethodInfo _sendHurryMoneyMethod;
        private static MethodInfo _sendHurryPopulationMethod;
        private static MethodInfo _sendHurryOrdersMethod;
        private static MethodInfo _sendResearchMethod;
        private static MethodInfo _sendEndTurnMethod;
        private static PropertyInfo _gameClientProperty;
        private static bool _reflectionInitialized;

        /// <summary>
        /// Initialize reflection for ClientManager access.
        /// </summary>
        private static void InitializeReflection(object clientManager)
        {
            if (_reflectionInitialized || clientManager == null) return;

            try
            {
                _clientManagerType = clientManager.GetType();

                // Core action check
                _canDoActionsMethod = _clientManagerType.GetMethod("canDoActions",
                    BindingFlags.Public | BindingFlags.Instance);

                // GameClient property
                _gameClientProperty = _clientManagerType.GetProperty("GameClient",
                    BindingFlags.Public | BindingFlags.Instance);

                // Unit commands - methods take Unit/Tile objects, not IDs
                // sendMoveUnit(Unit, Tile, Boolean, Boolean, Tile)
                _sendUnitMoveMethod = _clientManagerType.GetMethod("sendMoveUnit",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendAttack(Unit, Tile)
                _sendUnitAttackMethod = _clientManagerType.GetMethod("sendAttack",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendFortify(Unit)
                _sendUnitFortifyMethod = _clientManagerType.GetMethod("sendFortify",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendPass(Unit)
                _sendUnitPassMethod = _clientManagerType.GetMethod("sendPass",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendSleep(Unit)
                _sendUnitSleepMethod = _clientManagerType.GetMethod("sendSleep",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendSentry(Unit)
                _sendUnitSentryMethod = _clientManagerType.GetMethod("sendSentry",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendWake(Unit)
                _sendUnitWakeMethod = _clientManagerType.GetMethod("sendWake",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendDisband(Unit, Boolean)
                _sendUnitDisbandMethod = _clientManagerType.GetMethod("sendDisband",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendPromote(Unit, PromotionType)
                _sendUnitPromoteMethod = _clientManagerType.GetMethod("sendPromote",
                    BindingFlags.Public | BindingFlags.Instance);

                // City commands
                // sendBuildUnit(City, UnitType, Boolean, Tile, Boolean)
                _sendCityBuildUnitMethod = _clientManagerType.GetMethod("sendBuildUnit",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendBuildProject(City, ProjectType, Boolean, Boolean, Boolean)
                _sendCityBuildProjectMethod = _clientManagerType.GetMethod("sendBuildProject",
                    BindingFlags.Public | BindingFlags.Instance);
                // Hurry methods - each takes just (City)
                _sendHurryCivicsMethod = _clientManagerType.GetMethod("sendHurryCivics",
                    BindingFlags.Public | BindingFlags.Instance);
                _sendHurryTrainingMethod = _clientManagerType.GetMethod("sendHurryTraining",
                    BindingFlags.Public | BindingFlags.Instance);
                _sendHurryMoneyMethod = _clientManagerType.GetMethod("sendHurryMoney",
                    BindingFlags.Public | BindingFlags.Instance);
                _sendHurryPopulationMethod = _clientManagerType.GetMethod("sendHurryPopulation",
                    BindingFlags.Public | BindingFlags.Instance);
                _sendHurryOrdersMethod = _clientManagerType.GetMethod("sendHurryOrders",
                    BindingFlags.Public | BindingFlags.Instance);

                // Research - sendResearchTech(TechType)
                _sendResearchMethod = _clientManagerType.GetMethod("sendResearchTech",
                    BindingFlags.Public | BindingFlags.Instance);

                // Turn
                _sendEndTurnMethod = FindMethod(_clientManagerType, "sendEndTurn",
                    Type.EmptyTypes);

                // Log what we found
                Debug.Log($"[APIEndpoint] CommandExecutor reflection on {_clientManagerType.Name}:");
                Debug.Log($"[APIEndpoint]   canDoActions: {_canDoActionsMethod != null}");
                Debug.Log($"[APIEndpoint]   sendMoveUnit: {_sendUnitMoveMethod != null}");
                Debug.Log($"[APIEndpoint]   sendBuildUnit: {_sendCityBuildUnitMethod != null}");
                Debug.Log($"[APIEndpoint]   sendBuildProject: {_sendCityBuildProjectMethod != null}");
                Debug.Log($"[APIEndpoint]   sendHurryCivics: {_sendHurryCivicsMethod != null}");
                Debug.Log($"[APIEndpoint]   sendEndTurn: {_sendEndTurnMethod != null}");

                // Log methods containing "unit", "move", "send", "attack"
                Debug.Log($"[APIEndpoint] Methods on {_clientManagerType.Name}:");
                var methods = _clientManagerType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
                foreach (var m in methods)
                {
                    string nameLower = m.Name.ToLower();
                    if (nameLower.Contains("unit") || nameLower.Contains("move") ||
                        nameLower.Contains("attack") || nameLower.Contains("send") ||
                        nameLower.Contains("action") || nameLower.Contains("order"))
                    {
                        var paramStr = string.Join(", ", Array.ConvertAll(m.GetParameters(), p => p.ParameterType.Name));
                        Debug.Log($"[APIEndpoint]   Method: {m.Name}({paramStr})");
                    }
                }

                Debug.Log($"[APIEndpoint] CommandExecutor reflection initialized");
                _reflectionInitialized = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[APIEndpoint] CommandExecutor reflection failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Find a method with the given name and parameter types, or any matching name if not found.
        /// </summary>
        private static MethodInfo FindMethod(Type type, string name, Type[] paramTypes)
        {
            // Try exact match first
            var method = type.GetMethod(name, BindingFlags.Public | BindingFlags.Instance, null, paramTypes, null);
            if (method != null) return method;

            // Fall back to any method with that name
            return type.GetMethod(name, BindingFlags.Public | BindingFlags.Instance);
        }

        /// <summary>
        /// Execute a game command via ClientManager.
        /// Must be called from Unity's main thread.
        /// </summary>
        public static CommandResult Execute(object clientManager, Game game, GameCommand cmd)
        {
            var result = new CommandResult { RequestId = cmd.RequestId };

            if (clientManager == null)
            {
                result.Error = "ClientManager not available";
                return result;
            }

            InitializeReflection(clientManager);

            // Check if player can perform actions
            if (_canDoActionsMethod != null)
            {
                try
                {
                    bool canAct = (bool)_canDoActionsMethod.Invoke(clientManager, null);
                    if (!canAct)
                    {
                        result.Error = "Cannot perform actions (not player's turn or action blocked)";
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[CommandExecutor] canDoActions check failed: {ex.Message}");
                }
            }

            // Check for multiplayer (refuse commands in MP)
            if (game != null && game.isMultiplayer())
            {
                result.Error = "Commands not supported in multiplayer games";
                return result;
            }

            // Dispatch based on action
            string action = cmd.Action?.ToLowerInvariant() ?? "";

            switch (action)
            {
                // Unit Movement & Combat
                case "moveunit":
                    return ExecuteMoveUnit(clientManager, game, cmd, result);
                case "attack":
                    return ExecuteAttack(clientManager, game, cmd, result);
                case "fortify":
                    return ExecuteFortify(clientManager, game, cmd, result);
                case "pass":
                case "skip":
                    return ExecutePass(clientManager, game, cmd, result);
                case "sleep":
                    return ExecuteSleep(clientManager, game, cmd, result);
                case "sentry":
                    return ExecuteSentry(clientManager, game, cmd, result);
                case "wake":
                    return ExecuteWake(clientManager, game, cmd, result);
                case "disband":
                    return ExecuteDisband(clientManager, game, cmd, result);
                case "promote":
                    return ExecutePromote(clientManager, game, cmd, result);

                // City Production
                case "build":
                case "buildunit":
                    return ExecuteBuildUnit(clientManager, game, cmd, result);
                case "buildproject":
                    return ExecuteBuildProject(clientManager, game, cmd, result);
                case "hurry":
                case "hurryCivics":
                case "hurrycivics":
                    return ExecuteHurryCivics(clientManager, game, cmd, result);
                case "hurryTraining":
                case "hurrytraining":
                    return ExecuteHurryTraining(clientManager, game, cmd, result);
                case "hurryMoney":
                case "hurrymoney":
                    return ExecuteHurryMoney(clientManager, game, cmd, result);
                case "hurryPopulation":
                case "hurrypopulation":
                    return ExecuteHurryPopulation(clientManager, game, cmd, result);
                case "hurryOrders":
                case "hurryorders":
                    return ExecuteHurryOrders(clientManager, game, cmd, result);

                // Research
                case "research":
                    return ExecuteResearch(clientManager, game, cmd, result);

                // Turn Management
                case "endturn":
                    return ExecuteEndTurn(clientManager, game, cmd, result);

                default:
                    result.Error = $"Unknown action: {cmd.Action}";
                    return result;
            }
        }

        #region Parameter Extraction Helpers

        private static int GetIntParam(GameCommand cmd, string key, int defaultValue = -1)
        {
            if (cmd.Params == null || !cmd.Params.TryGetValue(key, out var value))
                return defaultValue;

            if (value is int i) return i;
            if (value is long l) return (int)l;
            if (value is double d) return (int)d;
            if (int.TryParse(value?.ToString(), out int parsed)) return parsed;

            return defaultValue;
        }

        private static string GetStringParam(GameCommand cmd, string key, string defaultValue = null)
        {
            if (cmd.Params == null || !cmd.Params.TryGetValue(key, out var value))
                return defaultValue;

            return value?.ToString() ?? defaultValue;
        }

        private static bool GetBoolParam(GameCommand cmd, string key, bool defaultValue = false)
        {
            if (cmd.Params == null || !cmd.Params.TryGetValue(key, out var value))
                return defaultValue;

            if (value is bool b) return b;
            if (bool.TryParse(value?.ToString(), out bool parsed)) return parsed;

            return defaultValue;
        }

        /// <summary>
        /// Try to get an integer parameter with detailed parse result.
        /// Distinguishes between missing parameters and invalid types.
        /// </summary>
        private static bool TryGetIntParam(GameCommand cmd, string key, out ParseResult<int> result)
        {
            result = new ParseResult<int>();

            if (cmd.Params == null || !cmd.Params.TryGetValue(key, out var value))
            {
                result.Found = false;
                return false;
            }

            result.Found = true;
            result.RawValue = value?.ToString() ?? "null";

            if (value is int i)
            {
                result.Valid = true;
                result.Value = i;
                return true;
            }
            if (value is long l)
            {
                result.Valid = true;
                result.Value = (int)l;
                return true;
            }
            if (value is double d)
            {
                result.Valid = true;
                result.Value = (int)d;
                return true;
            }
            if (int.TryParse(value?.ToString(), out int parsed))
            {
                result.Valid = true;
                result.Value = parsed;
                return true;
            }

            Debug.LogWarning($"[CommandExecutor] Parse failed for '{key}': expected int, got '{result.RawValue}'");
            result.Valid = false;
            return false;
        }

        /// <summary>
        /// Try to get a string parameter with detailed parse result.
        /// </summary>
        private static bool TryGetStringParam(GameCommand cmd, string key, out ParseResult<string> result)
        {
            result = new ParseResult<string>();

            if (cmd.Params == null || !cmd.Params.TryGetValue(key, out var value))
            {
                result.Found = false;
                return false;
            }

            result.Found = true;
            result.RawValue = value?.ToString() ?? "null";
            result.Value = result.RawValue;
            result.Valid = !string.IsNullOrEmpty(result.Value);

            if (!result.Valid)
            {
                Debug.LogWarning($"[CommandExecutor] Parse failed for '{key}': expected non-empty string, got '{result.RawValue}'");
            }

            return result.Valid;
        }

        /// <summary>
        /// Generate an appropriate error message based on parse result.
        /// </summary>
        private static string GetParamError<T>(string key, ParseResult<T> result, string expectedType)
        {
            if (!result.Found)
                return $"Missing required parameter: {key}";
            return $"Invalid type for parameter '{key}': expected {expectedType}, got '{result.RawValue}'";
        }

        /// <summary>
        /// Resolve a unit type string (e.g., "UNIT_WARRIOR") to UnitType enum.
        /// </summary>
        private static UnitType ResolveUnitType(Game game, string typeStr)
        {
            if (string.IsNullOrEmpty(typeStr) || game == null) return UnitType.NONE;

            Infos infos = game.infos();
            int count = (int)infos.unitsNum();

            for (int i = 0; i < count; i++)
            {
                var unitType = (UnitType)i;
                if (infos.unit(unitType).mzType.Equals(typeStr, StringComparison.OrdinalIgnoreCase))
                    return unitType;
            }

            return UnitType.NONE;
        }

        /// <summary>
        /// Resolve a tech type string (e.g., "TECH_FORESTRY") to TechType enum.
        /// </summary>
        private static TechType ResolveTechType(Game game, string typeStr)
        {
            if (string.IsNullOrEmpty(typeStr) || game == null) return TechType.NONE;

            Infos infos = game.infos();
            int count = (int)infos.techsNum();

            for (int i = 0; i < count; i++)
            {
                var techType = (TechType)i;
                if (infos.tech(techType).mzType.Equals(typeStr, StringComparison.OrdinalIgnoreCase))
                    return techType;
            }

            return TechType.NONE;
        }

        /// <summary>
        /// Resolve a project type string (e.g., "PROJECT_TREASURE") to ProjectType enum.
        /// </summary>
        private static ProjectType ResolveProjectType(Game game, string typeStr)
        {
            if (string.IsNullOrEmpty(typeStr) || game == null) return ProjectType.NONE;

            Infos infos = game.infos();
            int count = (int)infos.projectsNum();

            for (int i = 0; i < count; i++)
            {
                var projType = (ProjectType)i;
                if (infos.project(projType).mzType.Equals(typeStr, StringComparison.OrdinalIgnoreCase))
                    return projType;
            }

            return ProjectType.NONE;
        }

        /// <summary>
        /// Resolve a promotion type string (e.g., "PROMOTION_FIERCE") to PromotionType enum.
        /// </summary>
        private static PromotionType ResolvePromotionType(Game game, string typeStr)
        {
            if (string.IsNullOrEmpty(typeStr) || game == null) return PromotionType.NONE;

            Infos infos = game.infos();
            int count = (int)infos.promotionsNum();

            for (int i = 0; i < count; i++)
            {
                var promoType = (PromotionType)i;
                if (infos.promotion(promoType).mzType.Equals(typeStr, StringComparison.OrdinalIgnoreCase))
                    return promoType;
            }

            return PromotionType.NONE;
        }

        /// <summary>
        /// Resolve a yield type string (e.g., "YIELD_CIVICS") to YieldType enum.
        /// </summary>
        private static YieldType ResolveYieldType(Game game, string typeStr)
        {
            if (string.IsNullOrEmpty(typeStr) || game == null) return YieldType.NONE;

            Infos infos = game.infos();
            int count = (int)infos.yieldsNum();

            for (int i = 0; i < count; i++)
            {
                var yieldType = (YieldType)i;
                if (infos.yield(yieldType).mzType.Equals(typeStr, StringComparison.OrdinalIgnoreCase))
                    return yieldType;
            }

            return YieldType.NONE;
        }

        #endregion

        #region Command Implementations

        private static CommandResult ExecuteMoveUnit(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            if (!TryGetIntParam(cmd, "targetTileId", out var targetTileIdResult))
            {
                result.Error = GetParamError("targetTileId", targetTileIdResult, "integer");
                return result;
            }

            int unitId = unitIdResult.Value;
            int targetTileId = targetTileIdResult.Value;
            bool queueMove = GetBoolParam(cmd, "queue", false);
            bool marchMove = GetBoolParam(cmd, "march", false);
            int waypointTileId = GetIntParam(cmd, "waypointTileId", -1);

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            if (_sendUnitMoveMethod == null)
            {
                result.Error = "Move command not available";
                return result;
            }

            try
            {
                // Get Unit and Tile objects from game
                Unit unit = game.unit(unitId);
                if (unit == null)
                {
                    result.Error = $"Unit not found: {unitId}";
                    return result;
                }

                Tile targetTile = game.tile(targetTileId);
                if (targetTile == null)
                {
                    result.Error = $"Tile not found: {targetTileId}";
                    return result;
                }

                // Optional waypoint tile
                Tile waypointTile = waypointTileId >= 0 ? game.tile(waypointTileId) : null;

                // sendMoveUnit(Unit, Tile, Boolean march, Boolean queue, Tile waypoint)
                _sendUnitMoveMethod.Invoke(clientManager, new object[] { unit, targetTile, marchMove, queueMove, waypointTile });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Moved unit {unitId} to tile {targetTileId}{(waypointTile != null ? $" via waypoint {waypointTileId}" : "")}");
            }
            catch (Exception ex)
            {
                result.Error = $"Move failed: {ex.InnerException?.Message ?? ex.Message}";
                Debug.LogError($"[APIEndpoint] Move error: {ex}");
            }

            return result;
        }

        private static CommandResult ExecuteAttack(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            if (!TryGetIntParam(cmd, "targetTileId", out var targetTileIdResult))
            {
                result.Error = GetParamError("targetTileId", targetTileIdResult, "integer");
                return result;
            }

            int unitId = unitIdResult.Value;
            int targetTileId = targetTileIdResult.Value;

            if (_sendUnitAttackMethod == null)
            {
                result.Error = "Attack command not available";
                return result;
            }

            try
            {
                // sendAttack(Unit, Tile)
                Unit unit = game.unit(unitId);
                if (unit == null)
                {
                    result.Error = $"Unit not found: {unitId}";
                    return result;
                }
                Tile targetTile = game.tile(targetTileId);
                if (targetTile == null)
                {
                    result.Error = $"Tile not found: {targetTileId}";
                    return result;
                }
                _sendUnitAttackMethod.Invoke(clientManager, new object[] { unit, targetTile });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"Attack failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteFortify(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            int unitId = unitIdResult.Value;

            if (_sendUnitFortifyMethod == null)
            {
                result.Error = "Fortify command not available";
                return result;
            }

            try
            {
                // sendFortify(Unit)
                Unit unit = game.unit(unitId);
                if (unit == null)
                {
                    result.Error = $"Unit not found: {unitId}";
                    return result;
                }
                _sendUnitFortifyMethod.Invoke(clientManager, new object[] { unit });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"Fortify failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecutePass(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            int unitId = unitIdResult.Value;

            if (_sendUnitPassMethod == null)
            {
                result.Error = "Pass command not available";
                return result;
            }

            try
            {
                // sendPass(Unit)
                Unit unit = game.unit(unitId);
                if (unit == null)
                {
                    result.Error = $"Unit not found: {unitId}";
                    return result;
                }
                _sendUnitPassMethod.Invoke(clientManager, new object[] { unit });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"Pass failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteSleep(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            int unitId = unitIdResult.Value;

            if (_sendUnitSleepMethod == null)
            {
                result.Error = "Sleep command not available";
                return result;
            }

            try
            {
                // sendSleep(Unit)
                Unit unit = game.unit(unitId);
                if (unit == null)
                {
                    result.Error = $"Unit not found: {unitId}";
                    return result;
                }
                _sendUnitSleepMethod.Invoke(clientManager, new object[] { unit });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"Sleep failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteSentry(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            int unitId = unitIdResult.Value;

            if (_sendUnitSentryMethod == null)
            {
                result.Error = "Sentry command not available";
                return result;
            }

            try
            {
                // sendSentry(Unit)
                Unit unit = game.unit(unitId);
                if (unit == null)
                {
                    result.Error = $"Unit not found: {unitId}";
                    return result;
                }
                _sendUnitSentryMethod.Invoke(clientManager, new object[] { unit });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"Sentry failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteWake(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            int unitId = unitIdResult.Value;

            if (_sendUnitWakeMethod == null)
            {
                result.Error = "Wake command not available";
                return result;
            }

            try
            {
                // sendWake(Unit)
                Unit unit = game.unit(unitId);
                if (unit == null)
                {
                    result.Error = $"Unit not found: {unitId}";
                    return result;
                }
                _sendUnitWakeMethod.Invoke(clientManager, new object[] { unit });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"Wake failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteDisband(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            int unitId = unitIdResult.Value;
            bool force = GetBoolParam(cmd, "force", false);

            if (_sendUnitDisbandMethod == null)
            {
                result.Error = "Disband command not available";
                return result;
            }

            try
            {
                // sendDisband(Unit, Boolean)
                Unit unit = game.unit(unitId);
                if (unit == null)
                {
                    result.Error = $"Unit not found: {unitId}";
                    return result;
                }
                _sendUnitDisbandMethod.Invoke(clientManager, new object[] { unit, force });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"Disband failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecutePromote(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            if (!TryGetStringParam(cmd, "promotion", out var promotionResult))
            {
                result.Error = GetParamError("promotion", promotionResult, "string");
                return result;
            }

            int unitId = unitIdResult.Value;
            string promotionStr = promotionResult.Value;

            PromotionType promotionType = ResolvePromotionType(game, promotionStr);
            if (promotionType == PromotionType.NONE)
            {
                result.Error = $"Unknown promotion type: {promotionStr}";
                return result;
            }

            if (_sendUnitPromoteMethod == null)
            {
                result.Error = "Promote command not available";
                return result;
            }

            try
            {
                // sendPromote(Unit, PromotionType)
                Unit unit = game.unit(unitId);
                if (unit == null)
                {
                    result.Error = $"Unit not found: {unitId}";
                    return result;
                }
                _sendUnitPromoteMethod.Invoke(clientManager, new object[] { unit, promotionType });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"Promote failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteBuildUnit(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "cityId", out var cityIdResult))
            {
                result.Error = GetParamError("cityId", cityIdResult, "integer");
                return result;
            }

            if (!TryGetStringParam(cmd, "unitType", out var unitTypeResult))
            {
                result.Error = GetParamError("unitType", unitTypeResult, "string");
                return result;
            }

            int cityId = cityIdResult.Value;
            string unitTypeStr = unitTypeResult.Value;
            bool buyGoods = GetBoolParam(cmd, "buyGoods", false);
            bool first = GetBoolParam(cmd, "first", false);

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            // Resolve unit type
            UnitType unitType = ResolveUnitType(game, unitTypeStr);
            if (unitType == UnitType.NONE)
            {
                result.Error = $"Unknown unit type: {unitTypeStr}";
                return result;
            }

            // Get the city object
            City city = game.city(cityId);
            if (city == null)
            {
                result.Error = $"City not found: {cityId}";
                return result;
            }

            if (_sendCityBuildUnitMethod == null)
            {
                result.Error = "BuildUnit command not available";
                return result;
            }

            try
            {
                // sendBuildUnit(City, UnitType, Boolean buyGoods, Tile rallyTile, Boolean first)
                _sendCityBuildUnitMethod.Invoke(clientManager, new object[] { city, unitType, buyGoods, city.tile(), first });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Building unit {unitTypeStr} in city {cityId}");
            }
            catch (Exception ex)
            {
                result.Error = $"BuildUnit failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteBuildProject(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "cityId", out var cityIdResult))
            {
                result.Error = GetParamError("cityId", cityIdResult, "integer");
                return result;
            }

            if (!TryGetStringParam(cmd, "projectType", out var projectTypeResult))
            {
                result.Error = GetParamError("projectType", projectTypeResult, "string");
                return result;
            }

            int cityId = cityIdResult.Value;
            string projectTypeStr = projectTypeResult.Value;
            bool buyGoods = GetBoolParam(cmd, "buyGoods", false);
            bool first = GetBoolParam(cmd, "first", false);
            bool repeat = GetBoolParam(cmd, "repeat", false);

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            // Resolve project type
            ProjectType projectType = ResolveProjectType(game, projectTypeStr);
            if (projectType == ProjectType.NONE)
            {
                result.Error = $"Unknown project type: {projectTypeStr}";
                return result;
            }

            // Get the city object
            City city = game.city(cityId);
            if (city == null)
            {
                result.Error = $"City not found: {cityId}";
                return result;
            }

            if (_sendCityBuildProjectMethod == null)
            {
                result.Error = "BuildProject command not available";
                return result;
            }

            try
            {
                // sendBuildProject(City, ProjectType, Boolean buyGoods, Boolean first, Boolean repeat)
                _sendCityBuildProjectMethod.Invoke(clientManager, new object[] { city, projectType, buyGoods, first, repeat });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Building project {projectTypeStr} in city {cityId}");
            }
            catch (Exception ex)
            {
                result.Error = $"BuildProject failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteHurryCivics(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "cityId", out var cityIdResult))
            {
                result.Error = GetParamError("cityId", cityIdResult, "integer");
                return result;
            }

            int cityId = cityIdResult.Value;

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            City city = game.city(cityId);
            if (city == null)
            {
                result.Error = $"City not found: {cityId}";
                return result;
            }

            if (_sendHurryCivicsMethod == null)
            {
                result.Error = "HurryCivics command not available";
                return result;
            }

            try
            {
                // sendHurryCivics(City)
                _sendHurryCivicsMethod.Invoke(clientManager, new object[] { city });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Hurried production with civics in city {cityId}");
            }
            catch (Exception ex)
            {
                result.Error = $"HurryCivics failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteHurryTraining(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "cityId", out var cityIdResult))
            {
                result.Error = GetParamError("cityId", cityIdResult, "integer");
                return result;
            }

            int cityId = cityIdResult.Value;

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            City city = game.city(cityId);
            if (city == null)
            {
                result.Error = $"City not found: {cityId}";
                return result;
            }

            if (_sendHurryTrainingMethod == null)
            {
                result.Error = "HurryTraining command not available";
                return result;
            }

            try
            {
                // sendHurryTraining(City)
                _sendHurryTrainingMethod.Invoke(clientManager, new object[] { city });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Hurried production with training in city {cityId}");
            }
            catch (Exception ex)
            {
                result.Error = $"HurryTraining failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteHurryMoney(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "cityId", out var cityIdResult))
            {
                result.Error = GetParamError("cityId", cityIdResult, "integer");
                return result;
            }

            int cityId = cityIdResult.Value;

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            City city = game.city(cityId);
            if (city == null)
            {
                result.Error = $"City not found: {cityId}";
                return result;
            }

            if (_sendHurryMoneyMethod == null)
            {
                result.Error = "HurryMoney command not available";
                return result;
            }

            try
            {
                // sendHurryMoney(City)
                _sendHurryMoneyMethod.Invoke(clientManager, new object[] { city });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Hurried production with money in city {cityId}");
            }
            catch (Exception ex)
            {
                result.Error = $"HurryMoney failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteHurryPopulation(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "cityId", out var cityIdResult))
            {
                result.Error = GetParamError("cityId", cityIdResult, "integer");
                return result;
            }

            int cityId = cityIdResult.Value;

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            City city = game.city(cityId);
            if (city == null)
            {
                result.Error = $"City not found: {cityId}";
                return result;
            }

            if (_sendHurryPopulationMethod == null)
            {
                result.Error = "HurryPopulation command not available";
                return result;
            }

            try
            {
                // sendHurryPopulation(City)
                _sendHurryPopulationMethod.Invoke(clientManager, new object[] { city });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Hurried production with population in city {cityId}");
            }
            catch (Exception ex)
            {
                result.Error = $"HurryPopulation failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteHurryOrders(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "cityId", out var cityIdResult))
            {
                result.Error = GetParamError("cityId", cityIdResult, "integer");
                return result;
            }

            int cityId = cityIdResult.Value;

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            City city = game.city(cityId);
            if (city == null)
            {
                result.Error = $"City not found: {cityId}";
                return result;
            }

            if (_sendHurryOrdersMethod == null)
            {
                result.Error = "HurryOrders command not available";
                return result;
            }

            try
            {
                // sendHurryOrders(City)
                _sendHurryOrdersMethod.Invoke(clientManager, new object[] { city });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Hurried production with orders in city {cityId}");
            }
            catch (Exception ex)
            {
                result.Error = $"HurryOrders failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteResearch(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetStringParam(cmd, "tech", out var techResult))
            {
                result.Error = GetParamError("tech", techResult, "string");
                return result;
            }

            string techStr = techResult.Value;

            TechType techType = ResolveTechType(game, techStr);
            if (techType == TechType.NONE)
            {
                result.Error = $"Unknown tech type: {techStr}";
                return result;
            }

            if (_sendResearchMethod == null)
            {
                result.Error = "Research command not available";
                return result;
            }

            try
            {
                _sendResearchMethod.Invoke(clientManager, new object[] { techType });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"Research failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteEndTurn(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (_sendEndTurnMethod == null)
            {
                result.Error = "EndTurn command not available";
                return result;
            }

            try
            {
                // sendEndTurn(int iTurn, bool bForce) - iTurn is Game.getTurn(), not player index!
                int turn = game != null ? game.getTurn() : 0;
                bool force = GetBoolParam(cmd, "force", true); // default to true to actually end turn
                _sendEndTurnMethod.Invoke(clientManager, new object[] { turn, force });
                result.Success = true;
                Debug.Log($"[APIEndpoint] End turn {turn} (force={force})");
            }
            catch (Exception ex)
            {
                result.Error = $"EndTurn failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        #endregion
    }
}

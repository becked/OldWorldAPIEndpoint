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
        private static MethodInfo _sendCityBuildMethod;
        private static MethodInfo _sendCityHurryMethod;
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
                _sendCityBuildMethod = _clientManagerType.GetMethod("sendBuildUnit",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendHurryCivics(City), sendHurryTraining(City), etc.
                _sendCityHurryMethod = _clientManagerType.GetMethod("sendHurryCivics",
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
                Debug.Log($"[APIEndpoint]   sendUnitMove: {_sendUnitMoveMethod != null}");
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
                catch { }
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
                case "buildproject":
                    return ExecuteBuild(clientManager, game, cmd, result);
                case "hurry":
                    return ExecuteHurry(clientManager, game, cmd, result);

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
            int unitId = GetIntParam(cmd, "unitId");
            int targetTileId = GetIntParam(cmd, "targetTileId");
            bool queueMove = GetBoolParam(cmd, "queue", false);
            bool forceMove = GetBoolParam(cmd, "force", false);

            if (unitId < 0)
            {
                result.Error = "Missing required parameter: unitId";
                return result;
            }

            if (targetTileId < 0)
            {
                result.Error = "Missing required parameter: targetTileId";
                return result;
            }

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

                // sendMoveUnit(Unit, Tile, Boolean queue, Boolean force, Tile waypoint)
                _sendUnitMoveMethod.Invoke(clientManager, new object[] { unit, targetTile, queueMove, forceMove, null });
                result.Success = true;
                Debug.Log($"[APIEndpoint] Moved unit {unitId} to tile {targetTileId}");
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
            int unitId = GetIntParam(cmd, "unitId");
            int targetTileId = GetIntParam(cmd, "targetTileId");

            if (unitId < 0)
            {
                result.Error = "Missing required parameter: unitId";
                return result;
            }

            if (targetTileId < 0)
            {
                result.Error = "Missing required parameter: targetTileId";
                return result;
            }

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
            int unitId = GetIntParam(cmd, "unitId");

            if (unitId < 0)
            {
                result.Error = "Missing required parameter: unitId";
                return result;
            }

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
            int unitId = GetIntParam(cmd, "unitId");

            if (unitId < 0)
            {
                result.Error = "Missing required parameter: unitId";
                return result;
            }

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
            int unitId = GetIntParam(cmd, "unitId");

            if (unitId < 0)
            {
                result.Error = "Missing required parameter: unitId";
                return result;
            }

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
            int unitId = GetIntParam(cmd, "unitId");

            if (unitId < 0)
            {
                result.Error = "Missing required parameter: unitId";
                return result;
            }

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
            int unitId = GetIntParam(cmd, "unitId");

            if (unitId < 0)
            {
                result.Error = "Missing required parameter: unitId";
                return result;
            }

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
            int unitId = GetIntParam(cmd, "unitId");
            bool force = GetBoolParam(cmd, "force", false);

            if (unitId < 0)
            {
                result.Error = "Missing required parameter: unitId";
                return result;
            }

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
            int unitId = GetIntParam(cmd, "unitId");
            string promotionStr = GetStringParam(cmd, "promotion");

            if (unitId < 0)
            {
                result.Error = "Missing required parameter: unitId";
                return result;
            }

            if (string.IsNullOrEmpty(promotionStr))
            {
                result.Error = "Missing required parameter: promotion";
                return result;
            }

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

        private static CommandResult ExecuteBuild(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            int cityId = GetIntParam(cmd, "cityId");
            string unitTypeStr = GetStringParam(cmd, "unitType");
            bool rush = GetBoolParam(cmd, "rush", false);

            if (cityId < 0)
            {
                result.Error = "Missing required parameter: cityId";
                return result;
            }

            if (string.IsNullOrEmpty(unitTypeStr))
            {
                result.Error = "Missing required parameter: unitType";
                return result;
            }

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

            if (_sendCityBuildMethod == null)
            {
                result.Error = "Build command not available";
                return result;
            }

            try
            {
                // sendBuildUnit(City, UnitType, Boolean rush, Tile rallyTile, Boolean queue)
                _sendCityBuildMethod.Invoke(clientManager, new object[] { city, unitType, rush, null, false });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"Build failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteHurry(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            int cityId = GetIntParam(cmd, "cityId");
            string yieldStr = GetStringParam(cmd, "yield", "YIELD_CIVICS");

            if (cityId < 0)
            {
                result.Error = "Missing required parameter: cityId";
                return result;
            }

            YieldType yieldType = ResolveYieldType(game, yieldStr);
            if (yieldType == YieldType.NONE)
            {
                result.Error = $"Unknown yield type: {yieldStr}";
                return result;
            }

            if (_sendCityHurryMethod == null)
            {
                result.Error = "Hurry command not available";
                return result;
            }

            try
            {
                _sendCityHurryMethod.Invoke(clientManager, new object[] { cityId, yieldType });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"Hurry failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteResearch(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            string techStr = GetStringParam(cmd, "tech");

            if (string.IsNullOrEmpty(techStr))
            {
                result.Error = "Missing required parameter: tech";
                return result;
            }

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

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

        // Phase 1: Unit commands
        private static MethodInfo _sendHealMethod;
        private static MethodInfo _sendMarchMethod;
        private static MethodInfo _sendLockMethod;
        private static MethodInfo _sendPillageMethod;
        private static MethodInfo _sendBurnMethod;
        private static MethodInfo _sendUpgradeMethod;
        private static MethodInfo _sendSpreadReligionMethod;

        // Phase 2: Worker commands
        private static MethodInfo _sendBuildImprovementMethod;
        private static MethodInfo _sendUpgradeImprovementMethod;
        private static MethodInfo _sendAddRoadMethod;

        // Phase 3: City foundation commands
        private static MethodInfo _sendFoundCityMethod;
        private static MethodInfo _sendJoinCityMethod;

        // Phase 4: City production commands
        private static MethodInfo _sendBuildQueueMethod;

        // Phase 5: Research & decisions commands
        private static MethodInfo _sendRedrawTechMethod;
        private static MethodInfo _sendTargetTechMethod;
        private static MethodInfo _sendMakeDecisionMethod;
        private static MethodInfo _sendRemoveDecisionMethod;

        // Phase 6: Diplomacy commands
        private static MethodInfo _sendDiplomacyPlayerMethod;
        private static MethodInfo _sendDiplomacyTribeMethod;
        private static MethodInfo _sendGiftCityMethod;
        private static MethodInfo _sendGiftYieldMethod;
        private static MethodInfo _sendAllyTribeMethod;
        private static MethodInfo _getActivePlayerMethod;

        // Phase 7: Character management commands
        private static MethodInfo _sendMakeGovernorMethod;
        private static MethodInfo _sendReleaseGovernorMethod;
        private static MethodInfo _sendMakeUnitCharacterMethod;
        private static MethodInfo _sendReleaseUnitCharacterMethod;
        private static MethodInfo _sendMakeAgentMethod;
        private static MethodInfo _sendReleaseAgentMethod;
        private static MethodInfo _sendStartMissionMethod;

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

                // Phase 1: Unit commands
                // sendHeal(Unit, Boolean)
                _sendHealMethod = _clientManagerType.GetMethod("sendHeal",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendMarch(Unit)
                _sendMarchMethod = _clientManagerType.GetMethod("sendMarch",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendLock(Unit)
                _sendLockMethod = _clientManagerType.GetMethod("sendLock",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendPillage(Unit)
                _sendPillageMethod = _clientManagerType.GetMethod("sendPillage",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendBurn(Unit)
                _sendBurnMethod = _clientManagerType.GetMethod("sendBurn",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendUpgrade(Unit, UnitType, Boolean)
                _sendUpgradeMethod = _clientManagerType.GetMethod("sendUpgrade",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendSpreadReligion(Unit, Int32)
                _sendSpreadReligionMethod = _clientManagerType.GetMethod("sendSpreadReligion",
                    BindingFlags.Public | BindingFlags.Instance);

                // Phase 2: Worker commands
                // sendBuildImprovement(Unit, ImprovementType, Boolean, Boolean, Tile)
                _sendBuildImprovementMethod = _clientManagerType.GetMethod("sendBuildImprovement",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendUpgradeImprovement(Unit, Boolean)
                _sendUpgradeImprovementMethod = _clientManagerType.GetMethod("sendUpgradeImprovement",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendAddRoad(Unit, Boolean, Boolean, Tile)
                _sendAddRoadMethod = _clientManagerType.GetMethod("sendAddRoad",
                    BindingFlags.Public | BindingFlags.Instance);

                // Phase 3: City foundation commands
                // sendFoundCity(Unit, FamilyType, NationType)
                _sendFoundCityMethod = _clientManagerType.GetMethod("sendFoundCity",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendJoinCity(Unit)
                _sendJoinCityMethod = _clientManagerType.GetMethod("sendJoinCity",
                    BindingFlags.Public | BindingFlags.Instance);

                // Phase 4: City production commands
                // sendBuildQueue(City, Int32, Int32)
                _sendBuildQueueMethod = _clientManagerType.GetMethod("sendBuildQueue",
                    BindingFlags.Public | BindingFlags.Instance);

                // Phase 5: Research & decisions commands
                // sendRedrawTech()
                _sendRedrawTechMethod = _clientManagerType.GetMethod("sendRedrawTech",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendTargetTech(TechType)
                _sendTargetTechMethod = _clientManagerType.GetMethod("sendTargetTech",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendMakeDecision(Int32, Int32, Int32)
                _sendMakeDecisionMethod = _clientManagerType.GetMethod("sendMakeDecision",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendRemoveDecision(Int32)
                _sendRemoveDecisionMethod = _clientManagerType.GetMethod("sendRemoveDecision",
                    BindingFlags.Public | BindingFlags.Instance);

                // Phase 6: Diplomacy commands
                // sendDiplomacyPlayer(PlayerType, PlayerType, ActionType)
                _sendDiplomacyPlayerMethod = _clientManagerType.GetMethod("sendDiplomacyPlayer",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendDiplomacyTribe(TribeType, PlayerType, ActionType)
                _sendDiplomacyTribeMethod = _clientManagerType.GetMethod("sendDiplomacyTribe",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendGiftCity(City, PlayerType)
                _sendGiftCityMethod = _clientManagerType.GetMethod("sendGiftCity",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendGiftYield(YieldType, PlayerType, Boolean)
                _sendGiftYieldMethod = _clientManagerType.GetMethod("sendGiftYield",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendAllyTribe(TribeType, PlayerType)
                _sendAllyTribeMethod = _clientManagerType.GetMethod("sendAllyTribe",
                    BindingFlags.Public | BindingFlags.Instance);
                // getActivePlayer()
                _getActivePlayerMethod = _clientManagerType.GetMethod("getActivePlayer",
                    BindingFlags.Public | BindingFlags.Instance);

                // Phase 7: Character management commands
                // sendMakeGovernor(City, Character)
                _sendMakeGovernorMethod = _clientManagerType.GetMethod("sendMakeGovernor",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendReleaseGovernor(City)
                _sendReleaseGovernorMethod = _clientManagerType.GetMethod("sendReleaseGovernor",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendMakeUnitCharacter(Unit, Character, Boolean)
                _sendMakeUnitCharacterMethod = _clientManagerType.GetMethod("sendMakeUnitCharacter",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendReleaseUnitCharacter(Unit)
                _sendReleaseUnitCharacterMethod = _clientManagerType.GetMethod("sendReleaseUnitCharacter",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendMakeAgent(City, Character)
                _sendMakeAgentMethod = _clientManagerType.GetMethod("sendMakeAgent",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendReleaseAgent(City)
                _sendReleaseAgentMethod = _clientManagerType.GetMethod("sendReleaseAgent",
                    BindingFlags.Public | BindingFlags.Instance);
                // sendStartMission(MissionType, Int32, String, Boolean)
                _sendStartMissionMethod = _clientManagerType.GetMethod("sendStartMission",
                    BindingFlags.Public | BindingFlags.Instance);

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

                // Unit Commands - Phase 1
                case "heal":
                    return ExecuteHeal(clientManager, game, cmd, result);
                case "march":
                    return ExecuteMarch(clientManager, game, cmd, result);
                case "lock":
                    return ExecuteLock(clientManager, game, cmd, result);
                case "pillage":
                    return ExecutePillage(clientManager, game, cmd, result);
                case "burn":
                    return ExecuteBurn(clientManager, game, cmd, result);
                case "upgrade":
                    return ExecuteUpgrade(clientManager, game, cmd, result);
                case "spreadreligion":
                    return ExecuteSpreadReligion(clientManager, game, cmd, result);

                // Worker Commands - Phase 2
                case "buildimprovement":
                    return ExecuteBuildImprovement(clientManager, game, cmd, result);
                case "upgradeimprovement":
                    return ExecuteUpgradeImprovement(clientManager, game, cmd, result);
                case "addroad":
                    return ExecuteAddRoad(clientManager, game, cmd, result);

                // City Foundation - Phase 3
                case "foundcity":
                    return ExecuteFoundCity(clientManager, game, cmd, result);
                case "joincity":
                    return ExecuteJoinCity(clientManager, game, cmd, result);

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

                // Phase 4: City Production
                case "buildqueue":
                    return ExecuteBuildQueue(clientManager, game, cmd, result);

                // Phase 5: Research & Decisions
                case "redrawtech":
                    return ExecuteRedrawTech(clientManager, game, cmd, result);
                case "targettech":
                    return ExecuteTargetTech(clientManager, game, cmd, result);
                case "makedecision":
                    return ExecuteMakeDecision(clientManager, game, cmd, result);
                case "removedecision":
                    return ExecuteRemoveDecision(clientManager, game, cmd, result);

                // Turn Management
                case "endturn":
                    return ExecuteEndTurn(clientManager, game, cmd, result);

                // Phase 6: Diplomacy - Player
                case "declarewar":
                    return ExecuteDeclareWar(clientManager, game, cmd, result);
                case "makepeace":
                    return ExecuteMakePeace(clientManager, game, cmd, result);
                case "declaretruce":
                    return ExecuteDeclareTruce(clientManager, game, cmd, result);

                // Phase 6: Diplomacy - Tribe
                case "declarewartribe":
                    return ExecuteDeclareWarTribe(clientManager, game, cmd, result);
                case "makepeacetribe":
                    return ExecuteMakePeaceTribe(clientManager, game, cmd, result);
                case "declaretrucetribe":
                    return ExecuteDeclareTruceTribe(clientManager, game, cmd, result);

                // Phase 6: Diplomacy - Gifts & Alliance
                case "giftcity":
                    return ExecuteGiftCity(clientManager, game, cmd, result);
                case "giftyield":
                    return ExecuteGiftYield(clientManager, game, cmd, result);
                case "allytribe":
                    return ExecuteAllyTribe(clientManager, game, cmd, result);

                // Phase 7: Character Management - Governor
                case "assigngovernor":
                    return ExecuteAssignGovernor(clientManager, game, cmd, result);
                case "releasegovernor":
                    return ExecuteReleaseGovernor(clientManager, game, cmd, result);

                // Phase 7: Character Management - General
                case "assigngeneral":
                    return ExecuteAssignGeneral(clientManager, game, cmd, result);
                case "releasegeneral":
                    return ExecuteReleaseGeneral(clientManager, game, cmd, result);

                // Phase 7: Character Management - Agent
                case "assignagent":
                    return ExecuteAssignAgent(clientManager, game, cmd, result);
                case "releaseagent":
                    return ExecuteReleaseAgent(clientManager, game, cmd, result);

                // Phase 7: Character Management - Mission
                case "startmission":
                    return ExecuteStartMission(clientManager, game, cmd, result);

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

        /// <summary>
        /// Resolve an improvement type string (e.g., "IMPROVEMENT_FARM") to ImprovementType enum.
        /// </summary>
        private static ImprovementType ResolveImprovementType(Game game, string typeStr)
        {
            if (string.IsNullOrEmpty(typeStr) || game == null) return ImprovementType.NONE;

            Infos infos = game.infos();
            int count = (int)infos.improvementsNum();

            for (int i = 0; i < count; i++)
            {
                var impType = (ImprovementType)i;
                if (infos.improvement(impType).mzType.Equals(typeStr, StringComparison.OrdinalIgnoreCase))
                    return impType;
            }

            return ImprovementType.NONE;
        }

        /// <summary>
        /// Resolve a family type string (e.g., "FAMILY_ARTISANS") to FamilyType enum.
        /// </summary>
        private static FamilyType ResolveFamilyType(Game game, string typeStr)
        {
            if (string.IsNullOrEmpty(typeStr) || game == null) return FamilyType.NONE;

            Infos infos = game.infos();
            int count = (int)infos.familiesNum();

            for (int i = 0; i < count; i++)
            {
                var famType = (FamilyType)i;
                if (infos.family(famType).mzType.Equals(typeStr, StringComparison.OrdinalIgnoreCase))
                    return famType;
            }

            return FamilyType.NONE;
        }

        /// <summary>
        /// Resolve a nation type string (e.g., "NATION_ROME") to NationType enum.
        /// </summary>
        private static NationType ResolveNationType(Game game, string typeStr)
        {
            if (string.IsNullOrEmpty(typeStr) || game == null) return NationType.NONE;

            Infos infos = game.infos();
            int count = (int)infos.nationsNum();

            for (int i = 0; i < count; i++)
            {
                var natType = (NationType)i;
                if (infos.nation(natType).mzType.Equals(typeStr, StringComparison.OrdinalIgnoreCase))
                    return natType;
            }

            return NationType.NONE;
        }

        /// <summary>
        /// Resolve a tribe type string (e.g., "TRIBE_GAULS") to TribeType enum.
        /// </summary>
        private static TribeType ResolveTribeType(Game game, string typeStr)
        {
            if (string.IsNullOrEmpty(typeStr) || game == null) return TribeType.NONE;

            Infos infos = game.infos();
            int count = (int)infos.tribesNum();

            for (int i = 0; i < count; i++)
            {
                var tribeType = (TribeType)i;
                if (infos.tribe(tribeType).mzType.Equals(typeStr, StringComparison.OrdinalIgnoreCase))
                    return tribeType;
            }

            return TribeType.NONE;
        }

        /// <summary>
        /// Resolve a mission type string (e.g., "MISSION_NETWORK") to MissionType enum.
        /// </summary>
        private static MissionType ResolveMissionType(Game game, string typeStr)
        {
            if (string.IsNullOrEmpty(typeStr) || game == null) return MissionType.NONE;

            Infos infos = game.infos();
            int count = (int)infos.missionsNum();

            for (int i = 0; i < count; i++)
            {
                var misType = (MissionType)i;
                if (infos.mission(misType).mzType.Equals(typeStr, StringComparison.OrdinalIgnoreCase))
                    return misType;
            }

            return MissionType.NONE;
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

        #region Phase 1: Unit Commands

        private static CommandResult ExecuteHeal(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            int unitId = unitIdResult.Value;
            bool auto = GetBoolParam(cmd, "auto", false);

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            if (_sendHealMethod == null)
            {
                result.Error = "Heal command not available";
                return result;
            }

            try
            {
                Unit unit = game.unit(unitId);
                if (unit == null)
                {
                    result.Error = $"Unit not found: {unitId}";
                    return result;
                }
                _sendHealMethod.Invoke(clientManager, new object[] { unit, auto });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"Heal failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteMarch(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            int unitId = unitIdResult.Value;

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            if (_sendMarchMethod == null)
            {
                result.Error = "March command not available";
                return result;
            }

            try
            {
                Unit unit = game.unit(unitId);
                if (unit == null)
                {
                    result.Error = $"Unit not found: {unitId}";
                    return result;
                }
                _sendMarchMethod.Invoke(clientManager, new object[] { unit });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"March failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteLock(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            int unitId = unitIdResult.Value;

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            if (_sendLockMethod == null)
            {
                result.Error = "Lock command not available";
                return result;
            }

            try
            {
                Unit unit = game.unit(unitId);
                if (unit == null)
                {
                    result.Error = $"Unit not found: {unitId}";
                    return result;
                }
                _sendLockMethod.Invoke(clientManager, new object[] { unit });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"Lock failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecutePillage(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            int unitId = unitIdResult.Value;

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            if (_sendPillageMethod == null)
            {
                result.Error = "Pillage command not available";
                return result;
            }

            try
            {
                Unit unit = game.unit(unitId);
                if (unit == null)
                {
                    result.Error = $"Unit not found: {unitId}";
                    return result;
                }
                _sendPillageMethod.Invoke(clientManager, new object[] { unit });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"Pillage failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteBurn(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            int unitId = unitIdResult.Value;

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            if (_sendBurnMethod == null)
            {
                result.Error = "Burn command not available";
                return result;
            }

            try
            {
                Unit unit = game.unit(unitId);
                if (unit == null)
                {
                    result.Error = $"Unit not found: {unitId}";
                    return result;
                }
                _sendBurnMethod.Invoke(clientManager, new object[] { unit });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"Burn failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteUpgrade(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            if (!TryGetStringParam(cmd, "unitType", out var unitTypeResult))
            {
                result.Error = GetParamError("unitType", unitTypeResult, "string");
                return result;
            }

            int unitId = unitIdResult.Value;
            string unitTypeStr = unitTypeResult.Value;
            bool buyGoods = GetBoolParam(cmd, "buyGoods", false);

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            UnitType unitType = ResolveUnitType(game, unitTypeStr);
            if (unitType == UnitType.NONE)
            {
                result.Error = $"Unknown unit type: {unitTypeStr}";
                return result;
            }

            if (_sendUpgradeMethod == null)
            {
                result.Error = "Upgrade command not available";
                return result;
            }

            try
            {
                Unit unit = game.unit(unitId);
                if (unit == null)
                {
                    result.Error = $"Unit not found: {unitId}";
                    return result;
                }
                _sendUpgradeMethod.Invoke(clientManager, new object[] { unit, unitType, buyGoods });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"Upgrade failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteSpreadReligion(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            if (!TryGetIntParam(cmd, "cityId", out var cityIdResult))
            {
                result.Error = GetParamError("cityId", cityIdResult, "integer");
                return result;
            }

            int unitId = unitIdResult.Value;
            int cityId = cityIdResult.Value;

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            if (_sendSpreadReligionMethod == null)
            {
                result.Error = "SpreadReligion command not available";
                return result;
            }

            try
            {
                Unit unit = game.unit(unitId);
                if (unit == null)
                {
                    result.Error = $"Unit not found: {unitId}";
                    return result;
                }
                _sendSpreadReligionMethod.Invoke(clientManager, new object[] { unit, cityId });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"SpreadReligion failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        #endregion

        #region Phase 2: Worker Commands

        private static CommandResult ExecuteBuildImprovement(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            if (!TryGetStringParam(cmd, "improvementType", out var improvementTypeResult))
            {
                result.Error = GetParamError("improvementType", improvementTypeResult, "string");
                return result;
            }

            if (!TryGetIntParam(cmd, "tileId", out var tileIdResult))
            {
                result.Error = GetParamError("tileId", tileIdResult, "integer");
                return result;
            }

            int unitId = unitIdResult.Value;
            string improvementTypeStr = improvementTypeResult.Value;
            int tileId = tileIdResult.Value;
            bool buyGoods = GetBoolParam(cmd, "buyGoods", false);
            bool queue = GetBoolParam(cmd, "queue", false);

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            ImprovementType improvementType = ResolveImprovementType(game, improvementTypeStr);
            if (improvementType == ImprovementType.NONE)
            {
                result.Error = $"Unknown improvement type: {improvementTypeStr}";
                return result;
            }

            if (_sendBuildImprovementMethod == null)
            {
                result.Error = "BuildImprovement command not available";
                return result;
            }

            try
            {
                Unit unit = game.unit(unitId);
                if (unit == null)
                {
                    result.Error = $"Unit not found: {unitId}";
                    return result;
                }

                Tile tile = game.tile(tileId);
                if (tile == null)
                {
                    result.Error = $"Tile not found: {tileId}";
                    return result;
                }

                _sendBuildImprovementMethod.Invoke(clientManager, new object[] { unit, improvementType, buyGoods, queue, tile });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"BuildImprovement failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteUpgradeImprovement(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            int unitId = unitIdResult.Value;
            bool buyGoods = GetBoolParam(cmd, "buyGoods", false);

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            if (_sendUpgradeImprovementMethod == null)
            {
                result.Error = "UpgradeImprovement command not available";
                return result;
            }

            try
            {
                Unit unit = game.unit(unitId);
                if (unit == null)
                {
                    result.Error = $"Unit not found: {unitId}";
                    return result;
                }
                _sendUpgradeImprovementMethod.Invoke(clientManager, new object[] { unit, buyGoods });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"UpgradeImprovement failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteAddRoad(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            if (!TryGetIntParam(cmd, "tileId", out var tileIdResult))
            {
                result.Error = GetParamError("tileId", tileIdResult, "integer");
                return result;
            }

            int unitId = unitIdResult.Value;
            int tileId = tileIdResult.Value;
            bool buyGoods = GetBoolParam(cmd, "buyGoods", false);
            bool queue = GetBoolParam(cmd, "queue", false);

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            if (_sendAddRoadMethod == null)
            {
                result.Error = "AddRoad command not available";
                return result;
            }

            try
            {
                Unit unit = game.unit(unitId);
                if (unit == null)
                {
                    result.Error = $"Unit not found: {unitId}";
                    return result;
                }

                Tile tile = game.tile(tileId);
                if (tile == null)
                {
                    result.Error = $"Tile not found: {tileId}";
                    return result;
                }

                _sendAddRoadMethod.Invoke(clientManager, new object[] { unit, buyGoods, queue, tile });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"AddRoad failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        #endregion

        #region Phase 3: City Foundation Commands

        private static CommandResult ExecuteFoundCity(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            if (!TryGetStringParam(cmd, "familyType", out var familyTypeResult))
            {
                result.Error = GetParamError("familyType", familyTypeResult, "string");
                return result;
            }

            int unitId = unitIdResult.Value;
            string familyTypeStr = familyTypeResult.Value;
            string nationTypeStr = GetStringParam(cmd, "nationType", null);

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            FamilyType familyType = ResolveFamilyType(game, familyTypeStr);
            if (familyType == FamilyType.NONE)
            {
                result.Error = $"Unknown family type: {familyTypeStr}";
                return result;
            }

            NationType nationType = NationType.NONE;
            if (!string.IsNullOrEmpty(nationTypeStr))
            {
                nationType = ResolveNationType(game, nationTypeStr);
                if (nationType == NationType.NONE)
                {
                    result.Error = $"Unknown nation type: {nationTypeStr}";
                    return result;
                }
            }

            if (_sendFoundCityMethod == null)
            {
                result.Error = "FoundCity command not available";
                return result;
            }

            try
            {
                Unit unit = game.unit(unitId);
                if (unit == null)
                {
                    result.Error = $"Unit not found: {unitId}";
                    return result;
                }
                _sendFoundCityMethod.Invoke(clientManager, new object[] { unit, familyType, nationType });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"FoundCity failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteJoinCity(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            int unitId = unitIdResult.Value;

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            if (_sendJoinCityMethod == null)
            {
                result.Error = "JoinCity command not available";
                return result;
            }

            try
            {
                Unit unit = game.unit(unitId);
                if (unit == null)
                {
                    result.Error = $"Unit not found: {unitId}";
                    return result;
                }
                _sendJoinCityMethod.Invoke(clientManager, new object[] { unit });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"JoinCity failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        #endregion

        #region Phase 4: City Production Commands

        private static CommandResult ExecuteBuildQueue(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "cityId", out var cityIdResult))
            {
                result.Error = GetParamError("cityId", cityIdResult, "integer");
                return result;
            }

            if (!TryGetIntParam(cmd, "oldSlot", out var oldSlotResult))
            {
                result.Error = GetParamError("oldSlot", oldSlotResult, "integer");
                return result;
            }

            if (!TryGetIntParam(cmd, "newSlot", out var newSlotResult))
            {
                result.Error = GetParamError("newSlot", newSlotResult, "integer");
                return result;
            }

            int cityId = cityIdResult.Value;
            int oldSlot = oldSlotResult.Value;
            int newSlot = newSlotResult.Value;

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

            if (_sendBuildQueueMethod == null)
            {
                result.Error = "BuildQueue command not available";
                return result;
            }

            try
            {
                _sendBuildQueueMethod.Invoke(clientManager, new object[] { city, oldSlot, newSlot });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"BuildQueue failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        #endregion

        #region Phase 5: Research & Decisions Commands

        private static CommandResult ExecuteRedrawTech(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (_sendRedrawTechMethod == null)
            {
                result.Error = "RedrawTech command not available";
                return result;
            }

            try
            {
                _sendRedrawTechMethod.Invoke(clientManager, null);
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"RedrawTech failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteTargetTech(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetStringParam(cmd, "techType", out var techTypeResult))
            {
                result.Error = GetParamError("techType", techTypeResult, "string");
                return result;
            }

            string techTypeStr = techTypeResult.Value;

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            TechType techType = ResolveTechType(game, techTypeStr);
            if (techType == TechType.NONE)
            {
                result.Error = $"Unknown tech type: {techTypeStr}";
                return result;
            }

            if (_sendTargetTechMethod == null)
            {
                result.Error = "TargetTech command not available";
                return result;
            }

            try
            {
                _sendTargetTechMethod.Invoke(clientManager, new object[] { techType });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"TargetTech failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteMakeDecision(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "decisionId", out var decisionIdResult))
            {
                result.Error = GetParamError("decisionId", decisionIdResult, "integer");
                return result;
            }

            if (!TryGetIntParam(cmd, "choiceIndex", out var choiceIndexResult))
            {
                result.Error = GetParamError("choiceIndex", choiceIndexResult, "integer");
                return result;
            }

            int decisionId = decisionIdResult.Value;
            int choiceIndex = choiceIndexResult.Value;
            int data = GetIntParam(cmd, "data", 0);

            if (_sendMakeDecisionMethod == null)
            {
                result.Error = "MakeDecision command not available";
                return result;
            }

            try
            {
                _sendMakeDecisionMethod.Invoke(clientManager, new object[] { decisionId, choiceIndex, data });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"MakeDecision failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteRemoveDecision(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "decisionId", out var decisionIdResult))
            {
                result.Error = GetParamError("decisionId", decisionIdResult, "integer");
                return result;
            }

            int decisionId = decisionIdResult.Value;

            if (_sendRemoveDecisionMethod == null)
            {
                result.Error = "RemoveDecision command not available";
                return result;
            }

            try
            {
                _sendRemoveDecisionMethod.Invoke(clientManager, new object[] { decisionId });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"RemoveDecision failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        #endregion

        #region Phase 6: Diplomacy Commands

        private static CommandResult ExecuteDeclareWar(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "targetPlayer", out var targetPlayerResult))
            {
                result.Error = GetParamError("targetPlayer", targetPlayerResult, "integer");
                return result;
            }

            int targetPlayer = targetPlayerResult.Value;

            if (_sendDiplomacyPlayerMethod == null || _getActivePlayerMethod == null)
            {
                result.Error = "DeclareWar command not available";
                return result;
            }

            try
            {
                var activePlayer = _getActivePlayerMethod.Invoke(clientManager, null);
                // ActionType.DIPLOMACY_HOSTILE = 0 based on game patterns
                _sendDiplomacyPlayerMethod.Invoke(clientManager, new object[] { activePlayer, (PlayerType)targetPlayer, ActionType.DIPLOMACY_HOSTILE });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"DeclareWar failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteMakePeace(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "targetPlayer", out var targetPlayerResult))
            {
                result.Error = GetParamError("targetPlayer", targetPlayerResult, "integer");
                return result;
            }

            int targetPlayer = targetPlayerResult.Value;

            if (_sendDiplomacyPlayerMethod == null || _getActivePlayerMethod == null)
            {
                result.Error = "MakePeace command not available";
                return result;
            }

            try
            {
                var activePlayer = _getActivePlayerMethod.Invoke(clientManager, null);
                _sendDiplomacyPlayerMethod.Invoke(clientManager, new object[] { activePlayer, (PlayerType)targetPlayer, ActionType.DIPLOMACY_PEACE });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"MakePeace failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteDeclareTruce(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "targetPlayer", out var targetPlayerResult))
            {
                result.Error = GetParamError("targetPlayer", targetPlayerResult, "integer");
                return result;
            }

            int targetPlayer = targetPlayerResult.Value;

            if (_sendDiplomacyPlayerMethod == null || _getActivePlayerMethod == null)
            {
                result.Error = "DeclareTruce command not available";
                return result;
            }

            try
            {
                var activePlayer = _getActivePlayerMethod.Invoke(clientManager, null);
                _sendDiplomacyPlayerMethod.Invoke(clientManager, new object[] { activePlayer, (PlayerType)targetPlayer, ActionType.DIPLOMACY_TRUCE });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"DeclareTruce failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteDeclareWarTribe(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetStringParam(cmd, "tribeType", out var tribeTypeResult))
            {
                result.Error = GetParamError("tribeType", tribeTypeResult, "string");
                return result;
            }

            string tribeTypeStr = tribeTypeResult.Value;

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            TribeType tribeType = ResolveTribeType(game, tribeTypeStr);
            if (tribeType == TribeType.NONE)
            {
                result.Error = $"Unknown tribe type: {tribeTypeStr}";
                return result;
            }

            if (_sendDiplomacyTribeMethod == null || _getActivePlayerMethod == null)
            {
                result.Error = "DeclareWarTribe command not available";
                return result;
            }

            try
            {
                var activePlayer = _getActivePlayerMethod.Invoke(clientManager, null);
                _sendDiplomacyTribeMethod.Invoke(clientManager, new object[] { tribeType, activePlayer, ActionType.DIPLOMACY_HOSTILE_TRIBE });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"DeclareWarTribe failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteMakePeaceTribe(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetStringParam(cmd, "tribeType", out var tribeTypeResult))
            {
                result.Error = GetParamError("tribeType", tribeTypeResult, "string");
                return result;
            }

            string tribeTypeStr = tribeTypeResult.Value;

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            TribeType tribeType = ResolveTribeType(game, tribeTypeStr);
            if (tribeType == TribeType.NONE)
            {
                result.Error = $"Unknown tribe type: {tribeTypeStr}";
                return result;
            }

            if (_sendDiplomacyTribeMethod == null || _getActivePlayerMethod == null)
            {
                result.Error = "MakePeaceTribe command not available";
                return result;
            }

            try
            {
                var activePlayer = _getActivePlayerMethod.Invoke(clientManager, null);
                _sendDiplomacyTribeMethod.Invoke(clientManager, new object[] { tribeType, activePlayer, ActionType.DIPLOMACY_PEACE_TRIBE });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"MakePeaceTribe failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteDeclareTruceTribe(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetStringParam(cmd, "tribeType", out var tribeTypeResult))
            {
                result.Error = GetParamError("tribeType", tribeTypeResult, "string");
                return result;
            }

            string tribeTypeStr = tribeTypeResult.Value;

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            TribeType tribeType = ResolveTribeType(game, tribeTypeStr);
            if (tribeType == TribeType.NONE)
            {
                result.Error = $"Unknown tribe type: {tribeTypeStr}";
                return result;
            }

            if (_sendDiplomacyTribeMethod == null || _getActivePlayerMethod == null)
            {
                result.Error = "DeclareTruceTribe command not available";
                return result;
            }

            try
            {
                var activePlayer = _getActivePlayerMethod.Invoke(clientManager, null);
                _sendDiplomacyTribeMethod.Invoke(clientManager, new object[] { tribeType, activePlayer, ActionType.DIPLOMACY_TRUCE_TRIBE });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"DeclareTruceTribe failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteGiftCity(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "cityId", out var cityIdResult))
            {
                result.Error = GetParamError("cityId", cityIdResult, "integer");
                return result;
            }

            if (!TryGetIntParam(cmd, "targetPlayer", out var targetPlayerResult))
            {
                result.Error = GetParamError("targetPlayer", targetPlayerResult, "integer");
                return result;
            }

            int cityId = cityIdResult.Value;
            int targetPlayer = targetPlayerResult.Value;

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

            if (_sendGiftCityMethod == null)
            {
                result.Error = "GiftCity command not available";
                return result;
            }

            try
            {
                _sendGiftCityMethod.Invoke(clientManager, new object[] { city, (PlayerType)targetPlayer });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"GiftCity failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteGiftYield(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetStringParam(cmd, "yieldType", out var yieldTypeResult))
            {
                result.Error = GetParamError("yieldType", yieldTypeResult, "string");
                return result;
            }

            if (!TryGetIntParam(cmd, "targetPlayer", out var targetPlayerResult))
            {
                result.Error = GetParamError("targetPlayer", targetPlayerResult, "integer");
                return result;
            }

            string yieldTypeStr = yieldTypeResult.Value;
            int targetPlayer = targetPlayerResult.Value;
            bool reverse = GetBoolParam(cmd, "reverse", false);

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            YieldType yieldType = ResolveYieldType(game, yieldTypeStr);
            if (yieldType == YieldType.NONE)
            {
                result.Error = $"Unknown yield type: {yieldTypeStr}";
                return result;
            }

            if (_sendGiftYieldMethod == null)
            {
                result.Error = "GiftYield command not available";
                return result;
            }

            try
            {
                _sendGiftYieldMethod.Invoke(clientManager, new object[] { yieldType, (PlayerType)targetPlayer, reverse });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"GiftYield failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteAllyTribe(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetStringParam(cmd, "tribeType", out var tribeTypeResult))
            {
                result.Error = GetParamError("tribeType", tribeTypeResult, "string");
                return result;
            }

            string tribeTypeStr = tribeTypeResult.Value;

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            TribeType tribeType = ResolveTribeType(game, tribeTypeStr);
            if (tribeType == TribeType.NONE)
            {
                result.Error = $"Unknown tribe type: {tribeTypeStr}";
                return result;
            }

            if (_sendAllyTribeMethod == null || _getActivePlayerMethod == null)
            {
                result.Error = "AllyTribe command not available";
                return result;
            }

            try
            {
                var activePlayer = _getActivePlayerMethod.Invoke(clientManager, null);
                _sendAllyTribeMethod.Invoke(clientManager, new object[] { tribeType, activePlayer });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"AllyTribe failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        #endregion

        #region Phase 7: Character Management Commands

        private static CommandResult ExecuteAssignGovernor(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "cityId", out var cityIdResult))
            {
                result.Error = GetParamError("cityId", cityIdResult, "integer");
                return result;
            }

            if (!TryGetIntParam(cmd, "characterId", out var characterIdResult))
            {
                result.Error = GetParamError("characterId", characterIdResult, "integer");
                return result;
            }

            int cityId = cityIdResult.Value;
            int characterId = characterIdResult.Value;

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

            Character character = game.character(characterId);
            if (character == null)
            {
                result.Error = $"Character not found: {characterId}";
                return result;
            }

            if (_sendMakeGovernorMethod == null)
            {
                result.Error = "AssignGovernor command not available";
                return result;
            }

            try
            {
                _sendMakeGovernorMethod.Invoke(clientManager, new object[] { city, character });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"AssignGovernor failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteReleaseGovernor(object clientManager, Game game, GameCommand cmd, CommandResult result)
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

            if (_sendReleaseGovernorMethod == null)
            {
                result.Error = "ReleaseGovernor command not available";
                return result;
            }

            try
            {
                _sendReleaseGovernorMethod.Invoke(clientManager, new object[] { city });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"ReleaseGovernor failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteAssignGeneral(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            if (!TryGetIntParam(cmd, "characterId", out var characterIdResult))
            {
                result.Error = GetParamError("characterId", characterIdResult, "integer");
                return result;
            }

            int unitId = unitIdResult.Value;
            int characterId = characterIdResult.Value;

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            Unit unit = game.unit(unitId);
            if (unit == null)
            {
                result.Error = $"Unit not found: {unitId}";
                return result;
            }

            Character character = game.character(characterId);
            if (character == null)
            {
                result.Error = $"Character not found: {characterId}";
                return result;
            }

            if (_sendMakeUnitCharacterMethod == null)
            {
                result.Error = "AssignGeneral command not available";
                return result;
            }

            try
            {
                // sendMakeUnitCharacter(Unit, Character, bool bGeneral) - true for general
                _sendMakeUnitCharacterMethod.Invoke(clientManager, new object[] { unit, character, true });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"AssignGeneral failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteReleaseGeneral(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "unitId", out var unitIdResult))
            {
                result.Error = GetParamError("unitId", unitIdResult, "integer");
                return result;
            }

            int unitId = unitIdResult.Value;

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            Unit unit = game.unit(unitId);
            if (unit == null)
            {
                result.Error = $"Unit not found: {unitId}";
                return result;
            }

            if (_sendReleaseUnitCharacterMethod == null)
            {
                result.Error = "ReleaseGeneral command not available";
                return result;
            }

            try
            {
                _sendReleaseUnitCharacterMethod.Invoke(clientManager, new object[] { unit });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"ReleaseGeneral failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteAssignAgent(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetIntParam(cmd, "cityId", out var cityIdResult))
            {
                result.Error = GetParamError("cityId", cityIdResult, "integer");
                return result;
            }

            if (!TryGetIntParam(cmd, "characterId", out var characterIdResult))
            {
                result.Error = GetParamError("characterId", characterIdResult, "integer");
                return result;
            }

            int cityId = cityIdResult.Value;
            int characterId = characterIdResult.Value;

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

            Character character = game.character(characterId);
            if (character == null)
            {
                result.Error = $"Character not found: {characterId}";
                return result;
            }

            if (_sendMakeAgentMethod == null)
            {
                result.Error = "AssignAgent command not available";
                return result;
            }

            try
            {
                _sendMakeAgentMethod.Invoke(clientManager, new object[] { city, character });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"AssignAgent failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteReleaseAgent(object clientManager, Game game, GameCommand cmd, CommandResult result)
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

            if (_sendReleaseAgentMethod == null)
            {
                result.Error = "ReleaseAgent command not available";
                return result;
            }

            try
            {
                _sendReleaseAgentMethod.Invoke(clientManager, new object[] { city });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"ReleaseAgent failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        private static CommandResult ExecuteStartMission(object clientManager, Game game, GameCommand cmd, CommandResult result)
        {
            if (!TryGetStringParam(cmd, "missionType", out var missionTypeResult))
            {
                result.Error = GetParamError("missionType", missionTypeResult, "string");
                return result;
            }

            if (!TryGetIntParam(cmd, "characterId", out var characterIdResult))
            {
                result.Error = GetParamError("characterId", characterIdResult, "integer");
                return result;
            }

            string missionTypeStr = missionTypeResult.Value;
            int characterId = characterIdResult.Value;
            string target = GetStringParam(cmd, "target", "");
            bool cancel = GetBoolParam(cmd, "cancel", false);

            if (game == null)
            {
                result.Error = "Game not available";
                return result;
            }

            MissionType missionType = ResolveMissionType(game, missionTypeStr);
            if (missionType == MissionType.NONE)
            {
                result.Error = $"Unknown mission type: {missionTypeStr}";
                return result;
            }

            if (_sendStartMissionMethod == null)
            {
                result.Error = "StartMission command not available";
                return result;
            }

            try
            {
                _sendStartMissionMethod.Invoke(clientManager, new object[] { missionType, characterId, target, cancel });
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Error = $"StartMission failed: {ex.InnerException?.Message ?? ex.Message}";
            }

            return result;
        }

        #endregion

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

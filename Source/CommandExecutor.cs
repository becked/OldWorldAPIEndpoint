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
    ///
    /// This is a partial class - the main command implementations are in
    /// CommandExecutor.Generated.cs (auto-generated from game source).
    /// </summary>
    public static partial class CommandExecutor
    {
        private static bool _reflectionInitialized;
        private static MethodInfo _canDoActionsMethod;

        /// <summary>
        /// Execute a game command.
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

            // Delegate to generated command dispatcher
            return ExecuteGenerated(clientManager, game, cmd, result);
        }

        private static void InitializeReflection(object clientManager)
        {
            if (_reflectionInitialized || clientManager == null) return;

            try
            {
                var clientManagerType = clientManager.GetType();

                // Core action check
                _canDoActionsMethod = clientManagerType.GetMethod("canDoActions",
                    BindingFlags.Public | BindingFlags.Instance);

                // Initialize generated reflection
                InitializeReflectionGenerated(clientManager);

                _reflectionInitialized = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CommandExecutor] Reflection initialization failed: {ex.Message}");
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

        private static int? GetIntParamNullable(GameCommand cmd, string key)
        {
            if (cmd.Params == null || !cmd.Params.TryGetValue(key, out var value))
                return null;

            if (value == null) return null;
            if (value is int i) return i;
            if (value is long l) return (int)l;
            if (value is double d) return (int)d;
            if (int.TryParse(value.ToString(), out int parsed)) return parsed;

            return null;
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

        private static string GetParamError<T>(string key, ParseResult<T> result, string expectedType)
        {
            if (!result.Found)
                return $"Missing required parameter: {key}";
            return $"Invalid type for parameter '{key}': expected {expectedType}, got '{result.RawValue}'";
        }

        #endregion
    }
}

using System.Collections.Generic;

namespace OldWorldAPIEndpoint
{
    /// <summary>
    /// Represents a game command to be executed.
    /// </summary>
    public class GameCommand
    {
        /// <summary>
        /// The action to perform (e.g., "moveUnit", "attack", "endTurn").
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// Optional client-provided request ID for correlation.
        /// </summary>
        public string RequestId { get; set; }

        /// <summary>
        /// Command parameters (varies by action type).
        /// </summary>
        public Dictionary<string, object> Params { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Result of executing a single command.
    /// </summary>
    public class CommandResult
    {
        /// <summary>
        /// The request ID from the original command (for correlation).
        /// </summary>
        public string RequestId { get; set; }

        /// <summary>
        /// Whether the command executed successfully.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if the command failed.
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// Optional data returned by the command.
        /// </summary>
        public object Data { get; set; }
    }

    /// <summary>
    /// Result of validating a command before execution.
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Whether the command is valid and can be executed.
        /// </summary>
        public bool Valid { get; set; }

        /// <summary>
        /// Reason why the command is invalid (if applicable).
        /// </summary>
        public string Reason { get; set; }
    }

    /// <summary>
    /// A batch of commands to execute in sequence.
    /// </summary>
    public class BulkCommand
    {
        /// <summary>
        /// Optional request ID for the entire batch.
        /// </summary>
        public string RequestId { get; set; }

        /// <summary>
        /// List of commands to execute in order.
        /// </summary>
        public List<GameCommand> Commands { get; set; } = new List<GameCommand>();

        /// <summary>
        /// If true, stop execution when any command fails.
        /// Default is true.
        /// </summary>
        public bool StopOnError { get; set; } = true;
    }

    /// <summary>
    /// Result of a single command within a bulk execution.
    /// </summary>
    public class BulkCommandItemResult
    {
        /// <summary>
        /// Index of this command in the batch.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// The action that was attempted.
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// Whether this command succeeded.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if this command failed.
        /// </summary>
        public string Error { get; set; }
    }

    /// <summary>
    /// Result of executing a bulk command batch.
    /// </summary>
    public class BulkCommandResult
    {
        /// <summary>
        /// The request ID from the original bulk command.
        /// </summary>
        public string RequestId { get; set; }

        /// <summary>
        /// Whether all commands in the batch succeeded.
        /// </summary>
        public bool AllSucceeded { get; set; }

        /// <summary>
        /// Individual results for each command.
        /// </summary>
        public List<BulkCommandItemResult> Results { get; set; } = new List<BulkCommandItemResult>();

        /// <summary>
        /// If StopOnError was true and an error occurred, the index where execution stopped.
        /// </summary>
        public int? StoppedAtIndex { get; set; }
    }
}

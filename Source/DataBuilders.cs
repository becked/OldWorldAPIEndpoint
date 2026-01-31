using System.Collections.Generic;

namespace OldWorldAPIEndpoint
{
    /// <summary>
    /// Hand-written helpers for DataBuilders.
    /// The main entity builders are in DataBuilders.Generated.cs (auto-generated).
    /// </summary>
    public static partial class DataBuilders
    {
        /// <summary>
        /// Filter a tile data dictionary to only include specified fields.
        /// </summary>
        /// <param name="tileData">The full tile data from BuildTileObjectGenerated</param>
        /// <param name="requestedFields">Set of field names to include (case-insensitive)</param>
        /// <returns>New dictionary with only the requested fields that exist in the data</returns>
        public static Dictionary<string, object> FilterTileFields(
            object tileData,
            HashSet<string> requestedFields)
        {
            var source = (Dictionary<string, object>)tileData;
            var filtered = new Dictionary<string, object>();

            foreach (var field in requestedFields)
            {
                // TileFieldNames is case-insensitive, but dictionary keys are exact
                // Find the matching key in the source dictionary
                foreach (var kvp in source)
                {
                    if (string.Equals(kvp.Key, field, System.StringComparison.OrdinalIgnoreCase))
                    {
                        filtered[kvp.Key] = kvp.Value;
                        break;
                    }
                }
            }

            return filtered;
        }
    }
}

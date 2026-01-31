using System.Text;
using OldWorldCodeGen.Parsing;

namespace OldWorldCodeGen.Generation;

/// <summary>
/// Generates DataBuilders.Generated.cs from parsed entity getter methods.
/// </summary>
public class DataBuilderGenerator
{
    private readonly TypeAnalyzer _typeAnalyzer;

    public DataBuilderGenerator(TypeAnalyzer typeAnalyzer)
    {
        _typeAnalyzer = typeAnalyzer;
    }

    /// <summary>
    /// Classification of getter patterns for code generation.
    /// </summary>
    private enum GetterPattern
    {
        /// <summary>No params, simple return type - existing behavior</summary>
        Simple,
        /// <summary>Single enum param, returns value (e.g., getRating(RatingType) → int)</summary>
        EnumIndexed,
        /// <summary>No params, returns collection of enum (e.g., getTraits() → ReadOnlyList&lt;TraitType&gt;)</summary>
        EnumCollection,
        /// <summary>Can't auto-generate</summary>
        Unsupported
    }

    /// <summary>
    /// Classify a getter into a pattern for code generation.
    /// </summary>
    private GetterPattern ClassifyGetter(GetterSignature getter)
    {
        // Skip explicitly blocked getters
        if (SkippedGetters.Contains(getter.Name))
            return GetterPattern.Unsupported;

        // Skip methods with out/ref parameters - we can't auto-generate these
        if (getter.HasOutOrRefParams)
            return GetterPattern.Unsupported;

        // Pattern 1: Simple (existing behavior) - no params, supported simple return type
        if (!getter.HasParameters && IsSupportedReturnType(getter.ReturnType))
            return GetterPattern.Simple;

        // Pattern 2: Enum-indexed (e.g., getRating(RatingType) → int)
        if (getter.ParameterCount == 1 &&
            getter.ParameterTypes.Count > 0 &&
            IsEnumWithInfosLookup(getter.ParameterTypes[0]) &&
            IsSupportedValueType(getter.ReturnType))
            return GetterPattern.EnumIndexed;

        // Pattern 3: Collection of enum (e.g., getTraits() → ReadOnlyList<TraitType>)
        if (!getter.HasParameters &&
            getter.CollectionElementType != null &&
            IsEnumWithInfosLookup(getter.CollectionElementType))
            return GetterPattern.EnumCollection;

        return GetterPattern.Unsupported;
    }

    /// <summary>
    /// Enum types that are too large to auto-expand into dictionaries.
    /// These have thousands of values and would bloat the JSON output.
    /// </summary>
    private static readonly HashSet<string> BlockedEnumTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "EventStoryType",    // 5000+ event stories
        "BonusType",         // 1000+ bonus types
        "TextType",          // Localization keys
        "MemoryType",        // Character memories
        "AchievementType",   // Steam achievements
    };

    /// <summary>
    /// Getters to skip entirely - these produce noise that isn't useful to API consumers.
    /// </summary>
    private static readonly HashSet<string> SkippedGetters = new(StringComparer.OrdinalIgnoreCase)
    {
        "isImprovementBorderSpread",
        "isSpecialistCostCitizen",
    };

    /// <summary>
    /// Enum-indexed getters that should filter out zero values (not just -1).
    /// These are count fields that are noisy when zero.
    /// </summary>
    private static readonly HashSet<string> ZeroFilteredGetters = new(StringComparer.OrdinalIgnoreCase)
    {
        "getActiveImprovementClassCount",
        "getActiveImprovementCount",
        "getAmbitionDecisions",
        "getAssimilateYieldModifier",
        "getBuildUnitLevels",
        "getBuildUnitXP",
        "getChangeJobExtraOpinion",
        "getCognomenMinValue",
        "getDamageYieldModifier",
        "getEffectCityRebelProb",
        "getEffectPlayerCount",
        "getEffectUnitCount",
        "getExcessOverflow",
        "getExtraLuxuryCount",
        "getFamilyControl",
        "getFamilyOpinionCouncil",
        "getFamilyOpinionRate",
        "getFamilyTurnsNoLeader",
        "getGiftYieldQuantity",
        "getGoalStartedCount",
        "getHarvestYieldModifier",
        "getHappinessLevelYieldModifier",
        "getImprovementClassCostModifier",
        "getImprovementClassCount",
        "getImprovementClassDevelopChange",
        "getImprovementClassModifier",
        "getImprovementCost",
        "getImprovementCount",
        "getImprovementLawsRequired",
        "getImprovementModifier",
        "getImprovementRiverModifier",
        "getJobOpinion",
        "getJobOpinionRate",
        "getLivingCourtiersYield",
        "getLivingRoyalsYield",
        "getLuxuryCount",
        "getLuxuryTradeLength",
        "getMakeGovernorCost",
        "getMilitaryUnitFamilyCount",
        "getMissionCooldownTurnsLeft",
        "getNationEthnicity",
        "getNextTurnOverflow",
        "getProjectCount",
        "getProjectsProduced",
        "getQuestsFailed",
        "getRatingYieldRateAgentTotal",
        "getReligionCount",
        "getReligionOpinionRate",
        "getResourceRevealed",
        "getStateReligionUnitTraitTrainModifier",
        "getTechProgress",
        "getTheologyEstablishedCount",
        "getTradeOutpostYieldTotal",
        "getTribeEthnicity",
        "getUnitCostModifier",
        "getUnitProductionCount",
        "getUnitsProduced",
        "getUnitsProducedTurn",
        "getUnitTrainModifier",
        "getUnitTraitConsumptionModifier",
        "getUnitTraitCostModifier",
        "getUnitTraitTrainModifier",
        "getYieldOverflow",
        "getYieldProgress",
        "getYieldRateCourtier",
        "getYieldRateLeader",
        "getYieldRateLeaderSpouse",
        "getYieldRateSuccessor",
        "getYieldThreshold",
        "getYieldThresholdWhole",
        "getYieldTotal",
        "getYieldUpkeepNet",
    };

    /// <summary>
    /// Entities that should filter out zero values for all numeric properties.
    /// </summary>
    private static readonly HashSet<string> ZeroFilteredEntities = new(StringComparer.OrdinalIgnoreCase)
    {
        "Tile",
    };

    /// <summary>
    /// Getters that should preserve zero values even for ZeroFilteredEntities.
    /// These are identity/positional fields where 0 is a valid meaningful value.
    /// </summary>
    private static readonly HashSet<string> ZeroPreservedGetters = new(StringComparer.OrdinalIgnoreCase)
    {
        "getX",
        "getY",
        "getID",
        "getIndex",
    };

    /// <summary>
    /// Check if an enum type has an Infos lookup method and is suitable for expansion.
    /// </summary>
    private bool IsEnumWithInfosLookup(string typeName)
    {
        // Skip blocked large enum types
        if (BlockedEnumTypes.Contains(typeName))
            return false;

        var info = _typeAnalyzer.GetEnumTypeInfo(typeName);
        return info != null;
    }

    /// <summary>
    /// Check if a return type is a simple value type suitable for enum-indexed getters.
    /// </summary>
    private static bool IsSupportedValueType(string returnType)
    {
        var baseType = returnType.TrimEnd('?');
        return new[] { "int", "Int32", "long", "Int64", "bool", "Boolean", "string", "String",
                       "float", "Single", "double", "Double" }
            .Contains(baseType, StringComparer.OrdinalIgnoreCase);
    }

    public string Generate(Dictionary<string, List<GetterSignature>> entityGetters)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("// <auto-generated>");
        sb.AppendLine($"// Generated by OldWorldCodeGen at {DateTime.UtcNow:O}");
        sb.AppendLine("// Do not edit manually - regenerate with: dotnet run --project tools/OldWorldCodeGen");
        sb.AppendLine("// </auto-generated>");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using TenCrowns.GameCore;");
        sb.AppendLine();
        sb.AppendLine("namespace OldWorldAPIEndpoint");
        sb.AppendLine("{");
        sb.AppendLine("    public static partial class DataBuilders");
        sb.AppendLine("    {");
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Safely add a property to the data dictionary, catching any exceptions.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        private static void TryAdd(Dictionary<string, object> data, string key, Func<object> getValue)");
        sb.AppendLine("        {");
        sb.AppendLine("            try { data[key] = getValue(); }");
        sb.AppendLine("            catch { /* Skip properties that throw */ }");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Add a boolean property only if true (omit false values to reduce payload).");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        private static void TryAddIfTrue(Dictionary<string, object> data, string key, Func<bool> getValue)");
        sb.AppendLine("        {");
        sb.AppendLine("            try { if (getValue()) data[key] = true; }");
        sb.AppendLine("            catch { }");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Add an int property only if not -1 (omit sentinel values to reduce payload).");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        private static void TryAddIfNotNegativeOne(Dictionary<string, object> data, string key, Func<int> getValue)");
        sb.AppendLine("        {");
        sb.AppendLine("            try { var v = getValue(); if (v != -1) data[key] = v; }");
        sb.AppendLine("            catch { }");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Add a property only if not null.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        private static void TryAddIfNotNull(Dictionary<string, object> data, string key, Func<object> getValue)");
        sb.AppendLine("        {");
        sb.AppendLine("            try { var v = getValue(); if (v != null) data[key] = v; }");
        sb.AppendLine("            catch { }");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Add a property only if not null and not \"NONE\" (for enum string values).");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        private static void TryAddIfNotNullOrNone(Dictionary<string, object> data, string key, Func<object> getValue)");
        sb.AppendLine("        {");
        sb.AppendLine("            try { var v = getValue(); if (v != null && v.ToString() != \"NONE\") data[key] = v; }");
        sb.AppendLine("            catch { }");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Add a string property only if not null or empty.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        private static void TryAddIfNotNullOrEmpty(Dictionary<string, object> data, string key, Func<string> getValue)");
        sb.AppendLine("        {");
        sb.AppendLine("            try { var v = getValue(); if (!string.IsNullOrEmpty(v)) data[key] = v; }");
        sb.AppendLine("            catch { }");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Add an int property only if not zero and not -1 (for zero-filtered entities).");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        private static void TryAddIfNotZeroOrNegativeOne(Dictionary<string, object> data, string key, Func<int> getValue)");
        sb.AppendLine("        {");
        sb.AppendLine("            try { var v = getValue(); if (v != 0 && v != -1) data[key] = v; }");
        sb.AppendLine("            catch { }");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Add a float property only if not zero (for zero-filtered entities).");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        private static void TryAddIfNotZeroFloat(Dictionary<string, object> data, string key, Func<float> getValue)");
        sb.AppendLine("        {");
        sb.AppendLine("            try { var v = getValue(); if (v != 0f) data[key] = v; }");
        sb.AppendLine("            catch { }");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Add a double property only if not zero (for zero-filtered entities).");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        private static void TryAddIfNotZeroDouble(Dictionary<string, object> data, string key, Func<double> getValue)");
        sb.AppendLine("        {");
        sb.AppendLine("            try { var v = getValue(); if (v != 0d) data[key] = v; }");
        sb.AppendLine("            catch { }");
        sb.AppendLine("        }");
        sb.AppendLine();

        foreach (var (entityName, getters) in entityGetters.OrderBy(kv => kv.Key))
        {
            GenerateEntityBuilder(sb, entityName, getters);
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private void GenerateEntityBuilder(StringBuilder sb, string entityName, List<GetterSignature> getters)
    {
        // Classify all getters into patterns
        var classified = getters
            .Select(g => (getter: g, pattern: ClassifyGetter(g)))
            .ToList();

        var simpleGetters = classified
            .Where(c => c.pattern == GetterPattern.Simple)
            .Select(c => c.getter)
            .OrderBy(g => g.PropertyName)
            .ToList();

        var enumIndexedGetters = classified
            .Where(c => c.pattern == GetterPattern.EnumIndexed)
            .Select(c => c.getter)
            .OrderBy(g => g.PropertyName)
            .ToList();

        var enumCollectionGetters = classified
            .Where(c => c.pattern == GetterPattern.EnumCollection)
            .Select(c => c.getter)
            .OrderBy(g => g.PropertyName)
            .ToList();

        var unsupportedGetters = classified
            .Where(c => c.pattern == GetterPattern.Unsupported)
            .Select(c => c.getter)
            .OrderBy(g => g.Name)
            .ToList();

        var totalProps = simpleGetters.Count + enumIndexedGetters.Count + enumCollectionGetters.Count;

        sb.AppendLine($"        #region {entityName} Builder");
        sb.AppendLine();
        sb.AppendLine($"        /// <summary>");
        sb.AppendLine($"        /// Build a {entityName} object for JSON serialization.");
        sb.AppendLine($"        /// Auto-generated from {entityName}.cs - {totalProps} properties");
        sb.AppendLine($"        /// ({simpleGetters.Count} simple, {enumIndexedGetters.Count} enum-indexed, {enumCollectionGetters.Count} collections).");
        sb.AppendLine($"        /// Each property access is wrapped in try-catch for null safety.");
        sb.AppendLine($"        /// </summary>");
        sb.AppendLine($"        public static object Build{entityName}ObjectGenerated({entityName} entity, Game game, Infos infos)");
        sb.AppendLine("        {");
        sb.AppendLine("            var data = new Dictionary<string, object>();");
        sb.AppendLine();

        // Emit simple getters with type-appropriate helpers
        var isZeroFilteredEntity = ZeroFilteredEntities.Contains(entityName);

        foreach (var getter in simpleGetters)
        {
            var baseType = getter.ReturnType.TrimEnd('?');

            if (baseType == "bool" || baseType == "Boolean")
            {
                // Boolean: only include if true
                sb.AppendLine($"            TryAddIfTrue(data, \"{getter.PropertyName}\", () => entity.{getter.Name}());");
            }
            else if (baseType == "int" || baseType == "Int32")
            {
                // Int: use zero+sentinel filter for certain entities, otherwise skip -1 sentinel values
                if (isZeroFilteredEntity && !ZeroPreservedGetters.Contains(getter.Name))
                {
                    sb.AppendLine($"            TryAddIfNotZeroOrNegativeOne(data, \"{getter.PropertyName}\", () => entity.{getter.Name}());");
                }
                else
                {
                    sb.AppendLine($"            TryAddIfNotNegativeOne(data, \"{getter.PropertyName}\", () => entity.{getter.Name}());");
                }
            }
            else if (baseType == "float" || baseType == "Single")
            {
                // Float: use zero filter for certain entities
                if (isZeroFilteredEntity)
                {
                    sb.AppendLine($"            TryAddIfNotZeroFloat(data, \"{getter.PropertyName}\", () => entity.{getter.Name}());");
                }
                else
                {
                    sb.AppendLine($"            TryAddIfNotNull(data, \"{getter.PropertyName}\", () => entity.{getter.Name}());");
                }
            }
            else if (baseType == "double" || baseType == "Double")
            {
                // Double: use zero filter for certain entities
                if (isZeroFilteredEntity)
                {
                    sb.AppendLine($"            TryAddIfNotZeroDouble(data, \"{getter.PropertyName}\", () => entity.{getter.Name}());");
                }
                else
                {
                    sb.AppendLine($"            TryAddIfNotNull(data, \"{getter.PropertyName}\", () => entity.{getter.Name}());");
                }
            }
            else if (baseType == "string" || baseType == "String")
            {
                // String: filter nulls and empty strings
                sb.AppendLine($"            TryAddIfNotNullOrEmpty(data, \"{getter.PropertyName}\", () => entity.{getter.Name}());");
            }
            else if (IsEnumType(getter.ReturnType))
            {
                // Enum: filter nulls and "NONE" values
                var accessor = GenerateGetterAccessor(getter, entityName);
                sb.AppendLine($"            TryAddIfNotNullOrNone(data, \"{getter.PropertyName}\", () => {accessor});");
            }
            else
            {
                // All other types: filter nulls
                var accessor = GenerateGetterAccessor(getter, entityName);
                sb.AppendLine($"            TryAddIfNotNull(data, \"{getter.PropertyName}\", () => {accessor});");
            }
        }

        // Emit enum-indexed getters
        if (enumIndexedGetters.Any())
        {
            sb.AppendLine();
            sb.AppendLine("            // Enum-indexed properties");
            foreach (var getter in enumIndexedGetters)
            {
                EmitEnumIndexedGetter(sb, getter, "entity");
            }
        }

        // Emit enum collection getters
        if (enumCollectionGetters.Any())
        {
            sb.AppendLine();
            sb.AppendLine("            // Collection properties");
            foreach (var getter in enumCollectionGetters)
            {
                EmitEnumCollectionGetter(sb, getter, "entity");
            }
        }

        sb.AppendLine();
        sb.AppendLine("            return data;");
        sb.AppendLine("        }");
        sb.AppendLine();

        // Generate field names set for field filtering
        EmitFieldNamesSet(sb, entityName, simpleGetters, enumIndexedGetters, enumCollectionGetters);

        // Generate list of unsupported getters (for reference)
        if (unsupportedGetters.Any())
        {
            sb.AppendLine($"        // Unsupported getters ({unsupportedGetters.Count} total):");
            foreach (var getter in unsupportedGetters.Take(20))
            {
                var paramInfo = getter.HasParameters
                    ? $"({string.Join(", ", getter.ParameterTypes)})"
                    : "()";
                sb.AppendLine($"        // - {getter.Name}{paramInfo} -> {getter.ReturnType}");
            }
            if (unsupportedGetters.Count > 20)
            {
                sb.AppendLine($"        // ... and {unsupportedGetters.Count - 20} more");
            }
            sb.AppendLine();
        }

        sb.AppendLine($"        #endregion");
        sb.AppendLine();
    }

    private string GenerateGetterAccessor(GetterSignature getter, string entityName)
    {
        var entityVar = "entity";

        // Handle enum return types that need mzType resolution
        if (IsEnumType(getter.ReturnType))
        {
            var infosMethod = GetInfosMethodForEnum(getter.ReturnType);
            if (infosMethod != null)
            {
                return $"infos.{infosMethod}({entityVar}.{getter.Name}())?.mzType";
            }
            // Simple enum - just call ToString
            return $"{entityVar}.{getter.Name}().ToString()";
        }

        // Handle nullable types
        if (getter.ReturnType.EndsWith("?"))
        {
            return $"{entityVar}.{getter.Name}()";
        }

        // Simple types
        return $"{entityVar}.{getter.Name}()";
    }

    /// <summary>
    /// Emit code for an enum-indexed getter (e.g., getRating(RatingType) → int).
    /// Generates a dictionary mapping enum type strings to values.
    /// Filters out -1 (sentinel) and false values.
    /// </summary>
    private void EmitEnumIndexedGetter(StringBuilder sb, GetterSignature getter, string entityVar)
    {
        var enumType = getter.ParameterTypes[0];
        var enumInfo = _typeAnalyzer.GetEnumTypeInfo(enumType)!;
        var propertyName = getter.PropertyName + "s"; // rating → ratings
        var baseReturnType = getter.ReturnType.TrimEnd('?');

        sb.AppendLine($"            // {getter.Name}({enumType}) → Dictionary");
        sb.AppendLine($"            try");
        sb.AppendLine($"            {{");
        sb.AppendLine($"                var {propertyName} = new Dictionary<string, object>();");
        sb.AppendLine($"                for (int i = 0; i < (int)infos.{enumInfo.CountMethod}(); i++)");
        sb.AppendLine($"                {{");
        sb.AppendLine($"                    var enumVal = ({enumType})i;");
        sb.AppendLine($"                    var key = infos.{enumInfo.InfosMethod}(enumVal)?.mzType;");
        sb.AppendLine($"                    if (key != null && key != \"NONE\")");
        sb.AppendLine($"                    {{");

        // Add filtering based on return type
        if (baseReturnType == "int" || baseReturnType == "Int32")
        {
            // Some getters should filter zeros (counts that are noisy when 0)
            var filterCondition = ZeroFilteredGetters.Contains(getter.Name) ? "v > 0" : "v != -1";
            sb.AppendLine($"                        try {{ var v = {entityVar}.{getter.Name}(enumVal); if ({filterCondition}) {propertyName}[key] = v; }}");
        }
        else if (baseReturnType == "bool" || baseReturnType == "Boolean")
        {
            sb.AppendLine($"                        try {{ if ({entityVar}.{getter.Name}(enumVal)) {propertyName}[key] = true; }}");
        }
        else
        {
            sb.AppendLine($"                        try {{ {propertyName}[key] = {entityVar}.{getter.Name}(enumVal); }}");
        }

        sb.AppendLine($"                        catch {{ }}");
        sb.AppendLine($"                    }}");
        sb.AppendLine($"                }}");
        sb.AppendLine($"                if ({propertyName}.Count > 0) data[\"{propertyName}\"] = {propertyName};");
        sb.AppendLine($"            }}");
        sb.AppendLine($"            catch {{ }}");
        sb.AppendLine();
    }

    /// <summary>
    /// Emit code for an enum collection getter (e.g., getTraits() → ReadOnlyList&lt;TraitType&gt;).
    /// Generates an array of enum type strings.
    /// </summary>
    private void EmitEnumCollectionGetter(StringBuilder sb, GetterSignature getter, string entityVar)
    {
        var elementType = getter.CollectionElementType!;
        var enumInfo = _typeAnalyzer.GetEnumTypeInfo(elementType)!;
        var propertyName = getter.PropertyName; // traits stays traits

        sb.AppendLine($"            // {getter.Name}() → List<string>");
        sb.AppendLine($"            try");
        sb.AppendLine($"            {{");
        sb.AppendLine($"                var {propertyName} = new List<string>();");
        sb.AppendLine($"                foreach (var item in {entityVar}.{getter.Name}())");
        sb.AppendLine($"                {{");
        sb.AppendLine($"                    var name = infos.{enumInfo.InfosMethod}(item)?.mzType;");
        sb.AppendLine($"                    if (name != null && name != \"NONE\") {propertyName}.Add(name);");
        sb.AppendLine($"                }}");
        sb.AppendLine($"                data[\"{propertyName}\"] = {propertyName};");
        sb.AppendLine($"            }}");
        sb.AppendLine($"            catch {{ }}");
        sb.AppendLine();
    }

    /// <summary>
    /// Emit a static HashSet of valid field names for an entity type.
    /// Used for field filtering in API queries.
    /// </summary>
    private void EmitFieldNamesSet(
        StringBuilder sb,
        string entityName,
        List<GetterSignature> simpleGetters,
        List<GetterSignature> enumIndexedGetters,
        List<GetterSignature> enumCollectionGetters)
    {
        // Collect all field names with their actual JSON property names
        var fieldNames = new List<string>();

        // Simple getters use PropertyName directly
        fieldNames.AddRange(simpleGetters.Select(g => g.PropertyName));

        // Enum-indexed getters add "s" suffix (e.g., rating → ratings)
        fieldNames.AddRange(enumIndexedGetters.Select(g => g.PropertyName + "s"));

        // Enum collection getters use PropertyName directly
        fieldNames.AddRange(enumCollectionGetters.Select(g => g.PropertyName));

        // Sort for consistent output
        fieldNames.Sort(StringComparer.OrdinalIgnoreCase);

        sb.AppendLine($"        /// <summary>");
        sb.AppendLine($"        /// Valid field names for {entityName} objects (for ?fields= query filtering).");
        sb.AppendLine($"        /// Auto-generated - {fieldNames.Count} fields.");
        sb.AppendLine($"        /// </summary>");
        sb.AppendLine($"        public static readonly HashSet<string> {entityName}FieldNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)");
        sb.AppendLine("        {");

        foreach (var field in fieldNames)
        {
            sb.AppendLine($"            \"{field}\",");
        }

        sb.AppendLine("        };");
        sb.AppendLine();
    }

    private bool IsSupportedReturnType(string returnType)
    {
        var baseType = returnType.TrimEnd('?');

        // Primitives
        if (new[] { "int", "Int32", "long", "Int64", "bool", "Boolean", "string", "String",
                    "float", "Single", "double", "Double" }.Contains(baseType, StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        // Enums (types ending in Type)
        if (baseType.EndsWith("Type", StringComparison.Ordinal))
        {
            return true;
        }

        // Skip complex types like StringBuilder, IEnumerable, etc.
        if (baseType.Contains("Builder") || baseType.Contains("IEnumerable") ||
            baseType.Contains("List") || baseType.Contains("Dictionary") ||
            baseType.Contains("[]") || baseType.Contains("<"))
        {
            return false;
        }

        // Skip game object types that can't be serialized (have circular references)
        var unsupportedTypes = new[]
        {
            "CitySite", "CityQueueData", "TextVariable", "CharacterStoryData",
            "Tile", "City", "Unit", "Character", "Player", "Family", "Tribe"
        };
        if (unsupportedTypes.Contains(baseType, StringComparer.Ordinal))
        {
            return false;
        }

        // Skip Unity types that have self-referencing properties
        if (baseType.StartsWith("Vector") || baseType.StartsWith("Color") ||
            baseType == "Quaternion" || baseType == "Rect" || baseType == "Bounds")
        {
            return false;
        }

        // Skip void (shouldn't happen for getters)
        if (baseType == "void")
        {
            return false;
        }

        // Allow other simple types
        return true;
    }

    private bool IsEnumType(string returnType)
    {
        var baseType = returnType.TrimEnd('?');
        return baseType.EndsWith("Type", StringComparison.Ordinal);
    }

    private string? GetInfosMethodForEnum(string enumType)
    {
        var baseType = enumType.TrimEnd('?');

        // Map enum types to their infos accessor
        var mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["UnitType"] = "unit",
            ["TechType"] = "tech",
            ["ProjectType"] = "project",
            ["YieldType"] = "yield",
            ["ImprovementType"] = "improvement",
            ["ResourceType"] = "resource",
            ["FamilyType"] = "family",
            ["NationType"] = "nation",
            ["TribeType"] = "tribe",
            ["ReligionType"] = "religion",
            ["CultureType"] = "culture",
            ["TerrainType"] = "terrain",
            ["HeightType"] = "height",
            ["VegetationType"] = "vegetation",
            ["PromotionType"] = "promotion",
            ["TraitType"] = "trait",
            ["CouncilType"] = "council",
            ["LawType"] = "law",
            ["SpecialistType"] = "specialist",
            ["JobType"] = "job",
            ["MissionType"] = "mission",
            ["GoalType"] = "goal",
            ["CognomenType"] = "cognomen",
        };

        return mappings.TryGetValue(baseType, out var method) ? method : null;
    }
}

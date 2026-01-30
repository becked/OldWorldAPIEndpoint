using System.Text.RegularExpressions;

namespace OldWorldCodeGen.Parsing;

/// <summary>
/// Analyzes and categorizes types from game source code.
/// </summary>
public class TypeAnalyzer
{
    // Entity types that need ID → object resolution via game.xxx(id)
    private static readonly HashSet<string> EntityTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Unit", "City", "Tile", "Character", "Player", "Family", "Tribe"
    };

    // Primitive types
    private static readonly HashSet<string> PrimitiveTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "int", "Int32", "long", "Int64", "bool", "Boolean", "string", "String",
        "float", "Single", "double", "Double", "byte", "Byte", "short", "Int16"
    };

    // Complex types that we cannot handle - methods using these will be skipped
    private static readonly HashSet<string> UnsupportedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "ActionData", "HistoryGenerator.Event", "HistoryGenerator",
        "int[]", "List<int>", "HashSet<int>", "StringBuilder"
    };

    // Known enum types that end with "Type" and have Infos accessors
    // Maps enum type name → (infos method, count method, has NONE value)
    private static readonly Dictionary<string, (string InfosMethod, string CountMethod, bool HasNone)> KnownEnumTypes =
        new(StringComparer.OrdinalIgnoreCase)
    {
        ["UnitType"] = ("unit", "unitsNum", true),
        ["TechType"] = ("tech", "techsNum", true),
        ["ProjectType"] = ("project", "projectsNum", true),
        ["PromotionType"] = ("promotion", "promotionsNum", true),
        ["YieldType"] = ("yield", "yieldsNum", true),
        ["ImprovementType"] = ("improvement", "improvementsNum", true),
        ["ResourceType"] = ("resource", "resourcesNum", true),
        ["FamilyType"] = ("family", "familiesNum", true),
        ["NationType"] = ("nation", "nationsNum", true),
        ["TribeType"] = ("tribe", "tribesNum", true),
        ["ReligionType"] = ("religion", "religionsNum", true),
        ["CultureType"] = ("culture", "culturesNum", true),
        ["CouncilType"] = ("council", "councilsNum", true),
        ["MissionType"] = ("mission", "missionsNum", true),
        ["GoalType"] = ("goal", "goalsNum", true),
        ["TraitType"] = ("trait", "traitsNum", true),
        ["EffectUnitType"] = ("effectUnit", "effectUnitsNum", true),
        ["EffectCityType"] = ("effectCity", "effectCitiesNum", true),
        ["EffectPlayerType"] = ("effectPlayer", "effectPlayersNum", true),
        ["LawType"] = ("law", "lawsNum", true),
        ["TheologyType"] = ("theology", "theologiesNum", true),
        ["SpecialistType"] = ("specialist", "specialistsNum", true),
        ["ImprovementClassType"] = ("improvementClass", "improvementClassesNum", true),
        ["UnitTraitType"] = ("unitTrait", "unitTraitsNum", true),
        ["TerrainType"] = ("terrain", "terrainsNum", true),
        ["TerrainStampType"] = ("terrainStamp", "terrainStampsNum", true),
        ["HeightType"] = ("height", "heightsNum", true),
        ["VegetationType"] = ("vegetation", "vegetationNum", true), // singular!
        ["BonusType"] = ("bonus", "bonusesNum", true),
        ["EventTriggerType"] = ("eventTrigger", "eventTriggersNum", true),
        ["DecisionType"] = ("decision", "decisionsNum", true),
        ["JobType"] = ("job", "jobsNum", true),
        ["CognomenType"] = ("cognomen", "cognomensNum", true),
        ["CourtierType"] = ("courtier", "courtiersNum", true),
        ["RelationshipType"] = ("relationship", "relationshipsNum", true),
        ["DiplomacyType"] = ("diplomacy", "diplomaciesNum", true),
        ["WarType"] = ("war", "warsNum", true),
        ["TributeType"] = ("tribute", "tributesNum", true),
        ["RatingType"] = ("rating", "ratingsNum", true),
        ["AchievementType"] = ("achievement", "achievementsNum", true),
        ["ScenarioType"] = ("scenario", "scenariosNum", true),
        ["AssetType"] = ("asset", "assetsNum", true),
        ["OccurrenceType"] = ("occurrence", "occurrencesNum", true),
        ["MemoryType"] = ("memory", "memoriesNum", true),
        ["DynastyType"] = ("dynasty", "dynastiesNum", true), // note: dynasties not dynastys
        ["EventStoryType"] = ("eventStory", "eventStoriesNum", true), // note: stories not storys
        ["VictoryType"] = ("victory", "victoriesNum", true),
        ["CharacterType"] = ("character", "charactersNum", true),
        ["CharacterPortraitType"] = ("characterPortrait", "characterPortraitsNum", true),
        ["NameType"] = ("name", "namesNum", true),
        ["HotkeyType"] = ("hotkey", "hotkeysNum", true),
        ["PingType"] = ("ping", "pingsNum", true),
    };

    // Enum types that are simple (no Infos lookup, just Enum.Parse)
    // These either have no Infos accessor OR don't have a NONE value
    private static readonly HashSet<string> SimpleEnumTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "PlayerType", "TeamType", "GenderType", "DirectionType", "CitySiteType",
        "GameModeType", "TurnStyleType", "TurnTimerType", "DifficultyType",
        "LanguageType", "GameLogType", "PlayerOptionType",
        // These types don't have Infos lookup methods:
        "ActionType", "ChatType", "ReminderType", "RotationType",
        // These types exist but are internal/special:
        "EventType" // different from EventTriggerType
    };

    // Enum types that don't have .NONE as their default value
    private static readonly HashSet<string> EnumsWithoutNone = new(StringComparer.OrdinalIgnoreCase)
    {
        "ReminderType", "ActionType", "ChatType", "RotationType"
    };

    private readonly HashSet<string> _discoveredEnumTypes = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Categorize a parameter type.
    /// </summary>
    public ParameterKind CategorizeType(string typeName)
    {
        // Handle nullable types
        var baseType = typeName.TrimEnd('?');

        // Check for unsupported complex types first
        if (IsUnsupportedType(baseType))
            return ParameterKind.Unknown;

        // Handle arrays
        if (baseType.EndsWith("[]"))
            return ParameterKind.Collection;

        // Handle generics
        if (baseType.Contains('<'))
            return ParameterKind.Collection;

        if (PrimitiveTypes.Contains(baseType))
            return ParameterKind.Primitive;

        if (EntityTypes.Contains(baseType))
            return ParameterKind.Entity;

        if (KnownEnumTypes.ContainsKey(baseType) || SimpleEnumTypes.Contains(baseType))
        {
            _discoveredEnumTypes.Add(baseType);
            return ParameterKind.EnumType;
        }

        // Heuristic: types ending in "Type" are likely enums
        if (baseType.EndsWith("Type", StringComparison.Ordinal))
        {
            _discoveredEnumTypes.Add(baseType);
            return ParameterKind.EnumType;
        }

        return ParameterKind.Unknown;
    }

    /// <summary>
    /// Check if a type is explicitly unsupported.
    /// </summary>
    public bool IsUnsupportedType(string typeName)
    {
        var baseType = typeName.TrimEnd('?');
        return UnsupportedTypes.Contains(baseType) ||
               baseType.Contains("[]") ||
               baseType.Contains("List<") ||
               baseType.Contains("HashSet<") ||
               baseType.Contains("Dictionary<");
    }

    /// <summary>
    /// Get info for generating a type resolver for an enum type.
    /// </summary>
    public EnumTypeInfo? GetEnumTypeInfo(string typeName)
    {
        if (KnownEnumTypes.TryGetValue(typeName, out var info))
        {
            return new EnumTypeInfo
            {
                TypeName = typeName,
                InfosMethod = info.InfosMethod,
                CountMethod = info.CountMethod,
                HasNone = info.HasNone
            };
        }

        // For simple enums, we don't need infos lookup
        if (SimpleEnumTypes.Contains(typeName))
        {
            return null; // Will use Enum.TryParse instead
        }

        // Unknown enum - don't try to derive, mark as simple
        // This prevents generating incorrect Infos method calls
        return null;
    }

    /// <summary>
    /// Check if an enum type has a .NONE value.
    /// </summary>
    public bool EnumHasNoneValue(string typeName)
    {
        return !EnumsWithoutNone.Contains(typeName);
    }

    /// <summary>
    /// Check if this enum type uses simple Enum.TryParse (no Infos lookup).
    /// </summary>
    public bool IsSimpleEnumType(string typeName) => SimpleEnumTypes.Contains(typeName);

    /// <summary>
    /// Get the game.xxx() method name for resolving an entity ID.
    /// </summary>
    public string GetEntityResolverMethod(string typeName)
    {
        return typeName.ToLowerInvariant() switch
        {
            "unit" => "unit",
            "city" => "city",
            "tile" => "tile",
            "character" => "character",
            "player" => "player",
            "family" => "family",
            "tribe" => "tribe",
            _ => typeName.ToLowerInvariant()
        };
    }

    /// <summary>
    /// Get all enum types that need resolvers generated.
    /// </summary>
    public IEnumerable<EnumTypeInfo> GetEnumTypesNeedingResolvers()
    {
        foreach (var typeName in _discoveredEnumTypes)
        {
            var info = GetEnumTypeInfo(typeName);
            if (info != null)
                yield return info;
        }
    }

    /// <summary>
    /// Get all simple enum types discovered (use Enum.TryParse).
    /// </summary>
    public IEnumerable<string> GetSimpleEnumTypes()
    {
        return _discoveredEnumTypes.Where(IsSimpleEnumType);
    }

    /// <summary>
    /// Reset discovered types (for a fresh parse).
    /// </summary>
    public void Reset()
    {
        _discoveredEnumTypes.Clear();
    }

    #region API Parameter Name Transformation

    /// <summary>
    /// Transform a game parameter name (Hungarian notation) to API-friendly name.
    /// Rules:
    /// - pX → x_id (entity pointer)
    /// - eX → x_type (enum type)
    /// - zX → x (string)
    /// - bX → x (boolean)
    /// - iX → context-dependent (see TransformIntParam)
    /// - Non-Hungarian → pass through unchanged
    /// </summary>
    public static string ToApiParamName(string gameParam)
    {
        if (string.IsNullOrEmpty(gameParam) || gameParam.Length < 2)
            return gameParam;

        char prefix = gameParam[0];
        string rest = gameParam[1..];

        // Only transform if second char is uppercase (Hungarian notation)
        if (!char.IsUpper(rest[0]))
            return gameParam;

        string baseName = char.ToLower(rest[0]) + rest[1..];

        return prefix switch
        {
            'p' => ToSnakeCase(baseName) + "_id",
            'e' => ToSnakeCase(baseName) + "_type",
            'z' => ToSnakeCase(baseName),
            'b' => ToSnakeCase(baseName),
            'i' => TransformIntParam(baseName),
            _ => gameParam
        };
    }

    /// <summary>
    /// Transform integer parameter names.
    /// - Contains Id/ID suffix → strip suffix and add _id
    /// - Otherwise → strip to snake_case base name
    /// </summary>
    private static string TransformIntParam(string baseName)
    {
        // Rule 1: Contains Id/ID suffix → _id
        if (baseName.EndsWith("Id", StringComparison.Ordinal) || baseName.EndsWith("ID", StringComparison.Ordinal))
        {
            var suffixLen = baseName.EndsWith("ID", StringComparison.Ordinal) ? 2 : 2;
            var withoutSuffix = baseName[..^suffixLen];
            return ToSnakeCase(withoutSuffix) + "_id";
        }

        // Rule 2: Strip to base name
        return ToSnakeCase(baseName);
    }

    /// <summary>
    /// Convert camelCase/PascalCase to snake_case.
    /// </summary>
    private static string ToSnakeCase(string camelCase)
    {
        if (string.IsNullOrEmpty(camelCase))
            return camelCase;

        // Insert underscore before uppercase letters (except at start)
        return Regex.Replace(camelCase, "([a-z])([A-Z])", "$1_$2").ToLowerInvariant();
    }

    /// <summary>
    /// Detect collisions in API parameter names within a single command.
    /// Returns list of collision groups (API name → list of game params that map to it).
    /// </summary>
    public static Dictionary<string, List<string>> DetectCollisions(IEnumerable<string> gameParams)
    {
        var apiNames = new Dictionary<string, List<string>>();

        foreach (var gameParam in gameParams)
        {
            var apiName = ToApiParamName(gameParam);
            if (!apiNames.ContainsKey(apiName))
                apiNames[apiName] = new List<string>();
            apiNames[apiName].Add(gameParam);
        }

        return apiNames
            .Where(kv => kv.Value.Count > 1)
            .ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    #endregion
}

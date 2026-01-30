namespace OldWorldCodeGen.Parsing;

/// <summary>
/// Represents a parsed method signature from game source code.
/// </summary>
public class MethodSignature
{
    /// <summary>Original method name (e.g., "sendMoveUnit")</summary>
    public required string Name { get; set; }

    /// <summary>API action name derived from method (e.g., "moveUnit")</summary>
    public string ActionName => Name.StartsWith("send", StringComparison.OrdinalIgnoreCase)
        ? char.ToLowerInvariant(Name[4]) + Name[5..]
        : Name;

    /// <summary>Return type (usually "void" for send* methods)</summary>
    public required string ReturnType { get; set; }

    /// <summary>Parameters with type information</summary>
    public required List<ParameterInfo> Parameters { get; set; }

    /// <summary>Whether the method is virtual</summary>
    public bool IsVirtual { get; set; }

    /// <summary>Full signature string for comments (e.g., "sendMoveUnit(Unit, Tile, Boolean, Boolean, Tile)")</summary>
    public string SignatureComment => $"{Name}({string.Join(", ", Parameters.Select(p => p.Type))})";
}

/// <summary>
/// Represents a method parameter with type categorization.
/// </summary>
public class ParameterInfo
{
    /// <summary>Original parameter name from game code (e.g., "pUnit", "bShift")</summary>
    public required string Name { get; set; }

    /// <summary>API-friendly parameter name (e.g., "unit_id", "shift")</summary>
    public string ApiName => TypeAnalyzer.ToApiParamName(Name);

    /// <summary>Type name (e.g., "Unit", "bool", "UnitType")</summary>
    public required string Type { get; set; }

    /// <summary>Categorized kind for code generation</summary>
    public ParameterKind Kind { get; set; }

    /// <summary>Whether parameter has a default value</summary>
    public bool IsOptional { get; set; }

    /// <summary>Default value if optional (as string)</summary>
    public string? DefaultValue { get; set; }

    /// <summary>Whether this is a nullable type (e.g., int?)</summary>
    public bool IsNullable { get; set; }
}

/// <summary>
/// Categories of parameter types for code generation.
/// </summary>
public enum ParameterKind
{
    /// <summary>Primitive types: int, bool, string, long, double</summary>
    Primitive,

    /// <summary>Game entity types that need ID resolution: Unit, City, Tile, Character, Player</summary>
    Entity,

    /// <summary>Enum types that need type string resolution: UnitType, TechType, etc.</summary>
    EnumType,

    /// <summary>Collection types: List&lt;T&gt;, arrays</summary>
    Collection,

    /// <summary>Unknown or complex types</summary>
    Unknown
}

/// <summary>
/// Information about an enum type that needs a resolver.
/// </summary>
public class EnumTypeInfo
{
    /// <summary>Enum type name (e.g., "UnitType")</summary>
    public required string TypeName { get; set; }

    /// <summary>Infos accessor method (e.g., "unit" for infos.unit())</summary>
    public required string InfosMethod { get; set; }

    /// <summary>Count method (e.g., "unitsNum" for infos.unitsNum())</summary>
    public required string CountMethod { get; set; }

    /// <summary>Whether this enum has a .NONE value</summary>
    public bool HasNone { get; set; } = true;

    /// <summary>Default value expression for this enum type</summary>
    public string DefaultValue => HasNone ? $"{TypeName}.NONE" : $"default({TypeName})";
}

/// <summary>
/// Information about a getter method from entity classes.
/// </summary>
public class GetterSignature
{
    /// <summary>Method name (e.g., "getLegitimacy")</summary>
    public required string Name { get; set; }

    /// <summary>Property name derived from getter (e.g., "legitimacy")</summary>
    public string PropertyName
    {
        get
        {
            string baseName;
            if (Name.StartsWith("get", StringComparison.Ordinal) && Name.Length > 3)
                baseName = Name[3..];  // getID → ID, getHP → HP, getHPMax → HPMax
            else if (Name.StartsWith("is", StringComparison.Ordinal) && Name.Length > 2)
                baseName = Name;  // isAlive → isAlive (keep as-is)
            else if (Name.StartsWith("has", StringComparison.Ordinal) && Name.Length > 3)
                baseName = Name;  // hasUnit → hasUnit (keep as-is)
            else
                return Name;

            return ToCamelCase(baseName);
        }
    }

    /// <summary>
    /// Convert a name to proper camelCase, handling consecutive uppercase letters.
    /// Examples: ID → id, HP → hp, HPMax → hpMax, Name → name, isAlive → isAlive
    /// </summary>
    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;

        // Find where the leading uppercase run ends
        int i = 0;
        while (i < name.Length && char.IsUpper(name[i])) i++;

        if (i == 0) return name;  // Already starts lowercase (isAlive, hasUnit)
        if (i == 1) return char.ToLowerInvariant(name[0]) + name[1..];  // Single uppercase: Name → name
        if (i == name.Length) return name.ToLowerInvariant();  // All uppercase: ID → id, HP → hp

        // Multiple uppercase followed by lowercase: HPMax → hpMax, IDValue → idValue
        return name[..(i-1)].ToLowerInvariant() + name[(i-1)..];
    }

    /// <summary>Return type</summary>
    public required string ReturnType { get; set; }

    /// <summary>Whether the getter takes parameters (we skip parameterized getters for simple builders)</summary>
    public bool HasParameters { get; set; }

    /// <summary>Number of parameters</summary>
    public int ParameterCount { get; set; }
}

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace OldWorldCodeGen.Generation;

/// <summary>
/// Schema annotations loaded from YAML to supplement parsed game source.
/// Defines computed fields, exclusions, nested schemas, and endpoint routes.
/// </summary>
public class SchemaAnnotations
{
    /// <summary>
    /// Additional computed properties to add to entity schemas.
    /// Key is entity name (Player, City, etc.), value is property definitions.
    /// </summary>
    [YamlMember(Alias = "entity_additions")]
    public Dictionary<string, Dictionary<string, PropertyDefinition>> EntityAdditions { get; set; } = new();

    /// <summary>
    /// Properties to exclude from entity schemas (unsupported types).
    /// Key is entity name, value is list of property names to exclude.
    /// </summary>
    [YamlMember(Alias = "entity_exclusions")]
    public Dictionary<string, List<string>> EntityExclusions { get; set; } = new();

    /// <summary>
    /// Standalone nested schema definitions (BuildQueueItem, YieldDetails, etc.).
    /// </summary>
    [YamlMember(Alias = "nested_schemas")]
    public Dictionary<string, SchemaDefinition> NestedSchemas { get; set; } = new();

    /// <summary>
    /// Composite schema definitions (GameState, etc.).
    /// </summary>
    [YamlMember(Alias = "composite_schemas")]
    public Dictionary<string, SchemaDefinition> CompositeSchemas { get; set; } = new();

    /// <summary>
    /// Event schema definitions.
    /// </summary>
    [YamlMember(Alias = "event_schemas")]
    public Dictionary<string, SchemaDefinition> EventSchemas { get; set; } = new();

    /// <summary>
    /// Diplomacy schema definitions.
    /// </summary>
    [YamlMember(Alias = "diplomacy_schemas")]
    public Dictionary<string, SchemaDefinition> DiplomacySchemas { get; set; } = new();

    /// <summary>
    /// Player sub-resource schema definitions.
    /// </summary>
    [YamlMember(Alias = "player_schemas")]
    public Dictionary<string, SchemaDefinition> PlayerSchemas { get; set; } = new();

    /// <summary>
    /// Endpoint route definitions.
    /// </summary>
    [YamlMember(Alias = "endpoints")]
    public List<EndpointDefinition> Endpoints { get; set; } = new();

    /// <summary>
    /// Load annotations from a YAML file.
    /// </summary>
    public static SchemaAnnotations Load(string path)
    {
        if (!File.Exists(path))
        {
            Console.WriteLine($"Warning: Schema annotations file not found at {path}, using defaults");
            return new SchemaAnnotations();
        }

        var yaml = File.ReadAllText(path);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        return deserializer.Deserialize<SchemaAnnotations>(yaml) ?? new SchemaAnnotations();
    }

    /// <summary>
    /// Check if a property should be excluded for an entity.
    /// </summary>
    public bool IsExcluded(string entityName, string propertyName)
    {
        return EntityExclusions.TryGetValue(entityName, out var exclusions) &&
               exclusions.Contains(propertyName);
    }

    /// <summary>
    /// Get additional properties to add to an entity schema.
    /// </summary>
    public Dictionary<string, PropertyDefinition> GetAdditions(string entityName)
    {
        return EntityAdditions.TryGetValue(entityName, out var additions)
            ? additions
            : new Dictionary<string, PropertyDefinition>();
    }
}

/// <summary>
/// OpenAPI property definition.
/// </summary>
public class PropertyDefinition
{
    [YamlMember(Alias = "type")]
    public string? Type { get; set; }

    [YamlMember(Alias = "format")]
    public string? Format { get; set; }

    [YamlMember(Alias = "description")]
    public string? Description { get; set; }

    [YamlMember(Alias = "nullable")]
    public bool Nullable { get; set; }

    [YamlMember(Alias = "enum")]
    public List<string>? Enum { get; set; }

    [YamlMember(Alias = "items")]
    public PropertyDefinition? Items { get; set; }

    [YamlMember(Alias = "additionalProperties")]
    public PropertyDefinition? AdditionalProperties { get; set; }

    [YamlMember(Alias = "$ref")]
    public string? Ref { get; set; }
}

/// <summary>
/// Full OpenAPI schema definition.
/// </summary>
public class SchemaDefinition
{
    [YamlMember(Alias = "type")]
    public string? Type { get; set; }

    [YamlMember(Alias = "description")]
    public string? Description { get; set; }

    [YamlMember(Alias = "nullable")]
    public bool Nullable { get; set; }

    [YamlMember(Alias = "properties")]
    public Dictionary<string, PropertyDefinition>? Properties { get; set; }

    [YamlMember(Alias = "required")]
    public List<string>? Required { get; set; }

    [YamlMember(Alias = "additionalProperties")]
    public PropertyDefinition? AdditionalProperties { get; set; }
}

/// <summary>
/// Endpoint route definition.
/// </summary>
public class EndpointDefinition
{
    [YamlMember(Alias = "path")]
    public string Path { get; set; } = "";

    [YamlMember(Alias = "method")]
    public string Method { get; set; } = "get";

    [YamlMember(Alias = "operationId")]
    public string? OperationId { get; set; }

    [YamlMember(Alias = "summary")]
    public string? Summary { get; set; }

    [YamlMember(Alias = "description")]
    public string? Description { get; set; }

    [YamlMember(Alias = "tags")]
    public List<string>? Tags { get; set; }

    [YamlMember(Alias = "response")]
    public string Response { get; set; } = "";

    [YamlMember(Alias = "response_description")]
    public string? ResponseDescription { get; set; }

    [YamlMember(Alias = "items")]
    public string? Items { get; set; }

    [YamlMember(Alias = "pagination")]
    public bool Pagination { get; set; }

    [YamlMember(Alias = "params")]
    public List<ParameterDefinition>? Params { get; set; }
}

/// <summary>
/// Endpoint parameter definition.
/// </summary>
public class ParameterDefinition
{
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = "";

    [YamlMember(Alias = "in")]
    public string In { get; set; } = "path";

    [YamlMember(Alias = "type")]
    public string Type { get; set; } = "integer";

    [YamlMember(Alias = "description")]
    public string? Description { get; set; }

    [YamlMember(Alias = "required")]
    public bool Required { get; set; } = true;
}

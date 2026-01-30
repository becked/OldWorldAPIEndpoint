using System.Text;
using OldWorldCodeGen.Parsing;

namespace OldWorldCodeGen.Generation;

/// <summary>
/// Generates openapi.yaml from parsed method signatures and schema annotations.
/// </summary>
public class OpenApiGenerator
{
    private readonly TypeAnalyzer _typeAnalyzer;

    public OpenApiGenerator(TypeAnalyzer typeAnalyzer)
    {
        _typeAnalyzer = typeAnalyzer;
    }

    /// <summary>
    /// Generate complete OpenAPI specification including commands and read endpoints.
    /// </summary>
    public string Generate(
        List<MethodSignature> methods,
        Dictionary<string, List<GetterSignature>> entityGetters,
        SchemaAnnotations annotations,
        string version)
    {
        var supportedMethods = methods.Where(m => !HasUnsupportedParameters(m)).ToList();
        var sb = new StringBuilder();

        GenerateHeader(sb, version);
        GenerateServers(sb);
        GenerateTags(sb);
        GeneratePaths(sb, supportedMethods, annotations);
        GenerateComponents(sb, supportedMethods, entityGetters, annotations);

        return sb.ToString();
    }

    /// <summary>
    /// Generate command-only OpenAPI specification (backward compatible).
    /// </summary>
    public string Generate(List<MethodSignature> methods, string version)
    {
        return Generate(methods, new Dictionary<string, List<GetterSignature>>(), new SchemaAnnotations(), version);
    }

    #region Header & Metadata

    private void GenerateHeader(StringBuilder sb, string version)
    {
        sb.AppendLine("# Auto-generated from Old World game source");
        sb.AppendLine($"# Generated at: {DateTime.UtcNow:O}");
        sb.AppendLine("# Do not edit manually - regenerate with: dotnet run --project tools/OldWorldCodeGen");
        sb.AppendLine();
        sb.AppendLine("openapi: \"3.0.3\"");
        sb.AppendLine();
        sb.AppendLine("info:");
        sb.AppendLine("  title: Old World API");
        sb.AppendLine("  description: |");
        sb.AppendLine("    REST API for the Old World game mod that exposes game state and accepts commands");
        sb.AppendLine("    for companion apps, overlays, and external tools.");
        sb.AppendLine();
        sb.AppendLine("    ## Overview");
        sb.AppendLine();
        sb.AppendLine("    This API provides:");
        sb.AppendLine("    - **Read access** to game state including players, cities, characters, units, tribes, diplomacy, and events");
        sb.AppendLine("    - **Command execution** for unit actions, city production, research, and turn management");
        sb.AppendLine();
        sb.AppendLine("    All data uses game type strings (e.g., `NATION_ROME`, `YIELD_FOOD`, `UNIT_WARRIOR`) for unambiguous identification.");
        sb.AppendLine();
        sb.AppendLine("    **Note:** Commands are disabled in multiplayer games.");
        sb.AppendLine();
        sb.AppendLine("    ## Connection");
        sb.AppendLine();
        sb.AppendLine("    The HTTP REST server runs on port 9877 when the mod is active and a game is loaded.");
        sb.AppendLine($"  version: \"{version}\"");
        sb.AppendLine("  license:");
        sb.AppendLine("    name: MIT");
        sb.AppendLine("    url: https://opensource.org/licenses/MIT");
        sb.AppendLine("  contact:");
        sb.AppendLine("    name: Old World API Endpoint");
        sb.AppendLine("    url: https://github.com/becked/OldWorldAPIEndpoint");
        sb.AppendLine();
    }

    private void GenerateServers(StringBuilder sb)
    {
        sb.AppendLine("servers:");
        sb.AppendLine("  - url: http://localhost:9877");
        sb.AppendLine("    description: Local HTTP REST server");
        sb.AppendLine();
    }

    private void GenerateTags(StringBuilder sb)
    {
        sb.AppendLine("tags:");
        sb.AppendLine("  - name: State");
        sb.AppendLine("    description: Full game state");
        sb.AppendLine("  - name: Players");
        sb.AppendLine("    description: Player (nation) data");
        sb.AppendLine("  - name: Cities");
        sb.AppendLine("    description: City data");
        sb.AppendLine("  - name: Characters");
        sb.AppendLine("    description: Character data");
        sb.AppendLine("  - name: Units");
        sb.AppendLine("    description: Unit data");
        sb.AppendLine("  - name: Tiles");
        sb.AppendLine("    description: Map tile data");
        sb.AppendLine("  - name: Events");
        sb.AppendLine("    description: Turn-based event detection");
        sb.AppendLine("  - name: Tribes");
        sb.AppendLine("    description: Tribe (barbarian) data");
        sb.AppendLine("  - name: Diplomacy");
        sb.AppendLine("    description: Diplomatic relationships");
        sb.AppendLine("  - name: Religion");
        sb.AppendLine("    description: Global religion state");
        sb.AppendLine("  - name: Commands");
        sb.AppendLine("    description: Game command execution (single-player only)");
        sb.AppendLine("  - name: Config");
        sb.AppendLine("    description: Game configuration");
        sb.AppendLine();
    }

    #endregion

    #region Paths Generation

    private void GeneratePaths(StringBuilder sb, List<MethodSignature> methods, SchemaAnnotations annotations)
    {
        sb.AppendLine("paths:");

        // Generate read endpoints from annotations
        foreach (var endpoint in annotations.Endpoints)
        {
            GenerateReadEndpoint(sb, endpoint);
        }

        // Generate command endpoint
        GenerateCommandEndpoint(sb, methods);
    }

    private void GenerateReadEndpoint(StringBuilder sb, EndpointDefinition endpoint)
    {
        sb.AppendLine($"  {endpoint.Path}:");
        sb.AppendLine($"    {endpoint.Method}:");

        // Tags
        if (endpoint.Tags?.Count > 0)
        {
            sb.AppendLine("      tags:");
            foreach (var tag in endpoint.Tags)
            {
                sb.AppendLine($"        - {tag}");
            }
        }

        // Summary and description
        if (!string.IsNullOrEmpty(endpoint.Summary))
            sb.AppendLine($"      summary: {endpoint.Summary}");

        if (!string.IsNullOrEmpty(endpoint.Description))
        {
            sb.AppendLine("      description: |");
            sb.AppendLine($"        {endpoint.Description}");
        }

        // operationId - use explicit if provided, otherwise auto-generate
        var operationId = !string.IsNullOrEmpty(endpoint.OperationId)
            ? endpoint.OperationId
            : GenerateOperationId(endpoint.Path, endpoint.Method);
        sb.AppendLine($"      operationId: {operationId}");

        // Parameters
        if (endpoint.Params?.Count > 0 || endpoint.Pagination)
        {
            sb.AppendLine("      parameters:");

            if (endpoint.Params != null)
            {
                foreach (var param in endpoint.Params)
                {
                    sb.AppendLine($"        - name: {param.Name}");
                    sb.AppendLine($"          in: {param.In}");
                    sb.AppendLine($"          required: {param.Required.ToString().ToLowerInvariant()}");
                    sb.AppendLine("          schema:");
                    sb.AppendLine($"            type: {param.Type}");
                    if (!string.IsNullOrEmpty(param.Description))
                        sb.AppendLine($"          description: {param.Description}");
                }
            }

            if (endpoint.Pagination)
            {
                sb.AppendLine("        - name: offset");
                sb.AppendLine("          in: query");
                sb.AppendLine("          required: false");
                sb.AppendLine("          schema:");
                sb.AppendLine("            type: integer");
                sb.AppendLine("            default: 0");
                sb.AppendLine("          description: Number of tiles to skip");
                sb.AppendLine("        - name: limit");
                sb.AppendLine("          in: query");
                sb.AppendLine("          required: false");
                sb.AppendLine("          schema:");
                sb.AppendLine("            type: integer");
                sb.AppendLine("            default: 100");
                sb.AppendLine("          description: Maximum number of tiles to return");
            }
        }

        // Response
        sb.AppendLine("      responses:");
        sb.AppendLine("        '200':");
        var respDesc = endpoint.ResponseDescription ?? $"{endpoint.Response} data";
        sb.AppendLine($"          description: {respDesc}");
        sb.AppendLine("          content:");
        sb.AppendLine("            application/json:");
        sb.AppendLine("              schema:");

        if (endpoint.Response == "array" && !string.IsNullOrEmpty(endpoint.Items))
        {
            sb.AppendLine("                type: array");
            sb.AppendLine("                items:");
            sb.AppendLine($"                  $ref: '#/components/schemas/{endpoint.Items}'");
        }
        else
        {
            sb.AppendLine($"                $ref: '#/components/schemas/{endpoint.Response}'");
        }

        // Error responses
        sb.AppendLine("        '400':");
        sb.AppendLine("          $ref: '#/components/responses/BadRequest'");
        sb.AppendLine("        '404':");
        sb.AppendLine("          $ref: '#/components/responses/NotFound'");
        sb.AppendLine("        '503':");
        sb.AppendLine("          $ref: '#/components/responses/GameNotAvailable'");
        sb.AppendLine();
    }

    private void GenerateCommandEndpoint(StringBuilder sb, List<MethodSignature> methods)
    {
        sb.AppendLine("  /command:");
        sb.AppendLine("    post:");
        sb.AppendLine("      tags:");
        sb.AppendLine("        - Commands");
        sb.AppendLine("      summary: Execute a game command");
        sb.AppendLine("      description: |");
        sb.AppendLine("        Execute a game command. The action parameter determines which command");
        sb.AppendLine("        to run, and params contains the command-specific parameters.");
        sb.AppendLine("        Commands are disabled in multiplayer games.");
        sb.AppendLine("      operationId: executeCommand");
        sb.AppendLine("      requestBody:");
        sb.AppendLine("        required: true");
        sb.AppendLine("        content:");
        sb.AppendLine("          application/json:");
        sb.AppendLine("            schema:");
        sb.AppendLine("              $ref: '#/components/schemas/GameCommand'");
        sb.AppendLine("      responses:");
        sb.AppendLine("        '200':");
        sb.AppendLine("          description: Command result");
        sb.AppendLine("          content:");
        sb.AppendLine("            application/json:");
        sb.AppendLine("              schema:");
        sb.AppendLine("                $ref: '#/components/schemas/CommandResult'");
        sb.AppendLine("        '400':");
        sb.AppendLine("          $ref: '#/components/responses/BadRequest'");
        sb.AppendLine("        '503':");
        sb.AppendLine("          $ref: '#/components/responses/GameNotAvailable'");
        sb.AppendLine();

        // Bulk commands endpoint
        GenerateBulkCommandEndpoint(sb);
    }

    private void GenerateBulkCommandEndpoint(StringBuilder sb)
    {
        sb.AppendLine("  /commands:");
        sb.AppendLine("    post:");
        sb.AppendLine("      tags:");
        sb.AppendLine("        - Commands");
        sb.AppendLine("      summary: Execute multiple commands in sequence");
        sb.AppendLine("      description: |");
        sb.AppendLine("        Execute a batch of commands sequentially. Useful for operations like");
        sb.AppendLine("        move + attack sequences. If stopOnError is true (default), execution");
        sb.AppendLine("        stops at the first failure. Commands are disabled in multiplayer games.");
        sb.AppendLine("      operationId: executeBulkCommands");
        sb.AppendLine("      requestBody:");
        sb.AppendLine("        required: true");
        sb.AppendLine("        content:");
        sb.AppendLine("          application/json:");
        sb.AppendLine("            schema:");
        sb.AppendLine("              $ref: '#/components/schemas/BulkCommand'");
        sb.AppendLine("      responses:");
        sb.AppendLine("        '200':");
        sb.AppendLine("          description: Bulk command execution results");
        sb.AppendLine("          content:");
        sb.AppendLine("            application/json:");
        sb.AppendLine("              schema:");
        sb.AppendLine("                $ref: '#/components/schemas/BulkCommandResult'");
        sb.AppendLine("        '400':");
        sb.AppendLine("          $ref: '#/components/responses/BadRequest'");
        sb.AppendLine("        '503':");
        sb.AppendLine("          $ref: '#/components/responses/GameNotAvailable'");
        sb.AppendLine();
    }

    #endregion

    #region Components Generation

    private void GenerateComponents(
        StringBuilder sb,
        List<MethodSignature> methods,
        Dictionary<string, List<GetterSignature>> entityGetters,
        SchemaAnnotations annotations)
    {
        sb.AppendLine("components:");
        sb.AppendLine();

        // Responses
        GenerateResponses(sb);

        // Schemas
        sb.AppendLine("  schemas:");
        sb.AppendLine();

        // Entity response schemas (generated from getters + annotations)
        foreach (var (entityName, getters) in entityGetters.OrderBy(kv => kv.Key))
        {
            GenerateEntitySchema(sb, entityName, getters, annotations);
        }

        // Nested schemas from annotations
        foreach (var (schemaName, schemaDef) in annotations.NestedSchemas.OrderBy(kv => kv.Key))
        {
            GenerateSchemaFromDefinition(sb, schemaName, schemaDef);
        }

        // Composite schemas from annotations
        foreach (var (schemaName, schemaDef) in annotations.CompositeSchemas.OrderBy(kv => kv.Key))
        {
            GenerateSchemaFromDefinition(sb, schemaName, schemaDef);
        }

        // Event schemas from annotations
        foreach (var (schemaName, schemaDef) in annotations.EventSchemas.OrderBy(kv => kv.Key))
        {
            GenerateSchemaFromDefinition(sb, schemaName, schemaDef);
        }

        // Diplomacy schemas from annotations
        foreach (var (schemaName, schemaDef) in annotations.DiplomacySchemas.OrderBy(kv => kv.Key))
        {
            GenerateSchemaFromDefinition(sb, schemaName, schemaDef);
        }

        // Player sub-resource schemas from annotations
        foreach (var (schemaName, schemaDef) in annotations.PlayerSchemas.OrderBy(kv => kv.Key))
        {
            GenerateSchemaFromDefinition(sb, schemaName, schemaDef);
        }

        // Command schemas
        GenerateCommandSchemas(sb, methods);
    }

    private void GenerateResponses(StringBuilder sb)
    {
        sb.AppendLine("  responses:");
        sb.AppendLine("    BadRequest:");
        sb.AppendLine("      description: Bad request - invalid parameters");
        sb.AppendLine("      content:");
        sb.AppendLine("        application/json:");
        sb.AppendLine("          schema:");
        sb.AppendLine("            $ref: '#/components/schemas/Error'");
        sb.AppendLine("    NotFound:");
        sb.AppendLine("      description: Resource not found");
        sb.AppendLine("      content:");
        sb.AppendLine("        application/json:");
        sb.AppendLine("          schema:");
        sb.AppendLine("            $ref: '#/components/schemas/Error'");
        sb.AppendLine("    GameNotAvailable:");
        sb.AppendLine("      description: Game not available - no game loaded or in menu");
        sb.AppendLine("      content:");
        sb.AppendLine("        application/json:");
        sb.AppendLine("          schema:");
        sb.AppendLine("            $ref: '#/components/schemas/Error'");
        sb.AppendLine();
    }

    private void GenerateEntitySchema(
        StringBuilder sb,
        string entityName,
        List<GetterSignature> getters,
        SchemaAnnotations annotations)
    {
        sb.AppendLine($"    {entityName}:");
        sb.AppendLine("      type: object");
        sb.AppendLine($"      description: {entityName} entity data");
        sb.AppendLine("      properties:");

        // Get exclusions for this entity
        var exclusions = annotations.EntityExclusions.TryGetValue(entityName, out var excl) ? excl : new List<string>();

        // Generate properties from parsed getters
        foreach (var getter in getters.Where(g => !g.HasParameters).OrderBy(g => g.PropertyName))
        {
            if (exclusions.Contains(getter.PropertyName) || exclusions.Contains(getter.Name))
                continue;

            GenerateGetterProperty(sb, getter);
        }

        // Add computed properties from annotations
        var additions = annotations.GetAdditions(entityName);
        foreach (var (propName, propDef) in additions.OrderBy(kv => kv.Key))
        {
            GeneratePropertyFromDefinition(sb, propName, propDef, 8);
        }

        sb.AppendLine();
    }

    private void GenerateGetterProperty(StringBuilder sb, GetterSignature getter)
    {
        var propName = getter.PropertyName;
        var (openApiType, format, nullable) = MapReturnTypeToOpenApi(getter.ReturnType);

        sb.AppendLine($"        {propName}:");
        sb.AppendLine($"          type: {openApiType}");

        if (!string.IsNullOrEmpty(format))
            sb.AppendLine($"          format: {format}");

        if (nullable)
            sb.AppendLine("          nullable: true");
    }

    private (string Type, string? Format, bool Nullable) MapReturnTypeToOpenApi(string returnType)
    {
        var nullable = returnType.EndsWith("?");
        var baseType = returnType.TrimEnd('?');

        // Check for enum types (ends with "Type")
        if (baseType.EndsWith("Type", StringComparison.Ordinal))
        {
            return ("string", null, nullable);
        }

        return baseType.ToLowerInvariant() switch
        {
            "int" or "int32" => ("integer", "int32", nullable),
            "long" or "int64" => ("integer", "int64", nullable),
            "bool" or "boolean" => ("boolean", null, nullable),
            "string" => ("string", null, nullable),
            "float" or "single" => ("number", "float", nullable),
            "double" => ("number", "double", nullable),
            _ => ("string", null, nullable) // Default to string for unknown types
        };
    }

    private void GenerateSchemaFromDefinition(StringBuilder sb, string schemaName, SchemaDefinition schemaDef)
    {
        sb.AppendLine($"    {schemaName}:");
        sb.AppendLine($"      type: {schemaDef.Type ?? "object"}");

        if (schemaDef.Nullable)
            sb.AppendLine("      nullable: true");

        if (!string.IsNullOrEmpty(schemaDef.Description))
            sb.AppendLine($"      description: {schemaDef.Description}");

        if (schemaDef.Properties?.Count > 0)
        {
            sb.AppendLine("      properties:");
            foreach (var (propName, propDef) in schemaDef.Properties)
            {
                GeneratePropertyFromDefinition(sb, propName, propDef, 8);
            }
        }

        if (schemaDef.AdditionalProperties != null)
        {
            sb.AppendLine("      additionalProperties:");
            WritePropertyDefinition(sb, schemaDef.AdditionalProperties, 8);
        }

        if (schemaDef.Required?.Count > 0)
        {
            sb.AppendLine("      required:");
            foreach (var req in schemaDef.Required)
            {
                sb.AppendLine($"        - {req}");
            }
        }

        sb.AppendLine();
    }

    private void GeneratePropertyFromDefinition(StringBuilder sb, string propName, PropertyDefinition propDef, int indent)
    {
        var spaces = new string(' ', indent);
        sb.AppendLine($"{spaces}{propName}:");
        WritePropertyDefinition(sb, propDef, indent + 2);
    }

    private void WritePropertyDefinition(StringBuilder sb, PropertyDefinition propDef, int indent)
    {
        var spaces = new string(' ', indent);

        // Handle $ref
        if (!string.IsNullOrEmpty(propDef.Ref))
        {
            sb.AppendLine($"{spaces}$ref: '{propDef.Ref}'");
            if (propDef.Nullable)
                sb.AppendLine($"{spaces}nullable: true");
            return;
        }

        // Type
        if (!string.IsNullOrEmpty(propDef.Type))
            sb.AppendLine($"{spaces}type: {propDef.Type}");

        // Format
        if (!string.IsNullOrEmpty(propDef.Format))
            sb.AppendLine($"{spaces}format: {propDef.Format}");

        // Nullable
        if (propDef.Nullable)
            sb.AppendLine($"{spaces}nullable: true");

        // Description
        if (!string.IsNullOrEmpty(propDef.Description))
            sb.AppendLine($"{spaces}description: {propDef.Description}");

        // Enum
        if (propDef.Enum?.Count > 0)
        {
            sb.AppendLine($"{spaces}enum:");
            foreach (var val in propDef.Enum)
            {
                sb.AppendLine($"{spaces}  - {val}");
            }
        }

        // Items (for arrays)
        if (propDef.Items != null)
        {
            sb.AppendLine($"{spaces}items:");
            WritePropertyDefinition(sb, propDef.Items, indent + 2);
        }

        // AdditionalProperties (for objects/maps)
        if (propDef.AdditionalProperties != null)
        {
            sb.AppendLine($"{spaces}additionalProperties:");
            WritePropertyDefinition(sb, propDef.AdditionalProperties, indent + 2);
        }
    }

    #endregion

    #region Command Schema Generation

    private void GenerateCommandSchemas(StringBuilder sb, List<MethodSignature> methods)
    {
        var supportedMethods = methods.Where(m => !HasUnsupportedParameters(m)).ToList();

        // GameCommand schema
        sb.AppendLine("    GameCommand:");
        sb.AppendLine("      type: object");
        sb.AppendLine("      description: Command request");
        sb.AppendLine("      required:");
        sb.AppendLine("        - action");
        sb.AppendLine("      properties:");
        sb.AppendLine("        action:");
        sb.AppendLine("          type: string");
        sb.AppendLine("          description: The command action to execute");
        sb.AppendLine("          enum:");
        foreach (var method in supportedMethods.OrderBy(m => m.ActionName))
        {
            sb.AppendLine($"            - {method.ActionName}");
        }
        sb.AppendLine("        requestId:");
        sb.AppendLine("          type: string");
        sb.AppendLine("          description: Optional request ID for tracking");
        sb.AppendLine("        params:");
        sb.AppendLine("          type: object");
        sb.AppendLine("          description: Command-specific parameters");
        sb.AppendLine("          additionalProperties: true");
        sb.AppendLine();

        // CommandResult schema
        sb.AppendLine("    CommandResult:");
        sb.AppendLine("      type: object");
        sb.AppendLine("      description: Command execution result");
        sb.AppendLine("      properties:");
        sb.AppendLine("        success:");
        sb.AppendLine("          type: boolean");
        sb.AppendLine("          description: Whether the command succeeded");
        sb.AppendLine("        error:");
        sb.AppendLine("          type: string");
        sb.AppendLine("          description: Error message if command failed");
        sb.AppendLine("        requestId:");
        sb.AppendLine("          type: string");
        sb.AppendLine("          description: Echo of the request ID if provided");
        sb.AppendLine();

        // Individual command parameter schemas
        sb.AppendLine("    # Command Parameter Schemas");
        sb.AppendLine("    # These document the expected parameters for each command");
        sb.AppendLine();

        foreach (var method in supportedMethods.OrderBy(m => m.ActionName))
        {
            GenerateCommandSchema(sb, method);
        }
    }

    private bool HasUnsupportedParameters(MethodSignature method)
    {
        return method.Parameters.Any(p =>
            p.Kind == ParameterKind.Unknown ||
            p.Kind == ParameterKind.Collection ||
            _typeAnalyzer.IsUnsupportedType(p.Type));
    }

    private void GenerateCommandSchema(StringBuilder sb, MethodSignature method)
    {
        var schemaName = ToPascalCase(method.ActionName) + "Params";

        sb.AppendLine($"    {schemaName}:");
        sb.AppendLine("      type: object");

        var requiredParams = method.Parameters.Where(p => !p.IsOptional && !p.IsNullable).ToList();
        if (requiredParams.Any())
        {
            sb.AppendLine("      required:");
            foreach (var param in requiredParams)
            {
                sb.AppendLine($"        - {param.ApiName}");
            }
        }

        if (method.Parameters.Any())
        {
            sb.AppendLine("      properties:");
            foreach (var param in method.Parameters)
            {
                GenerateParamProperty(sb, param);
            }
        }

        sb.AppendLine();
    }

    private void GenerateParamProperty(StringBuilder sb, ParameterInfo param)
    {
        sb.AppendLine($"        {param.ApiName}:");

        var (openApiType, format) = GetOpenApiType(param);
        sb.AppendLine($"          type: {openApiType}");

        if (format != null)
            sb.AppendLine($"          format: {format}");

        var description = GetParamDescription(param);
        if (description != null)
            sb.AppendLine($"          description: \"{description}\"");

        sb.AppendLine($"          x-game-param: {param.Name}");

        if (param.IsOptional && param.DefaultValue != null)
        {
            var defaultVal = FormatDefaultValue(param.DefaultValue, openApiType);
            if (defaultVal != null)
                sb.AppendLine($"          default: {defaultVal}");
        }
    }

    private (string Type, string? Format) GetOpenApiType(ParameterInfo param)
    {
        var baseType = param.Type.TrimEnd('?');

        return param.Kind switch
        {
            ParameterKind.Entity => ("integer", "int32"),
            ParameterKind.EnumType => ("string", null),
            ParameterKind.Primitive => baseType.ToLowerInvariant() switch
            {
                "int" or "int32" => ("integer", "int32"),
                "long" or "int64" => ("integer", "int64"),
                "bool" or "boolean" => ("boolean", null),
                "string" => ("string", null),
                "float" or "single" => ("number", "float"),
                "double" => ("number", "double"),
                _ => ("string", null)
            },
            ParameterKind.Collection => ("array", null),
            _ => ("string", null)
        };
    }

    private string? GetParamDescription(ParameterInfo param)
    {
        var gameName = param.Name;

        if (gameName.StartsWith("p", StringComparison.Ordinal) && param.Kind == ParameterKind.Entity)
        {
            var entityType = param.Type.TrimEnd('?');
            return $"{entityType} ID (game param: {gameName})";
        }

        if (gameName.StartsWith("e", StringComparison.Ordinal) && param.Kind == ParameterKind.EnumType)
        {
            return $"{param.Type.TrimEnd('?')} value (e.g., {GetExampleEnumValue(param.Type)}) (game param: {gameName})";
        }

        if (gameName.StartsWith("b", StringComparison.Ordinal) && param.Type.TrimEnd('?').ToLowerInvariant() == "bool")
        {
            var readable = string.Concat(gameName.Skip(1).Select((c, i) =>
                i > 0 && char.IsUpper(c) ? " " + char.ToLower(c) : c.ToString()));
            return $"{readable.Trim()} (game param: {gameName})";
        }

        if (gameName.StartsWith("i", StringComparison.Ordinal) && param.Type.TrimEnd('?').ToLowerInvariant() == "int")
        {
            var readable = string.Concat(gameName.Skip(1).Select((c, i) =>
                i > 0 && char.IsUpper(c) ? " " + char.ToLower(c) : c.ToString()));
            return $"{readable.Trim()} (game param: {gameName})";
        }

        if (gameName.StartsWith("z", StringComparison.Ordinal) && param.Type.TrimEnd('?').ToLowerInvariant() == "string")
        {
            var readable = string.Concat(gameName.Skip(1).Select((c, i) =>
                i > 0 && char.IsUpper(c) ? " " + char.ToLower(c) : c.ToString()));
            return $"{readable.Trim()} (game param: {gameName})";
        }

        return null;
    }

    private string GetExampleEnumValue(string typeName)
    {
        var baseName = typeName.TrimEnd('?').Replace("Type", "").ToUpperInvariant();
        return $"{baseName}_EXAMPLE";
    }

    private string? FormatDefaultValue(string defaultValue, string openApiType)
    {
        return openApiType switch
        {
            "boolean" => defaultValue.ToLowerInvariant(),
            "integer" => defaultValue,
            "number" => defaultValue,
            "string" => defaultValue == "null" ? null : $"\"{defaultValue}\"",
            _ => null
        };
    }

    private static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return char.ToUpperInvariant(input[0]) + input[1..];
    }

    /// <summary>
    /// Generate operationId from path and HTTP method.
    /// Examples:
    ///   GET /state → getState
    ///   GET /players → getPlayers
    ///   GET /player/{index} → getPlayer
    ///   GET /player/{index}/units → getPlayerUnits
    ///   GET /tile/{x}/{y} → getTileByCoords
    /// </summary>
    private static string GenerateOperationId(string path, string method)
    {
        // Start with the HTTP method in lowercase
        var prefix = method.ToLowerInvariant();

        // Remove leading slash and split into segments
        var segments = path.TrimStart('/').Split('/');

        var parts = new List<string>();
        var hasPathParams = false;

        foreach (var segment in segments)
        {
            if (segment.StartsWith("{") && segment.EndsWith("}"))
            {
                // Path parameter - note it but don't add to name directly
                hasPathParams = true;
                var paramName = segment.Trim('{', '}');

                // Special case: coordinate params like {x}/{y} get combined
                if (paramName == "x" || paramName == "y")
                {
                    // Will be handled after loop
                    continue;
                }
            }
            else
            {
                // Regular segment - capitalize for camelCase
                parts.Add(ToPascalCase(segment));
            }
        }

        // Check for coordinate pattern: /tile/{x}/{y}
        if (path.Contains("{x}") && path.Contains("{y}"))
        {
            parts.Add("ByCoords");
        }

        // Build the operation ID
        var operationId = prefix + string.Join("", parts);

        return operationId;
    }

    #endregion
}

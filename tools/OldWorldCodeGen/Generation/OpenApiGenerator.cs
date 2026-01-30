using System.Text;
using OldWorldCodeGen.Parsing;

namespace OldWorldCodeGen.Generation;

/// <summary>
/// Generates openapi.yaml from parsed method signatures.
/// </summary>
public class OpenApiGenerator
{
    private readonly TypeAnalyzer _typeAnalyzer;

    public OpenApiGenerator(TypeAnalyzer typeAnalyzer)
    {
        _typeAnalyzer = typeAnalyzer;
    }

    public string Generate(List<MethodSignature> methods)
    {
        // Filter out methods with unsupported parameter types (same logic as CommandExecutorGenerator)
        var supportedMethods = methods.Where(m => !HasUnsupportedParameters(m)).ToList();

        var sb = new StringBuilder();

        // Header
        sb.AppendLine("# Auto-generated from Old World game source");
        sb.AppendLine($"# Generated at: {DateTime.UtcNow:O}");
        sb.AppendLine("# Do not edit manually - regenerate with: dotnet run --project tools/OldWorldCodeGen");
        sb.AppendLine();
        sb.AppendLine("openapi: \"3.0.3\"");
        sb.AppendLine();
        sb.AppendLine("info:");
        sb.AppendLine("  title: Old World API");
        sb.AppendLine("  description: |");
        sb.AppendLine("    REST API for controlling Old World game. This API is auto-generated");
        sb.AppendLine("    from the game's source code and exposes all available game commands.");
        sb.AppendLine("  version: \"2.3.0\"");
        sb.AppendLine("  contact:");
        sb.AppendLine("    name: Old World API Endpoint");
        sb.AppendLine("    url: https://github.com/becked/OldWorldAPIEndpoint");
        sb.AppendLine();

        // Servers
        sb.AppendLine("servers:");
        sb.AppendLine("  - url: http://localhost:9876");
        sb.AppendLine("    description: Local game instance");
        sb.AppendLine();

        // Paths
        sb.AppendLine("paths:");
        sb.AppendLine("  /command:");
        sb.AppendLine("    post:");
        sb.AppendLine("      summary: Execute a game command");
        sb.AppendLine("      description: |");
        sb.AppendLine("        Execute a game command. The action parameter determines which command");
        sb.AppendLine("        to run, and params contains the command-specific parameters.");
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
        sb.AppendLine();

        // Components
        sb.AppendLine("components:");
        sb.AppendLine("  schemas:");
        sb.AppendLine();

        // GameCommand schema
        sb.AppendLine("    GameCommand:");
        sb.AppendLine("      type: object");
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

        return sb.ToString();
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

        // Required params (use API names)
        var requiredParams = method.Parameters.Where(p => !p.IsOptional && !p.IsNullable).ToList();
        if (requiredParams.Any())
        {
            sb.AppendLine("      required:");
            foreach (var param in requiredParams)
            {
                sb.AppendLine($"        - {param.ApiName}");
            }
        }

        // Properties
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
        // Use API name for the property key
        sb.AppendLine($"        {param.ApiName}:");

        var (openApiType, format) = GetOpenApiType(param);
        sb.AppendLine($"          type: {openApiType}");

        if (format != null)
        {
            sb.AppendLine($"          format: {format}");
        }

        // Description based on type
        var description = GetParamDescription(param);
        if (description != null)
        {
            sb.AppendLine($"          description: \"{description}\"");
        }

        // Add x-game-param extension to document the original game parameter name
        sb.AppendLine($"          x-game-param: {param.Name}");

        // Default value
        if (param.IsOptional && param.DefaultValue != null)
        {
            var defaultVal = FormatDefaultValue(param.DefaultValue, openApiType);
            if (defaultVal != null)
            {
                sb.AppendLine($"          default: {defaultVal}");
            }
        }
    }

    private (string Type, string? Format) GetOpenApiType(ParameterInfo param)
    {
        var baseType = param.Type.TrimEnd('?');

        return param.Kind switch
        {
            ParameterKind.Entity => ("integer", "int32"), // Entity IDs are integers
            ParameterKind.EnumType => ("string", null),   // Enum types are passed as strings
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
        // Generate description based on parameter name patterns (using game name)
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
            // Convert bCamelCase to readable form
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

        // Non-Hungarian params - just return null (they're self-descriptive)
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
}

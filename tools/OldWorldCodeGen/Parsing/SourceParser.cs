using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace OldWorldCodeGen.Parsing;

/// <summary>
/// Parses C# source files using Roslyn to extract method signatures.
/// </summary>
public class SourceParser
{
    private readonly TypeAnalyzer _typeAnalyzer;

    public SourceParser(TypeAnalyzer typeAnalyzer)
    {
        _typeAnalyzer = typeAnalyzer;
    }

    /// <summary>
    /// Parse ClientManager.cs and extract all send* method signatures.
    /// </summary>
    public List<MethodSignature> ParseSendMethods(string filePath)
    {
        var code = File.ReadAllText(filePath);
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetCompilationUnitRoot();

        var methods = root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Where(m => m.Identifier.Text.StartsWith("send", StringComparison.OrdinalIgnoreCase))
            .Where(m => m.Modifiers.Any(SyntaxKind.PublicKeyword))
            .Select(ParseMethodDeclaration)
            .ToList();

        Console.WriteLine($"[SourceParser] Found {methods.Count} send* methods in {Path.GetFileName(filePath)}");
        return methods;
    }

    /// <summary>
    /// Parse an entity class (Player.cs, City.cs, etc.) and extract getter methods.
    /// </summary>
    public List<GetterSignature> ParseEntityGetters(string filePath)
    {
        var code = File.ReadAllText(filePath);
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetCompilationUnitRoot();

        var getters = root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Where(m => m.Modifiers.Any(SyntaxKind.PublicKeyword))
            .Where(m => IsGetterMethod(m.Identifier.Text))
            .Where(m => m.ReturnType.ToString() != "void")
            .Select(ParseGetterDeclaration)
            .ToList();

        Console.WriteLine($"[SourceParser] Found {getters.Count} getter methods in {Path.GetFileName(filePath)}");
        return getters;
    }

    private static bool IsGetterMethod(string name)
    {
        return name.StartsWith("get", StringComparison.Ordinal) ||
               name.StartsWith("is", StringComparison.Ordinal) ||
               name.StartsWith("has", StringComparison.Ordinal);
    }

    private MethodSignature ParseMethodDeclaration(MethodDeclarationSyntax method)
    {
        var parameters = method.ParameterList.Parameters
            .Select(ParseParameter)
            .ToList();

        return new MethodSignature
        {
            Name = method.Identifier.Text,
            ReturnType = method.ReturnType.ToString(),
            Parameters = parameters,
            IsVirtual = method.Modifiers.Any(SyntaxKind.VirtualKeyword)
        };
    }

    private GetterSignature ParseGetterDeclaration(MethodDeclarationSyntax method)
    {
        var parameters = method.ParameterList.Parameters;

        // Check for out/ref parameters which we can't handle in code generation
        bool hasOutOrRefParams = parameters.Any(p =>
            p.Modifiers.Any(SyntaxKind.OutKeyword) ||
            p.Modifiers.Any(SyntaxKind.RefKeyword));

        var paramTypes = parameters
            .Select(p => p.Type?.ToString() ?? "object")
            .ToList();

        var returnType = method.ReturnType.ToString();
        string? elementType = ExtractCollectionElementType(returnType);

        return new GetterSignature
        {
            Name = method.Identifier.Text,
            ReturnType = returnType,
            HasParameters = paramTypes.Count > 0,
            ParameterCount = paramTypes.Count,
            ParameterTypes = paramTypes,
            CollectionElementType = elementType,
            HasOutOrRefParams = hasOutOrRefParams
        };
    }

    /// <summary>
    /// Extract the element type from collection return types like ReadOnlyList&lt;T&gt;, List&lt;T&gt;, IEnumerable&lt;T&gt;.
    /// </summary>
    private static string? ExtractCollectionElementType(string returnType)
    {
        var match = Regex.Match(returnType, @"(?:ReadOnlyList|List|IEnumerable|IReadOnlyList)<(\w+)>");
        return match.Success ? match.Groups[1].Value : null;
    }

    private ParameterInfo ParseParameter(ParameterSyntax param)
    {
        var typeName = param.Type?.ToString() ?? "object";
        var isNullable = typeName.EndsWith("?") || param.Type is NullableTypeSyntax;
        var hasDefault = param.Default != null;

        var info = new ParameterInfo
        {
            Name = param.Identifier.Text,
            Type = typeName,
            Kind = _typeAnalyzer.CategorizeType(typeName),
            IsOptional = hasDefault,
            DefaultValue = param.Default?.Value.ToString(),
            IsNullable = isNullable
        };

        return info;
    }

    /// <summary>
    /// Parse a source file and return raw syntax tree for inspection.
    /// </summary>
    public SyntaxTree ParseFile(string filePath)
    {
        var code = File.ReadAllText(filePath);
        return CSharpSyntaxTree.ParseText(code);
    }
}

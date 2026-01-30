using System.Xml.Linq;
using CommandLine;
using OldWorldCodeGen.Parsing;
using OldWorldCodeGen.Generation;

namespace OldWorldCodeGen;

class Program
{
    public class Options
    {
        [Option('s', "source", Required = false, HelpText = "Path to game source directory (Reference/Source/Base)")]
        public string? SourcePath { get; set; }

        [Option('o', "output", Required = false, HelpText = "Output directory for generated files")]
        public string? OutputPath { get; set; }

        [Option("openapi", Required = false, HelpText = "Output path for openapi.yaml")]
        public string? OpenApiPath { get; set; }

        [Option('v', "verbose", Required = false, HelpText = "Verbose output")]
        public bool Verbose { get; set; }

        [Option("parse-only", Required = false, HelpText = "Only parse and output method list (no code generation)")]
        public bool ParseOnly { get; set; }
    }

    static int Main(string[] args)
    {
        return Parser.Default.ParseArguments<Options>(args)
            .MapResult(
                opts => Run(opts),
                errs => 1
            );
    }

    static int Run(Options opts)
    {
        // Resolve paths
        var sourcePath = opts.SourcePath ?? ResolveDefaultSourcePath();
        var outputPath = opts.OutputPath ?? Path.GetFullPath("../../Source");
        var openApiPath = opts.OpenApiPath ?? Path.GetFullPath("../../docs/openapi.yaml");

        if (sourcePath == null)
        {
            Console.Error.WriteLine("Error: Could not determine game source path.");
            Console.Error.WriteLine("Set OLDWORLD_PATH environment variable or use --source option.");
            return 1;
        }

        Console.WriteLine($"Source path: {sourcePath}");
        Console.WriteLine($"Output path: {outputPath}");
        Console.WriteLine($"OpenAPI path: {openApiPath}");
        Console.WriteLine();

        // Verify source exists
        var clientManagerPath = Path.Combine(sourcePath, "Game", "ClientCore", "ClientManager.cs");
        if (!File.Exists(clientManagerPath))
        {
            Console.Error.WriteLine($"Error: ClientManager.cs not found at {clientManagerPath}");
            return 1;
        }

        try
        {
            // Parse source files
            var typeAnalyzer = new TypeAnalyzer();
            var parser = new SourceParser(typeAnalyzer);

            Console.WriteLine("Parsing ClientManager.cs...");
            var sendMethods = parser.ParseSendMethods(clientManagerPath);

            if (opts.ParseOnly)
            {
                PrintMethodSummary(sendMethods, typeAnalyzer);
                return 0;
            }

            // Parse entity classes for data builders
            var entityGetters = new Dictionary<string, List<GetterSignature>>();
            var entityClasses = new[] { "Player", "City", "Unit", "Character", "Tile" };

            foreach (var entityClass in entityClasses)
            {
                var entityPath = Path.Combine(sourcePath, "Game", "GameCore", $"{entityClass}.cs");
                if (File.Exists(entityPath))
                {
                    Console.WriteLine($"Parsing {entityClass}.cs...");
                    entityGetters[entityClass] = parser.ParseEntityGetters(entityPath);
                }
                else
                {
                    Console.WriteLine($"Warning: {entityClass}.cs not found at {entityPath}");
                }
            }

            Console.WriteLine();

            // Generate code
            Console.WriteLine("Generating CommandExecutor.Generated.cs...");
            var cmdExecGen = new CommandExecutorGenerator(typeAnalyzer);
            var commandExecutorCode = cmdExecGen.Generate(sendMethods);

            var cmdExecPath = Path.Combine(outputPath, "CommandExecutor.Generated.cs");
            Directory.CreateDirectory(Path.GetDirectoryName(cmdExecPath)!);
            File.WriteAllText(cmdExecPath, commandExecutorCode);
            Console.WriteLine($"  Written to: {cmdExecPath}");

            // Load schema annotations
            var annotationsPath = Path.GetFullPath("schema-annotations.yaml");
            Console.WriteLine($"Loading schema annotations from: {annotationsPath}");
            var annotations = SchemaAnnotations.Load(annotationsPath);
            Console.WriteLine($"  Loaded {annotations.Endpoints.Count} endpoints, {annotations.NestedSchemas.Count} nested schemas");

            // Generate OpenAPI
            Console.WriteLine("Generating openapi.yaml...");
            var modInfoPath = Path.GetFullPath("../../ModInfo.xml");
            var version = ReadVersionFromModInfo(modInfoPath);
            Console.WriteLine($"  Using version from ModInfo.xml: {version}");
            var openApiGen = new OpenApiGenerator(typeAnalyzer);
            var openApiYaml = openApiGen.Generate(sendMethods, entityGetters, annotations, version);

            Directory.CreateDirectory(Path.GetDirectoryName(openApiPath)!);
            File.WriteAllText(openApiPath, openApiYaml);
            Console.WriteLine($"  Written to: {openApiPath}");

            // Generate data builders
            Console.WriteLine("Generating DataBuilders.Generated.cs...");
            var dataBuilderGen = new DataBuilderGenerator(typeAnalyzer);
            var dataBuilderCode = dataBuilderGen.Generate(entityGetters);

            var dataBuilderPath = Path.Combine(outputPath, "DataBuilders.Generated.cs");
            File.WriteAllText(dataBuilderPath, dataBuilderCode);
            Console.WriteLine($"  Written to: {dataBuilderPath}");

            Console.WriteLine();
            Console.WriteLine("Generation complete!");
            Console.WriteLine($"  Commands generated: {sendMethods.Count}");
            Console.WriteLine($"  Entity builders: {entityGetters.Count}");

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            if (opts.Verbose)
            {
                Console.Error.WriteLine(ex.StackTrace);
            }
            return 1;
        }
    }

    static string? ResolveDefaultSourcePath()
    {
        // Try OLDWORLD_PATH environment variable
        var oldWorldPath = Environment.GetEnvironmentVariable("OLDWORLD_PATH");
        if (!string.IsNullOrEmpty(oldWorldPath))
        {
            var sourcePath = Path.Combine(oldWorldPath, "Reference", "Source", "Base");
            if (Directory.Exists(sourcePath))
                return sourcePath;
        }

        // Try common macOS path
        var macPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Library/Application Support/Steam/steamapps/common/Old World/Reference/Source/Base"
        );
        if (Directory.Exists(macPath))
            return macPath;

        return null;
    }

    static string ReadVersionFromModInfo(string modInfoPath)
    {
        if (!File.Exists(modInfoPath))
        {
            throw new FileNotFoundException($"ModInfo.xml not found at {modInfoPath}");
        }

        var doc = XDocument.Load(modInfoPath);
        var version = doc.Root?.Element("modversion")?.Value;

        if (string.IsNullOrEmpty(version))
        {
            throw new InvalidOperationException("Could not read modversion from ModInfo.xml");
        }

        return version;
    }

    static void PrintMethodSummary(List<MethodSignature> methods, TypeAnalyzer typeAnalyzer)
    {
        Console.WriteLine($"\nFound {methods.Count} send* methods:\n");

        foreach (var method in methods.OrderBy(m => m.Name))
        {
            Console.WriteLine($"  {method.SignatureComment}");
            foreach (var param in method.Parameters)
            {
                var kindStr = param.Kind switch
                {
                    ParameterKind.Entity => "[Entity]",
                    ParameterKind.EnumType => "[Enum]",
                    ParameterKind.Primitive => "[Prim]",
                    ParameterKind.Collection => "[Coll]",
                    _ => "[?]"
                };
                var optStr = param.IsOptional ? $" = {param.DefaultValue}" : "";
                Console.WriteLine($"    {kindStr,-8} {param.Type,-20} {param.Name}{optStr}");
            }
            Console.WriteLine();
        }

        // Summary of enum types
        var enumTypes = typeAnalyzer.GetEnumTypesNeedingResolvers().ToList();
        Console.WriteLine($"Enum types needing resolvers: {enumTypes.Count}");
        foreach (var enumType in enumTypes.OrderBy(e => e.TypeName))
        {
            Console.WriteLine($"  {enumType.TypeName} -> infos.{enumType.InfosMethod}(), infos.{enumType.CountMethod}()");
        }

        var simpleEnums = typeAnalyzer.GetSimpleEnumTypes().ToList();
        if (simpleEnums.Any())
        {
            Console.WriteLine($"\nSimple enum types (Enum.TryParse): {simpleEnums.Count}");
            foreach (var enumType in simpleEnums.OrderBy(e => e))
            {
                Console.WriteLine($"  {enumType}");
            }
        }
    }
}

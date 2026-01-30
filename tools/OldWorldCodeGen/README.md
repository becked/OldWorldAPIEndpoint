# OldWorldCodeGen

Roslyn-based code generator that parses decompiled Old World game source and generates:

- `Source/CommandExecutor.Generated.cs` - Command implementations for all 209 game commands
- `Source/DataBuilders.Generated.cs` - Entity builders for Player, City, Unit, Character, Tile
- `docs/openapi.yaml` - OpenAPI 3.0 specification

## Prerequisites

Game source must be available at one of:
- `$OLDWORLD_PATH/Reference/Source/Base/` (set via environment variable or `.env`)
- `~/Library/Application Support/Steam/steamapps/common/Old World/Reference/Source/Base/` (macOS default)

## Usage

```bash
# From repository root
dotnet run --project tools/OldWorldCodeGen

# With custom paths
dotnet run --project tools/OldWorldCodeGen -- --source /path/to/Source/Base --output /path/to/output

# Parse only (no code generation, just list methods)
dotnet run --project tools/OldWorldCodeGen -- --parse-only
```

## Options

| Option | Description |
|--------|-------------|
| `-s, --source` | Path to game source directory (Reference/Source/Base) |
| `-o, --output` | Output directory for generated C# files |
| `--openapi` | Output path for openapi.yaml |
| `-v, --verbose` | Verbose error output |
| `--parse-only` | Only parse and list methods, no code generation |

## How It Works

1. Parses `ClientManager.cs` to find all `send*` methods (game commands)
2. Categorizes parameters as Entity, EnumType, or Primitive
3. Parses entity classes (Player, City, Unit, Character, Tile) for getter methods
4. Generates type resolvers for enum types using Infos lookups
5. Outputs generated code and OpenAPI spec

The OpenAPI version is read from `ModInfo.xml` at the repository root.

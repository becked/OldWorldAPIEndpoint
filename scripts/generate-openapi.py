#!/usr/bin/env python3
"""
Generate typed OpenAPI schemas from commands.yaml.

Reads command definitions from commands.yaml and generates typed OpenAPI
schemas using oneOf + discriminator pattern. This enables OpenAPI codegen
tools like progenitor (Rust) to generate fully typed client code.

Usage:
    python3 scripts/generate-openapi.py

Output:
    Updates docs/openapi.yaml with typed command schemas.

Dependencies:
    pip install pyyaml
"""

from __future__ import annotations

import re
import sys
from pathlib import Path
from typing import Any


def load_yaml(path: Path) -> dict[str, Any]:
    """Load a YAML file. Uses simple parsing to avoid PyYAML dependency issues."""
    try:
        import yaml
        with open(path, encoding="utf-8") as f:
            return yaml.safe_load(f)
    except ImportError:
        print("Error: PyYAML is required. Install with: pip install pyyaml")
        sys.exit(1)


def save_yaml(path: Path, data: dict[str, Any]) -> None:
    """Save data to a YAML file with proper formatting."""
    import yaml

    class CustomDumper(yaml.SafeDumper):
        """Custom YAML dumper that handles formatting better."""
        pass

    # Preserve order and use block style
    def represent_dict(dumper: yaml.SafeDumper, data: dict) -> yaml.Node:
        return dumper.represent_mapping("tag:yaml.org,2002:map", data.items())

    def represent_str(dumper: yaml.SafeDumper, data: str) -> yaml.Node:
        # Use literal block style for multiline strings
        if "\n" in data:
            return dumper.represent_scalar("tag:yaml.org,2002:str", data, style="|")
        return dumper.represent_scalar("tag:yaml.org,2002:str", data)

    CustomDumper.add_representer(dict, represent_dict)
    CustomDumper.add_representer(str, represent_str)

    with open(path, "w", encoding="utf-8") as f:
        yaml.dump(data, f, Dumper=CustomDumper, default_flow_style=False,
                  allow_unicode=True, sort_keys=False, width=120)


def to_pascal_case(name: str) -> str:
    """Convert camelCase to PascalCase (e.g., moveUnit -> MoveUnit)."""
    return name[0].upper() + name[1:] if name else name


def generate_param_schema(param_name: str, param_def: dict[str, Any]) -> dict[str, Any]:
    """Generate OpenAPI schema for a single parameter."""
    schema: dict[str, Any] = {}

    param_type = param_def.get("type", "string")
    schema["type"] = param_type

    if "description" in param_def:
        schema["description"] = param_def["description"]

    if "default" in param_def:
        schema["default"] = param_def["default"]

    return schema


def generate_params_schema(cmd_name: str, params: dict[str, Any]) -> dict[str, Any] | None:
    """Generate OpenAPI schema for command parameters."""
    if not params:
        return None

    schema: dict[str, Any] = {
        "type": "object",
        "properties": {}
    }

    required_params = []
    for param_name, param_def in params.items():
        schema["properties"][param_name] = generate_param_schema(param_name, param_def)
        if param_def.get("required", False):
            required_params.append(param_name)

    if required_params:
        schema["required"] = required_params

    return schema


def generate_command_schema(
    cmd_name: str,
    cmd_def: dict[str, Any],
    params_schema_name: str | None
) -> dict[str, Any]:
    """Generate OpenAPI schema for a single command."""
    schema: dict[str, Any] = {
        "type": "object",
        "description": cmd_def.get("description", f"Execute {cmd_name} command"),
        "required": ["action"],
        "properties": {
            "action": {
                "type": "string",
                "const": cmd_name
            },
            "requestId": {
                "type": "string",
                "description": "Optional client-provided ID for response correlation"
            }
        }
    }

    if params_schema_name:
        schema["required"].append("params")
        schema["properties"]["params"] = {
            "$ref": f"#/components/schemas/{params_schema_name}"
        }

    return schema


def collect_all_commands(commands_data: dict[str, Any]) -> list[tuple[str, dict[str, Any], str]]:
    """
    Collect all commands from categories.
    Returns list of (command_name, command_def, category_name).
    """
    commands = []
    categories = commands_data.get("categories", {})

    for cat_name, cat_def in categories.items():
        cat_commands = cat_def.get("commands", {})
        for cmd_name, cmd_def in cat_commands.items():
            commands.append((cmd_name, cmd_def, cat_name))

    return commands


def generate_schemas(commands_data: dict[str, Any]) -> dict[str, dict[str, Any]]:
    """
    Generate all OpenAPI schemas from commands.yaml data.
    Returns dict of schema_name -> schema_definition.
    """
    schemas: dict[str, Any] = {}
    one_of_refs: list[dict[str, str]] = []
    discriminator_mapping: dict[str, str] = {}

    all_commands = collect_all_commands(commands_data)

    for cmd_name, cmd_def, _cat_name in all_commands:
        pascal_name = to_pascal_case(cmd_name)
        command_schema_name = f"{pascal_name}Command"
        params_schema_name = f"{pascal_name}Params"

        # Generate params schema if command has params
        params = cmd_def.get("params", {})
        params_schema = generate_params_schema(cmd_name, params)

        if params_schema:
            schemas[params_schema_name] = params_schema
        else:
            params_schema_name = None

        # Generate command schema
        command_schema = generate_command_schema(cmd_name, cmd_def, params_schema_name)
        schemas[command_schema_name] = command_schema

        # Add to oneOf and discriminator
        one_of_refs.append({"$ref": f"#/components/schemas/{command_schema_name}"})
        discriminator_mapping[cmd_name] = f"#/components/schemas/{command_schema_name}"

        # Handle aliases
        aliases = cmd_def.get("aliases", [])
        for alias in aliases:
            discriminator_mapping[alias] = f"#/components/schemas/{command_schema_name}"

    # Generate main GameCommand schema with oneOf + discriminator
    schemas["GameCommand"] = {
        "oneOf": one_of_refs,
        "discriminator": {
            "propertyName": "action",
            "mapping": discriminator_mapping
        }
    }

    return schemas


def update_openapi_spec(
    spec: dict[str, Any],
    generated_schemas: dict[str, dict[str, Any]]
) -> dict[str, Any]:
    """
    Update OpenAPI spec with generated schemas.
    Preserves existing non-command schemas.
    """
    components = spec.setdefault("components", {})
    existing_schemas = components.get("schemas", {})

    # Identify schemas to preserve (non-command schemas)
    command_related_patterns = [
        r"^[A-Z][a-z]+Command$",  # FooCommand
        r"^[A-Z][a-z]+Params$",   # FooParams
        r"^GameCommand$"
    ]

    preserved_schemas = {}
    for name, schema in existing_schemas.items():
        is_command_schema = any(re.match(p, name) for p in command_related_patterns)
        if not is_command_schema:
            preserved_schemas[name] = schema

    # Merge: preserved schemas first, then generated schemas
    new_schemas = {**preserved_schemas, **generated_schemas}
    components["schemas"] = new_schemas

    return spec


def main() -> int:
    """Main entry point."""
    script_dir = Path(__file__).parent
    project_root = script_dir.parent
    commands_path = project_root / "commands.yaml"
    openapi_path = project_root / "docs" / "openapi.yaml"

    # Validate paths
    if not commands_path.exists():
        print(f"Error: commands.yaml not found at {commands_path}")
        return 1

    if not openapi_path.exists():
        print(f"Error: openapi.yaml not found at {openapi_path}")
        return 1

    print(f"Loading commands from {commands_path}")
    commands_data = load_yaml(commands_path)

    print(f"Loading OpenAPI spec from {openapi_path}")
    openapi_spec = load_yaml(openapi_path)

    # Generate schemas
    all_commands = collect_all_commands(commands_data)
    print(f"Found {len(all_commands)} commands in {len(commands_data.get('categories', {}))} categories")

    generated_schemas = generate_schemas(commands_data)
    command_schemas = sum(1 for name in generated_schemas if name.endswith("Command"))
    params_schemas = sum(1 for name in generated_schemas if name.endswith("Params"))
    print(f"Generated {command_schemas} command schemas and {params_schemas} params schemas")

    # Update spec
    updated_spec = update_openapi_spec(openapi_spec, generated_schemas)

    # Save
    print(f"Writing updated spec to {openapi_path}")
    save_yaml(openapi_path, updated_spec)

    print("Done!")
    return 0


if __name__ == "__main__":
    sys.exit(main())

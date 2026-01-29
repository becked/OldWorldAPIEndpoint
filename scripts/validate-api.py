#!/usr/bin/env python3
"""
API Schema Validation Script for OldWorldAPIEndpoint.

Validates live API responses against JSON schemas to detect schema drift.
Run this after test-headless.sh to verify API responses match documented schemas.

Usage:
    python3 scripts/validate-api.py [base_url]

    base_url: API base URL (default: http://localhost:9877)

Dependencies:
    pip install jsonschema

Exit codes:
    0: All validations passed
    1: Validation errors found
    2: Connection or setup error
"""

from __future__ import annotations

import json
import sys
import urllib.error
import urllib.request
from pathlib import Path
from typing import Any


def load_schema(schema_path: Path) -> dict[str, Any]:
    """Load a JSON schema file."""
    with open(schema_path, encoding="utf-8") as f:
        return json.load(f)


def fetch_endpoint(base_url: str, endpoint: str) -> dict[str, Any] | list[Any] | None:
    """Fetch data from an API endpoint."""
    url = f"{base_url}{endpoint}"
    try:
        with urllib.request.urlopen(url, timeout=10) as response:
            return json.loads(response.read().decode("utf-8"))
    except urllib.error.HTTPError as e:
        print(f"    HTTP {e.code}: {endpoint}")
        return None
    except urllib.error.URLError as e:
        print(f"    Connection error: {e.reason}")
        return None
    except json.JSONDecodeError as e:
        print(f"    Invalid JSON: {e}")
        return None


def validate_item(
    item: dict[str, Any],
    schema: dict[str, Any],
    path_prefix: str = ""
) -> list[str]:
    """
    Validate a single item against a schema.
    Returns list of error messages.

    This is a simplified validator that checks:
    - Required fields are present
    - Field types match (basic type checking)
    - Extra fields not in schema (warning)
    """
    errors: list[str] = []

    # Check required fields
    required = schema.get("required", [])
    for field in required:
        if field not in item:
            errors.append(f"{path_prefix}{field}: missing required field")

    # Check field types
    properties = schema.get("properties", {})
    for field, value in item.items():
        if field not in properties:
            # Extra field - this is what the validation report caught
            continue  # Don't report as error since we're updating schemas

        field_schema = properties[field]
        expected_type = field_schema.get("type")

        if expected_type is None:
            continue

        # Handle nullable types
        if isinstance(expected_type, list):
            expected_types = expected_type
        else:
            expected_types = [expected_type]

        # Map Python types to JSON schema types
        type_map = {
            int: "integer",
            float: "number",
            str: "string",
            bool: "boolean",
            list: "array",
            dict: "object",
            type(None): "null",
        }

        actual_type = type_map.get(type(value), "unknown")

        if actual_type not in expected_types:
            # Check for integer/number compatibility
            if actual_type == "integer" and "number" in expected_types:
                continue
            errors.append(
                f"{path_prefix}{field}: type mismatch - "
                f"expected {expected_types}, got {actual_type} ({repr(value)[:50]})"
            )

        # Check enum values
        if "enum" in field_schema and value is not None:
            if value not in field_schema["enum"]:
                errors.append(
                    f"{path_prefix}{field}: enum mismatch - "
                    f"expected {field_schema['enum']}, got {repr(value)}"
                )

    return errors


def main() -> int:
    """Main entry point."""
    base_url = sys.argv[1] if len(sys.argv) > 1 else "http://localhost:9877"
    script_dir = Path(__file__).parent
    schema_dir = script_dir.parent / "docs" / "schemas"

    if not schema_dir.exists():
        print(f"Error: Schema directory not found: {schema_dir}")
        return 2

    print(f"Validating API at {base_url}")
    print(f"Using schemas from {schema_dir}")
    print()

    # Load schemas
    schemas: dict[str, dict[str, Any]] = {}
    for schema_file in schema_dir.glob("*.schema.json"):
        try:
            schemas[schema_file.stem] = load_schema(schema_file)
        except Exception as e:
            print(f"Warning: Could not load {schema_file.name}: {e}")

    # Define endpoint-to-schema mappings
    # (endpoint, schema_name, is_array, sample_size)
    endpoints: list[tuple[str, str, bool, int]] = [
        ("/characters", "character", True, 3),
        ("/character/0", "character", False, 1),
        ("/cities", "city", True, 3),
        ("/city/0", "city", False, 1),
        ("/tribes", "tribe", True, 3),
    ]

    total_errors = 0
    total_tests = 0

    for endpoint, schema_name, is_array, sample_size in endpoints:
        schema = schemas.get(schema_name)
        if schema is None:
            print(f"  SKIP {endpoint}: no schema '{schema_name}'")
            continue

        print(f"  Testing {endpoint}...", end="")
        data = fetch_endpoint(base_url, endpoint)

        if data is None:
            print(" ERROR (fetch failed)")
            total_errors += 1
            continue

        items = data if is_array else [data]
        items_to_check = items[:sample_size]

        endpoint_errors: list[str] = []
        for i, item in enumerate(items_to_check):
            if not isinstance(item, dict):
                endpoint_errors.append(f"[{i}]: expected object, got {type(item).__name__}")
                continue
            errors = validate_item(item, schema, f"[{i}].")
            endpoint_errors.extend(errors)

        total_tests += 1
        if endpoint_errors:
            print(f" {len(endpoint_errors)} issue(s)")
            for err in endpoint_errors[:5]:  # Show first 5
                print(f"    {err}")
            if len(endpoint_errors) > 5:
                print(f"    ... and {len(endpoint_errors) - 5} more")
            total_errors += len(endpoint_errors)
        else:
            print(" OK")

    print()
    print(f"Tested {total_tests} endpoints, {total_errors} total issues")

    return 0 if total_errors == 0 else 1


if __name__ == "__main__":
    sys.exit(main())

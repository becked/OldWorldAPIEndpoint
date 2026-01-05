#!/bin/bash
# deploy.sh - Build and deploy the mod to Old World mods folder

set -e

# Load environment configuration
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
if [ -f "$SCRIPT_DIR/.env" ]; then
    source "$SCRIPT_DIR/.env"
else
    echo "Error: .env file not found"
    echo "Copy .env.example to .env and configure paths for your system"
    exit 1
fi

# Validate required variables
if [ -z "$OLDWORLD_PATH" ] || [ "$OLDWORLD_PATH" = "/path/to/Steam/steamapps/common/Old World" ]; then
    echo "Error: OLDWORLD_PATH not configured in .env"
    exit 1
fi

MOD_DIR="$OLDWORLD_MODS_PATH/OldWorldAPIEndpoint"

echo "=== Building OldWorldAPIEndpoint ==="
export OldWorldPath="$OLDWORLD_PATH"
dotnet build -c Release

echo ""
echo "=== Deploying to Old World ==="
mkdir -p "$MOD_DIR"
cp ModInfo.xml "$MOD_DIR/"
cp bin/OldWorldAPIEndpoint.dll "$MOD_DIR/"

# Copy Newtonsoft.Json.dll (required dependency)
NEWTONSOFT_DLL=$(find ~/.nuget/packages/newtonsoft.json/13.0.3 -name "Newtonsoft.Json.dll" -path "*net45*" | head -1)
if [ -n "$NEWTONSOFT_DLL" ]; then
    cp "$NEWTONSOFT_DLL" "$MOD_DIR/"
    echo "Copied Newtonsoft.Json.dll"
else
    echo "WARNING: Newtonsoft.Json.dll not found!"
fi

echo ""
echo "=== Deployed successfully ==="
ls -la "$MOD_DIR"

echo ""
echo "Next steps:"
echo "1. Launch Old World"
echo "2. Enable 'Old World API Endpoint' in Mod Manager"
echo "3. Start or load a game"
echo "4. In terminal: nc localhost 9876"
echo "5. End a turn and watch for JSON output"

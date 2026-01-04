#!/bin/bash
# deploy.sh - Build and deploy the mod to Old World mods folder

set -e

MOD_DIR="$HOME/Library/Application Support/OldWorld/Mods/OldWorldAPIEndpoint"

echo "=== Building OldWorldAPIEndpoint ==="
# Path to Old World installation (note: app is OldWorld.app not "Old World.app")
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

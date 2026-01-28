#!/bin/bash
# workshop-upload.sh - Build and upload mod to Steam Workshop via SteamCMD
#
# Prerequisites:
#   1. Install SteamCMD: brew install steamcmd
#   2. Have Steam Guard ready (you'll need to authenticate)
#
# First upload:  ./workshop-upload.sh
# Updates:       ./workshop-upload.sh <publishedfileid>

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$SCRIPT_DIR"

# Load .env for build
if [ -f ".env" ]; then
    source ".env"
else
    echo "Error: .env file not found (needed for build)"
    exit 1
fi

# Build first
echo "=== Building mod ==="
export OldWorldPath="$OLDWORLD_PATH"
dotnet build -c Release

# Prepare workshop content folder
echo ""
echo "=== Preparing workshop content ==="
rm -rf workshop_content
mkdir -p workshop_content

cp ModInfo.xml workshop_content/
cp bin/OldWorldAPIEndpoint.dll workshop_content/

# Copy Newtonsoft.Json.dll
NEWTONSOFT_DLL=$(find ~/.nuget/packages/newtonsoft.json/13.0.3 -name "Newtonsoft.Json.dll" -path "*net45*" | head -1)
if [ -n "$NEWTONSOFT_DLL" ]; then
    cp "$NEWTONSOFT_DLL" workshop_content/
    echo "Copied Newtonsoft.Json.dll"
else
    echo "WARNING: Newtonsoft.Json.dll not found!"
fi

echo "Content prepared:"
ls -la workshop_content/

# Handle publishedfileid for updates (command line overrides .env)
PUBLISHED_ID="${1:-$STEAM_WORKSHOP_ID}"

# Create temp VDF with absolute paths (SteamCMD needs them)
echo ""
echo "=== Generating upload VDF ==="
sed -e "s|\"contentfolder\".*|\"contentfolder\" \"$SCRIPT_DIR/workshop_content\"|" \
    -e "s|\"previewfile\".*|\"previewfile\" \"$SCRIPT_DIR/logo.png\"|" \
    -e "s/\"publishedfileid\" *\"[^\"]*\"/\"publishedfileid\" \"$PUBLISHED_ID\"/" \
    workshop.vdf > workshop_upload.vdf
VDF_FILE="workshop_upload.vdf"

if [ -n "$PUBLISHED_ID" ]; then
    echo "Updating existing item: $PUBLISHED_ID"
else
    echo "Creating new workshop item"
fi

echo ""
echo "=== Uploading to Steam Workshop ==="

# Get Steam username
if [ -n "$STEAM_USERNAME" ]; then
    USERNAME="$STEAM_USERNAME"
else
    read -p "Steam username: " USERNAME
fi

echo "Logging in as: $USERNAME"
echo "(You may be prompted for password and Steam Guard code)"
echo ""

# Run SteamCMD
steamcmd +login "$USERNAME" +workshop_build_item "$SCRIPT_DIR/$VDF_FILE" +quit

# Cleanup temp file (comment out to debug VDF issues)
rm -f workshop_upload.vdf

echo ""
echo "=== Upload complete ==="
echo ""
echo "If this was a new upload, note the 'publishedfileid' from the output above."
echo "Save it for future updates: ./workshop-upload.sh <publishedfileid>"

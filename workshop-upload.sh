#!/bin/bash
# workshop-upload.sh - Build and upload mod to Steam Workshop via SteamCMD
#
# Prerequisites:
#   1. Install SteamCMD: brew install steamcmd
#   2. Have Steam Guard ready (you'll need to authenticate)
#
# Usage: ./workshop-upload.sh [changelog]
# Examples:
#   ./workshop-upload.sh                    # Upload with changelog from CHANGELOG.md
#   ./workshop-upload.sh "Fixed bug X"      # Upload with custom changelog message
#
# Version is always read from ModInfo.xml. Use ./bump-version.sh to change it.

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

# Read version from ModInfo.xml (single source of truth)
VERSION=$(sed -n 's/.*<modversion>\([^<]*\)<\/modversion>.*/\1/p' ModInfo.xml)
if [ -z "$VERSION" ]; then
    echo "Error: Could not extract version from ModInfo.xml"
    exit 1
fi
echo "Version: $VERSION"

# Changelog: use argument if provided, otherwise extract from CHANGELOG.md
CHANGELOG="${1:-}"
if [ -z "$CHANGELOG" ] && [ -f "CHANGELOG.md" ]; then
    # Extract changelog for current version from CHANGELOG.md
    CHANGELOG=$(awk -v ver="$VERSION" '
        /^## \[/ {
            if (found) exit
            if ($0 ~ "\\[" ver "\\]") { found=1; next }
        }
        found && /^## \[/ { exit }
        found { print }
    ' CHANGELOG.md | sed '/^$/d' | head -20)
fi

# Format changenote with version prefix (use actual newlines, awk will escape them)
if [ -n "$CHANGELOG" ]; then
    CHANGENOTE="v$VERSION

$CHANGELOG"
else
    CHANGENOTE="v$VERSION"
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

# Get publishedfileid from .env (required for updates)
PUBLISHED_ID="$STEAM_WORKSHOP_ID"

# Create temp VDF with absolute paths (SteamCMD needs them)
echo ""
echo "=== Generating upload VDF ==="

# Sanitize changenote for VDF format:
# - Replace double quotes with single quotes (escaped quotes break VDF parser)
ESCAPED_CHANGENOTE=$(printf '%s' "$CHANGENOTE" | sed "s/\"/'/g")

# Build VDF by processing line by line, inserting changenote with actual newlines
{
    while IFS= read -r line; do
        case "$line" in
            *'"contentfolder"'*)
                printf '\t"contentfolder"\t\t"%s"\n' "$SCRIPT_DIR/workshop_content"
                ;;
            *'"previewfile"'*)
                printf '\t"previewfile"\t\t"%s"\n' "$SCRIPT_DIR/logo.png"
                ;;
            *'"publishedfileid"'*)
                printf '\t"publishedfileid"\t\t"%s"\n' "$PUBLISHED_ID"
                ;;
            *'"changenote"'*)
                printf '\t"changenote"\t\t"%s"\n' "$ESCAPED_CHANGENOTE"
                ;;
            *)
                printf '%s\n' "$line"
                ;;
        esac
    done < workshop.vdf
} > workshop_upload.vdf
VDF_FILE="workshop_upload.vdf"

echo "Changenote: $(echo "$CHANGENOTE" | head -1)..."

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
echo "Add it to your .env file as STEAM_WORKSHOP_ID for future updates."

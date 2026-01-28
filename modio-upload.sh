#!/bin/bash
# modio-upload.sh - Build and upload mod to mod.io
#
# Prerequisites:
#   1. Get an OAuth2 access token from https://mod.io/me/access (read+write)
#   2. Add MODIO_ACCESS_TOKEN to your .env file
#
# Usage: ./modio-upload.sh [changelog]
# Examples:
#   ./modio-upload.sh                    # Upload with version from ModInfo.xml, changelog from CHANGELOG.md
#   ./modio-upload.sh "Fixed bug X"      # Upload with custom changelog message
#
# Version is always read from ModInfo.xml. Use ./bump-version.sh to change it.

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$SCRIPT_DIR"

# Load .env
if [ -f ".env" ]; then
    source ".env"
else
    echo "Error: .env file not found"
    exit 1
fi

# Check required variables
if [ -z "$MODIO_ACCESS_TOKEN" ]; then
    echo "Error: MODIO_ACCESS_TOKEN not set in .env"
    echo "Get one from https://mod.io/me/access (OAuth 2 section, read+write)"
    exit 1
fi

if [ -z "$MODIO_GAME_ID" ] || [ -z "$MODIO_MOD_ID" ]; then
    echo "Error: MODIO_GAME_ID and MODIO_MOD_ID must be set in .env"
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
    # Finds section starting with ## [VERSION] and captures until next ## or end
    CHANGELOG=$(awk -v ver="$VERSION" '
        /^## \[/ {
            if (found) exit
            if ($0 ~ "\\[" ver "\\]") { found=1; next }
        }
        found && /^## \[/ { exit }
        found { print }
    ' CHANGELOG.md | sed '/^$/d' | head -20)
fi

# Mod metadata (matches Steam Workshop)
MOD_NAME="Old World API Endpoint"
MOD_SUMMARY="Exposes game state via TCP and HTTP APIs, enabling companion apps, overlays, and external tools to read live game information."
MOD_HOMEPAGE="https://becked.github.io/OldWorldAPIEndpoint"

# Build first
echo "=== Building mod ==="
export OldWorldPath="$OLDWORLD_PATH"
dotnet build -c Release

# Step 1: Update mod profile text fields (PUT with x-www-form-urlencoded)
echo ""
echo "=== Updating mod profile (text fields) ==="

# Read description from file and URL-encode it
if [ -f "mod-description.html" ]; then
    DESCRIPTION=$(cat mod-description.html)
    echo "Including description from mod-description.html"
else
    echo "Warning: mod-description.html not found, skipping description"
    DESCRIPTION=""
fi

# Build the form data
FORM_DATA="name=$(printf '%s' "$MOD_NAME" | jq -sRr @uri)"
FORM_DATA+="&summary=$(printf '%s' "$MOD_SUMMARY" | jq -sRr @uri)"
FORM_DATA+="&homepage_url=$(printf '%s' "$MOD_HOMEPAGE" | jq -sRr @uri)"
if [ -n "$DESCRIPTION" ]; then
    FORM_DATA+="&description=$(printf '%s' "$DESCRIPTION" | jq -sRr @uri)"
fi

RESPONSE=$(curl -s -w "\n%{http_code}" -X PUT \
    "https://api.mod.io/v1/games/$MODIO_GAME_ID/mods/$MODIO_MOD_ID" \
    -H "Authorization: Bearer $MODIO_ACCESS_TOKEN" \
    -H "Content-Type: application/x-www-form-urlencoded" \
    -H "Accept: application/json" \
    -d "$FORM_DATA")

HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
BODY=$(echo "$RESPONSE" | sed '$d')

if [ "$HTTP_CODE" = "200" ]; then
    echo "Profile text fields updated successfully"
else
    echo "Warning: Profile update failed (HTTP $HTTP_CODE)"
    echo "$BODY" | python3 -m json.tool 2>/dev/null || echo "$BODY"
fi

# Step 2: Upload logo via media endpoint (POST with multipart/form-data)
if [ -f "logo.png" ]; then
    echo ""
    echo "=== Uploading logo ==="

    RESPONSE=$(curl -s -w "\n%{http_code}" -X POST \
        "https://api.mod.io/v1/games/$MODIO_GAME_ID/mods/$MODIO_MOD_ID/media" \
        -H "Authorization: Bearer $MODIO_ACCESS_TOKEN" \
        -H "Accept: application/json" \
        -F "logo=@logo.png")

    HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
    BODY=$(echo "$RESPONSE" | sed '$d')

    if [ "$HTTP_CODE" = "201" ]; then
        echo "Logo uploaded successfully"
    else
        echo "Warning: Logo upload failed (HTTP $HTTP_CODE)"
        echo "$BODY" | python3 -m json.tool 2>/dev/null || echo "$BODY"
    fi
else
    echo "Warning: logo.png not found, skipping logo upload"
fi

# Step 3: Prepare and upload modfile
echo ""
echo "=== Preparing upload package ==="
rm -rf modio_content modio_upload.zip
mkdir -p modio_content

cp ModInfo.xml modio_content/
cp bin/OldWorldAPIEndpoint.dll modio_content/

# Copy Newtonsoft.Json.dll
NEWTONSOFT_DLL=$(find ~/.nuget/packages/newtonsoft.json/13.0.3 -name "Newtonsoft.Json.dll" -path "*net45*" | head -1)
if [ -n "$NEWTONSOFT_DLL" ]; then
    cp "$NEWTONSOFT_DLL" modio_content/
    echo "Copied Newtonsoft.Json.dll"
else
    echo "WARNING: Newtonsoft.Json.dll not found!"
fi

echo "Content prepared:"
ls -la modio_content/

# Create zip file
echo ""
echo "=== Creating zip file ==="
cd modio_content
zip -r ../modio_upload.zip .
cd ..
echo "Created modio_upload.zip ($(du -h modio_upload.zip | cut -f1))"

# Upload modfile
echo ""
echo "=== Uploading modfile to mod.io ==="
echo "Game ID: $MODIO_GAME_ID"
echo "Mod ID: $MODIO_MOD_ID"

CURL_ARGS=(
    -X POST
    "https://api.mod.io/v1/games/$MODIO_GAME_ID/mods/$MODIO_MOD_ID/files"
    -H "Authorization: Bearer $MODIO_ACCESS_TOKEN"
    -H "Accept: application/json"
    -F "filedata=@modio_upload.zip"
)

if [ -n "$VERSION" ]; then
    echo "Version: $VERSION"
    CURL_ARGS+=(-F "version=$VERSION")
fi

if [ -n "$CHANGELOG" ]; then
    echo "Changelog: $CHANGELOG"
    CURL_ARGS+=(-F "changelog=$CHANGELOG")
fi

echo ""

# Execute upload
RESPONSE=$(curl -s -w "\n%{http_code}" "${CURL_ARGS[@]}")
HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
BODY=$(echo "$RESPONSE" | sed '$d')

if [ "$HTTP_CODE" = "201" ]; then
    echo "Modfile upload successful!"
    echo ""
    echo "Response:"
    echo "$BODY" | python3 -m json.tool 2>/dev/null || echo "$BODY"
else
    echo "Modfile upload failed (HTTP $HTTP_CODE)"
    echo ""
    echo "Response:"
    echo "$BODY" | python3 -m json.tool 2>/dev/null || echo "$BODY"
    exit 1
fi

# Cleanup
rm -rf modio_content modio_upload.zip

echo ""
echo "=== Upload complete ==="
echo "View your mod: https://mod.io/g/oldworld/m/old-world-api-endpoint1"

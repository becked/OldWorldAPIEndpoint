#!/bin/bash
# bump-version.sh - Bump the mod version in ModInfo.xml
#
# Usage:
#   ./bump-version.sh patch    # 0.0.2 -> 0.0.3
#   ./bump-version.sh minor    # 0.0.2 -> 0.1.0
#   ./bump-version.sh major    # 0.0.2 -> 1.0.0
#   ./bump-version.sh 1.2.3    # Set explicit version

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$SCRIPT_DIR"

MODINFO="ModInfo.xml"

if [ ! -f "$MODINFO" ]; then
    echo "Error: $MODINFO not found"
    exit 1
fi

# Extract current version (portable - works on macOS and Linux)
CURRENT=$(sed -n 's/.*<modversion>\([^<]*\)<\/modversion>.*/\1/p' "$MODINFO")
if [ -z "$CURRENT" ]; then
    echo "Error: Could not extract version from $MODINFO"
    exit 1
fi

echo "Current version: $CURRENT"

# Parse version components
IFS='.' read -r MAJOR MINOR PATCH <<< "$CURRENT"

# Determine new version
case "${1:-}" in
    major)
        MAJOR=$((MAJOR + 1))
        MINOR=0
        PATCH=0
        ;;
    minor)
        MINOR=$((MINOR + 1))
        PATCH=0
        ;;
    patch)
        PATCH=$((PATCH + 1))
        ;;
    "")
        echo "Usage: $0 <major|minor|patch|X.Y.Z>"
        echo ""
        echo "Examples:"
        echo "  $0 patch    # $CURRENT -> $MAJOR.$MINOR.$((PATCH + 1))"
        echo "  $0 minor    # $CURRENT -> $MAJOR.$((MINOR + 1)).0"
        echo "  $0 major    # $CURRENT -> $((MAJOR + 1)).0.0"
        echo "  $0 1.2.3    # Set explicit version"
        exit 1
        ;;
    *)
        # Explicit version provided
        if [[ "$1" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
            IFS='.' read -r MAJOR MINOR PATCH <<< "$1"
        else
            echo "Error: Invalid version format '$1'. Expected X.Y.Z"
            exit 1
        fi
        ;;
esac

NEW_VERSION="$MAJOR.$MINOR.$PATCH"
echo "New version: $NEW_VERSION"

# Update ModInfo.xml (sed -i '' for macOS compatibility)
if [[ "$OSTYPE" == "darwin"* ]]; then
    sed -i '' "s|<modversion>$CURRENT</modversion>|<modversion>$NEW_VERSION</modversion>|" "$MODINFO"
else
    sed -i "s|<modversion>$CURRENT</modversion>|<modversion>$NEW_VERSION</modversion>|" "$MODINFO"
fi

# Also update workshop_content/ModInfo.xml if it exists
if [ -f "workshop_content/ModInfo.xml" ]; then
    if [[ "$OSTYPE" == "darwin"* ]]; then
        sed -i '' "s|<modversion>$CURRENT</modversion>|<modversion>$NEW_VERSION</modversion>|" "workshop_content/ModInfo.xml"
    else
        sed -i "s|<modversion>$CURRENT</modversion>|<modversion>$NEW_VERSION</modversion>|" "workshop_content/ModInfo.xml"
    fi
fi

echo ""
echo "Updated $MODINFO: $CURRENT -> $NEW_VERSION"

# Remind about changelog
if [ -f "CHANGELOG.md" ]; then
    echo ""
    echo "Don't forget to update CHANGELOG.md with changes for $NEW_VERSION"
else
    echo ""
    echo "Consider creating CHANGELOG.md to track changes"
fi

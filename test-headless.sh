#!/bin/bash
# test-headless.sh - Automated headless test for OldWorldAPIEndpoint
# Captures TCP JSON output from the mod during headless game runs

set -e

# Configuration
SAVE_FILE="${1:-/tmp/APITestSave.zip}"
TURNS="${2:-5}"
PORT=9876
TCP_OUTPUT="/tmp/api_tcp_output.txt"
GAME_LOG="/tmp/api_game_log.txt"
OLD_WORLD_APP="$OLDWORLD_PATH/OldWorld.app/Contents/MacOS/OldWorld"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m'

echo -e "${CYAN}╔════════════════════════════════════════════╗${NC}"
echo -e "${CYAN}║   OldWorldAPIEndpoint Headless Test        ║${NC}"
echo -e "${CYAN}╚════════════════════════════════════════════╝${NC}"
echo ""

# Check save file
if [ ! -f "$SAVE_FILE" ]; then
    echo -e "${RED}Error: Save file not found: $SAVE_FILE${NC}"
    echo "Usage: $0 [save_file] [turns]"
    exit 1
fi

echo "Save file: $SAVE_FILE"
echo "Turns: $TURNS"
echo ""

# Build and deploy
echo -e "${YELLOW}[1/4] Building and deploying mod...${NC}"
cd "$(dirname "$0")"
./deploy.sh > /dev/null 2>&1
echo -e "${GREEN}      Done${NC}"

# Clean up any previous processes
pkill -f "nc.*$PORT" 2>/dev/null || true
sleep 1

# Clear output files
> "$TCP_OUTPUT"
> "$GAME_LOG"

# Start TCP client with retry loop BEFORE the game
# This ensures we're ready to receive when the server comes up
echo -e "${YELLOW}[2/4] Starting TCP client (retry loop)...${NC}"
(
    connected=0
    for i in {1..120}; do
        if nc localhost $PORT >> "$TCP_OUTPUT" 2>/dev/null; then
            connected=1
            break
        fi
        sleep 0.25
    done
    if [ $connected -eq 0 ]; then
        echo "TCP client: failed to connect after 30s" >> "$TCP_OUTPUT"
    fi
) &
CLIENT_PID=$!
echo -e "${GREEN}      Client PID: $CLIENT_PID${NC}"

# Run the game
echo -e "${YELLOW}[3/4] Running Old World headless (${TURNS} turns)...${NC}"
arch -x86_64 "$OLD_WORLD_APP" "$SAVE_FILE" -batchmode -headless -autorunturns "$TURNS" > "$GAME_LOG" 2>&1 &
GAME_PID=$!
echo -e "${GREEN}      Game PID: $GAME_PID${NC}"

# Wait for game with progress indicator
echo -n "      Waiting: "
while kill -0 $GAME_PID 2>/dev/null; do
    echo -n "."
    sleep 2
done
echo " done"

# Give TCP a moment to flush
sleep 2

# Kill the TCP client loop
kill $CLIENT_PID 2>/dev/null || true
wait $CLIENT_PID 2>/dev/null || true

# Results
echo -e "${YELLOW}[4/4] Results${NC}"
echo ""

# Show game log excerpts
echo -e "${CYAN}── Game Log (APIEndpoint entries) ──${NC}"
grep -E "\[APIEndpoint\]" "$GAME_LOG" 2>/dev/null || echo "(none)"
echo ""

# Show TCP output
echo -e "${CYAN}── TCP Output ──${NC}"
if [ -s "$TCP_OUTPUT" ]; then
    LINE_COUNT=$(wc -l < "$TCP_OUTPUT" | tr -d ' ')
    echo -e "${GREEN}Captured $LINE_COUNT JSON message(s):${NC}"
    echo ""

    # Pretty print each JSON line
    while IFS= read -r line; do
        if [ -n "$line" ]; then
            echo "$line" | python3 -m json.tool 2>/dev/null || echo "$line"
            echo ""
        fi
    done < "$TCP_OUTPUT"
else
    echo -e "${RED}No TCP output captured!${NC}"
    echo ""
    echo "Troubleshooting:"
    echo "  - Check mod is enabled in Old World Mod Manager"
    echo "  - Check game log: grep APIEndpoint $GAME_LOG"
    exit 1
fi

echo -e "${CYAN}── Files ──${NC}"
echo "TCP output: $TCP_OUTPUT"
echo "Game log:   $GAME_LOG"
echo ""
echo -e "${GREEN}Test complete!${NC}"

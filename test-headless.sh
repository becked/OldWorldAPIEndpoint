#!/bin/bash
# test-headless.sh - Automated headless test for OldWorldAPIEndpoint
# Captures TCP JSON output from the mod during headless game runs

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

# Configuration
SAVE_FILE="${1:-/tmp/APITestSave.zip}"
TURNS="${2:-5}"
TCP_PORT=9876
HTTP_PORT=9877
TCP_OUTPUT="/tmp/api_tcp_output.txt"
HTTP_OUTPUT="/tmp/api_http_output.txt"
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
echo -e "${YELLOW}[1/5] Building and deploying mod...${NC}"
cd "$(dirname "$0")"
./deploy.sh > /dev/null 2>&1
echo -e "${GREEN}      Done${NC}"

# Clean up any previous processes
pkill -f "nc.*$TCP_PORT" 2>/dev/null || true
sleep 1

# Clear output files
> "$TCP_OUTPUT"
> "$HTTP_OUTPUT"
> "$GAME_LOG"

# Start TCP client with retry loop BEFORE the game
# This ensures we're ready to receive when the server comes up
echo -e "${YELLOW}[2/5] Starting TCP client (retry loop)...${NC}"
(
    connected=0
    for i in {1..120}; do
        if nc localhost $TCP_PORT >> "$TCP_OUTPUT" 2>/dev/null; then
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
echo -e "${YELLOW}[3/5] Running Old World headless (${TURNS} turns)...${NC}"
arch -x86_64 "$OLD_WORLD_APP" "$SAVE_FILE" -batchmode -headless -autorunturns "$TURNS" > "$GAME_LOG" 2>&1 &
GAME_PID=$!
echo -e "${GREEN}      Game PID: $GAME_PID${NC}"

# Test HTTP endpoints WHILE game is running
echo -e "${YELLOW}[4/5] Testing HTTP endpoints (while game runs)...${NC}"

# Helper function to test an endpoint
test_endpoint() {
    local endpoint=$1
    local name=$2

    echo -n "  $name... "
    HTTP_STATUS=$(curl -s -o /tmp/http_response.json -w "%{http_code}" "http://localhost:$HTTP_PORT$endpoint" 2>/dev/null)

    if [ "$HTTP_STATUS" -eq 200 ]; then
        if python3 -m json.tool /tmp/http_response.json > /dev/null 2>&1; then
            echo -e "${GREEN}OK${NC} (200, valid JSON)"
            echo "=== $endpoint ===" >> "$HTTP_OUTPUT"
            python3 -m json.tool /tmp/http_response.json >> "$HTTP_OUTPUT" 2>/dev/null
            echo "" >> "$HTTP_OUTPUT"
            return 0
        else
            echo -e "${RED}FAIL${NC} (200, invalid JSON)"
            return 1
        fi
    elif [ "$HTTP_STATUS" -eq 503 ]; then
        echo -e "${YELLOW}SKIP${NC} (503 - game not available)"
        return 1
    else
        echo -e "${RED}FAIL${NC} (HTTP $HTTP_STATUS)"
        return 1
    fi
}

# Use TCP as sync signal - wait for first TCP data before testing HTTP
# TCP data means game is loaded and running
echo -n "  Waiting for game (via TCP sync): "
HTTP_READY=0
for i in {1..120}; do
    # Check if game process is still running
    if ! kill -0 $GAME_PID 2>/dev/null; then
        echo -e " ${YELLOW}game exited${NC}"
        break
    fi
    # Check if TCP has received data (means game is loaded)
    if [ -s "$TCP_OUTPUT" ]; then
        echo -e "${GREEN}TCP data received - game ready${NC}"
        HTTP_READY=1
        break
    fi
    echo -n "."
    sleep 0.25
done

if [ $HTTP_READY -eq 1 ]; then
    echo ""
    echo "  Testing collection endpoints:"
    test_endpoint "/state" "Full state"
    test_endpoint "/players" "Players"
    test_endpoint "/cities" "Cities"
    test_endpoint "/characters" "Characters"
    test_endpoint "/tribes" "Tribes"
    test_endpoint "/team-diplomacy" "Team diplomacy"
    test_endpoint "/team-alliances" "Team alliances"
    test_endpoint "/tribe-diplomacy" "Tribe diplomacy"
    test_endpoint "/tribe-alliances" "Tribe alliances"

    echo ""
    echo "  Testing single-entity endpoints:"
    test_endpoint "/player/0" "Player 0"

    # Get a city ID from /cities response
    CITY_ID=$(curl -s "http://localhost:$HTTP_PORT/cities" 2>/dev/null | python3 -c "import sys,json; cities=json.load(sys.stdin); print(cities[0]['id'] if cities else '')" 2>/dev/null)
    if [ -n "$CITY_ID" ]; then
        test_endpoint "/city/$CITY_ID" "City $CITY_ID"
    fi

    # Get a character ID from /characters response
    CHAR_ID=$(curl -s "http://localhost:$HTTP_PORT/characters" 2>/dev/null | python3 -c "import sys,json; chars=json.load(sys.stdin); print(chars[0]['id'] if chars else '')" 2>/dev/null)
    if [ -n "$CHAR_ID" ]; then
        test_endpoint "/character/$CHAR_ID" "Character $CHAR_ID"
    fi

    echo ""
    echo "  Testing error cases:"
    echo -n "  Invalid city ID... "
    HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" "http://localhost:$HTTP_PORT/city/999999" 2>/dev/null)
    if [ "$HTTP_STATUS" -eq 404 ]; then
        echo -e "${GREEN}OK${NC} (404 as expected)"
    else
        echo -e "${RED}FAIL${NC} (got $HTTP_STATUS, expected 404)"
    fi

    echo -n "  Unknown endpoint... "
    HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" "http://localhost:$HTTP_PORT/nonexistent" 2>/dev/null)
    if [ "$HTTP_STATUS" -eq 404 ]; then
        echo -e "${GREEN}OK${NC} (404 as expected)"
    else
        echo -e "${RED}FAIL${NC} (got $HTTP_STATUS, expected 404)"
    fi
else
    echo -e " ${RED}timeout${NC}"
    echo -e "  ${RED}HTTP server not available - skipping HTTP tests${NC}"
fi

echo ""

# Wait for game to finish
echo -n "      Waiting for game to finish: "
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
echo -e "${YELLOW}[5/5] Results${NC}"
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
echo "TCP output:  $TCP_OUTPUT"
echo "HTTP output: $HTTP_OUTPUT"
echo "Game log:    $GAME_LOG"
echo ""
echo -e "${GREEN}Test complete!${NC}"

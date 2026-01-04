#!/bin/bash
# Watch the headless test log file in real-time
# Run this while playing Old World to see which hooks fire

LOG_FILE="$HOME/oldworld_headless_test.log"

echo "=== Watching Old World hook log ==="
echo "Log file: $LOG_FILE"
echo ""
echo "Launch Old World with the test mod enabled."
echo "Load a game and play to see which hooks fire."
echo ""
echo "Press Ctrl+C to stop watching."
echo ""

# Create file if doesn't exist
touch "$LOG_FILE"

# Watch with highlighting for important hooks
tail -f "$LOG_FILE" | while read line; do
    if echo "$line" | grep -q "OnNewTurnServer"; then
        echo -e "\033[1;32m$line\033[0m"  # Green
    elif echo "$line" | grep -q "OnGameServerReady\|OnGameClientReady"; then
        echo -e "\033[1;33m$line\033[0m"  # Yellow
    elif echo "$line" | grep -q "ERROR\|WARNING"; then
        echo -e "\033[1;31m$line\033[0m"  # Red
    elif echo "$line" | grep -q "Initialize\|Shutdown"; then
        echo -e "\033[1;36m$line\033[0m"  # Cyan
    else
        echo "$line"
    fi
done

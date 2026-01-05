#!/bin/bash
# Test script for Old World headless mode hook behavior
#
# This script launches Old World with various command-line options and monitors
# which mod hooks are called. The test mod logs all hooks to ~/oldworld_headless_test.log

set -e

# Load environment configuration
SCRIPT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
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

OLD_WORLD_APP="$OLDWORLD_PATH/OldWorld.app/Contents/MacOS/OldWorld"
LOG_FILE="$HOME/oldworld_headless_test.log"

echo "=== Old World Headless Mode Test ==="
echo ""
echo "Log file: $LOG_FILE"
echo ""

# Clear previous log
rm -f "$LOG_FILE"

# Function to show log tail
show_log() {
    echo ""
    echo "=== Log contents ==="
    if [ -f "$LOG_FILE" ]; then
        cat "$LOG_FILE"
    else
        echo "(no log file created)"
    fi
    echo "==================="
}

# Function to run test with timeout
run_test() {
    local test_name="$1"
    shift
    local args="$@"

    echo ""
    echo ">>> Test: $test_name"
    echo ">>> Args: $args"
    echo ""

    rm -f "$LOG_FILE"

    # Run with timeout (30 seconds)
    # Note: Unity games may require specific handling
    timeout 30 "$OLD_WORLD_APP" $args 2>&1 || true

    show_log
}

echo "Available command line test options:"
echo ""
echo "1) Normal GUI launch (control - requires manual quit)"
echo "2) Unity -batchmode (Unity's headless mode)"
echo "3) Unity -batchmode -nographics"
echo "4) Unity -quit (quit immediately after init)"
echo "5) Watch log file only (launch game manually)"
echo ""
read -p "Select test (1-5): " choice

case $choice in
    1)
        echo ""
        echo "Launching Old World normally..."
        echo ">>> Start a game and play a turn, then quit"
        echo ">>> Log will appear at: $LOG_FILE"
        echo ""
        "$OLD_WORLD_APP" &
        PID=$!
        echo "Game PID: $PID"
        echo "Press Enter when done testing (will kill game)..."
        read
        kill $PID 2>/dev/null || true
        show_log
        ;;
    2)
        run_test "Unity batchmode" -batchmode
        ;;
    3)
        run_test "Unity batchmode + nographics" -batchmode -nographics
        ;;
    4)
        run_test "Unity quit" -quit
        ;;
    5)
        echo ""
        echo "Watching log file. Launch the game manually."
        echo "Press Ctrl+C to stop watching."
        echo ""
        touch "$LOG_FILE"
        tail -f "$LOG_FILE"
        ;;
    *)
        echo "Invalid choice"
        exit 1
        ;;
esac

echo ""
echo "Test complete."

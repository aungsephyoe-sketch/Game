#!/bin/bash
# Prints the errors from the last time the game ran (send a screenshot of this to Claude).
LOG=$(ls -t ~/Library/Logs/*/"Blade Legends"/Player.log ~/Library/Logs/*/"Hashira Chronicles"/Player.log 2>/dev/null | head -1)
if [ -z "$LOG" ]; then echo "   (no game log found yet)"; exit 0; fi
ERR=$(grep -E "Exception|Error|error|\[Cutscene|\[UI\]|\[Battle\]|\[CharacterVisual\]|\[Mission|\[GameManager\]" "$LOG" | grep -v "^UnityEngine\.\|Fallback handler\|Loading GUID" | head -25)
if [ -z "$ERR" ]; then echo "   No errors in the game log."; else echo "   Errors from the game log ($LOG):"; echo "$ERR"; fi

#!/bin/bash
# Unlocks every slayer in YOUR save on THIS Mac only (other players are not affected).
# It drops a small "unlock_all" file next to your save; the game sees it at launch and adds every slayer.
# Undo:  bash ~/Game/tools/unlock_all.sh off
SAVE=$(find "$HOME/Library/Application Support" -maxdepth 3 -name "hashira_save.json" 2>/dev/null | grep -i "blade legends" | head -1)
if [ -z "$SAVE" ]; then
  SAVE=$(find "$HOME/Library/Application Support" -maxdepth 3 -name "hashira_save.json" 2>/dev/null | head -1)
fi
if [ -z "$SAVE" ]; then
  echo "!! No save found yet. Launch the game once (bash ~/Game/run.sh), quit it, then run this again."
  exit 1
fi
DIR=$(dirname "$SAVE")
if [ "$1" = "off" ]; then
  rm -f "$DIR/unlock_all"
  echo "Unlock file removed (slayers already unlocked stay unlocked)."
else
  touch "$DIR/unlock_all"
  echo "Done. Every slayer will be unlocked the next time you open the game."
  echo "   ($DIR/unlock_all)"
fi

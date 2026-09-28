#!/bin/bash
# Builds the latest version of the game and launches it (macOS).
# Usage:  bash ~/Game/run.sh
cd "$(dirname "$0")"
UNITY="/Applications/Unity/Hub/Editor/6000.6.3f1/Unity.app/Contents/MacOS/Unity"
if [ ! -x "$UNITY" ]; then
  UNITY=$(ls -d /Applications/Unity/Hub/Editor/*/Unity.app/Contents/MacOS/Unity 2>/dev/null | tail -1)
fi

echo "== 1/4 Getting the latest code"
git pull --ff-only || { echo "!! git pull failed (see above)."; exit 1; }
git log --oneline -1

echo "   Updating art files..."
bash tools/fetch_art.sh

echo "== 2/4 Checking Unity"
if [ -z "$UNITY" ] || [ ! -x "$UNITY" ]; then echo "!! Unity editor not found in /Applications/Unity/Hub/Editor"; exit 1; fi
if pgrep -f "Unity.app/Contents/MacOS/Unity" | grep -v $$ >/dev/null && pgrep -fl "Unity.app/Contents/MacOS/Unity" | grep -q -- "-projectpath\|-projectPath"; then
  echo "!! The Unity editor is open. Quit it (Cmd+Q), then run this script again."; exit 1
fi
if ! pgrep -x "Unity Hub" >/dev/null; then
  echo "   Starting Unity Hub (needed for your Unity license)..."
  open -a "Unity Hub"; sleep 20
fi

echo "== 3/4 Building (takes a few minutes, Terminal will look idle)"
rm -rf Builds/Desktop
"$UNITY" -batchmode -quit -projectPath "$PWD" \
  -executeMethod HashiraChronicles.EditorTools.BuildTools.BuildDesktop -logFile "$PWD/build.log"

echo "== 4/4 Result"
if [ -d Builds/Desktop/BladeLegends.app ]; then
  echo "   Build OK - launching."
  open Builds/Desktop/BladeLegends.app
  echo "   Checking the game log in 25 seconds (keep playing)..."
  sleep 25
  bash tools/show_errors.sh
else
  echo "!! BUILD FAILED. Screenshot everything below and send it to Claude:"
  echo "------------------------------------------------------------------"
  grep -E "error CS|Licens|compiler errors|Build Failed|BuildFailed|Error building|\[Hashira|Exception" build.log | grep -v "0.1 kb" | head -40
  echo "------------------------------------------------------------------"
fi

#!/bin/bash
# Downloads any external art listed in tools/art_manifest.txt, and removes art from earlier art directions.
cd "$(dirname "$0")/.."
# The earlier realistic Higgsfield art and 3D models are no longer part of the game's style.
for old in Assets/Resources/Art Assets/Art/Reference Assets/Resources/Characters/ren.glb Assets/Resources/Enemies/boss_veyrath.glb; do
  if [ -e "$old" ]; then rm -rf "$old" "$old.meta"; echo "   Removed old art: $old"; fi
done
ok=0; fail=0
while read -r dest url; do
  [ -z "$dest" ] && continue
  case "$dest" in \#*) continue;; esac
  out="Assets/$dest"
  [ -s "$out" ] && continue
  mkdir -p "$(dirname "$out")"
  if curl -sSfL --retry 2 -o "$out.part" "$url"; then mv "$out.part" "$out"; ok=$((ok+1)); else rm -f "$out.part"; fail=$((fail+1)); fi
done < tools/art_manifest.txt
[ $ok -gt 0 ] || [ $fail -gt 0 ] && echo "   Art: $ok downloaded, $fail failed."
exit 0

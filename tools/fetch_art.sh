#!/bin/bash
# Downloads the Higgsfield-generated art listed in tools/art_manifest.txt into Assets/.
# Safe to re-run: files that already exist are skipped.
cd "$(dirname "$0")/.."
ok=0; skip=0; fail=0
while read -r dest url; do
  [ -z "$dest" ] && continue
  case "$dest" in \#*) continue;; esac
  out="Assets/$dest"
  if [ -s "$out" ]; then skip=$((skip+1)); continue; fi
  mkdir -p "$(dirname "$out")"
  if curl -sSfL --retry 2 -o "$out.part" "$url"; then mv "$out.part" "$out"; ok=$((ok+1)); else rm -f "$out.part"; echo "   !! could not download $dest"; fail=$((fail+1)); fi
done < tools/art_manifest.txt
echo "   Art: $ok downloaded, $skip already present, $fail failed."

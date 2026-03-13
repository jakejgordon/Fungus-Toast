#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SOURCE_DIR="$SCRIPT_DIR/bin/Debug/netstandard2.1"
TARGET_DIR="$SCRIPT_DIR/../FungusToast.Unity/Assets/Plugins"
TOUCH_FILE="$SCRIPT_DIR/../FungusToast.Unity/Assets/Scripts/Unity/ForceRecompile.cs"

mkdir -p "$TARGET_DIR"

cp -f "$SOURCE_DIR/FungusToast.Core.dll" "$TARGET_DIR/"
if [[ -f "$SOURCE_DIR/FungusToast.Core.pdb" ]]; then
  cp -f "$SOURCE_DIR/FungusToast.Core.pdb" "$TARGET_DIR/"
fi
if [[ -f "$SOURCE_DIR/FungusToast.Core.xml" ]]; then
  cp -f "$SOURCE_DIR/FungusToast.Core.xml" "$TARGET_DIR/"
fi

printf '// Touched on: %s\n' "$(date -Iseconds)" > "$TOUCH_FILE"

echo "Copied FungusToast.Core artifacts to $TARGET_DIR"
echo "Touched $TOUCH_FILE"

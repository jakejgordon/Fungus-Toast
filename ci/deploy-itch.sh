#!/usr/bin/env bash
set -euo pipefail

echo "Starting itch.io deployment..."
echo "BUILD_PATH=${BUILD_PATH:-}"
echo "BUILD_NUMBER=${BUILD_NUMBER:-}"

if [ -z "${BUILD_PATH:-}" ]; then
  echo "Error: BUILD_PATH is not set. Point it at the macOS build artifact or zip to upload."
  exit 1
fi

if [ ! -e "$BUILD_PATH" ]; then
  echo "Error: BUILD_PATH does not exist: $BUILD_PATH"
  exit 1
fi

if [ -z "${BUTLER_API_KEY:-}" ]; then
  echo "Error: BUTLER_API_KEY is not set."
  exit 1
fi

if [ -z "${ITCH_TARGET:-}" ]; then
  echo "Error: ITCH_TARGET is not set. Example: jakejgordon2/fungus-toast:macos-stable"
  exit 1
fi

BUTLER_DIR="$HOME/butler"
mkdir -p "$BUTLER_DIR"

platform_arch="$(uname -m)"
case "$platform_arch" in
  arm64|aarch64)
    butler_archive="darwin-arm64"
    ;;
  x86_64|amd64)
    butler_archive="darwin-amd64"
    ;;
  *)
    echo "Error: Unsupported macOS architecture: $platform_arch"
    exit 1
    ;;
esac

butler_url="https://broth.itch.ovh/butler/${butler_archive}/LATEST/archive/default"

echo "Downloading butler from $butler_url..."
curl -L --fail -o "$BUTLER_DIR/butler.zip" "$butler_url"
unzip -o "$BUTLER_DIR/butler.zip" -d "$BUTLER_DIR"
chmod +x "$BUTLER_DIR/butler"

echo "Butler version:"
"$BUTLER_DIR/butler" -V

echo "Pushing build to itch.io target: $ITCH_TARGET"
"$BUTLER_DIR/butler" push "$BUILD_PATH" "$ITCH_TARGET" --userversion "${BUILD_NUMBER:-manual}"

echo "Deployment complete."
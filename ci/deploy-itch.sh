#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
VERSION_FILE="$REPO_ROOT/version.txt"
LAST_DEPLOYED_VERSION_FILE="$REPO_ROOT/last-deployed-version.txt"

read_version_file() {
  local path="$1"
  local description="$2"
  local allow_missing_or_empty="${3:-false}"

  if [ ! -f "$path" ]; then
    if [ "$allow_missing_or_empty" = "true" ]; then
      printf '%s' ""
      return 0
    fi

    echo "Error: Unable to find $description file at $path"
    exit 1
  fi

  local value=""
  IFS= read -r value < "$path" || true
  value="$(printf '%s' "$value" | tr -d '\r' | sed 's/^[[:space:]]*//;s/[[:space:]]*$//')"

  if [ -z "$value" ]; then
    if [ "$allow_missing_or_empty" = "true" ]; then
      printf '%s' ""
      return 0
    fi

    echo "Error: The $description file at $path is empty"
    exit 1
  fi

  if [[ ! "$value" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
    echo "Error: The $description file at $path must contain a semantic version in Major.Minor.BugFix format on the first line"
    exit 1
  fi

  printf '%s' "$value"
}

version_is_newer_than() {
  local current_version="$1"
  local previous_version="$2"
  local current_major current_minor current_patch
  local previous_major previous_minor previous_patch

  IFS=. read -r current_major current_minor current_patch <<< "$current_version"
  IFS=. read -r previous_major previous_minor previous_patch <<< "$previous_version"

  if (( current_major > previous_major )); then
    return 0
  fi

  if (( current_major < previous_major )); then
    return 1
  fi

  if (( current_minor > previous_minor )); then
    return 0
  fi

  if (( current_minor < previous_minor )); then
    return 1
  fi

  if (( current_patch > previous_patch )); then
    return 0
  fi

  return 1
}

RELEASE_VERSION="$(read_version_file "$VERSION_FILE" "current release version")"
LAST_DEPLOYED_VERSION="$(read_version_file "$LAST_DEPLOYED_VERSION_FILE" "last deployed version" true)"

echo "Starting itch.io deployment..."
echo "BUILD_PATH=${BUILD_PATH:-}"
echo "BUILD_NUMBER=${BUILD_NUMBER:-}"
echo "RELEASE_VERSION=$RELEASE_VERSION"

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

if [ -n "$LAST_DEPLOYED_VERSION" ] && ! version_is_newer_than "$RELEASE_VERSION" "$LAST_DEPLOYED_VERSION"; then
  echo "Error: Release version $RELEASE_VERSION is not newer than the last deployed version $LAST_DEPLOYED_VERSION. Update version.txt before publishing to itch.io."
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
"$BUTLER_DIR/butler" push "$BUILD_PATH" "$ITCH_TARGET" --userversion "$RELEASE_VERSION"

echo "Deployment complete."
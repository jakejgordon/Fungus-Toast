#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
UNITY_PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
REPO_ROOT="$(cd "$UNITY_PROJECT_ROOT/.." && pwd)"
VERSION_FILE="$UNITY_PROJECT_ROOT/version.txt"
LAST_DEPLOYED_VERSION_FILE="$UNITY_PROJECT_ROOT/last-deployed-version.txt"

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

version_is_older_than() {
	local current_version="$1"
	local minimum_version="$2"
	local current_major current_minor current_patch
	local minimum_major minimum_minor minimum_patch

	IFS=. read -r current_major current_minor current_patch <<< "$current_version"
	IFS=. read -r minimum_major minimum_minor minimum_patch <<< "$minimum_version"

	if (( current_major < minimum_major )); then
		return 0
	fi

	if (( current_major > minimum_major )); then
		return 1
	fi

	if (( current_minor < minimum_minor )); then
		return 0
	fi

	if (( current_minor > minimum_minor )); then
		return 1
	fi

	if (( current_patch < minimum_patch )); then
		return 0
	fi

	return 1
}

resolve_butler_path() {
	if [ -n "${BUTLER_PATH:-}" ]; then
		if [ ! -x "$BUTLER_PATH" ]; then
			echo "Error: BUTLER_PATH is set but not executable: $BUTLER_PATH"
			exit 1
		fi

		printf '%s' "$BUTLER_PATH"
		return 0
	fi

	local command_path=""
	command_path="$(command -v butler 2>/dev/null || true)"
	if [ -n "$command_path" ] && [ -x "$command_path" ]; then
		printf '%s' "$command_path"
		return 0
	fi

	local itch_root="${HOME}/Library/Application Support/itch/broth/butler"
	local chosen_version_file="$itch_root/.chosen-version"
	if [ -f "$chosen_version_file" ]; then
		local chosen_version=""
		IFS= read -r chosen_version < "$chosen_version_file" || true
		chosen_version="$(printf '%s' "$chosen_version" | tr -d '\r' | sed 's/^[[:space:]]*//;s/[[:space:]]*$//')"
		local bundled_path="$itch_root/versions/$chosen_version/butler"
		if [ -n "$chosen_version" ] && [ -x "$bundled_path" ]; then
			printf '%s' "$bundled_path"
			return 0
		fi
	fi

	local cached_path="${HOME}/butler/butler"
	if [ -x "$cached_path" ]; then
		printf '%s' "$cached_path"
		return 0
	fi

	return 1
}

download_butler() {
	local butler_dir="$1"
	local butler_archive="$2"
	local butler_zip="$butler_dir/butler.zip"
	local butler_path="$butler_dir/butler"
	local butler_url="https://broth.itch.zone/butler/${butler_archive}/LATEST/archive/default"

	echo "Downloading butler..." >&2
	if curl -L --fail --retry 3 --retry-delay 2 --connect-timeout 15 -o "$butler_zip" "$butler_url"; then
		unzip -o -q "$butler_zip" -d "$butler_dir"
		chmod +x "$butler_path"
		printf '%s' "$butler_path"
		return 0
	fi

	local existing_butler=""
	existing_butler="$(resolve_butler_path || true)"
	if [ -n "$existing_butler" ]; then
		echo "Download failed, falling back to existing butler at $existing_butler" >&2
		printf '%s' "$existing_butler"
		return 0
	fi

	echo "Error: Unable to download butler from $butler_url, and no existing butler binary was found." >&2
	echo "Set BUTLER_PATH to a bundled butler binary or allow access to broth.itch.zone from Unity Cloud Build." >&2
	exit 1
}

RELEASE_VERSION="$(read_version_file "$VERSION_FILE" "current release version")"
LAST_DEPLOYED_VERSION="$(read_version_file "$LAST_DEPLOYED_VERSION_FILE" "last deployed version" true)"

if [ -z "${BUILD_PATH:-}" ] && [ -n "${UNITY_PLAYER_PATH:-}" ]; then
	BUILD_PATH="$UNITY_PLAYER_PATH"
fi

if [ -z "${BUILD_PATH:-}" ] && [ $# -ge 2 ] && [ -e "$2" ]; then
	BUILD_PATH="$2"
fi

if [ -z "${BUILD_PATH:-}" ] && [ $# -ge 1 ] && [ -e "$1" ]; then
	BUILD_PATH="$1"
fi

echo "Starting itch.io deployment..."
echo "PWD=$(pwd)"
echo "SCRIPT_DIR=$SCRIPT_DIR"
echo "UNITY_PROJECT_ROOT=$UNITY_PROJECT_ROOT"
echo "REPO_ROOT=$REPO_ROOT"
echo "ARG1=${1:-}"
echo "ARG2=${2:-}"
echo "ARG3=${3:-}"
echo "BUILD_PATH=${BUILD_PATH:-}"
echo "UNITY_PLAYER_PATH=${UNITY_PLAYER_PATH:-}"
echo "BUILD_NUMBER=${BUILD_NUMBER:-}"
echo "RELEASE_VERSION=$RELEASE_VERSION"

if [ -z "${BUILD_PATH:-}" ]; then
	echo "Error: BUILD_PATH is not set. Point it at the macOS build artifact or zip to upload, or run this in Unity Build Automation so UNITY_PLAYER_PATH is available."
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

if [ -n "$LAST_DEPLOYED_VERSION" ] && version_is_older_than "$RELEASE_VERSION" "$LAST_DEPLOYED_VERSION"; then
	echo "Error: Release version $RELEASE_VERSION is older than the last deployed Windows version $LAST_DEPLOYED_VERSION. Update FungusToast.Unity/version.txt before publishing to itch.io."
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

BUTLER_BIN="$(download_butler "$BUTLER_DIR" "$butler_archive")"

echo "Butler version:"
"$BUTLER_BIN" -V

echo "Pushing build to itch.io target: $ITCH_TARGET"
"$BUTLER_BIN" push "$BUILD_PATH" "$ITCH_TARGET" --userversion "$RELEASE_VERSION"

echo "Deployment complete."
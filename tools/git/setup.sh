#!/usr/bin/env bash
# Configures this clone's local git for painless Unity collaboration.
# Mirror of tools/git/setup.ps1 for bash/macOS/Linux/WSL. Run once per clone:
#   bash tools/git/setup.sh [/path/to/UnityYAMLMerge]
set -euo pipefail

repo_root="$(git rev-parse --show-toplevel)"
cd "$repo_root"

git config core.hooksPath 'tools/git/hooks'
git config rerere.enabled true
git config rerere.autoUpdate true
chmod +x tools/git/hooks/* 2>/dev/null || true
echo "core.hooksPath  = tools/git/hooks"
echo "rerere.enabled  = true"

tool="${1:-}"
if [[ -z "$tool" ]]; then
    version="$(sed -n 's/^m_EditorVersion:[[:space:]]*//p' FungusToast.Unity/ProjectSettings/ProjectVersion.txt 2>/dev/null | head -1 | tr -d '\r')"
    for c in \
        "/Applications/Unity/Hub/Editor/$version/Unity.app/Contents/Tools/UnityYAMLMerge" \
        "$HOME/Unity/Hub/Editor/$version/Editor/Data/Tools/UnityYAMLMerge" \
        "/opt/unity/editors/$version/Editor/Data/Tools/UnityYAMLMerge" \
        "/c/Program Files/Unity/Hub/Editor/$version/Editor/Data/Tools/UnityYAMLMerge.exe"
    do
        [[ -n "$version" && -x "$c" ]] && tool="$c" && break
    done
fi

if [[ -n "$tool" && -e "$tool" ]]; then
    git config merge.unityyamlmerge.name 'Unity SmartMerge'
    git config merge.unityyamlmerge.driver "'$tool' merge -p %O %B %A %A"
    git config merge.unityyamlmerge.recursive binary
    echo "merge.unityyamlmerge -> $tool"
else
    echo "WARNING: UnityYAMLMerge not found; pass its path as an argument." >&2
    echo "Unity YAML files fall back to the default text merge until then." >&2
fi

echo
echo "Done. See docs/UNITY_CONCURRENT_WORKFLOW.md for the playtest-while-agents-work setup."

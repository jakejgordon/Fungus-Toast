#!/usr/bin/env bash
# Mirrors .agents/skills/<name> into .claude/skills/<name> via symlinks so
# Claude Code can discover the same skills used by other agent tooling in
# this repo. .agents/skills/ remains the canonical, git-tracked source of
# truth; .claude/skills/ is a local-only, gitignored mirror. Safe to re-run.
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
source_root="$repo_root/.agents/skills"
target_root="$repo_root/.claude/skills"

if [ ! -d "$source_root" ]; then
    echo "Source directory not found: $source_root" >&2
    exit 1
fi

mkdir -p "$target_root"

for skill_path in "$source_root"/*/; do
    name="$(basename "$skill_path")"
    target="$target_root/$name"
    source="$source_root/$name"

    if [ -L "$target" ] && [ "$(readlink "$target")" = "$source" ]; then
        echo "OK      $name (already linked)"
        continue
    fi

    if [ -e "$target" ] || [ -L "$target" ]; then
        echo "RELINK  $name (removing stale entry)"
        rm -rf "$target"
    fi

    ln -s "$source" "$target"
    echo "LINKED  $name"
done

echo ""
echo "Done. .claude/skills/ now mirrors .agents/skills/ via symlinks."

#!/usr/bin/env python3
"""Checks that relative links in git-tracked Markdown files resolve to real files.

Usage: python scripts/check_markdown_links.py

Only checks relative file links/images (e.g. `[text](../docs/FOO.md)`).
http(s)/mailto links and in-page `#anchor` links are skipped. Exits non-zero
if any link target is missing, so it can run as a CI gate.
"""
import re
import subprocess
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent
LINK_RE = re.compile(r'!?\[[^\]]*\]\(([^)]+)\)')


def tracked_markdown_files():
    out = subprocess.run(
        ["git", "ls-files", "*.md"],
        cwd=REPO_ROOT, capture_output=True, text=True, check=True,
    )
    return [line.strip() for line in out.stdout.splitlines() if line.strip()]


def find_broken_links(files):
    broken = []
    for rel_path in files:
        full_path = REPO_ROOT / rel_path
        text = full_path.read_text(encoding="utf-8")
        for match in LINK_RE.finditer(text):
            target = match.group(1).strip()
            if target.startswith(("http://", "https://", "mailto:", "#")):
                continue
            target_path = target.split("#", 1)[0].strip()
            if not target_path:
                continue
            resolved = (full_path.parent / target_path).resolve()
            if not resolved.exists():
                line_no = text.count("\n", 0, match.start()) + 1
                broken.append((rel_path, line_no, target))
    return broken


def main():
    files = tracked_markdown_files()
    broken = find_broken_links(files)
    if broken:
        print(f"Found {len(broken)} broken markdown link(s):\n")
        for rel_path, line_no, target in broken:
            print(f"  {rel_path}:{line_no} -> {target}")
        return 1
    print(f"Checked {len(files)} markdown files. No broken relative links found.")
    return 0


if __name__ == "__main__":
    sys.exit(main())

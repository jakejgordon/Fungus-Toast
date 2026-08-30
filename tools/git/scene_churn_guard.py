#!/usr/bin/env python3
"""Revert cosmetic-only churn in high-noise Unity YAML files before it is committed.

Unity rewrites scene/prefab files on every save with non-semantic drift:
sub-pixel layout floats, persisted scrollbar/slider state, `m_EditorClassIdentifier`
whitespace, and transient `{fileID: 0}` object-reference nulls that resolve again on
the next full import. When that is the *only* change to a guarded file, committing it
adds pure noise and invites merge conflicts.

This checks each staged guarded file against HEAD. If every changed line is
recognisably cosmetic, the file is restored to HEAD (working tree + index) and the
commit proceeds without it. Any substantive change is left completely untouched.

Bypass with `git commit --no-verify` or `SCENE_CHURN_GUARD=0 git commit ...`.
"""
from __future__ import annotations

import os
import re
import subprocess
import sys

# Files worth guarding. Keep this tight: only files that churn constantly and whose
# real edits are rare and deliberate.
GUARDED = {
    "FungusToast.Unity/Assets/Scenes/SampleScene.unity",
}

# A "drift" number is what Unity's layout solver / prefab-override serializer emits:
# many fractional digits, or scientific notation. Deliberate edits use round-ish
# values (`900`, `-120.5`), so requiring this shape keeps intentional layout changes
# from being mistaken for churn.
_DRIFT = r"-?\d+\.\d{3,}(?:e-?\d+)?|-?\d+(?:\.\d+)?e-?\d+"
_LAYOUT_KEYS = (
    r"m_AnchoredPosition|m_SizeDelta|m_LocalPosition|m_LocalScale|m_Pivot|"
    r"m_LocalEulerAnglesHint|m_LocalRotation"
)

# A changed (+/-) diff line is "cosmetic" if, once the leading +/- is stripped, it
# matches one of these. Everything else counts as a real change.
COSMETIC_LINE = re.compile(
    rf"""^\s*(
        m_EditorClassIdentifier:\s* |                                  # trailing-whitespace flip
        m_Value:\s*-?\d+(?:\.\d+)?(?:e-?\d+)?\s* |                      # persisted scrollbar/slider state
        ({_LAYOUT_KEYS})(?:\.[xyzw])?:\s*
            (?:\{{[^}}]*(?:{_DRIFT})[^}}]*\}}|{_DRIFT})\s* |            # layout-solver drift only
        value:\s*(?:{_DRIFT})\s* |                                     # prefab-override float drift
        m_Size:\s*(?:{_DRIFT})\s*
    )$""",
    re.VERBOSE,
)

# Inline-serialized vector line (e.g. "  x: 1.23456, y: -7.89012, z: 0"); cosmetic
# only when at least one component carries the drift signature.
_VECTOR_LINE = re.compile(r"^\s*x:\s*-?\d.*,\s*y:\s*-?\d")
_HAS_DRIFT = re.compile(r"\d\.\d{3,}|\de-?\d")

# `foo: {fileID: 0}` — a nulled object reference. Cosmetic only when the opposite
# side of the diff sets the *same key* to a real reference (transient import miss).
NULL_REF = re.compile(r"^\s*([A-Za-z0-9_]+):\s*\{fileID:\s*0\}\s*$")
REAL_REF = re.compile(r"^\s*([A-Za-z0-9_]+):\s*\{fileID:\s*\d+,\s*guid:\s*[0-9a-f]{32},\s*type:\s*\d+\}\s*$")


def staged_files() -> list[str]:
    out = subprocess.run(
        ["git", "diff", "--cached", "--name-only", "--diff-filter=M"],
        capture_output=True, text=True, check=True,
    ).stdout
    return [line.strip().replace("\\", "/") for line in out.splitlines() if line.strip()]


def diff_lines(path: str) -> list[str]:
    out = subprocess.run(
        ["git", "diff", "--cached", "--unified=0", "--", path],
        capture_output=True, text=True, check=True,
    ).stdout
    return out.splitlines()


def change_is_cosmetic_only(path: str) -> bool:
    added: list[str] = []
    removed: list[str] = []
    for line in diff_lines(path):
        if line.startswith("+++") or line.startswith("---") or line.startswith("@@"):
            continue
        if line.startswith("+"):
            added.append(line[1:])
        elif line.startswith("-"):
            removed.append(line[1:])

    if not added and not removed:
        return False

    added_null_keys = {m.group(1) for line in added if (m := NULL_REF.match(line))}
    added_real_keys = {m.group(1) for line in added if (m := REAL_REF.match(line))}
    removed_null_keys = {m.group(1) for line in removed if (m := NULL_REF.match(line))}
    removed_real_keys = {m.group(1) for line in removed if (m := REAL_REF.match(line))}

    # A key whose object reference flips null<->real between the two sides is a
    # transient asset-import miss, not a real edit.
    flip_keys = (added_null_keys & removed_real_keys) | (added_real_keys & removed_null_keys)

    def line_ok(line: str) -> bool:
        if COSMETIC_LINE.match(line):
            return True
        if _VECTOR_LINE.match(line) and _HAS_DRIFT.search(line):
            return True
        m = NULL_REF.match(line) or REAL_REF.match(line)
        return bool(m and m.group(1) in flip_keys)

    return all(line_ok(line) for line in added) and all(line_ok(line) for line in removed)


def main() -> int:
    if os.environ.get("SCENE_CHURN_GUARD") == "0":
        return 0
    try:
        targets = [f for f in staged_files() if f in GUARDED]
    except subprocess.CalledProcessError:
        return 0

    reverted: list[str] = []
    for path in targets:
        try:
            if change_is_cosmetic_only(path):
                subprocess.run(["git", "checkout", "HEAD", "--", path], check=True)
                subprocess.run(["git", "reset", "--quiet", "HEAD", "--", path], check=False)
                reverted.append(path)
        except subprocess.CalledProcessError:
            continue

    if reverted:
        sys.stderr.write(
            "\nscene-churn-guard: reverted cosmetic-only Unity churn so it stays out of the commit:\n"
        )
        for path in reverted:
            sys.stderr.write(f"  - {path}\n")
        sys.stderr.write(
            "If one of these had a real change, re-stage it and commit with --no-verify.\n\n"
        )
    return 0


if __name__ == "__main__":
    sys.exit(main())

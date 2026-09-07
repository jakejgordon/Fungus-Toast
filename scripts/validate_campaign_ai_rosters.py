#!/usr/bin/env python3
"""Validates the campaign AI rosters against the frozen P7 measurement.

Every campaign level's opponents live in a BoardPreset asset, and nothing at
build time checks that the resulting difficulty curve makes sense. This script
does, so that an edit that flattens or inverts the ladder fails loudly instead
of shipping.

Checks, in order of how badly a failure would hurt:

  * every strategy named by a preset was measured in the P7 campaign panel, so a
    typo cannot silently fall through to a random-fill lineup;
  * a fixed lineup never repeats a strategy, since a duplicate spends two seats
    on one opponent;
  * a pool holds at least as many strategies as it has seats, or the level
    quietly fields fewer AIs than it was authored for;
  * the mean measured strength of each level exceeds the level before it.

The mean is a *relative* difficulty index, not a prediction of in-game share:
shares are measured against a mixed panel and do not add up within one lineup.

Usage: python scripts/validate_campaign_ai_rosters.py
"""
from __future__ import annotations

import re
import statistics
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
PRESETS = REPO / "FungusToast.Unity/Assets/Configs/Board Presets"
BANDS = REPO / "FungusToast.Core/docs/second-level/AI_P7_CAMPAIGN_BANDS_V1.md"
ROW = re.compile(r"\| (\S+) \| (\w+|—) \| ([\d.]+) \| (\d+) \| (\w+) \|")


def measured_shares() -> dict[str, float]:
    shares = {m.group(1): float(m.group(3))
              for m in (ROW.match(line) for line in BANDS.read_text(encoding="utf-8").splitlines())
              if m}
    if not shares:
        raise SystemExit(f"no measured strategies parsed from {BANDS}")
    return shares


def parse_preset(path: Path) -> dict:
    """Reads only the fields that decide a level's opponents."""
    preset = {"path": path, "id": None, "seats": 0, "fixed": [], "pool": []}
    section = None
    for raw in path.read_text(encoding="utf-8").splitlines():
        field = re.match(r"^  (\w+):(.*)$", raw)
        if field:
            section, value = field.group(1), field.group(2).strip()
            if section == "presetId":
                preset["id"] = value
            elif section == "pooledAiPlayerCount":
                preset["seats"] = int(value or 0)
            continue
        if section == "aiPlayers":
            entry = re.match(r"^  - strategyName: (\S+)", raw)
            if entry:
                preset["fixed"].append(entry.group(1))
        elif section == "aiStrategyPool":
            entry = re.match(r"^  - (\S+)\s*$", raw)
            if entry:
                preset["pool"].append(entry.group(1))
    return preset


def level_index(preset_id: str) -> tuple[int, str]:
    match = re.fullmatch(r"Campaign(\d+)", preset_id or "")
    return (int(match.group(1)), "") if match else (10**6, preset_id or "")


def main() -> int:
    shares = measured_shares()
    presets = sorted((parse_preset(p) for p in PRESETS.glob("*.asset")),
                     key=lambda p: level_index(p["id"]))

    failures: list[str] = []
    previous_mean = None
    previous_id = None

    print(f"{'level':<12}{'mode':<7}{'seats':>6}{'listed':>7}{'mean':>8}")
    for preset in presets:
        pid, fixed, pool = preset["id"], preset["fixed"], preset["pool"]
        names, mode = (fixed, "fixed") if fixed else (pool, "pool")
        seats = len(fixed) if fixed else preset["seats"]
        if not names:
            failures.append(f"{pid}: fields no AI strategies at all")
            continue

        unknown = [n for n in names if n not in shares]
        if unknown:
            failures.append(f"{pid}: not in the measured panel, so it may not resolve: {unknown}")
        if mode == "fixed":
            duplicates = sorted({n for n in fixed if fixed.count(n) > 1})
            if duplicates:
                failures.append(f"{pid}: fixed lineup repeats {duplicates}, spending seats on one opponent")
        elif len(pool) < seats:
            failures.append(f"{pid}: pool holds {len(pool)} strategies for {seats} seats, so the level under-fields")

        known = [shares[n] for n in names if n in shares]
        if not known:
            continue
        mean = statistics.mean(known)
        print(f"{pid:<12}{mode:<7}{seats:>6}{len(names):>7}{mean:>8.3f}")
        if previous_mean is not None and mean <= previous_mean:
            failures.append(
                f"{pid}: mean {mean:.3f} does not exceed {previous_id} at {previous_mean:.3f}, "
                "so the ladder stalls or inverts here")
        previous_mean, previous_id = mean, pid

    if failures:
        print("\nFAILURES")
        for failure in failures:
            print(f"  {failure}")
        return 1

    print(f"\n{len(presets)} campaign levels validate; difficulty rises at every step.")
    return 0


if __name__ == "__main__":
    sys.exit(main())

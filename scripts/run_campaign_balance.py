#!/usr/bin/env python3
from __future__ import annotations

import argparse
import re
import subprocess
from pathlib import Path
from typing import Dict, List

ROOT = Path(__file__).resolve().parents[1]
PROGRESSION = ROOT / "FungusToast.Unity/Assets/Configs/Campaign/CampaignProgression.asset"
BOARD_PRESETS_DIR = ROOT / "FungusToast.Unity/Assets/Configs/Board Presets"
SIM_PROJECT = ROOT / "FungusToast.Simulation/FungusToast.Simulation.csproj"
PLAYER_PROXY = "TST_CampaignPlayer_SafeBaseline"


def parse_progression(path: Path) -> List[dict]:
    levels: List[dict] = []
    current = None
    for raw in path.read_text().splitlines():
        line = raw.rstrip()
        m = re.match(r"\s*- levelIndex: (\d+)", line)
        if m:
            if current:
                levels.append(current)
            current = {"levelIndex": int(m.group(1))}
            continue
        if current is None:
            continue
        m = re.match(r"\s*boardPreset: \{fileID: .* guid: ([0-9a-f]+),", line)
        if m:
            current["guid"] = m.group(1)
            continue
        m = re.match(r"\s*enableNutrientPatches: (\d+)", line)
        if m:
            current["enableNutrientPatches"] = m.group(1) == "1"
    if current:
        levels.append(current)
    return levels


def build_guid_map(directory: Path) -> Dict[str, Path]:
    mapping: Dict[str, Path] = {}
    for meta in directory.glob("*.asset.meta"):
        guid = None
        for line in meta.read_text().splitlines():
            if line.startswith("guid: "):
                guid = line.split(":", 1)[1].strip()
                break
        if guid:
            mapping[guid] = meta.with_suffix("")
    return mapping


def parse_board_preset(path: Path) -> dict:
    preset = {"path": str(path), "aiPlayers": []}
    for raw in path.read_text().splitlines():
        line = raw.strip()
        if line.startswith("presetId:"):
            preset["presetId"] = line.split(":", 1)[1].strip()
        elif line.startswith("boardWidth:"):
            preset["boardWidth"] = int(line.split(":", 1)[1].strip())
        elif line.startswith("boardHeight:"):
            preset["boardHeight"] = int(line.split(":", 1)[1].strip())
        elif line.startswith("- strategyName:"):
            preset["aiPlayers"].append(line.split(":", 1)[1].strip())
    return preset


def run_level(level: dict, preset: dict, games: int, seed: int, dry_run: bool) -> int:
    lineup = [PLAYER_PROXY] + preset["aiPlayers"]
    experiment_id = f"campaign_balance_lvl{level['levelIndex']:02d}_{preset['presetId']}_g{games}_seed{seed}"
    cmd = [
        "dotnet",
        "run",
        "--project",
        str(SIM_PROJECT),
        "--",
        "--games",
        str(games),
        "--width",
        str(preset["boardWidth"]),
        "--height",
        str(preset["boardHeight"]),
        "--strategy-set",
        "Campaign",
        "--strategy-names",
        ",".join(lineup),
        "--seed",
        str(seed + level["levelIndex"]),
        "--experiment-id",
        experiment_id,
        "--no-keyboard",
    ]
    if len(lineup) != 8:
        cmd.extend(["--players", str(len(lineup))])
    if not level.get("enableNutrientPatches", True):
        cmd.append("--no-nutrient-patches")

    print(f"\n=== Campaign level {level['levelIndex']} :: {preset['presetId']} ===")
    print(f"Board: {preset['boardWidth']}x{preset['boardHeight']} | Players: {len(lineup)} | Nutrients: {'On' if level.get('enableNutrientPatches', True) else 'Off'}")
    print(f"Lineup: {', '.join(lineup)}")
    print("Command:")
    print(" ".join(cmd))
    if dry_run:
        return 0
    completed = subprocess.run(cmd, cwd=ROOT)
    return completed.returncode


def main() -> int:
    parser = argparse.ArgumentParser(description="Run campaign-balance simulations using the safe player proxy against exact campaign level AI lineups.")
    parser.add_argument("--level", type=int, help="Run only a single campaign level index.")
    parser.add_argument("--games", type=int, default=20)
    parser.add_argument("--seed", type=int, default=20260327)
    parser.add_argument("--dry-run", action="store_true")
    args = parser.parse_args()

    levels = parse_progression(PROGRESSION)
    guid_map = build_guid_map(BOARD_PRESETS_DIR)

    selected = [lvl for lvl in levels if args.level is None or lvl["levelIndex"] == args.level]
    if not selected:
        raise SystemExit("No matching campaign levels found.")

    for level in selected:
        preset_path = guid_map.get(level.get("guid", ""))
        if preset_path is None:
            raise SystemExit(f"Could not resolve board preset for campaign level {level['levelIndex']}.")
        preset = parse_board_preset(preset_path)
        code = run_level(level, preset, args.games, args.seed, args.dry_run)
        if code != 0:
            return code

    return 0


if __name__ == "__main__":
    raise SystemExit(main())

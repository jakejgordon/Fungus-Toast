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
    preset = {"path": str(path), "aiPlayers": [], "aiStrategyPool": [], "pooledAiPlayerCount": 0}
    in_ai_players = False
    in_ai_pool = False

    for raw in path.read_text().splitlines():
        line = raw.strip()
        if not line:
            continue

        if line.startswith("presetId:"):
            preset["presetId"] = line.split(":", 1)[1].strip()
            in_ai_players = False
            in_ai_pool = False
        elif line.startswith("boardWidth:"):
            preset["boardWidth"] = int(line.split(":", 1)[1].strip())
            in_ai_players = False
            in_ai_pool = False
        elif line.startswith("boardHeight:"):
            preset["boardHeight"] = int(line.split(":", 1)[1].strip())
            in_ai_players = False
            in_ai_pool = False
        elif line.startswith("aiPlayers:"):
            in_ai_players = True
            in_ai_pool = False
        elif line.startswith("pooledAiPlayerCount:"):
            preset["pooledAiPlayerCount"] = int(line.split(":", 1)[1].strip())
            in_ai_players = False
            in_ai_pool = False
        elif line.startswith("aiStrategyPool:"):
            in_ai_players = False
            in_ai_pool = True
        elif in_ai_players and line.startswith("- strategyName:"):
            preset["aiPlayers"].append(line.split(":", 1)[1].strip())
        elif in_ai_pool and line.startswith("-"):
            value = line[1:].strip()
            if value:
                preset["aiStrategyPool"].append(value)
        elif not raw.startswith(" ") and not raw.startswith("\t"):
            in_ai_players = False
            in_ai_pool = False

    return preset


def stable_string_hash(value: str) -> int:
    if not value:
        return 0

    h = 23
    for c in value:
        h = ((h * 31) + ord(c)) & 0xFFFFFFFF
    if h >= 0x80000000:
        h -= 0x100000000
    return h


def resolve_campaign_ai_names(level_index: int, preset: dict, seed: int) -> List[str]:
    if preset["aiPlayers"]:
        return list(preset["aiPlayers"])

    pool = []
    seen = set()
    for name in preset.get("aiStrategyPool", []):
        if name and name not in seen:
            seen.add(name)
            pool.append(name)

    desired_count = min(preset.get("pooledAiPlayerCount", 0), len(pool))
    if desired_count <= 0:
        return []

    preset_hash = stable_string_hash(preset.get("presetId", ""))
    campaign_seed = ((seed * 397) ^ level_index ^ preset_hash) & 0xFFFFFFFF
    import random
    rng = random.Random(campaign_seed)
    shuffled = list(pool)
    shuffled.sort(key=lambda _: rng.random())
    return shuffled[:desired_count]


def run_level(level: dict, preset: dict, games: int, seed: int, dry_run: bool) -> int:
    resolved_ai = resolve_campaign_ai_names(level["levelIndex"], preset, seed)
    lineup = [PLAYER_PROXY] + resolved_ai
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

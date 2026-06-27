#!/usr/bin/env python3
from __future__ import annotations

import argparse
import csv
import importlib.util
import json
import math
import random
import re
import subprocess
import sys
from concurrent.futures import ThreadPoolExecutor, as_completed
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable

CAMPAIGN_PROXY_STRATEGY = "TST_CampaignPlayer_SafeBaseline"
CURATED_HOTDOG_VALIDATION_OPPONENTS = [
    "CMP_TierCap_GrowthResilience_Easy",
    "CMP_Reclaim_Scavenger_Easy",
    "CMP_Surge_BeaconTempo_Medium",
    "CMP_Control_AnabolicRebirth_Medium",
    "CMP_Growth_PutridTendrils_Medium",
    "CMP_Economy_LateSpike_Hard",
    "CMP_AnabolicBeaconRhizolith_Elite",
]
REFERENCE_LAYOUTS: dict[int, list[tuple[int, int]]] = {
    2: [(128, 80), (32, 80)],
    3: [(141, 80), (50, 133), (50, 27)],
    4: [(128, 128), (32, 128), (32, 32), (128, 32)],
    5: [(114, 104), (67, 120), (38, 80), (67, 40), (114, 56)],
    6: [(136, 95), (92, 126), (37, 123), (24, 65), (68, 34), (123, 37)],
    7: [(139, 94), (106, 135), (54, 135), (21, 94), (32, 42), (80, 19), (128, 42)],
    8: [(142, 106), (106, 142), (54, 142), (18, 106), (18, 54), (54, 18), (106, 18), (142, 54)],
}
CAMPAIGN_DIFFICULTIES = ["Training", "Easy", "Medium", "Hard", "Elite", "Boss"]


@dataclass(frozen=True)
class PresetSpec:
    preset_id: str
    level_index: int
    board_width: int
    board_height: int
    total_players: int
    ai_strategy_names: list[str]


@dataclass(frozen=True)
class ShapeInfo:
    sprite_name: str
    shape_source: str
    blocked_tile_ids: tuple[int, ...]
    shape_key: str


def load_validate_backgrounds_module(repo_root: Path):
    module_path = repo_root / "scripts/validate_board_backgrounds.py"
    spec = importlib.util.spec_from_file_location("validate_board_backgrounds", module_path)
    module = importlib.util.module_from_spec(spec)
    sys.modules[spec.name] = module
    assert spec.loader is not None
    spec.loader.exec_module(module)
    return module


def parse_preset_specs(board_preset_dir: Path) -> list[PresetSpec]:
    specs: list[PresetSpec] = []
    for path in sorted(board_preset_dir.glob("*.asset")):
        text = path.read_text(encoding="utf-8")
        preset_id = require_match(r"^  presetId: (.+)$", text, path).strip()
        board_width = int(require_match(r"^  boardWidth: (\d+)$", text, path))
        board_height = int(require_match(r"^  boardHeight: (\d+)$", text, path))
        level_match = re.search(r"(\d+)$", preset_id)
        level_index = int(level_match.group(1)) if level_match else 0

        fixed_ai = re.findall(r"^  - strategyName: (.+)$", extract_block(text, "aiPlayers"), re.M)
        fixed_ai = [name.strip() for name in fixed_ai if name.strip()]
        pooled_count_match = re.search(r"^  pooledAiPlayerCount: (\d+)$", text, re.M)
        pooled_ai_count = int(pooled_count_match.group(1)) if pooled_count_match else 0
        ai_pool = [name.strip() for name in re.findall(r"^  - (.+)$", extract_block(text, "aiStrategyPool"), re.M) if name.strip()]

        if fixed_ai:
            ai_strategy_names = fixed_ai
        elif pooled_ai_count > 0 and ai_pool:
            ai_strategy_names = resolve_pooled_ai_names(ai_pool, pooled_ai_count, level_index, preset_id)
        else:
            ai_strategy_names = []

        specs.append(PresetSpec(
            preset_id=preset_id,
            level_index=level_index,
            board_width=board_width,
            board_height=board_height,
            total_players=1 + len(ai_strategy_names),
            ai_strategy_names=ai_strategy_names,
        ))

    return specs


def extract_block(text: str, field_name: str) -> str:
    lines = text.splitlines()
    collecting = False
    block: list[str] = []
    field_prefix = f"  {field_name}:"
    for line in lines:
        if not collecting:
            if line.startswith(field_prefix):
                collecting = True
            continue

        if re.match(r"^  [A-Za-z0-9_].*:", line):
            break

        block.append(line)

    return "\n".join(block)


def require_match(pattern: str, text: str, path: Path) -> str:
    match = re.search(pattern, text, re.M)
    if not match:
        raise ValueError(f"Could not find pattern {pattern!r} in {path}")
    return match.group(1)


def resolve_pooled_ai_names(ai_pool: list[str], pooled_ai_count: int, level_index: int, preset_id: str) -> list[str]:
    unique_eligible: list[str] = []
    for name in ai_pool:
        if name not in unique_eligible:
            unique_eligible.append(name)

    desired_count = min(pooled_ai_count, len(unique_eligible))
    seed = get_pooled_ai_selection_seed(0, level_index, preset_id)
    rng = random.Random(seed)
    shuffled = unique_eligible[:]
    rng.shuffle(shuffled)
    return shuffled[:desired_count]


def get_pooled_ai_selection_seed(run_seed: int, level_index: int, preset_id: str) -> int:
    return unchecked(get_boss_pool_seed(run_seed, level_index) ^ get_stable_string_hash(preset_id))


def get_boss_pool_seed(run_seed: int, level_index: int) -> int:
    return unchecked((run_seed * 397) ^ level_index)


def get_stable_string_hash(value: str) -> int:
    if not value:
        return 0

    hash_value = 23
    for char in value:
        hash_value = unchecked((hash_value * 31) + ord(char))
    return hash_value


def unchecked(value: int) -> int:
    value &= 0xFFFFFFFF
    if value >= 0x80000000:
        value -= 0x100000000
    return value


def resolve_shape_info(repo_root: Path, preset_specs: Iterable[PresetSpec]) -> dict[tuple[int, int], ShapeInfo]:
    validate = load_validate_backgrounds_module(repo_root)
    asset_path = repo_root / "FungusToast.Unity/Assets/Configs/Toast Configs/ToastBoardMedium.asset"
    sprite_dir = repo_root / "FungusToast.Unity/Assets/Sprites/UI/Bread Backgrounds"

    sprite_guid_map = validate.build_sprite_guid_map(sprite_dir)
    asset = validate.load_unity_yaml(asset_path)["MonoBehaviour"]
    metadata_by_guid = validate.build_metadata_map(asset, sprite_guid_map)
    default_settings = validate.build_settings(asset, metadata_by_guid, sprite_guid_map, "default", "default background", 1, 10000, 1, 10000)
    overrides = [
        validate.build_settings(
            override_data,
            metadata_by_guid,
            sprite_guid_map,
            f"override-{index + 1}",
            validate.describe_override(override_data),
            int(override_data.get("minBoardWidth", 1)),
            int(override_data.get("maxBoardWidth", 10000)),
            int(override_data.get("minBoardHeight", 1)),
            int(override_data.get("maxBoardHeight", 10000)),
        )
        for index, override_data in enumerate(asset.get("boardBackgroundOverrides", []))
    ]
    sprite_images = {
        guid: validate.decode_rgba_png(path)
        for guid, path in sprite_guid_map.items()
        if guid in metadata_by_guid
        or guid == default_settings.sprite_guid
        or any(override.sprite_guid == guid for override in overrides)
    }

    shape_by_size: dict[tuple[int, int], ShapeInfo] = {}
    for preset in preset_specs:
        key = (preset.board_width, preset.board_height)
        if key in shape_by_size:
            continue

        settings = validate.resolve_settings(preset.board_width, preset.board_height, default_settings, overrides)
        blocked_tile_ids = compute_blocked_tile_ids(validate, preset.board_width, preset.board_height, settings, sprite_images)
        shape_key = compute_shape_key(preset.board_width, preset.board_height, blocked_tile_ids)
        shape_by_size[key] = ShapeInfo(
            sprite_name=settings.sprite_path.name,
            shape_source=detect_shape_source(validate, preset.board_width, preset.board_height, settings, sprite_images),
            blocked_tile_ids=tuple(blocked_tile_ids),
            shape_key=shape_key,
        )

    return shape_by_size


def detect_shape_source(validate, board_width: int, board_height: int, settings, sprite_images) -> str:
    probe = validate.evaluate_probe(board_width, board_height, settings, sprite_images)
    return probe.shape_source


def compute_blocked_tile_ids(validate, board_width: int, board_height: int, settings, sprite_images) -> list[int]:
    image = sprite_images[settings.sprite_guid]
    effective_safe_area = validate.get_effective_safe_area(settings, board_width, board_height)
    effective_ellipse = validate.get_effective_playable_ellipse(settings)
    effective_profile = validate.get_effective_playable_horizontal_span_profile(settings)
    clip_offsets = validate.build_clip_budget_sample_offsets(
        validate.PLAYABLE_SURFACE_TILE_SCALE,
        settings.max_tile_clip_fraction,
        settings.tile_clip_sample_resolution,
    )

    explicit_blocked = validate.sanitize_explicit_blocked_tile_ids(settings.explicit_blocked_tile_ids, board_width, board_height) if settings.use_explicit_blocked_tile_ids else set()
    baked_blocked = validate.get_matching_baked_blocked_tile_ids(settings.metadata, board_width, board_height)
    alpha_threshold = validate.clamp01(settings.alpha_playable_threshold)
    minimum_tile_coverage = validate.clamp01(settings.min_tile_coverage)
    blocked: list[int] = []

    for tile_y in range(board_height):
        for tile_x in range(board_width):
            tile_id = (tile_y * board_width) + tile_x
            if tile_id in explicit_blocked or tile_id in baked_blocked:
                blocked.append(tile_id)
                continue

            if baked_blocked:
                playable = True
            elif effective_profile is not None:
                satisfies_clip_budget = (
                    not clip_offsets
                    or validate.evaluate_tile_horizontal_span_profile_clip_budget(
                        effective_profile,
                        board_width,
                        board_height,
                        tile_x,
                        tile_y,
                        clip_offsets,
                    )
                )
                satisfies_coverage = (
                    minimum_tile_coverage <= 0.0
                    or validate.evaluate_tile_horizontal_span_profile_coverage(
                        effective_profile,
                        board_width,
                        board_height,
                        tile_x,
                        tile_y,
                        minimum_tile_coverage,
                    )
                )
                playable = satisfies_clip_budget and satisfies_coverage
            elif effective_ellipse is not None:
                satisfies_clip_budget = (
                    not clip_offsets
                    or validate.evaluate_tile_ellipse_clip_budget(
                        effective_ellipse,
                        board_width,
                        board_height,
                        tile_x,
                        tile_y,
                        clip_offsets,
                    )
                )
                satisfies_coverage = (
                    minimum_tile_coverage <= 0.0
                    or validate.evaluate_tile_ellipse_coverage(
                        effective_ellipse,
                        board_width,
                        board_height,
                        tile_x,
                        tile_y,
                        minimum_tile_coverage,
                    )
                )
                playable = satisfies_clip_budget and satisfies_coverage
            else:
                satisfies_clip_budget = (
                    not clip_offsets
                    or validate.evaluate_tile_clip_budget(
                        image,
                        effective_safe_area,
                        board_width,
                        board_height,
                        tile_x,
                        tile_y,
                        alpha_threshold,
                        clip_offsets,
                    )
                )
                satisfies_coverage = (
                    minimum_tile_coverage <= 0.0
                    or validate.evaluate_tile_coverage(
                        image,
                        effective_safe_area,
                        board_width,
                        board_height,
                        tile_x,
                        tile_y,
                        alpha_threshold,
                        minimum_tile_coverage,
                    )
                )
                playable = satisfies_clip_budget and satisfies_coverage

            if not playable:
                blocked.append(tile_id)

    return blocked


def compute_shape_key(board_width: int, board_height: int, blocked_tile_ids: Iterable[int]) -> str:
    material = f"{board_width}x{board_height}|" + ",".join(str(tile_id) for tile_id in sorted(set(blocked_tile_ids)))
    return __import__("hashlib").sha256(material.encode("utf-8")).hexdigest()


def scale_reference_positions(board_width: int, board_height: int, player_count: int) -> list[tuple[int, int]]:
    reference_positions = REFERENCE_LAYOUTS[player_count]
    return [
        (
            scale_coordinate(x, 160, board_width),
            scale_coordinate(y, 160, board_height),
        )
        for x, y in reference_positions
    ]


def scale_coordinate(coordinate: int, reference_board_size: int, target_board_size: int) -> int:
    if target_board_size <= 1:
        return 0
    reference_max = max(1, reference_board_size - 1)
    target_max = target_board_size - 1
    return max(0, min(target_board_size - 1, round((coordinate / reference_max) * target_max)))


def resolve_starting_positions(board_width: int, board_height: int, player_count: int, blocked_tile_ids: Iterable[int]) -> list[tuple[int, int]]:
    blocked = set(blocked_tile_ids)
    playable = [(x, y) for y in range(board_height) for x in range(board_width) if (y * board_width) + x not in blocked]
    resolved: list[tuple[int, int]] = []
    for preferred_x, preferred_y in scale_reference_positions(board_width, board_height, player_count):
        if ((preferred_y * board_width) + preferred_x) not in blocked and (preferred_x, preferred_y) not in resolved:
            resolved.append((preferred_x, preferred_y))
            continue

        best: tuple[int, int] | None = None
        best_distance: int | None = None
        best_tile_id: int | None = None
        for candidate_x, candidate_y in playable:
            if (candidate_x, candidate_y) in resolved:
                continue
            distance = ((candidate_x - preferred_x) ** 2) + ((candidate_y - preferred_y) ** 2)
            tile_id = (candidate_y * board_width) + candidate_x
            if best is None or distance < best_distance or (distance == best_distance and tile_id < best_tile_id):
                best = (candidate_x, candidate_y)
                best_distance = distance
                best_tile_id = tile_id

        if best is None:
            raise RuntimeError(f"No playable starting position found for {board_width}x{board_height}, players={player_count}")
        resolved.append(best)

    return resolved


def run_simulation(
    repo_root: Path,
    games: int,
    board_width: int,
    board_height: int,
    strategy_set: str,
    strategy_names: list[str],
    blocked_tile_file: Path,
    experiment_id: str,
) -> Path:
    command = [
        "dotnet",
        "run",
        "--no-build",
        "--project",
        str(repo_root / "FungusToast.Simulation/FungusToast.Simulation.csproj"),
        "--",
        "--games",
        str(games),
        "--width",
        str(board_width),
        "--height",
        str(board_height),
        "--rotate-slots",
        "--no-nutrient-patches",
        "--no-mycovariants",
        "--no-keyboard",
        "--blocked-tiles-file",
        str(blocked_tile_file),
        "--strategy-set",
        strategy_set,
        "--strategy-names",
        ",".join(strategy_names),
        "--experiment-id",
        experiment_id,
    ]
    subprocess.run(command, cwd=repo_root, check=True)
    return repo_root / "FungusToast.Simulation/bin/Debug/net8.0/SimulationParquet" / experiment_id


def summarize_proxy_slot_results(run_folder: Path, proxy_strategy_name: str, resolved_positions: list[tuple[int, int]]) -> list[dict[str, object]]:
    slot_rows = load_proxy_slot_summary_from_parquet(run_folder, proxy_strategy_name)
    sorted_rows = sorted(
        slot_rows,
        key=lambda row: (-float(row["win_pct"]), -float(row["avg_living_cells"]), int(row["AssignedSlot"])),
    )
    rank_by_slot = {
        int(row["AssignedSlot"]): rank + 1
        for rank, row in enumerate(sorted_rows)
    }

    summary_rows: list[dict[str, object]] = []
    for row in sorted(slot_rows, key=lambda row: int(row["AssignedSlot"])):
        slot_index = int(row["AssignedSlot"])
        x, y = resolved_positions[slot_index]
        summary_rows.append({
            "slot_index": slot_index,
            "x": x,
            "y": y,
            "favor_rank": rank_by_slot[slot_index],
            "win_percentage": float(row["win_pct"]),
            "games": int(row["games"]),
            "wins": int(row["wins"]),
            "avg_living_cells": float(row["avg_living_cells"]),
            "avg_dead_cells": float(row["avg_dead_cells"]),
            "avg_toxins": float(row["avg_toxins"]),
        })

    return summary_rows


def load_proxy_slot_summary_from_parquet(run_folder: Path, proxy_strategy_name: str) -> list[dict[str, object]]:
    analytics_python = run_folder.parents[5] / "FungusToast.Analytics/.venv/bin/python"
    snippet = f"""
import json
from pathlib import Path
import pandas as pd

run = Path({json.dumps(str(run_folder))})
proxy = {json.dumps(proxy_strategy_name)}
players = pd.read_parquet(run / "players.parquet")
proxy_rows = players[players["StrategyName"] == proxy]
grouped = proxy_rows.groupby("AssignedSlot", as_index=False).agg(
    games=("GameIndex", "count"),
    wins=("IsWinner", "sum"),
    avg_living_cells=("LivingCells", "mean"),
    avg_dead_cells=("DeadCells", "mean"),
    avg_toxins=("EndGameToxinCells", "mean"),
)
grouped["win_pct"] = grouped["wins"] / grouped["games"] * 100.0
print(grouped.to_json(orient="records"))
"""
    result = subprocess.run(
        [str(analytics_python), "-c", snippet],
        check=True,
        capture_output=True,
        text=True,
    )
    return json.loads(result.stdout)


def write_outputs(
    json_path: Path,
    cs_path: Path,
    metadata_rows: list[dict[str, object]],
) -> None:
    json_path.parent.mkdir(parents=True, exist_ok=True)
    cs_path.parent.mkdir(parents=True, exist_ok=True)
    json_path.write_text(json.dumps(metadata_rows, indent=2) + "\n", encoding="utf-8")
    cs_path.write_text(render_csharp_catalog(metadata_rows), encoding="utf-8")


def render_csharp_catalog(metadata_rows: list[dict[str, object]]) -> str:
    lines: list[str] = [
        "using System.Collections.Generic;",
        "",
        "namespace FungusToast.Core.Board",
        "{",
        "    public static partial class CampaignBoardStartingPositionCatalog",
        "    {",
        "        private static readonly Dictionary<(string PresetId, int PlayerCount), CampaignBoardStartingPositionMetadata> MetadataByPresetAndPlayerCount =",
        "            new()",
        "            {",
    ]

    for row in metadata_rows:
        preset_id = row["preset_id"]
        player_count = row["player_count"]
        shape_key = row["shape_key"]
        board_width = row["board_width"]
        board_height = row["board_height"]
        sprite_name = row["sprite_name"]
        shape_source = row["shape_source"]
        lines.append(f'                [("{preset_id}", {player_count})] = new CampaignBoardStartingPositionMetadata(')
        lines.append(f'                    presetId: "{preset_id}",')
        lines.append(f'                    shapeKey: "{shape_key}",')
        lines.append(f"                    boardWidth: {board_width},")
        lines.append(f"                    boardHeight: {board_height},")
        lines.append(f"                    playerCount: {player_count},")
        lines.append(f'                    spriteName: "{sprite_name}",')
        lines.append(f'                    shapeSource: "{shape_source}",')
        lines.append("                    entries: new CampaignBoardStartingPositionEntry[]")
        lines.append("                    {")
        for entry in row["entries"]:
            lines.append(
                "                        new CampaignBoardStartingPositionEntry("
                f"slotIndex: {entry['slot_index']}, x: {entry['x']}, y: {entry['y']}, favorRank: {entry['favor_rank']}, winPercentage: {entry['win_percentage']:.6f}),"
            )
        lines.append("                    }),")
        lines.append("")

    lines.append("            };")
    lines.append("    }")
    lines.append("}")
    lines.append("")
    return "\n".join(lines)


def write_validation_csv(path: Path, rows: list[dict[str, object]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    fieldnames = [
        "experiment_id",
        "board_width",
        "board_height",
        "player_count",
        "slot_index",
        "x",
        "y",
        "favor_rank",
        "win_percentage",
        "games",
        "wins",
        "avg_living_cells",
        "avg_dead_cells",
        "avg_toxins",
    ]
    with path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=fieldnames)
        writer.writeheader()
        writer.writerows(rows)


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate campaign starting-position metadata from slot-rotation simulations.")
    parser.add_argument("--repo", type=Path, default=Path(__file__).resolve().parents[1], help="Repo root.")
    parser.add_argument("--games", type=int, default=100, help="Games per simulation run.")
    parser.add_argument("--max-workers", type=int, default=4, help="Maximum number of simulation processes to run in parallel.")
    args = parser.parse_args()

    repo_root = args.repo.resolve()
    preset_specs = parse_preset_specs(repo_root / "FungusToast.Unity/Assets/Configs/Board Presets")
    shape_info_by_size = resolve_shape_info(repo_root, preset_specs)

    tmp_dir = repo_root / "tmp/campaign_starting_position_metadata"
    tmp_dir.mkdir(parents=True, exist_ok=True)

    metadata_work_items: list[tuple[PresetSpec, ShapeInfo, Path, list[tuple[int, int]], str, list[str]]] = []
    for preset in preset_specs:
        if preset.total_players < 2 or preset.total_players > 8:
            continue

        shape_info = shape_info_by_size[(preset.board_width, preset.board_height)]
        blocked_tile_file = tmp_dir / f"{preset.preset_id}_{preset.board_width}x{preset.board_height}_blocked.txt"
        blocked_tile_file.write_text("\n".join(str(tile_id) for tile_id in shape_info.blocked_tile_ids) + "\n", encoding="utf-8")
        resolved_positions = resolve_starting_positions(
            preset.board_width,
            preset.board_height,
            preset.total_players,
            shape_info.blocked_tile_ids,
        )
        strategy_names = [CAMPAIGN_PROXY_STRATEGY, *preset.ai_strategy_names]
        experiment_id = f"campaign_starting_pos_{preset.preset_id.lower()}_g{args.games}"
        metadata_work_items.append((preset, shape_info, blocked_tile_file, resolved_positions, experiment_id, strategy_names))

    hotdog_shape = shape_info_by_size[(115, 115)]
    hotdog_blocked_file = tmp_dir / "hotdog_115x115_blocked.txt"
    hotdog_blocked_file.write_text("\n".join(str(tile_id) for tile_id in hotdog_shape.blocked_tile_ids) + "\n", encoding="utf-8")
    hotdog_work_items: list[tuple[int, Path, list[tuple[int, int]], str, list[str]]] = []
    for player_count in range(3, 9):
        strategy_names = [CAMPAIGN_PROXY_STRATEGY, *CURATED_HOTDOG_VALIDATION_OPPONENTS[: player_count - 1]]
        experiment_id = f"hotdog_validation_115_p{player_count}_g{args.games}"
        resolved_positions = resolve_starting_positions(115, 115, player_count, hotdog_shape.blocked_tile_ids)
        hotdog_work_items.append((player_count, hotdog_blocked_file, resolved_positions, experiment_id, strategy_names))

    def run_work_item(board_width: int, board_height: int, blocked_tile_file: Path, experiment_id: str, strategy_names: list[str]) -> Path:
        return run_simulation(
            repo_root,
            args.games,
            board_width,
            board_height,
            "Campaign",
            strategy_names,
            blocked_tile_file,
            experiment_id,
        )

    max_workers = max(1, args.max_workers)
    metadata_rows: list[dict[str, object]] = []
    validation_rows: list[dict[str, object]] = []
    future_map: dict[object, tuple[str, object]] = {}
    with ThreadPoolExecutor(max_workers=max_workers) as executor:
        for preset, shape_info, blocked_tile_file, resolved_positions, experiment_id, strategy_names in metadata_work_items:
            future = executor.submit(
                run_work_item,
                preset.board_width,
                preset.board_height,
                blocked_tile_file,
                experiment_id,
                strategy_names,
            )
            future_map[future] = ("metadata", (preset, shape_info, resolved_positions, experiment_id))

        for player_count, blocked_tile_file, resolved_positions, experiment_id, strategy_names in hotdog_work_items:
            future = executor.submit(
                run_work_item,
                115,
                115,
                blocked_tile_file,
                experiment_id,
                strategy_names,
            )
            future_map[future] = ("hotdog", (player_count, resolved_positions, experiment_id))

        for future in as_completed(future_map):
            kind, payload = future_map[future]
            run_folder = future.result()
            if kind == "metadata":
                preset, shape_info, resolved_positions, _experiment_id = payload
                entries = summarize_proxy_slot_results(run_folder, CAMPAIGN_PROXY_STRATEGY, resolved_positions)
                metadata_rows.append({
                    "preset_id": preset.preset_id,
                    "board_width": preset.board_width,
                    "board_height": preset.board_height,
                    "player_count": preset.total_players,
                    "shape_key": shape_info.shape_key,
                    "sprite_name": shape_info.sprite_name,
                    "shape_source": shape_info.shape_source,
                    "entries": entries,
                })
            else:
                player_count, resolved_positions, experiment_id = payload
                for row in summarize_proxy_slot_results(run_folder, CAMPAIGN_PROXY_STRATEGY, resolved_positions):
                    validation_rows.append({
                        "experiment_id": experiment_id,
                        "board_width": 115,
                        "board_height": 115,
                        "player_count": player_count,
                        **row,
                    })

    metadata_rows.sort(key=lambda row: (int(row["board_width"]), int(row["player_count"])))
    validation_rows.sort(key=lambda row: (int(row["player_count"]), int(row["slot_index"])))

    write_outputs(
        repo_root / "FungusToast.Core/Board/Generated/campaign_board_starting_positions.json",
        repo_root / "FungusToast.Core/Board/CampaignBoardStartingPositionCatalog.g.cs",
        metadata_rows,
    )
    write_validation_csv(
        repo_root / "docs/generated/hotdog_115_slot_validation.csv",
        validation_rows,
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

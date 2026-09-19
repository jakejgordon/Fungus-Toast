from __future__ import annotations

import argparse
from dataclasses import dataclass
import json
from pathlib import Path
import re
from typing import Iterable

import pandas as pd


PROXY_STRATEGY_NAME = "TST_CampaignPlayer_SafeBaseline"
STANDARD_CONFIRMATION_GAMES = 100
# Mirrors the first-pass safe-proxy curve in CAMPAIGN_HELPER.md. Levels 11+
# intentionally remain unspecified until the late-campaign policy is settled.
SAFE_PROXY_TARGET_BANDS = {
    0: (0.90, 1.00),
    1: (0.90, 1.00),
    2: (0.90, 1.00),
    3: (0.70, 0.90),
    4: (0.50, 0.70),
    5: (0.35, 0.55),
    6: (0.25, 0.45),
    7: (0.15, 0.35),
    8: (0.10, 0.25),
    9: (0.05, 0.15),
    10: (0.00, 0.10),
}
RUN_NAME_PATTERN = re.compile(
    r"^campaign_balance_lvl(?P<level>\d+)_(?P<preset>.+)_g(?P<games>\d+)_seed(?P<seed>\d+)$"
)


@dataclass(frozen=True)
class CampaignLevelMeasurement:
    level: int
    experiment_id: str
    games: int
    proxy_win_rate: float
    provenance: str
    code_identity: str


@dataclass(frozen=True)
class CampaignLadderRow:
    measurement: CampaignLevelMeasurement
    change_from_previous: float | None
    status: str


@dataclass(frozen=True)
class CampaignLadderAnalysis:
    rows: tuple[CampaignLadderRow, ...]
    missing_levels: tuple[int, ...]
    inversions: tuple[CampaignLadderRow, ...]


def read_expected_campaign_levels(progression_path: Path) -> tuple[int, ...]:
    if not progression_path.is_file():
        raise ValueError(f"Campaign progression asset not found: {progression_path}")

    levels = {
        int(match.group(1))
        for line in progression_path.read_text(encoding="utf-8").splitlines()
        if (match := re.match(r"\s*- levelIndex: (\d+)\s*$", line))
    }
    if not levels:
        raise ValueError(f"No campaign level indices found in: {progression_path}")
    return tuple(sorted(levels))


def discover_campaign_runs(run_root: Path, seed: int) -> dict[int, Path]:
    if not run_root.is_dir():
        raise ValueError(f"Campaign artifact root not found: {run_root}")

    runs: dict[int, Path] = {}
    for path in sorted(run_root.iterdir(), key=lambda candidate: candidate.name):
        if not path.is_dir():
            continue
        match = RUN_NAME_PATTERN.match(path.name)
        if match is None:
            continue
        if int(match.group("games")) != STANDARD_CONFIRMATION_GAMES:
            continue
        if int(match.group("seed")) != seed:
            continue

        level = int(match.group("level"))
        if level in runs:
            raise ValueError(
                f"Multiple 100-game campaign artifacts resolve to level {level} and seed {seed}: "
                f"{runs[level].name}, {path.name}"
            )
        runs[level] = path

    if not runs:
        raise ValueError(
            f"No 100-game campaign artifacts found under {run_root} for seed {seed}."
        )
    return runs


def _to_snake(name: str) -> str:
    step1 = re.sub(r"(.)([A-Z][a-z]+)", r"\1_\2", name)
    step2 = re.sub(r"([a-z0-9])([A-Z])", r"\1_\2", step1)
    return step2.lower()


def _normalize_columns(frame: pd.DataFrame) -> pd.DataFrame:
    return frame.rename(columns={column: _to_snake(column) for column in frame.columns})


def _read_provenance(run_folder: Path, expected_games: int) -> tuple[str, str]:
    resolved_path = run_folder / "resolved-manifest.json"
    if resolved_path.is_file():
        manifest = json.loads(resolved_path.read_text(encoding="utf-8"))
        sampling = manifest.get("sampling", {})
        analysis = manifest.get("analysis", {})
        games_requested = sampling.get("gamesRequested")
        games_completed = sampling.get("gamesCompleted")
        completion_status = str(sampling.get("completionStatus", "")).lower()
        evidence_stage = str(analysis.get("evidenceStage", "")).lower()

        if games_requested != expected_games or games_completed != expected_games:
            raise ValueError(
                f"{run_folder.name} is not a complete {expected_games}-game artifact "
                f"(requested={games_requested}, completed={games_completed})."
            )
        if completion_status != "complete":
            raise ValueError(f"{run_folder.name} completion status is {completion_status!r}, not 'complete'.")
        if evidence_stage != "holdout":
            raise ValueError(f"{run_folder.name} evidence stage is {evidence_stage!r}, not 'holdout'.")
        code_identity = str(manifest.get("code", {}).get("commit", "")).strip()
        if not code_identity:
            raise ValueError(f"{run_folder.name} resolved manifest has no code commit identity.")
        return "Resolved holdout manifest", code_identity

    legacy_path = run_folder / "manifest.json"
    if not legacy_path.is_file():
        raise ValueError(f"{run_folder.name} has neither resolved-manifest.json nor manifest.json.")

    manifest = json.loads(legacy_path.read_text(encoding="utf-8"))
    games_requested = manifest.get("NumberOfGamesRequested")
    games_exported = manifest.get("actualGamesExported")
    if games_requested != expected_games or games_exported != expected_games:
        raise ValueError(
            f"{run_folder.name} is not a complete legacy {expected_games}-game artifact "
            f"(requested={games_requested}, exported={games_exported})."
        )
    return (
        f"Legacy manifest {manifest.get('schemaVersion', '(unknown)')} (stage/code unavailable)",
        "legacy-unavailable",
    )


def load_campaign_measurement(
    level: int,
    run_folder: Path,
    proxy_strategy_name: str = PROXY_STRATEGY_NAME,
) -> CampaignLevelMeasurement:
    match = RUN_NAME_PATTERN.match(run_folder.name)
    if match is None or int(match.group("level")) != level:
        raise ValueError(f"Campaign artifact folder does not match level {level}: {run_folder.name}")

    players_path = run_folder / "players.parquet"
    if not players_path.is_file():
        raise ValueError(f"Missing players.parquet: {players_path}")

    provenance, code_identity = _read_provenance(run_folder, STANDARD_CONFIRMATION_GAMES)
    players = _normalize_columns(pd.read_parquet(players_path))
    required_columns = {"game_index", "strategy_name"}
    missing_columns = sorted(required_columns.difference(players.columns))
    if missing_columns:
        raise ValueError(
            f"{run_folder.name}/players.parquet is missing: {', '.join(missing_columns)}"
        )

    proxy_rows = players.loc[players["strategy_name"] == proxy_strategy_name].copy()
    if proxy_rows.empty:
        raise ValueError(f"{run_folder.name} has no rows for proxy strategy {proxy_strategy_name!r}.")
    if proxy_rows["game_index"].nunique() != STANDARD_CONFIRMATION_GAMES:
        raise ValueError(
            f"{run_folder.name} has proxy rows for {proxy_rows['game_index'].nunique()} unique games; "
            f"expected {STANDARD_CONFIRMATION_GAMES}."
        )
    if len(proxy_rows) != STANDARD_CONFIRMATION_GAMES:
        raise ValueError(
            f"{run_folder.name} has {len(proxy_rows)} proxy rows; expected one per game."
        )

    if "win_credit" in proxy_rows.columns:
        proxy_win_rate = float(proxy_rows["win_credit"].mean())
    elif "is_winner" in proxy_rows.columns:
        proxy_win_rate = float(proxy_rows["is_winner"].astype(float).mean())
    else:
        raise ValueError(
            f"{run_folder.name}/players.parquet has neither win_credit nor is_winner."
        )

    return CampaignLevelMeasurement(
        level=level,
        experiment_id=run_folder.name,
        games=STANDARD_CONFIRMATION_GAMES,
        proxy_win_rate=proxy_win_rate,
        provenance=provenance,
        code_identity=code_identity,
    )


def analyze_campaign_ladder(
    measurements: Iterable[CampaignLevelMeasurement],
    expected_levels: Iterable[int],
) -> CampaignLadderAnalysis:
    ordered = sorted(measurements, key=lambda measurement: measurement.level)
    if not ordered:
        raise ValueError("At least one campaign measurement is required.")
    if len({measurement.level for measurement in ordered}) != len(ordered):
        raise ValueError("Campaign measurements contain duplicate levels.")
    code_identities = {measurement.code_identity for measurement in ordered}
    if len(code_identities) != 1:
        raise ValueError(
            "Campaign measurements do not share one code identity: "
            + ", ".join(sorted(code_identities))
        )

    rows: list[CampaignLadderRow] = []
    previous: CampaignLevelMeasurement | None = None
    for measurement in ordered:
        if previous is None:
            row = CampaignLadderRow(measurement, None, "Baseline")
        elif measurement.level != previous.level + 1:
            row = CampaignLadderRow(measurement, None, "Not compared (level gap)")
        else:
            change = measurement.proxy_win_rate - previous.proxy_win_rate
            if change > 0:
                status = "INVERSION"
            elif change < 0:
                status = "Expected direction"
            else:
                status = "Flat"
            row = CampaignLadderRow(measurement, change, status)
        rows.append(row)
        previous = measurement

    measured_levels = {measurement.level for measurement in ordered}
    missing_levels = tuple(sorted(set(expected_levels).difference(measured_levels)))
    inversions = tuple(row for row in rows if row.status == "INVERSION")
    return CampaignLadderAnalysis(tuple(rows), missing_levels, inversions)


def render_campaign_ladder_report(
    analysis: CampaignLadderAnalysis,
    seed: int,
    proxy_strategy_name: str = PROXY_STRATEGY_NAME,
) -> str:
    lines = [
        "# Campaign Measured Difficulty Ladder",
        "",
        "> Report-only diagnostic. An inversion does not fail CI or authorize a balance change.",
        "",
        f"- Proxy strategy: `{proxy_strategy_name}`",
        f"- Confirmation size: {STANDARD_CONFIRMATION_GAMES} games per measured level",
        f"- Artifact seed: `{seed}`",
        "- Expected direction: proxy win rate should stay flat or decrease as campaign level increases",
        "",
        "## Level results",
        "",
        "| Level | Experiment | Games | Proxy win rate | Target band | Band result | Change vs prior level | Ladder status | Provenance |",
        "|---:|---|---:|---:|---:|---|---:|---|---|",
    ]

    for row in analysis.rows:
        change = "—" if row.change_from_previous is None else f"{row.change_from_previous * 100:+.1f} pp"
        target_band = SAFE_PROXY_TARGET_BANDS.get(row.measurement.level)
        if target_band is None:
            target_text = "Not defined"
            band_result = "Not assessed"
        else:
            low, high = target_band
            target_text = f"{low:.0%}–{high:.0%}"
            if row.measurement.proxy_win_rate < low:
                band_result = "Below"
            elif row.measurement.proxy_win_rate > high:
                band_result = "Above"
            else:
                band_result = "Inside"
        lines.append(
            f"| {row.measurement.level} | `{row.measurement.experiment_id}` | "
            f"{row.measurement.games} | {row.measurement.proxy_win_rate:.1%} | {target_text} | "
            f"{band_result} | {change} | {row.status} | {row.measurement.provenance} |"
        )

    lines.extend(["", "## Findings", ""])
    if analysis.inversions:
        lines.append(f"- Measured inversions: **{len(analysis.inversions)}**")
        for row in analysis.inversions:
            previous_level = row.measurement.level - 1
            lines.append(
                f"- Level {previous_level} → {row.measurement.level}: proxy win rate increased "
                f"by {row.change_from_previous * 100:.1f} percentage points."
            )
    else:
        lines.append("- Measured inversions: **0**")

    if analysis.missing_levels:
        missing = ", ".join(str(level) for level in analysis.missing_levels)
        lines.append(f"- Missing standard confirmation artifacts: {missing}")
    else:
        lines.append("- Missing standard confirmation artifacts: none")

    lines.extend(
        [
            "",
            "Interpret inversions as review prompts, not causal conclusions. Check target bands, "
            "confidence intervals, board geometry, lineup composition, and campaign loadouts before tuning.",
            "",
        ]
    )
    return "\n".join(lines)


def main() -> int:
    repository_root = Path(__file__).resolve().parents[1]
    parser = argparse.ArgumentParser(
        description="Report measured campaign difficulty inversions from 100-game Parquet artifacts."
    )
    parser.add_argument("--run-root", type=Path, required=True)
    parser.add_argument("--seed", type=int, required=True)
    parser.add_argument(
        "--progression",
        type=Path,
        default=repository_root / "FungusToast.Unity/Assets/Configs/Campaign/CampaignProgression.asset",
    )
    parser.add_argument("--proxy-strategy", default=PROXY_STRATEGY_NAME)
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()

    expected_levels = read_expected_campaign_levels(args.progression)
    run_folders = discover_campaign_runs(args.run_root, args.seed)
    measurements = [
        load_campaign_measurement(level, folder, args.proxy_strategy)
        for level, folder in sorted(run_folders.items())
    ]
    analysis = analyze_campaign_ladder(measurements, expected_levels)
    report = render_campaign_ladder_report(analysis, args.seed, args.proxy_strategy)

    output_path = args.output or args.run_root / f"campaign_difficulty_ladder_seed{args.seed}.md"
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(report, encoding="utf-8")
    print(f"Campaign difficulty report: {output_path}")
    print(f"Measured inversions: {len(analysis.inversions)} (report-only; exit status remains successful)")
    if analysis.missing_levels:
        print("Missing standard confirmation levels: " + ", ".join(map(str, analysis.missing_levels)))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

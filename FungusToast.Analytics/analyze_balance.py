from __future__ import annotations

import argparse
from pathlib import Path
import numpy as np
import pandas as pd
import re


def _to_snake(name: str) -> str:
    step1 = re.sub(r"(.)([A-Z][a-z]+)", r"\1_\2", name)
    step2 = re.sub(r"([a-z0-9])([A-Z])", r"\1_\2", step1)
    return step2.lower()


def _normalize_columns(df: pd.DataFrame) -> pd.DataFrame:
    renamed = {c: _to_snake(c) for c in df.columns}
    return df.rename(columns=renamed)


def _zscore(series: pd.Series) -> pd.Series:
    std = float(series.std(ddof=0))
    if std == 0 or np.isnan(std):
        return pd.Series(np.zeros(len(series)), index=series.index)
    return (series - float(series.mean())) / std


def _confidence_weight(samples: pd.Series) -> pd.Series:
    return np.clip(np.sqrt(samples) / 10.0, 0.1, 1.0)


def _rate_ci_width(p: pd.Series, n: pd.Series) -> pd.Series:
    n_safe = n.clip(lower=1)
    return 1.96 * np.sqrt((p * (1.0 - p)) / n_safe)


def _parse_tier_num(tier_value: str) -> int:
    match = re.search(r"(\d+)$", str(tier_value))
    return int(match.group(1)) if match else 0


def _empty_mutation_scores() -> pd.DataFrame:
    cols = [
        "mutation_id",
        "mutation_name",
        "mutation_tier",
        "mutation_category",
        "tier_num",
        "eligible_samples",
        "picks",
        "pick_rate_eligible",
        "win_rate_when_picked",
        "win_rate_when_not_picked",
        "win_lift",
        "win_lift_shrunk",
        "avg_level",
        "avg_first_upgrade_round",
        "early_level_intensity",
        "reached_l2_rate",
        "reached_l3_rate",
        "reached_l5_rate",
        "confidence",
        "ci_width",
        "balance_score",
        "recommendation",
    ]
    return pd.DataFrame(columns=cols)


def _empty_mycovariant_scores() -> pd.DataFrame:
    cols = [
        "mycovariant_id",
        "mycovariant_name",
        "mycovariant_type",
        "eligible_samples",
        "picks",
        "pick_rate_eligible",
        "win_rate_when_picked",
        "win_rate_when_not_picked",
        "win_lift",
        "win_lift_shrunk",
        "avg_total_effect",
        "trigger_rate",
        "confidence",
        "ci_width",
        "balance_score",
        "recommendation",
    ]
    return pd.DataFrame(columns=cols)


def build_mutation_scores(players: pd.DataFrame, mutations: pd.DataFrame) -> pd.DataFrame:
    key_cols = ["game_index", "player_id"]
    if mutations.empty:
        return _empty_mutation_scores()

    mutation_defs = (
        mutations[["mutation_id", "mutation_name", "mutation_tier", "mutation_category"]]
        .drop_duplicates()
        .copy()
    )
    mutation_defs["tier_num"] = mutation_defs["mutation_tier"].map(_parse_tier_num)

    picks = (
        mutations[["game_index", "player_id", "mutation_id", "mutation_level", "first_upgrade_round"]]
        .drop_duplicates(subset=["game_index", "player_id", "mutation_id"])
        .copy()
    )

    player_max_tier = (
        mutations.assign(tier_num=mutations["mutation_tier"].map(_parse_tier_num))
        .groupby(key_cols, as_index=False)
        .agg(player_max_tier=("tier_num", "max"))
    )

    players_base = players[["game_index", "player_id", "is_winner"]].drop_duplicates().copy()
    players_base = players_base.merge(player_max_tier, on=key_cols, how="left")
    players_base["player_max_tier"] = players_base["player_max_tier"].fillna(0).astype(int)

    panel = players_base.assign(_k=1).merge(mutation_defs.assign(_k=1), on="_k", how="outer").drop(columns=["_k"])
    panel["eligible"] = (panel["tier_num"] == 1) | (panel["player_max_tier"] >= panel["tier_num"])
    panel = panel[panel["eligible"]].copy()

    panel = panel.merge(picks, on=["game_index", "player_id", "mutation_id"], how="left")
    panel["picked"] = panel["mutation_level"].notna()
    panel["mutation_level"] = panel["mutation_level"].fillna(0)

    picked_only = panel[panel["picked"]].copy()
    if not picked_only.empty:
        picked_only["early_level_intensity"] = picked_only["mutation_level"] / (picked_only["first_upgrade_round"].fillna(999) + 1.0)

    grouped = panel.groupby(["mutation_id", "mutation_name", "mutation_tier", "mutation_category", "tier_num"], as_index=False).agg(
        eligible_samples=("picked", "size"),
        picks=("picked", "sum"),
        win_rate_when_not_picked=("is_winner", lambda s: float(s.mean())),
    )

    picked_stats = picked_only.groupby("mutation_id", as_index=False).agg(
        win_rate_when_picked=("is_winner", "mean"),
        avg_level=("mutation_level", "mean"),
        avg_first_upgrade_round=("first_upgrade_round", "mean"),
        early_level_intensity=("early_level_intensity", "mean"),
        reached_l2_rate=("mutation_level", lambda s: float((s >= 2).mean())),
        reached_l3_rate=("mutation_level", lambda s: float((s >= 3).mean())),
        reached_l5_rate=("mutation_level", lambda s: float((s >= 5).mean())),
    )

    grouped = grouped.merge(picked_stats, on="mutation_id", how="left")
    grouped["win_rate_when_picked"] = grouped["win_rate_when_picked"].fillna(grouped["win_rate_when_not_picked"])
    grouped["avg_level"] = grouped["avg_level"].fillna(0.0)
    grouped["avg_first_upgrade_round"] = grouped["avg_first_upgrade_round"].fillna(np.nan)
    grouped["early_level_intensity"] = grouped["early_level_intensity"].fillna(0.0)
    grouped["reached_l2_rate"] = grouped["reached_l2_rate"].fillna(0.0)
    grouped["reached_l3_rate"] = grouped["reached_l3_rate"].fillna(0.0)
    grouped["reached_l5_rate"] = grouped["reached_l5_rate"].fillna(0.0)

    grouped["pick_rate_eligible"] = grouped["picks"] / grouped["eligible_samples"].clip(lower=1)
    grouped["win_lift"] = grouped["win_rate_when_picked"] - grouped["win_rate_when_not_picked"]

    prior_strength = 25.0
    grouped["win_lift_shrunk"] = grouped["win_lift"] * (grouped["picks"] / (grouped["picks"] + prior_strength))

    grouped["confidence"] = _confidence_weight(grouped["picks"])
    grouped["ci_width"] = _rate_ci_width(grouped["win_rate_when_picked"], grouped["picks"].clip(lower=1))

    score_raw = (
        0.40 * _zscore(grouped["win_lift_shrunk"])
        + 0.20 * _zscore(grouped["pick_rate_eligible"])
        + 0.20 * _zscore(grouped["early_level_intensity"])
        + 0.15 * _zscore(grouped["reached_l3_rate"])
        + 0.05 * _zscore(grouped["reached_l5_rate"])
    )

    grouped["balance_score"] = score_raw * grouped["confidence"] - grouped["ci_width"]
    grouped["recommendation"] = np.where(
        grouped["balance_score"] >= 0.50,
        "OP candidate",
        np.where(grouped["balance_score"] <= -0.50, "UP candidate", "Neutral"),
    )

    return grouped.sort_values("balance_score", ascending=False)


def build_mycovariant_scores(players: pd.DataFrame, mycovariants: pd.DataFrame) -> pd.DataFrame:
    key_cols = ["game_index", "player_id"]
    if mycovariants.empty:
        return _empty_mycovariant_scores()

    myco_defs = mycovariants[["mycovariant_id", "mycovariant_name", "mycovariant_type"]].drop_duplicates().copy()

    effect_by_pick = (
        mycovariants.groupby(["game_index", "player_id", "mycovariant_id"], as_index=False)
        .agg(total_effect=("effect_value", "sum"), triggered=("triggered", "max"))
    )

    myco_eligible_players = mycovariants[key_cols].drop_duplicates().copy()
    players_base = players[key_cols + ["is_winner"]].drop_duplicates().copy()
    players_base = players_base.merge(
        myco_eligible_players.assign(has_myco_phase=True),
        on=key_cols,
        how="left",
    )
    players_base = players_base[players_base["has_myco_phase"].fillna(False)].copy()

    panel = players_base.assign(_k=1).merge(myco_defs.assign(_k=1), on="_k", how="outer").drop(columns=["_k"])
    panel = panel.merge(effect_by_pick, on=["game_index", "player_id", "mycovariant_id"], how="left")
    panel["picked"] = panel["total_effect"].notna() | panel["triggered"].notna()
    panel["total_effect"] = panel["total_effect"].fillna(0)
    panel["triggered"] = panel["triggered"].fillna(False).astype(bool)

    grouped = panel.groupby(["mycovariant_id", "mycovariant_name", "mycovariant_type"], as_index=False).agg(
        eligible_samples=("picked", "size"),
        picks=("picked", "sum"),
        win_rate_when_not_picked=("is_winner", lambda s: float(s.mean())),
    )

    picked_stats = panel[panel["picked"]].groupby("mycovariant_id", as_index=False).agg(
        win_rate_when_picked=("is_winner", "mean"),
        avg_total_effect=("total_effect", "mean"),
        trigger_rate=("triggered", "mean"),
    )

    grouped = grouped.merge(picked_stats, on="mycovariant_id", how="left")
    grouped["win_rate_when_picked"] = grouped["win_rate_when_picked"].fillna(grouped["win_rate_when_not_picked"])
    grouped["avg_total_effect"] = grouped["avg_total_effect"].fillna(0.0)
    grouped["trigger_rate"] = grouped["trigger_rate"].fillna(0.0)

    grouped["pick_rate_eligible"] = grouped["picks"] / grouped["eligible_samples"].clip(lower=1)
    grouped["win_lift"] = grouped["win_rate_when_picked"] - grouped["win_rate_when_not_picked"]

    prior_strength = 20.0
    grouped["win_lift_shrunk"] = grouped["win_lift"] * (grouped["picks"] / (grouped["picks"] + prior_strength))

    grouped["confidence"] = _confidence_weight(grouped["picks"])
    grouped["ci_width"] = _rate_ci_width(grouped["win_rate_when_picked"], grouped["picks"].clip(lower=1))

    score_raw = (
        0.45 * _zscore(grouped["win_lift_shrunk"])
        + 0.25 * _zscore(grouped["pick_rate_eligible"])
        + 0.20 * _zscore(grouped["avg_total_effect"])
        + 0.10 * _zscore(grouped["trigger_rate"])
    )

    grouped["balance_score"] = score_raw * grouped["confidence"] - grouped["ci_width"]
    grouped["recommendation"] = np.where(
        grouped["balance_score"] >= 0.50,
        "OP candidate",
        np.where(grouped["balance_score"] <= -0.50, "UP candidate", "Neutral"),
    )

    return grouped.sort_values("balance_score", ascending=False)


def write_markdown_report(mutation_scores: pd.DataFrame, mycovariant_scores: pd.DataFrame, output_path: Path) -> None:
    top_mut_op = mutation_scores.head(10)
    top_mut_up = mutation_scores.tail(10).sort_values("balance_score")
    top_myco_op = mycovariant_scores.head(10)
    top_myco_up = mycovariant_scores.tail(10).sort_values("balance_score")

    def _table(df: pd.DataFrame, cols: list[str]) -> str:
        if df.empty:
            return "_No data_\n"
        return df[cols].to_markdown(index=False) + "\n"

    lines = [
        "# Balance Recommendations",
        "",
        "Scoring notes:",
        "- Uses eligibility-aware denominators instead of all player-games.",
        "- Uses shrinkage on win-lift for sparse/high-tier picks.",
        "- For mutations, includes low-tier timing/intensity metrics via first-upgrade timing and level milestones.",
        "",
        "## Mutations - OP Candidates",
        _table(top_mut_op, ["mutation_name", "mutation_tier", "mutation_category", "eligible_samples", "picks", "pick_rate_eligible", "win_lift_shrunk", "avg_level", "avg_first_upgrade_round", "early_level_intensity", "reached_l3_rate", "balance_score", "recommendation"]),
        "## Mutations - UP Candidates",
        _table(top_mut_up, ["mutation_name", "mutation_tier", "mutation_category", "eligible_samples", "picks", "pick_rate_eligible", "win_lift_shrunk", "avg_level", "avg_first_upgrade_round", "early_level_intensity", "reached_l3_rate", "balance_score", "recommendation"]),
        "## Mycovariants - OP Candidates",
        _table(top_myco_op, ["mycovariant_name", "mycovariant_type", "eligible_samples", "picks", "pick_rate_eligible", "win_lift_shrunk", "avg_total_effect", "trigger_rate", "balance_score", "recommendation"]),
        "## Mycovariants - UP Candidates",
        _table(top_myco_up, ["mycovariant_name", "mycovariant_type", "eligible_samples", "picks", "pick_rate_eligible", "win_lift_shrunk", "avg_total_effect", "trigger_rate", "balance_score", "recommendation"]),
    ]

    output_path.write_text("\n".join(lines), encoding="utf-8")


def main() -> None:
    parser = argparse.ArgumentParser(description="Analyze FungusToast simulation Parquet exports for OP/UP recommendations.")
    parser.add_argument("--run-folder", required=True, help="Path to one simulation export folder containing parquet files.")
    parser.add_argument("--output-dir", required=False, help="Output directory for analysis artifacts. Defaults to run folder.")
    args = parser.parse_args()

    run_folder = Path(args.run_folder)
    if not run_folder.exists():
        raise FileNotFoundError(f"Run folder not found: {run_folder}")

    output_dir = Path(args.output_dir) if args.output_dir else run_folder
    output_dir.mkdir(parents=True, exist_ok=True)

    players = pd.read_parquet(run_folder / "players.parquet")
    mutations = pd.read_parquet(run_folder / "mutations.parquet")
    mycovariants = pd.read_parquet(run_folder / "mycovariants.parquet")

    players = _normalize_columns(players)
    mutations = _normalize_columns(mutations)
    mycovariants = _normalize_columns(mycovariants)

    mutation_scores = build_mutation_scores(players, mutations)
    mycovariant_scores = build_mycovariant_scores(players, mycovariants)

    mutation_scores.to_csv(output_dir / "mutation_recommendations.csv", index=False)
    mycovariant_scores.to_csv(output_dir / "mycovariant_recommendations.csv", index=False)
    write_markdown_report(mutation_scores, mycovariant_scores, output_dir / "balance_recommendations.md")

    print(f"Analysis complete. Artifacts written to: {output_dir}")


if __name__ == "__main__":
    main()

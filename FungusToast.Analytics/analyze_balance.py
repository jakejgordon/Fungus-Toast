from __future__ import annotations

import argparse
from itertools import combinations
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
        "is_universal",
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
        "power_score",
        "balance_score",
        "recommendation",
    ]
    return pd.DataFrame(columns=cols)


def build_mutation_by_opponent_theme(players: pd.DataFrame, mutations: pd.DataFrame) -> pd.DataFrame:
    if players.empty or mutations.empty or "dominant_opponent_theme" not in players.columns:
        return pd.DataFrame(
            columns=[
                "dominant_opponent_theme",
                "mutation_id",
                "mutation_name",
                "eligible_samples",
                "picks",
                "pick_rate",
                "win_rate_when_picked",
                "win_rate_when_not_picked",
                "win_lift",
            ]
        )

    players_base = players[["game_index", "player_id", "is_winner", "dominant_opponent_theme"]].drop_duplicates().copy()
    players_base["dominant_opponent_theme"] = players_base["dominant_opponent_theme"].fillna("Unknown")
    mutation_defs = mutations[["mutation_id", "mutation_name"]].drop_duplicates().copy()

    player_totals = players_base.groupby("dominant_opponent_theme", as_index=False).agg(
        eligible_samples=("player_id", "size"),
        wins_total=("is_winner", "sum"),
    )

    picked = (
        mutations[["game_index", "player_id", "mutation_id", "mutation_name"]]
        .drop_duplicates(subset=["game_index", "player_id", "mutation_id"])
        .merge(players_base, on=["game_index", "player_id"], how="inner")
    )

    picked_stats = picked.groupby(["dominant_opponent_theme", "mutation_id", "mutation_name"], as_index=False).agg(
        picks=("player_id", "size"),
        wins_picked=("is_winner", "sum"),
    )
    picked_stats["win_rate_when_picked"] = picked_stats["wins_picked"] / picked_stats["picks"].clip(lower=1)

    themes = player_totals[["dominant_opponent_theme"]].drop_duplicates().copy()
    panel = themes.assign(_k=1).merge(mutation_defs.assign(_k=1), on="_k", how="outer").drop(columns=["_k"])
    panel = panel.merge(player_totals, on="dominant_opponent_theme", how="left")
    panel = panel.merge(picked_stats, on=["dominant_opponent_theme", "mutation_id", "mutation_name"], how="left")

    panel["picks"] = panel["picks"].fillna(0).astype(int)
    panel["wins_picked"] = panel["wins_picked"].fillna(0.0)
    panel["eligible_samples"] = panel["eligible_samples"].fillna(0).astype(int)
    panel["wins_total"] = panel["wins_total"].fillna(0.0)
    panel["win_rate_when_picked"] = panel["win_rate_when_picked"].fillna(panel["wins_total"] / panel["eligible_samples"].clip(lower=1))

    not_picked_samples = (panel["eligible_samples"] - panel["picks"]).clip(lower=0)
    not_picked_wins = panel["wins_total"] - panel["wins_picked"]

    panel["win_rate_when_not_picked"] = np.where(
        not_picked_samples > 0,
        not_picked_wins / not_picked_samples.clip(lower=1),
        panel["win_rate_when_picked"],
    )
    panel["pick_rate"] = panel["picks"] / panel["eligible_samples"].clip(lower=1)
    panel["win_lift"] = panel["win_rate_when_picked"] - panel["win_rate_when_not_picked"]

    return panel.sort_values(["dominant_opponent_theme", "win_lift"], ascending=[True, False])


def build_mutation_synergies(players: pd.DataFrame, mutations: pd.DataFrame, min_pair_samples: int = 10) -> pd.DataFrame:
    if players.empty or mutations.empty:
        return pd.DataFrame(
            columns=[
                "mutation_a_id",
                "mutation_a_name",
                "mutation_b_id",
                "mutation_b_name",
                "pair_samples",
                "pair_win_rate",
                "pair_pick_rate",
                "win_lift_vs_global",
                "synergy_score",
            ]
        )

    players_base = players[["game_index", "player_id", "is_winner"]].drop_duplicates().copy()
    key_to_win = {
        (int(row.game_index), int(row.player_id)): bool(row.is_winner)
        for row in players_base.itertuples(index=False)
    }
    total_player_games = len(players_base)
    global_win_rate = float(players_base["is_winner"].mean()) if total_player_games > 0 else 0.0

    picked = mutations[["game_index", "player_id", "mutation_id", "mutation_name"]].drop_duplicates(
        subset=["game_index", "player_id", "mutation_id"]
    )
    name_by_id = dict(
        picked[["mutation_id", "mutation_name"]].drop_duplicates().itertuples(index=False, name=None)
    )

    pair_counts: dict[tuple[int, int], int] = {}
    pair_wins: dict[tuple[int, int], int] = {}

    for row in picked.groupby(["game_index", "player_id"])["mutation_id"]:
        (game_index, player_id), mutation_ids = row
        unique_ids = sorted(set(int(mid) for mid in mutation_ids.tolist()))
        if len(unique_ids) < 2:
            continue

        is_winner = key_to_win.get((int(game_index), int(player_id)), False)
        for a, b in combinations(unique_ids, 2):
            key = (a, b)
            pair_counts[key] = pair_counts.get(key, 0) + 1
            if is_winner:
                pair_wins[key] = pair_wins.get(key, 0) + 1

    rows = []
    for (a, b), samples in pair_counts.items():
        if samples < min_pair_samples:
            continue

        wins = pair_wins.get((a, b), 0)
        pair_win_rate = wins / samples
        pair_pick_rate = samples / max(total_player_games, 1)
        win_lift = pair_win_rate - global_win_rate
        rows.append(
            {
                "mutation_a_id": a,
                "mutation_a_name": name_by_id.get(a, f"Mutation {a}"),
                "mutation_b_id": b,
                "mutation_b_name": name_by_id.get(b, f"Mutation {b}"),
                "pair_samples": samples,
                "pair_win_rate": pair_win_rate,
                "pair_pick_rate": pair_pick_rate,
                "win_lift_vs_global": win_lift,
                "synergy_score": win_lift * np.sqrt(samples),
            }
        )

    if not rows:
        return pd.DataFrame(
            columns=[
                "mutation_a_id",
                "mutation_a_name",
                "mutation_b_id",
                "mutation_b_name",
                "pair_samples",
                "pair_win_rate",
                "pair_pick_rate",
                "win_lift_vs_global",
                "synergy_score",
            ]
        )

    return pd.DataFrame(rows).sort_values("synergy_score", ascending=False)


def build_mycovariant_mutation_interactions(
    players: pd.DataFrame,
    mutations: pd.DataFrame,
    mycovariants: pd.DataFrame,
    min_combo_samples: int = 10,
) -> pd.DataFrame:
    if players.empty or mutations.empty or mycovariants.empty:
        return pd.DataFrame(
            columns=[
                "mycovariant_id",
                "mycovariant_name",
                "mutation_id",
                "mutation_name",
                "combo_samples",
                "combo_win_rate",
                "combo_lift_vs_global",
                "interaction_score",
            ]
        )

    players_base = players[["game_index", "player_id", "is_winner"]].drop_duplicates().copy()
    global_win_rate = float(players_base["is_winner"].mean()) if len(players_base) > 0 else 0.0

    mutation_presence = mutations[["game_index", "player_id", "mutation_id", "mutation_name"]].drop_duplicates(
        subset=["game_index", "player_id", "mutation_id"]
    )
    myco_presence = mycovariants[["game_index", "player_id", "mycovariant_id", "mycovariant_name"]].drop_duplicates(
        subset=["game_index", "player_id", "mycovariant_id"]
    )

    combos = mutation_presence.merge(myco_presence, on=["game_index", "player_id"], how="inner")
    combos = combos.merge(players_base, on=["game_index", "player_id"], how="inner")

    if combos.empty:
        return pd.DataFrame(
            columns=[
                "mycovariant_id",
                "mycovariant_name",
                "mutation_id",
                "mutation_name",
                "combo_samples",
                "combo_win_rate",
                "combo_lift_vs_global",
                "interaction_score",
            ]
        )

    grouped = combos.groupby(["mycovariant_id", "mycovariant_name", "mutation_id", "mutation_name"], as_index=False).agg(
        combo_samples=("player_id", "size"),
        combo_win_rate=("is_winner", "mean"),
    )
    grouped = grouped[grouped["combo_samples"] >= min_combo_samples].copy()

    if grouped.empty:
        return grouped

    grouped["combo_lift_vs_global"] = grouped["combo_win_rate"] - global_win_rate
    grouped["interaction_score"] = grouped["combo_lift_vs_global"] * np.sqrt(grouped["combo_samples"])
    return grouped.sort_values("interaction_score", ascending=False)


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

    if "is_universal" not in mycovariants.columns:
        mycovariants["is_universal"] = False

    myco_defs = mycovariants[["mycovariant_id", "mycovariant_name", "mycovariant_type", "is_universal"]].drop_duplicates().copy()

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

    grouped = panel.groupby(["mycovariant_id", "mycovariant_name", "mycovariant_type", "is_universal"], as_index=False).agg(
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

    power_score_raw = (
        0.55 * _zscore(grouped["win_lift_shrunk"])
        + 0.30 * _zscore(grouped["avg_total_effect"])
        + 0.15 * _zscore(grouped["trigger_rate"])
    )

    grouped["power_score"] = power_score_raw * grouped["confidence"] - grouped["ci_width"]
    grouped["balance_score"] = score_raw * grouped["confidence"] - grouped["ci_width"]
    grouped["recommendation"] = np.where(
        grouped["balance_score"] >= 0.50,
        "OP candidate",
        np.where(grouped["balance_score"] <= -0.50, "UP candidate", "Neutral"),
    )

    return grouped.sort_values("balance_score", ascending=False)


def _filter_scores_for_report(
    df: pd.DataFrame,
    min_confidence: float,
    min_picks: int,
    min_eligible_samples: int,
) -> pd.DataFrame:
    if df.empty:
        return df

    return df[
        (df["confidence"] >= min_confidence)
        & (df["picks"] >= min_picks)
        & (df["eligible_samples"] >= min_eligible_samples)
    ].copy()


def write_markdown_report(
    mutation_scores: pd.DataFrame,
    mycovariant_scores: pd.DataFrame,
    mutation_by_opponent_theme: pd.DataFrame,
    mutation_synergies: pd.DataFrame,
    myco_mutation_interactions: pd.DataFrame,
    output_path: Path,
    min_confidence: float,
    min_picks: int,
    min_eligible_samples: int,
) -> None:
    report_mutations = _filter_scores_for_report(
        mutation_scores,
        min_confidence=min_confidence,
        min_picks=min_picks,
        min_eligible_samples=min_eligible_samples,
    )
    report_mycovariants = _filter_scores_for_report(
        mycovariant_scores,
        min_confidence=min_confidence,
        min_picks=min_picks,
        min_eligible_samples=min_eligible_samples,
    )

    top_mut_op = report_mutations.head(10)
    top_mut_up = report_mutations.tail(10).sort_values("balance_score")
    top_myco_op = report_mycovariants.head(10)
    top_myco_up = report_mycovariants.tail(10).sort_values("balance_score")
    top_theme_sensitive = mutation_by_opponent_theme.sort_values("win_lift", ascending=False).head(12)
    top_synergies = mutation_synergies.head(12)
    top_interactions = myco_mutation_interactions.head(12)

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
        f"- Report filtering: confidence >= {min_confidence:.2f}, picks >= {min_picks}, eligible_samples >= {min_eligible_samples}.",
        "",
        "## Mutations - OP Candidates",
        _table(top_mut_op, ["mutation_name", "mutation_tier", "mutation_category", "eligible_samples", "picks", "pick_rate_eligible", "win_lift_shrunk", "avg_level", "avg_first_upgrade_round", "early_level_intensity", "reached_l3_rate", "balance_score", "recommendation"]),
        "## Mutations - UP Candidates",
        _table(top_mut_up, ["mutation_name", "mutation_tier", "mutation_category", "eligible_samples", "picks", "pick_rate_eligible", "win_lift_shrunk", "avg_level", "avg_first_upgrade_round", "early_level_intensity", "reached_l3_rate", "balance_score", "recommendation"]),
        "## Mycovariants - OP Candidates",
        _table(top_myco_op, ["mycovariant_name", "mycovariant_type", "is_universal", "eligible_samples", "picks", "pick_rate_eligible", "win_lift_shrunk", "avg_total_effect", "trigger_rate", "power_score", "balance_score", "recommendation"]),
        "## Mycovariants - UP Candidates",
        _table(top_myco_up, ["mycovariant_name", "mycovariant_type", "is_universal", "eligible_samples", "picks", "pick_rate_eligible", "win_lift_shrunk", "avg_total_effect", "trigger_rate", "power_score", "balance_score", "recommendation"]),
        "## Mutations by Opponent Theme (Highest Lift)",
        _table(top_theme_sensitive, ["dominant_opponent_theme", "mutation_name", "eligible_samples", "picks", "pick_rate", "win_rate_when_picked", "win_rate_when_not_picked", "win_lift"]),
        "## Mutation Synergy Candidates",
        _table(top_synergies, ["mutation_a_name", "mutation_b_name", "pair_samples", "pair_win_rate", "win_lift_vs_global", "synergy_score"]),
        "## Mycovariant-Mutation Interaction Candidates",
        _table(top_interactions, ["mycovariant_name", "mutation_name", "combo_samples", "combo_win_rate", "combo_lift_vs_global", "interaction_score"]),
    ]

    output_path.write_text("\n".join(lines), encoding="utf-8")


def main() -> None:
    parser = argparse.ArgumentParser(description="Analyze FungusToast simulation Parquet exports for OP/UP recommendations.")
    parser.add_argument("--run-folder", required=True, help="Path to one simulation export folder containing parquet files.")
    parser.add_argument("--output-dir", required=False, help="Output directory for analysis artifacts. Defaults to run folder.")
    parser.add_argument("--min-confidence", type=float, default=0.4, help="Minimum confidence required for markdown recommendations.")
    parser.add_argument("--min-picks", type=int, default=15, help="Minimum picks required for markdown recommendations.")
    parser.add_argument("--min-eligible-samples", type=int, default=50, help="Minimum eligible samples required for markdown recommendations.")
    parser.add_argument("--min-pair-samples", type=int, default=10, help="Minimum pair samples for mutation synergy rows.")
    parser.add_argument("--min-combo-samples", type=int, default=10, help="Minimum samples for mycovariant-mutation interaction rows.")
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
    mutation_by_opponent_theme = build_mutation_by_opponent_theme(players, mutations)
    mutation_synergies = build_mutation_synergies(players, mutations, min_pair_samples=args.min_pair_samples)
    myco_mutation_interactions = build_mycovariant_mutation_interactions(
        players,
        mutations,
        mycovariants,
        min_combo_samples=args.min_combo_samples,
    )

    mutation_scores.to_csv(output_dir / "mutation_recommendations.csv", index=False)
    mycovariant_scores.to_csv(output_dir / "mycovariant_recommendations.csv", index=False)
    mutation_by_opponent_theme.to_csv(output_dir / "mutation_by_opponent_theme.csv", index=False)
    mutation_synergies.to_csv(output_dir / "mutation_synergies.csv", index=False)
    myco_mutation_interactions.to_csv(output_dir / "mycovariant_mutation_interactions.csv", index=False)
    write_markdown_report(
        mutation_scores,
        mycovariant_scores,
        mutation_by_opponent_theme,
        mutation_synergies,
        myco_mutation_interactions,
        output_dir / "balance_recommendations.md",
        min_confidence=args.min_confidence,
        min_picks=args.min_picks,
        min_eligible_samples=args.min_eligible_samples,
    )

    print(f"Analysis complete. Artifacts written to: {output_dir}")


if __name__ == "__main__":
    main()

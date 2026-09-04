from __future__ import annotations

import argparse
import json
from itertools import combinations
from pathlib import Path
import numpy as np
import pandas as pd
import re


ANALYSIS_VERSION = "fungus-toast.analysis.v2"


def _to_snake(name: str) -> str:
    step1 = re.sub(r"(.)([A-Z][a-z]+)", r"\1_\2", name)
    step2 = re.sub(r"([a-z0-9])([A-Z])", r"\1_\2", step1)
    return step2.lower()


def _normalize_columns(df: pd.DataFrame) -> pd.DataFrame:
    renamed = {c: _to_snake(c) for c in df.columns}
    return df.rename(columns=renamed)


def _ensure_win_credit(players: pd.DataFrame) -> pd.DataFrame:
    if "win_credit" in players.columns:
        return players
    required = {"final_rank", "players_tied_at_final_rank"}
    missing = sorted(required.difference(players.columns))
    if missing:
        raise ValueError(f"players.parquet cannot derive tie-aware win credit; missing: {', '.join(missing)}")
    players = players.copy()
    tie_count = players["players_tied_at_final_rank"].clip(lower=1)
    players["win_credit"] = np.where(players["final_rank"] == 1, 1.0 / tie_count, 0.0)
    return players


def _ensure_strategy_identity(df: pd.DataFrame) -> pd.DataFrame:
    if df.empty or "strategy_name" not in df.columns:
        return df
    df = df.copy()
    if "strategy_id" not in df.columns:
        slug = df["strategy_name"].astype(str).str.lower().str.replace(r"[^a-z0-9]+", "-", regex=True).str.strip("-")
        df["strategy_id"] = "legacy.unversioned." + slug + ".v1"
    if "strategy_definition_fingerprint" not in df.columns:
        df["strategy_definition_fingerprint"] = "legacy-artifact-unavailable"
    return df


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


def _wilson_interval(successes: pd.Series, samples: pd.Series) -> tuple[pd.Series, pd.Series]:
    n = samples.clip(lower=1).astype(float)
    p = successes / n
    z = 1.96
    denominator = 1.0 + z * z / n
    center = (p + z * z / (2.0 * n)) / denominator
    margin = z * np.sqrt((p * (1.0 - p) + z * z / (4.0 * n)) / n) / denominator
    return center - margin, center + margin


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


def _empty_nutrient_summary() -> pd.DataFrame:
    cols = [
        "strategy_name",
        "strategy_id",
        "strategy_definition_fingerprint",
        "strategy_theme",
        "samples",
        "win_rate",
        "avg_nutrient_claims",
        "avg_nutrient_mutation_points",
        "avg_claimed_cluster_size",
        "avg_nutrient_mp_share_of_income",
        "players_with_nutrient_claims_rate",
    ]
    return pd.DataFrame(columns=cols)


def _empty_player_summary() -> pd.DataFrame:
    cols = [
        "player",
        "strategy_id",
        "strategy_definition_fingerprint",
        "strategy_theme",
        "games",
        "wins",
        "win_pct",
        "avg_living_cells",
        "avg_normalized_board_share",
        "win_rate_surplus",
        "avg_final_rank",
        "avg_normalized_rank",
        "normalized_board_share_ci95_low", "normalized_board_share_ci95_high",
        "win_rate_surplus_ci95_low", "win_rate_surplus_ci95_high",
        "normalized_rank_ci95_low", "normalized_rank_ci95_high",
        "board_share_effect_size", "rank_effect_size",
        "context_count", "worst_context_normalized_board_share", "board_share_context_range",
        "avg_dead_cells",
        "avg_toxins",
    ]
    return pd.DataFrame(columns=cols)


def _empty_growth_source_summary() -> pd.DataFrame:
    cols = [
        "strategy_id",
        "strategy_definition_fingerprint",
        "player",
        "total_living",
        "growth_source",
        "count",
        "pct_from_growth_source",
    ]
    return pd.DataFrame(columns=cols)


def _prepare_outcome_metrics(players: pd.DataFrame) -> pd.DataFrame:
    players = _ensure_win_credit(_ensure_strategy_identity(players)).copy()
    outcome_group_columns = ["condition_id", "game_index"]
    if "total_living_cells" not in players.columns:
        players["total_living_cells"] = players.groupby(outcome_group_columns)["living_cells"].transform("sum")
    if "player_count" not in players.columns:
        players["player_count"] = players.groupby(outcome_group_columns)["player_id"].transform("size")
    if "final_rank" not in players.columns:
        players["final_rank"] = players.groupby(outcome_group_columns)["living_cells"].rank(method="min", ascending=False)
    players["normalized_board_share"] = np.where(
        players["total_living_cells"] > 0,
        players["living_cells"] * players["player_count"] / players["total_living_cells"],
        0.0,
    )
    players["normalized_rank"] = np.where(
        players["player_count"] > 1,
        (players["player_count"] - players["final_rank"]) / (players["player_count"] - 1),
        1.0,
    )
    return players


def build_paired_comparison(control: pd.DataFrame, treatment: pd.DataFrame) -> pd.DataFrame:
    required = {
        "pairing_group_id", "pair_id", "condition_id", "game_index", "game_seed",
        "assigned_slot", "player_id", "player_count", "strategy_name", "strategy_id",
        "strategy_definition_fingerprint", "living_cells", "win_credit",
    }
    for label, frame in (("control", control), ("treatment", treatment)):
        missing = sorted(required.difference(frame.columns))
        if missing:
            raise ValueError(f"{label} players.parquet is missing paired-analysis columns: {', '.join(missing)}")
        if frame.empty:
            raise ValueError(f"{label} players.parquet is empty")
        if frame["pair_id"].isna().any() or (frame["pair_id"].astype(str).str.strip() == "").any():
            raise ValueError(f"{label} players.parquet contains blank pair_id values")
        if frame.duplicated(["pair_id", "assigned_slot"]).any():
            raise ValueError(f"{label} players.parquet contains duplicate pair_id/assigned_slot rows")

    control_groups = set(control["pairing_group_id"].astype(str))
    treatment_groups = set(treatment["pairing_group_id"].astype(str))
    if len(control_groups) != 1 or control_groups != treatment_groups or "" in control_groups:
        raise ValueError("control and treatment must share one non-blank pairing_group_id")

    control_metrics = _prepare_outcome_metrics(control)
    treatment_metrics = _prepare_outcome_metrics(treatment)
    keys = ["pair_id", "assigned_slot"]
    merged = control_metrics.merge(
        treatment_metrics,
        on=keys,
        how="outer",
        suffixes=("_control", "_treatment"),
        indicator=True,
        validate="one_to_one",
    )
    if not (merged["_merge"] == "both").all():
        missing_count = int((merged["_merge"] != "both").sum())
        raise ValueError(f"paired analysis requires complete pairs; {missing_count} slot-pairs are unmatched")

    invariant_columns = ["game_seed", "player_count", "starting_x", "starting_y", "board_geometry_fingerprint", "random_stream_contract_version"]
    for column in invariant_columns:
        left = f"{column}_control"
        right = f"{column}_treatment"
        if left in merged.columns and right in merged.columns and not merged[left].equals(merged[right]):
            raise ValueError(f"paired control/treatment mismatch in {column}")

    identity_columns = [
        "strategy_id_control", "strategy_definition_fingerprint_control", "strategy_name_control",
        "strategy_id_treatment", "strategy_definition_fingerprint_treatment", "strategy_name_treatment",
    ]
    rows = []
    for identity, group in merged.groupby(identity_columns, dropna=False, sort=True):
        row = dict(zip(identity_columns, identity))
        row["pairing_group_id"] = next(iter(control_groups))
        row["pairs"] = len(group)
        for metric in ("normalized_board_share", "normalized_rank", "win_credit"):
            control_values = group[f"{metric}_control"].astype(float)
            treatment_values = group[f"{metric}_treatment"].astype(float)
            differences = treatment_values - control_values
            samples = len(differences)
            difference_std = float(differences.std(ddof=1)) if samples > 1 else 0.0
            margin = 1.96 * difference_std / np.sqrt(samples) if samples > 0 else np.nan
            can_correlate = samples > 1 and float(control_values.std(ddof=1)) > 0 and float(treatment_values.std(ddof=1)) > 0
            correlation = float(control_values.corr(treatment_values)) if can_correlate else np.nan
            unpaired_variance = float(control_values.var(ddof=1) + treatment_values.var(ddof=1)) if samples > 1 else np.nan
            paired_variance = float(differences.var(ddof=1)) if samples > 1 else np.nan
            if samples <= 1 or np.isnan(paired_variance):
                variance_ratio = np.nan
            elif paired_variance <= np.finfo(float).eps:
                variance_ratio = np.inf
            else:
                variance_ratio = unpaired_variance / paired_variance
            row[f"control_mean_{metric}"] = float(control_values.mean())
            row[f"treatment_mean_{metric}"] = float(treatment_values.mean())
            row[f"paired_difference_{metric}"] = float(differences.mean())
            row[f"paired_difference_{metric}_ci95_low"] = float(differences.mean() - margin)
            row[f"paired_difference_{metric}_ci95_high"] = float(differences.mean() + margin)
            row[f"observed_correlation_{metric}"] = correlation
            row[f"paired_vs_unpaired_variance_ratio_{metric}"] = variance_ratio
        rows.append(row)
    return pd.DataFrame(rows)


def build_preregistered_verdict(
    paired_summary: pd.DataFrame,
    control_manifest: dict,
    treatment_manifest: dict,
) -> dict:
    control_budget = control_manifest.get("totalGameBudget")
    treatment_budget = treatment_manifest.get("totalGameBudget")
    if not isinstance(control_budget, int) or control_budget != treatment_budget:
        raise ValueError("control and treatment must declare the same total game budget")
    if control_manifest.get("runtimeBudgetSeconds") != treatment_manifest.get("runtimeBudgetSeconds"):
        raise ValueError("control and treatment must declare the same runtime budget")
    control_sampling = control_manifest.get("sampling", {})
    treatment_sampling = treatment_manifest.get("sampling", {})
    if control_sampling.get("completionStatus") != "complete" or treatment_sampling.get("completionStatus") != "complete":
        raise ValueError("a verdict requires complete control and treatment artifacts")
    completed_games = int(control_sampling.get("gamesCompleted", 0)) + int(treatment_sampling.get("gamesCompleted", 0))
    if completed_games > control_budget:
        raise ValueError(
            f"combined control/treatment games ({completed_games}) exceed the declared total game budget ({control_budget})"
        )
    control_analysis = control_manifest.get("analysis")
    treatment_analysis = treatment_manifest.get("analysis")
    if not control_analysis or not treatment_analysis:
        raise ValueError("both resolved manifests must contain an analysis plan before a verdict can be issued")
    if control_analysis != treatment_analysis:
        raise ValueError("control and treatment analysis plans do not match")
    if control_analysis.get("analysisVersion") != ANALYSIS_VERSION:
        raise ValueError(f"unsupported preregistered analysis version: {control_analysis.get('analysisVersion')}")
    evidence_stage = control_analysis.get("evidenceStage")
    minimum_pairs_by_stage = {"comparison": 50, "holdout": 100}
    if evidence_stage not in minimum_pairs_by_stage:
        raise ValueError("a verdict requires preregistered comparison or holdout evidence")
    hypothesis = control_analysis.get("hypothesis")
    if not hypothesis:
        raise ValueError("no preregistered hypothesis exists; refusing to issue a verdict")
    if hypothesis.get("estimand") != "pairedMeanDifference":
        raise ValueError("only pairedMeanDifference hypotheses are supported")

    context_id = hypothesis.get("primaryContextId")
    if set(paired_summary["pairing_group_id"].astype(str)) != {context_id}:
        raise ValueError("paired results do not match the preregistered primary context")
    target_id = hypothesis.get("targetStrategyId")
    target_rows = paired_summary[
        (paired_summary["strategy_id_control"] == target_id)
        & (paired_summary["strategy_id_treatment"] == target_id)
    ]
    if len(target_rows) != 1:
        raise ValueError(f"preregistered target strategy '{target_id}' did not resolve to exactly one paired row")

    metric_names = {
        "normalizedBoardShare": "normalized_board_share",
        "normalizedRank": "normalized_rank",
        "winCredit": "win_credit",
    }
    declared_metric = hypothesis.get("primaryMetric")
    if declared_metric not in metric_names:
        raise ValueError(f"unsupported preregistered primary metric: {declared_metric}")
    metric = metric_names[declared_metric]
    row = target_rows.iloc[0]
    minimum_pairs = minimum_pairs_by_stage[evidence_stage]
    if int(row["pairs"]) < minimum_pairs:
        raise ValueError(
            f"preregistered {evidence_stage} verdict requires {minimum_pairs} complete pairs; found {int(row['pairs'])}"
        )
    estimate = float(row[f"paired_difference_{metric}"])
    ci_low = float(row[f"paired_difference_{metric}_ci95_low"])
    ci_high = float(row[f"paired_difference_{metric}_ci95_high"])
    margin = float(hypothesis.get("margin"))
    direction = hypothesis.get("direction")
    if direction == "increase":
        supported = ci_low > margin
    elif direction == "decrease":
        supported = ci_high < -margin
    elif direction == "nonInferiority":
        supported = ci_low > -margin
    else:
        raise ValueError(f"unsupported preregistered direction: {direction}")

    return {
        "analysis_version": ANALYSIS_VERSION,
        "hypothesis_id": hypothesis.get("hypothesisId"),
        "primary_context_id": context_id,
        "target_strategy_id": target_id,
        "primary_metric": declared_metric,
        "estimand": hypothesis.get("estimand"),
        "direction": direction,
        "evidence_stage": evidence_stage,
        "margin": margin,
        "total_game_budget": control_budget,
        "combined_games_completed": completed_games,
        "pairs": int(row["pairs"]),
        "estimate": estimate,
        "ci95_low": ci_low,
        "ci95_high": ci_high,
        "verdict": "supported" if supported else "not_supported",
    }


def build_player_summary(players: pd.DataFrame) -> pd.DataFrame:
    players = _ensure_strategy_identity(players)
    players = _ensure_strategy_identity(players)
    required_columns = {
        "strategy_name",
        "strategy_id",
        "strategy_definition_fingerprint",
        "strategy_theme",
        "condition_id",
        "game_index",
        "win_credit",
        "living_cells",
        "dead_cells",
        "end_game_toxin_cells",
    }
    if players.empty:
        return _empty_player_summary()
    missing_columns = sorted(required_columns.difference(players.columns))
    if missing_columns:
        raise ValueError(f"players.parquet is missing required player-summary columns: {', '.join(missing_columns)}")

    outcome_group_columns = ["condition_id", "game_index"]
    metrics = _prepare_outcome_metrics(players)
    metrics["win_rate_surplus"] = metrics["win_credit"].astype(float) - 1.0 / metrics["player_count"].clip(lower=1)

    identity_keys = ["strategy_id", "strategy_definition_fingerprint"]
    grouped = metrics.groupby(identity_keys, as_index=False).agg(
        strategy_name=("strategy_name", "first"),
        strategy_theme=("strategy_theme", "first"),
        games=("game_index", "count"),
        wins=("win_credit", "sum"),
        avg_living_cells=("living_cells", "mean"),
        avg_normalized_board_share=("normalized_board_share", "mean"),
        win_rate_surplus=("win_rate_surplus", "mean"),
        avg_final_rank=("final_rank", "mean"),
        avg_normalized_rank=("normalized_rank", "mean"),
        avg_dead_cells=("dead_cells", "mean"),
        avg_toxins=("end_game_toxin_cells", "mean"),
    )
    grouped["player"] = grouped["strategy_name"]
    grouped["win_pct"] = grouped["wins"] / grouped["games"].clip(lower=1) * 100.0
    grouped["normalized_board_share_ci95_low"] = grouped["avg_normalized_board_share"] - 1.96 * metrics.groupby(identity_keys)["normalized_board_share"].std().fillna(0).to_numpy() / np.sqrt(grouped["games"])
    grouped["normalized_board_share_ci95_high"] = grouped["avg_normalized_board_share"] + 1.96 * metrics.groupby(identity_keys)["normalized_board_share"].std().fillna(0).to_numpy() / np.sqrt(grouped["games"])
    win_low, win_high = _wilson_interval(grouped["wins"], grouped["games"])
    equal_expectation = metrics.groupby(identity_keys)["player_count"].apply(lambda counts: (1.0 / counts).mean()).to_numpy()
    grouped["win_rate_surplus_ci95_low"] = win_low - equal_expectation
    grouped["win_rate_surplus_ci95_high"] = win_high - equal_expectation
    rank_std = metrics.groupby(identity_keys)["normalized_rank"].std().fillna(0).to_numpy()
    grouped["normalized_rank_ci95_low"] = grouped["avg_normalized_rank"] - 1.96 * rank_std / np.sqrt(grouped["games"])
    grouped["normalized_rank_ci95_high"] = grouped["avg_normalized_rank"] + 1.96 * rank_std / np.sqrt(grouped["games"])
    share_std = metrics.groupby(identity_keys)["normalized_board_share"].std().fillna(0).to_numpy()
    grouped["board_share_effect_size"] = np.where(share_std > 0, (grouped["avg_normalized_board_share"] - 1.0) / share_std, 0.0)
    grouped["rank_effect_size"] = np.where(rank_std > 0, (grouped["avg_normalized_rank"] - 0.5) / rank_std, 0.0)
    context_key = "condition_id"
    context = metrics.groupby(identity_keys + [context_key], as_index=False).agg(context_share=("normalized_board_share", "mean"))
    robustness = context.groupby(identity_keys, as_index=False).agg(context_count=(context_key, "count"), worst_context_normalized_board_share=("context_share", "min"), best_context_normalized_board_share=("context_share", "max"))
    robustness["board_share_context_range"] = robustness["best_context_normalized_board_share"] - robustness["worst_context_normalized_board_share"]
    grouped = grouped.merge(robustness.drop(columns=["best_context_normalized_board_share"]), on=identity_keys, how="left")

    ordered = grouped[
        [
            "player",
            "strategy_id",
            "strategy_definition_fingerprint",
            "strategy_theme",
            "games",
            "wins",
            "win_pct",
            "avg_living_cells",
            "avg_normalized_board_share",
            "win_rate_surplus",
            "avg_final_rank",
            "avg_normalized_rank",
            "normalized_board_share_ci95_low", "normalized_board_share_ci95_high",
            "win_rate_surplus_ci95_low", "win_rate_surplus_ci95_high",
            "normalized_rank_ci95_low", "normalized_rank_ci95_high",
            "board_share_effect_size", "rank_effect_size",
            "context_count", "worst_context_normalized_board_share", "board_share_context_range",
            "avg_dead_cells",
            "avg_toxins",
        ]
    ].sort_values(["win_pct", "avg_living_cells", "player"], ascending=[False, False, True])

    return ordered.reset_index(drop=True)


def build_growth_source_summary(players: pd.DataFrame, living_cell_sources: pd.DataFrame) -> pd.DataFrame:
    players = _ensure_strategy_identity(players)
    living_cell_sources = _ensure_strategy_identity(living_cell_sources)
    players = _ensure_strategy_identity(players)
    living_cell_sources = _ensure_strategy_identity(living_cell_sources)
    required_player_columns = {"strategy_id", "strategy_definition_fingerprint", "strategy_name", "living_cells"}
    required_source_columns = {"strategy_id", "strategy_definition_fingerprint", "strategy_name", "growth_source_display_name", "living_cell_count"}
    if (
        players.empty
        or living_cell_sources.empty
        or not required_player_columns.issubset(players.columns)
        or not required_source_columns.issubset(living_cell_sources.columns)
    ):
        return _empty_growth_source_summary()

    identity_keys = ["strategy_id", "strategy_definition_fingerprint"]
    total_living = players.groupby(identity_keys, as_index=False).agg(total_living=("living_cells", "sum"))
    grouped = living_cell_sources.groupby(identity_keys + ["growth_source_display_name"], as_index=False).agg(
        strategy_name=("strategy_name", "first"), count=("living_cell_count", "sum")
    )
    grouped = grouped.merge(total_living, on=identity_keys, how="left")
    grouped["total_living"] = grouped["total_living"].fillna(0)
    grouped["pct_from_growth_source"] = np.where(
        grouped["total_living"] > 0,
        grouped["count"] / grouped["total_living"] * 100.0,
        0.0,
    )
    grouped["player"] = grouped["strategy_name"]
    grouped["growth_source"] = grouped["growth_source_display_name"]

    ordered = grouped[
        ["strategy_id", "strategy_definition_fingerprint", "player", "total_living", "growth_source", "count", "pct_from_growth_source"]
    ].sort_values(["total_living", "count", "player", "growth_source"], ascending=[False, False, True, True])

    return ordered.reset_index(drop=True)


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

    players_base = players[["game_index", "player_id", "win_credit", "dominant_opponent_theme"]].drop_duplicates().copy()
    players_base["dominant_opponent_theme"] = players_base["dominant_opponent_theme"].fillna("Unknown")
    mutation_defs = mutations[["mutation_id", "mutation_name"]].drop_duplicates().copy()

    player_totals = players_base.groupby("dominant_opponent_theme", as_index=False).agg(
        eligible_samples=("player_id", "size"),
        wins_total=("win_credit", "sum"),
    )

    picked = (
        mutations[["game_index", "player_id", "mutation_id", "mutation_name"]]
        .drop_duplicates(subset=["game_index", "player_id", "mutation_id"])
        .merge(players_base, on=["game_index", "player_id"], how="inner")
    )

    picked_stats = picked.groupby(["dominant_opponent_theme", "mutation_id", "mutation_name"], as_index=False).agg(
        picks=("player_id", "size"),
        wins_picked=("win_credit", "sum"),
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

    players_base = players[["game_index", "player_id", "win_credit"]].drop_duplicates().copy()
    key_to_win = {
        (int(row.game_index), int(row.player_id)): float(row.win_credit)
        for row in players_base.itertuples(index=False)
    }
    total_player_games = len(players_base)
    global_win_rate = float(players_base["win_credit"].mean()) if total_player_games > 0 else 0.0

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

        win_credit = key_to_win.get((int(game_index), int(player_id)), 0.0)
        for a, b in combinations(unique_ids, 2):
            key = (a, b)
            pair_counts[key] = pair_counts.get(key, 0) + 1
            pair_wins[key] = pair_wins.get(key, 0.0) + win_credit

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

    players_base = players[["game_index", "player_id", "win_credit"]].drop_duplicates().copy()
    global_win_rate = float(players_base["win_credit"].mean()) if len(players_base) > 0 else 0.0

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
        combo_win_rate=("win_credit", "mean"),
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

    players_base = players[["game_index", "player_id", "win_credit"]].drop_duplicates().copy()
    panel = players_base.assign(_k=1).merge(mutation_defs.assign(_k=1), on="_k", how="outer").drop(columns=["_k"])
    panel = panel.merge(picks, on=["game_index", "player_id", "mutation_id"], how="left")
    panel["picked"] = panel["mutation_level"].notna()
    panel["mutation_level"] = panel["mutation_level"].fillna(0)

    picked_only = panel[panel["picked"]].copy()
    if not picked_only.empty:
        picked_only["early_level_intensity"] = picked_only["mutation_level"] / (picked_only["first_upgrade_round"].fillna(999) + 1.0)

    grouped = panel.groupby(["mutation_id", "mutation_name", "mutation_tier", "mutation_category", "tier_num"], as_index=False).agg(
        eligible_samples=("picked", "size"),
        picks=("picked", "sum"),
    )

    not_picked_stats = panel[~panel["picked"]].groupby("mutation_id", as_index=False).agg(
        win_rate_when_not_picked=("win_credit", "mean"),
    )

    picked_stats = picked_only.groupby("mutation_id", as_index=False).agg(
        win_rate_when_picked=("win_credit", "mean"),
        avg_level=("mutation_level", "mean"),
        avg_first_upgrade_round=("first_upgrade_round", "mean"),
        early_level_intensity=("early_level_intensity", "mean"),
        reached_l2_rate=("mutation_level", lambda s: float((s >= 2).mean())),
        reached_l3_rate=("mutation_level", lambda s: float((s >= 3).mean())),
        reached_l5_rate=("mutation_level", lambda s: float((s >= 5).mean())),
    )

    grouped = grouped.merge(picked_stats, on="mutation_id", how="left")
    grouped = grouped.merge(not_picked_stats, on="mutation_id", how="left")
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

    grouped["balance_score"] = np.nan
    grouped["recommendation"] = "Non-evidential observational screen"

    return grouped.sort_values("win_lift_shrunk", ascending=False, na_position="last")


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
    players_base = players[key_cols + ["win_credit"]].drop_duplicates().copy()
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
    )

    not_picked_stats = panel[~panel["picked"]].groupby("mycovariant_id", as_index=False).agg(
        win_rate_when_not_picked=("win_credit", "mean"),
    )

    picked_stats = panel[panel["picked"]].groupby("mycovariant_id", as_index=False).agg(
        win_rate_when_picked=("win_credit", "mean"),
        avg_total_effect=("total_effect", "mean"),
        trigger_rate=("triggered", "mean"),
    )

    grouped = grouped.merge(picked_stats, on="mycovariant_id", how="left")
    grouped = grouped.merge(not_picked_stats, on="mycovariant_id", how="left")
    grouped["avg_total_effect"] = grouped["avg_total_effect"].fillna(0.0)
    grouped["trigger_rate"] = grouped["trigger_rate"].fillna(0.0)

    grouped["pick_rate_eligible"] = grouped["picks"] / grouped["eligible_samples"].clip(lower=1)
    grouped["win_lift"] = grouped["win_rate_when_picked"] - grouped["win_rate_when_not_picked"]

    prior_strength = 20.0
    grouped["win_lift_shrunk"] = grouped["win_lift"] * (grouped["picks"] / (grouped["picks"] + prior_strength))

    grouped["confidence"] = _confidence_weight(grouped["picks"])
    grouped["ci_width"] = _rate_ci_width(grouped["win_rate_when_picked"], grouped["picks"].clip(lower=1))

    grouped["power_score"] = np.nan
    grouped["balance_score"] = np.nan
    grouped["recommendation"] = "Non-evidential observational screen"

    return grouped.sort_values("win_lift_shrunk", ascending=False, na_position="last")


def build_nutrient_summary(players: pd.DataFrame) -> pd.DataFrame:
    players = _ensure_strategy_identity(players)
    required_columns = {
        "strategy_name",
        "strategy_id",
        "strategy_definition_fingerprint",
        "strategy_theme",
        "win_credit",
        "nutrient_claims",
        "nutrient_mutation_points_earned",
        "mutation_point_income",
    }
    if players.empty or not required_columns.issubset(players.columns):
        return _empty_nutrient_summary()

    nutrient_df = players.copy()
    nutrient_df["nutrient_claims"] = nutrient_df["nutrient_claims"].fillna(0)
    nutrient_df["nutrient_mutation_points_earned"] = nutrient_df["nutrient_mutation_points_earned"].fillna(0)
    nutrient_df["mutation_point_income"] = nutrient_df["mutation_point_income"].fillna(0)
    nutrient_df["claimed_cluster_size"] = np.where(
        nutrient_df["nutrient_claims"] > 0,
        nutrient_df["nutrient_mutation_points_earned"] / nutrient_df["nutrient_claims"],
        0.0,
    )
    nutrient_df["nutrient_mp_share_of_income"] = np.where(
        nutrient_df["mutation_point_income"] > 0,
        nutrient_df["nutrient_mutation_points_earned"] / nutrient_df["mutation_point_income"],
        0.0,
    )
    nutrient_df["has_nutrient_claim"] = nutrient_df["nutrient_claims"] > 0

    grouped = nutrient_df.groupby(["strategy_id", "strategy_definition_fingerprint"], as_index=False).agg(
        strategy_name=("strategy_name", "first"),
        strategy_theme=("strategy_theme", "first"),
        samples=("player_id", "size"),
        win_rate=("win_credit", "mean"),
        avg_nutrient_claims=("nutrient_claims", "mean"),
        avg_nutrient_mutation_points=("nutrient_mutation_points_earned", "mean"),
        avg_claimed_cluster_size=("claimed_cluster_size", "mean"),
        avg_nutrient_mp_share_of_income=("nutrient_mp_share_of_income", "mean"),
        players_with_nutrient_claims_rate=("has_nutrient_claim", "mean"),
    )

    return grouped.sort_values(
        ["avg_nutrient_mutation_points", "avg_claimed_cluster_size", "win_rate"],
        ascending=[False, False, False],
    )


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
    player_summary: pd.DataFrame,
    growth_source_summary: pd.DataFrame,
    mutation_scores: pd.DataFrame,
    mycovariant_scores: pd.DataFrame,
    mutation_by_opponent_theme: pd.DataFrame,
    mutation_synergies: pd.DataFrame,
    myco_mutation_interactions: pd.DataFrame,
    nutrient_summary: pd.DataFrame,
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

    top_mut_observed = report_mutations.sort_values("win_lift_shrunk", ascending=False).head(10)
    bottom_mut_observed = report_mutations.sort_values("win_lift_shrunk", ascending=True).head(10)
    top_myco_observed = report_mycovariants.sort_values("win_lift_shrunk", ascending=False).head(10)
    bottom_myco_observed = report_mycovariants.sort_values("win_lift_shrunk", ascending=True).head(10)
    top_theme_sensitive = mutation_by_opponent_theme.sort_values("win_lift", ascending=False).head(12)
    top_synergies = mutation_synergies.head(12)
    top_interactions = myco_mutation_interactions.head(12)
    top_nutrient_strategies = nutrient_summary.head(12)
    full_player_summary = player_summary.copy()
    full_growth_source_summary = growth_source_summary.copy()

    def _table(df: pd.DataFrame, cols: list[str]) -> str:
        if df.empty:
            return "_No data_\n"
        return df[cols].to_markdown(index=False) + "\n"

    lines = [
        "# Exploratory Balance Diagnostics",
        "",
        "Scoring notes:",
        "- Mutation presence contrasts are observational screens over all player-games; they do not reconstruct legal offers.",
        "- Uses shrinkage on win-lift for sparse/high-tier picks.",
        "- Pick rate, timing, level intensity, and effect totals describe AI appetite; they are not mutation-power evidence.",
        "- Mutation, mycovariant, synergy, and interaction tables cannot support balance changes without controlled intervention.",
        f"- Report filtering: confidence >= {min_confidence:.2f}, picks >= {min_picks}, eligible_samples >= {min_eligible_samples}.",
        "",
        "## Post-Simulation Player Summary",
        _table(full_player_summary.round(3), ["player", "win_pct", "avg_living_cells", "avg_normalized_board_share", "win_rate_surplus", "avg_final_rank", "avg_normalized_rank", "avg_dead_cells", "avg_toxins"]),
        "## Growth Source Composition",
        _table(full_growth_source_summary.round(2), ["player", "total_living", "growth_source", "count", "pct_from_growth_source"]),
        "## Mutations - Highest Observed Association (Non-Evidential)",
        _table(top_mut_observed, ["mutation_name", "mutation_tier", "mutation_category", "eligible_samples", "picks", "pick_rate_eligible", "win_rate_when_picked", "win_rate_when_not_picked", "win_lift_shrunk", "avg_level", "avg_first_upgrade_round", "recommendation"]),
        "## Mutations - Lowest Observed Association (Non-Evidential)",
        _table(bottom_mut_observed, ["mutation_name", "mutation_tier", "mutation_category", "eligible_samples", "picks", "pick_rate_eligible", "win_rate_when_picked", "win_rate_when_not_picked", "win_lift_shrunk", "avg_level", "avg_first_upgrade_round", "recommendation"]),
        "## Mycovariants - Highest Observed Association (Non-Evidential)",
        _table(top_myco_observed, ["mycovariant_name", "mycovariant_type", "is_universal", "eligible_samples", "picks", "pick_rate_eligible", "win_rate_when_picked", "win_rate_when_not_picked", "win_lift_shrunk", "avg_total_effect", "trigger_rate", "recommendation"]),
        "## Mycovariants - Lowest Observed Association (Non-Evidential)",
        _table(bottom_myco_observed, ["mycovariant_name", "mycovariant_type", "is_universal", "eligible_samples", "picks", "pick_rate_eligible", "win_rate_when_picked", "win_rate_when_not_picked", "win_lift_shrunk", "avg_total_effect", "trigger_rate", "recommendation"]),
        "## Mutations by Opponent Theme (Highest Lift)",
        _table(top_theme_sensitive, ["dominant_opponent_theme", "mutation_name", "eligible_samples", "picks", "pick_rate", "win_rate_when_picked", "win_rate_when_not_picked", "win_lift"]),
        "## Mutation Co-Occurrence Associations (Non-Evidential)",
        _table(top_synergies, ["mutation_a_name", "mutation_b_name", "pair_samples", "pair_win_rate", "win_lift_vs_global", "synergy_score"]),
        "## Mycovariant-Mutation Co-Occurrence Associations (Non-Evidential)",
        _table(top_interactions, ["mycovariant_name", "mutation_name", "combo_samples", "combo_win_rate", "combo_lift_vs_global", "interaction_score"]),
        "## Nutrient Economy by Strategy",
        _table(top_nutrient_strategies, ["strategy_name", "strategy_theme", "samples", "win_rate", "avg_nutrient_claims", "avg_nutrient_mutation_points", "avg_claimed_cluster_size", "avg_nutrient_mp_share_of_income", "players_with_nutrient_claims_rate"]),
    ]

    output_path.write_text("\n".join(lines), encoding="utf-8")


def main() -> None:
    parser = argparse.ArgumentParser(description="Analyze FungusToast simulation Parquet exports for descriptive and exploratory diagnostics.")
    parser.add_argument("--run-folder", required=True, help="Path to one simulation export folder containing parquet files.")
    parser.add_argument("--paired-treatment-folder", required=False, help="Optional treatment run folder sharing pair IDs with --run-folder (the control).")
    parser.add_argument("--emit-verdict", action="store_true", help="Issue only the preregistered primary verdict; requires a paired treatment and matching resolved analysis plans.")
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
    mutations_path = run_folder / "mutations.parquet"
    mycovariants_path = run_folder / "mycovariants.parquet"
    mutations = pd.read_parquet(mutations_path) if mutations_path.exists() else pd.DataFrame()
    mycovariants = pd.read_parquet(mycovariants_path) if mycovariants_path.exists() else pd.DataFrame()
    living_cell_sources_path = run_folder / "living_cell_sources.parquet"
    living_cell_sources = pd.read_parquet(living_cell_sources_path) if living_cell_sources_path.exists() else pd.DataFrame()

    players = _ensure_strategy_identity(_normalize_columns(players))
    players = _ensure_win_credit(players)
    paired_comparison = None
    if args.paired_treatment_folder:
        treatment_folder = Path(args.paired_treatment_folder)
        if not treatment_folder.exists():
            raise FileNotFoundError(f"Paired treatment folder not found: {treatment_folder}")
        treatment_players = pd.read_parquet(treatment_folder / "players.parquet")
        treatment_players = _ensure_win_credit(_ensure_strategy_identity(_normalize_columns(treatment_players)))
        paired_comparison = build_paired_comparison(players, treatment_players)
    if args.emit_verdict and paired_comparison is None:
        raise ValueError("--emit-verdict requires --paired-treatment-folder")
    mutations = _ensure_strategy_identity(_normalize_columns(mutations))
    mycovariants = _ensure_strategy_identity(_normalize_columns(mycovariants))
    if not living_cell_sources.empty:
        living_cell_sources = _ensure_strategy_identity(_normalize_columns(living_cell_sources))

    player_summary = build_player_summary(players)
    growth_source_summary = build_growth_source_summary(players, living_cell_sources)
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
    nutrient_summary = build_nutrient_summary(players)

    player_summary.to_csv(output_dir / "post_simulation_player_summary.csv", index=False)
    growth_source_summary.to_csv(output_dir / "growth_source_summary.csv", index=False)
    mutation_scores.to_csv(output_dir / "mutation_recommendations.csv", index=False)
    mycovariant_scores.to_csv(output_dir / "mycovariant_recommendations.csv", index=False)
    mutation_by_opponent_theme.to_csv(output_dir / "mutation_by_opponent_theme.csv", index=False)
    mutation_synergies.to_csv(output_dir / "mutation_synergies.csv", index=False)
    myco_mutation_interactions.to_csv(output_dir / "mycovariant_mutation_interactions.csv", index=False)
    nutrient_summary.to_csv(output_dir / "nutrient_economy_summary.csv", index=False)
    if paired_comparison is not None:
        paired_comparison.to_csv(output_dir / "paired_comparison.csv", index=False)
    if args.emit_verdict:
        control_manifest_path = run_folder / "resolved-manifest.json"
        treatment_manifest_path = Path(args.paired_treatment_folder) / "resolved-manifest.json"
        if not control_manifest_path.exists() or not treatment_manifest_path.exists():
            raise FileNotFoundError("--emit-verdict requires resolved-manifest.json in both run folders")
        verdict = build_preregistered_verdict(
            paired_comparison,
            json.loads(control_manifest_path.read_text(encoding="utf-8")),
            json.loads(treatment_manifest_path.read_text(encoding="utf-8")),
        )
        (output_dir / "preregistered_verdict.json").write_text(
            json.dumps(verdict, indent=2, sort_keys=True) + "\n",
            encoding="utf-8",
        )
    write_markdown_report(
        player_summary,
        growth_source_summary,
        mutation_scores,
        mycovariant_scores,
        mutation_by_opponent_theme,
        mutation_synergies,
        myco_mutation_interactions,
        nutrient_summary,
        output_dir / "balance_recommendations.md",
        min_confidence=args.min_confidence,
        min_picks=args.min_picks,
        min_eligible_samples=args.min_eligible_samples,
    )

    print(f"Analysis complete. Artifacts written to: {output_dir}")


if __name__ == "__main__":
    main()

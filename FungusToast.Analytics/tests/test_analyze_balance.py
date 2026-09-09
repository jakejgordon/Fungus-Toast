import importlib.util
from pathlib import Path
import unittest

import numpy as np
import pandas as pd


MODULE_PATH = Path(__file__).resolve().parents[1] / "analyze_balance.py"
SPEC = importlib.util.spec_from_file_location("analyze_balance", MODULE_PATH)
ANALYZE_BALANCE = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
SPEC.loader.exec_module(ANALYZE_BALANCE)


class AnalyzeBalanceTests(unittest.TestCase):
    def test_player_summary_requires_condition_identity(self):
        players = self._players().drop(columns=["condition_id"])

        with self.assertRaisesRegex(ValueError, "condition_id"):
            ANALYZE_BALANCE.build_player_summary(players)

    def test_mutation_not_picked_rate_excludes_picked_rows_and_withdraws_verdict(self):
        players = self._players()
        mutations = pd.DataFrame(
            [
                {
                    "game_index": 1,
                    "player_id": 0,
                    "mutation_id": 10,
                    "mutation_name": "Test Mutation",
                    "mutation_tier": "Tier1",
                    "mutation_category": "Growth",
                    "mutation_level": 1,
                    "first_upgrade_round": 1,
                }
            ]
        )

        scores = ANALYZE_BALANCE.build_mutation_scores(players, mutations)
        row = scores.iloc[0]

        self.assertEqual(1.0, row["win_rate_when_picked"])
        self.assertEqual(0.0, row["win_rate_when_not_picked"])
        self.assertTrue(np.isnan(row["balance_score"]))
        self.assertEqual("Non-evidential observational screen", row["recommendation"])

    def test_legacy_rows_derive_fractional_credit_for_tied_first_place(self):
        players = pd.DataFrame(
            {
                "final_rank": [1, 1, 3, 4],
                "players_tied_at_final_rank": [2, 2, 1, 1],
            }
        )

        credited = ANALYZE_BALANCE._ensure_win_credit(players)

        self.assertEqual([0.5, 0.5, 0.0, 0.0], credited["win_credit"].tolist())
        self.assertEqual(1.0, credited["win_credit"].sum())

    def test_player_summary_groups_by_identity_not_mutable_theme(self):
        players = pd.concat([self._players(), self._players().assign(strategy_theme="drifted")], ignore_index=True)
        players["strategy_id"] = players["strategy_name"].map(lambda name: f"id.{name}")
        players["strategy_definition_fingerprint"] = "same-definition"

        summary = ANALYZE_BALANCE.build_player_summary(players)

        self.assertEqual(4, len(summary))
        self.assertTrue((summary["games"] == 2).all())

    def test_execution_health_aggregates_decisions_and_fallback_rate(self):
        players = self._players().assign(
            ai_mutation_spending_decisions=[10, 20, 5, 5],
            ai_mutation_fallback_spends=[2, 10, 0, 5],
        )

        health = ANALYZE_BALANCE.build_strategy_execution_health(players)

        self.assertEqual(4, len(health))
        self.assertAlmostEqual(0.5, health.loc[health["strategy_name"] == "strategy-1", "fallback_rate"].iloc[0])

    def test_execution_health_does_not_invent_legacy_telemetry(self):
        health = ANALYZE_BALANCE.build_strategy_execution_health(self._players())

        self.assertTrue(health.empty)

    def test_category_profiles_aggregate_final_levels_by_strategy_and_category(self):
        mutations = pd.DataFrame(
            [
                {"strategy_name": "strategy-0", "strategy_id": "id.0", "strategy_definition_fingerprint": "fp", "mutation_category": "Growth", "mutation_level": 2},
                {"strategy_name": "strategy-0", "strategy_id": "id.0", "strategy_definition_fingerprint": "fp", "mutation_category": "Growth", "mutation_level": 3},
                {"strategy_name": "strategy-0", "strategy_id": "id.0", "strategy_definition_fingerprint": "fp", "mutation_category": "Economy", "mutation_level": 1},
            ]
        )

        profiles = ANALYZE_BALANCE.build_strategy_category_profiles(mutations)

        self.assertEqual(2, len(profiles))
        self.assertEqual(5, profiles.loc[profiles["mutation_category"] == "Growth", "total_levels"].iloc[0])

    def test_paired_comparison_uses_slot_pairs_and_reports_observed_gain(self):
        control = self._paired_players([6, 7, 8], [4, 3, 2], treatment=False)
        treatment = self._paired_players([7, 8, 9], [3, 2, 1], treatment=True)

        summary = ANALYZE_BALANCE.build_paired_comparison(control, treatment)
        player_zero = summary[summary["strategy_id_control"] == "strategy-0"].iloc[0]

        self.assertEqual(3, player_zero["pairs"])
        self.assertAlmostEqual(0.2, player_zero["paired_difference_normalized_board_share"])
        self.assertGreater(player_zero["paired_vs_unpaired_variance_ratio_normalized_board_share"], 1.0)

    def test_paired_comparison_rejects_seed_mismatch(self):
        control = self._paired_players([6, 7], [4, 3], treatment=False)
        treatment = self._paired_players([7, 8], [3, 2], treatment=True)
        treatment.loc[treatment["game_index"] == 2, "game_seed"] = 999

        with self.assertRaisesRegex(ValueError, "game_seed"):
            ANALYZE_BALANCE.build_paired_comparison(control, treatment)

    def test_preregistered_verdict_uses_only_declared_target_metric(self):
        control_zero = [6 + (i % 3) for i in range(50)]
        control_one = [10 - value for value in control_zero]
        treatment_zero = [value + 1 for value in control_zero]
        treatment_one = [value - 1 for value in control_one]
        control = self._paired_players(control_zero, control_one, treatment=False)
        treatment = self._paired_players(treatment_zero, treatment_one, treatment=True)
        paired = ANALYZE_BALANCE.build_paired_comparison(control, treatment)
        analysis = {
            "analysisVersion": ANALYZE_BALANCE.ANALYSIS_VERSION,
            "evidenceStage": "comparison",
            "hypothesis": {
                "hypothesisId": "share-increase",
                "primaryContextId": "paired-test",
                "targetStrategyId": "strategy-0",
                "primaryMetric": "normalizedBoardShare",
                "estimand": "pairedMeanDifference",
                "direction": "increase",
                "margin": 0.1,
            },
        }

        verdict = ANALYZE_BALANCE.build_preregistered_verdict(
            paired,
            self._resolved_manifest(analysis, games=50),
            self._resolved_manifest(analysis, games=50),
        )

        self.assertEqual("supported", verdict["verdict"])
        self.assertEqual("normalizedBoardShare", verdict["primary_metric"])

    def test_preregistered_verdict_refuses_missing_hypothesis(self):
        paired = ANALYZE_BALANCE.build_paired_comparison(
            self._paired_players([6, 7], [4, 3], treatment=False),
            self._paired_players([7, 8], [3, 2], treatment=True),
        )
        analysis = {"analysisVersion": ANALYZE_BALANCE.ANALYSIS_VERSION, "evidenceStage": "comparison", "hypothesis": None}

        with self.assertRaisesRegex(ValueError, "no preregistered hypothesis"):
            ANALYZE_BALANCE.build_preregistered_verdict(
                paired,
                self._resolved_manifest(analysis, games=50),
                self._resolved_manifest(analysis, games=50),
            )

    def test_preregistered_verdict_supports_a_strategy_swap_treatment(self):
        """A candidate swap puts a different strategy in the target slot in each arm, so the
        control side of the target is named separately. Without controlStrategyId the verdict
        could only describe treatments that left strategy identity unchanged."""
        control_zero = [6 + (i % 3) for i in range(50)]
        control_one = [10 - value for value in control_zero]
        treatment_zero = [value + 1 for value in control_zero]
        treatment_one = [value - 1 for value in control_one]
        control = self._paired_players(control_zero, control_one, treatment=False, slot_zero_strategy_id="parent")
        treatment = self._paired_players(treatment_zero, treatment_one, treatment=True, slot_zero_strategy_id="candidate")
        paired = ANALYZE_BALANCE.build_paired_comparison(control, treatment)
        analysis = {
            "analysisVersion": ANALYZE_BALANCE.ANALYSIS_VERSION,
            "evidenceStage": "comparison",
            "hypothesis": {
                "hypothesisId": "candidate-beats-parent",
                "primaryContextId": "paired-test",
                "targetStrategyId": "candidate",
                "controlStrategyId": "parent",
                "primaryMetric": "normalizedBoardShare",
                "estimand": "pairedMeanDifference",
                "direction": "increase",
                "margin": 0.1,
            },
        }

        verdict = ANALYZE_BALANCE.build_preregistered_verdict(
            paired,
            self._resolved_manifest(analysis, games=50),
            self._resolved_manifest(analysis, games=50),
        )

        self.assertEqual("supported", verdict["verdict"])
        self.assertEqual("candidate", verdict["target_strategy_id"])
        self.assertEqual("parent", verdict["control_strategy_id"])

    def test_preregistered_verdict_refuses_a_swap_that_omits_the_control_strategy(self):
        """Omitting controlStrategyId means "the same strategy in both arms", so a swap must fail
        loudly rather than silently resolving to no rows."""
        control = self._paired_players([6, 7], [4, 3], treatment=False, slot_zero_strategy_id="parent")
        treatment = self._paired_players([7, 8], [3, 2], treatment=True, slot_zero_strategy_id="candidate")
        paired = ANALYZE_BALANCE.build_paired_comparison(control, treatment)
        analysis = {
            "analysisVersion": ANALYZE_BALANCE.ANALYSIS_VERSION,
            "evidenceStage": "comparison",
            "hypothesis": {
                "hypothesisId": "candidate-beats-parent",
                "primaryContextId": "paired-test",
                "targetStrategyId": "candidate",
                "primaryMetric": "normalizedBoardShare",
                "estimand": "pairedMeanDifference",
                "direction": "increase",
                "margin": 0.1,
            },
        }

        with self.assertRaisesRegex(ValueError, "did not resolve to exactly one paired row"):
            ANALYZE_BALANCE.build_preregistered_verdict(
                paired,
                self._resolved_manifest(analysis, games=50),
                self._resolved_manifest(analysis, games=50),
            )

    @staticmethod
    def _players():
        return pd.DataFrame(
            [
                {
                    "condition_id": "control",
                    "strategy_name": f"strategy-{player_id}",
                    "strategy_theme": "test",
                    "game_index": 1,
                    "player_id": player_id,
                    "is_winner": player_id == 0,
                    "win_credit": 1.0 if player_id == 0 else 0.0,
                    "living_cells": 10 - player_id,
                    "dead_cells": player_id,
                    "end_game_toxin_cells": 0,
                }
                for player_id in range(4)
            ]
        )

    @staticmethod
    def _resolved_manifest(analysis, games):
        return {
            "analysis": analysis,
            "totalGameBudget": games * 2,
            "runtimeBudgetSeconds": 600,
            "sampling": {"completionStatus": "complete", "gamesCompleted": games},
            "games": [{"runtimeMilliseconds": 1.0} for _ in range(games)],
        }

    @staticmethod
    def _paired_players(player_zero_living, player_one_living, treatment, slot_zero_strategy_id="strategy-0"):
        rows = []
        for game_index, (zero, one) in enumerate(zip(player_zero_living, player_one_living), start=1):
            for player_id, living in enumerate((zero, one)):
                rows.append(
                    {
                        "pairing_group_id": "paired-test",
                        "pair_id": f"paired-test:{game_index}:{100 + game_index}",
                        "condition_id": "treatment" if treatment else "control",
                        "game_index": game_index,
                        "game_seed": 100 + game_index,
                        "assigned_slot": player_id,
                        "player_id": player_id,
                        "player_count": 2,
                        "strategy_name": slot_zero_strategy_id if player_id == 0 else f"strategy-{player_id}",
                        "strategy_id": slot_zero_strategy_id if player_id == 0 else f"strategy-{player_id}",
                        "strategy_definition_fingerprint": "treatment" if treatment else "control",
                        "strategy_theme": "test",
                        "living_cells": living,
                        "total_living_cells": zero + one,
                        "final_rank": 1 if living == max(zero, one) else 2,
                        "win_credit": 1.0 if living == max(zero, one) else 0.0,
                        "dead_cells": 0,
                        "end_game_toxin_cells": 0,
                        "starting_x": player_id,
                        "starting_y": player_id,
                        "board_geometry_fingerprint": "same-board",
                        "random_stream_contract_version": "same-rng",
                    }
                )
        return pd.DataFrame(rows)


if __name__ == "__main__":
    unittest.main()

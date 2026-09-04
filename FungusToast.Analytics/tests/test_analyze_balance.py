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


if __name__ == "__main__":
    unittest.main()

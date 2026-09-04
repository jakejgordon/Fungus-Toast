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
                    "living_cells": 10 - player_id,
                    "dead_cells": player_id,
                    "end_game_toxin_cells": 0,
                }
                for player_id in range(4)
            ]
        )


if __name__ == "__main__":
    unittest.main()

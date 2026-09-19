import importlib.util
import json
from pathlib import Path
import sys
from tempfile import TemporaryDirectory
import unittest

import pandas as pd


MODULE_PATH = Path(__file__).resolve().parents[1] / "analyze_campaign_ladder.py"
SPEC = importlib.util.spec_from_file_location("analyze_campaign_ladder", MODULE_PATH)
ANALYZE_CAMPAIGN_LADDER = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
sys.modules[SPEC.name] = ANALYZE_CAMPAIGN_LADDER
SPEC.loader.exec_module(ANALYZE_CAMPAIGN_LADDER)


class AnalyzeCampaignLadderTests(unittest.TestCase):
    def test_analysis_flags_only_adjacent_win_rate_increases(self):
        measurements = [
            self._measurement(level=0, win_rate=0.90),
            self._measurement(level=1, win_rate=0.75),
            self._measurement(level=2, win_rate=0.80),
            self._measurement(level=4, win_rate=0.95),
        ]

        analysis = ANALYZE_CAMPAIGN_LADDER.analyze_campaign_ladder(
            measurements,
            expected_levels=range(5),
        )

        self.assertEqual([3], list(analysis.missing_levels))
        self.assertEqual([2], [row.measurement.level for row in analysis.inversions])
        self.assertEqual("Not compared (level gap)", analysis.rows[-1].status)
        self.assertAlmostEqual(0.05, analysis.inversions[0].change_from_previous)

    def test_report_describes_inversions_without_a_failure_verdict(self):
        analysis = ANALYZE_CAMPAIGN_LADDER.analyze_campaign_ladder(
            [self._measurement(0, 0.70), self._measurement(1, 0.80)],
            expected_levels=(0, 1),
        )

        report = ANALYZE_CAMPAIGN_LADDER.render_campaign_ladder_report(analysis, seed=123)

        self.assertIn("Measured inversions: **1**", report)
        self.assertIn("Level 0 → 1", report)
        self.assertIn("+10.0 pp", report)
        self.assertIn("90%–100%", report)
        self.assertIn("Below", report)
        self.assertIn("does not fail CI", report)

    def test_loader_accepts_complete_legacy_parquet_artifact(self):
        with TemporaryDirectory() as temp_dir:
            run_folder = Path(temp_dir) / "campaign_balance_lvl02_Campaign2_g100_seed123"
            run_folder.mkdir()
            self._write_players(run_folder)
            (run_folder / "manifest.json").write_text(
                json.dumps(
                    {
                        "schemaVersion": "v5",
                        "NumberOfGamesRequested": 100,
                        "actualGamesExported": 100,
                    }
                ),
                encoding="utf-8",
            )

            measurement = ANALYZE_CAMPAIGN_LADDER.load_campaign_measurement(2, run_folder)

        self.assertEqual(2, measurement.level)
        self.assertEqual(0.60, measurement.proxy_win_rate)
        self.assertIn("Legacy manifest v5", measurement.provenance)

    def test_loader_rejects_incomplete_resolved_artifact(self):
        with TemporaryDirectory() as temp_dir:
            run_folder = Path(temp_dir) / "campaign_balance_lvl00_Campaign0_g100_seed123"
            run_folder.mkdir()
            self._write_players(run_folder)
            (run_folder / "resolved-manifest.json").write_text(
                json.dumps(
                    {
                        "analysis": {"evidenceStage": "holdout"},
                        "sampling": {
                            "gamesRequested": 100,
                            "gamesCompleted": 99,
                            "completionStatus": "interrupted",
                        },
                    }
                ),
                encoding="utf-8",
            )

            with self.assertRaisesRegex(ValueError, "not a complete 100-game artifact"):
                ANALYZE_CAMPAIGN_LADDER.load_campaign_measurement(0, run_folder)

    def test_loader_accepts_complete_resolved_holdout_and_prefers_win_credit(self):
        with TemporaryDirectory() as temp_dir:
            run_folder = Path(temp_dir) / "campaign_balance_lvl00_Campaign0_g100_seed123"
            run_folder.mkdir()
            self._write_players(run_folder, win_credit=0.25)
            (run_folder / "resolved-manifest.json").write_text(
                json.dumps(
                    {
                        "analysis": {"evidenceStage": "holdout"},
                        "sampling": {
                            "gamesRequested": 100,
                            "gamesCompleted": 100,
                            "completionStatus": "complete",
                        },
                        "code": {"commit": "abc123"},
                    }
                ),
                encoding="utf-8",
            )

            measurement = ANALYZE_CAMPAIGN_LADDER.load_campaign_measurement(0, run_folder)

        self.assertEqual(0.25, measurement.proxy_win_rate)
        self.assertEqual("abc123", measurement.code_identity)
        self.assertEqual("Resolved holdout manifest", measurement.provenance)

    def test_discovery_rejects_duplicate_level_and_seed(self):
        with TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            (root / "campaign_balance_lvl01_Campaign1_g100_seed123").mkdir()
            (root / "campaign_balance_lvl01_Alternate_g100_seed123").mkdir()

            with self.assertRaisesRegex(ValueError, "Multiple 100-game campaign artifacts"):
                ANALYZE_CAMPAIGN_LADDER.discover_campaign_runs(root, seed=123)

    def test_analysis_rejects_mixed_code_identities(self):
        first = self._measurement(0, 0.90)
        second = ANALYZE_CAMPAIGN_LADDER.CampaignLevelMeasurement(
            level=1,
            experiment_id="level-1",
            games=100,
            proxy_win_rate=0.80,
            provenance="test",
            code_identity="different-code",
        )

        with self.assertRaisesRegex(ValueError, "do not share one code identity"):
            ANALYZE_CAMPAIGN_LADDER.analyze_campaign_ladder([first, second], expected_levels=(0, 1))

    @staticmethod
    def _measurement(level, win_rate):
        return ANALYZE_CAMPAIGN_LADDER.CampaignLevelMeasurement(
            level=level,
            experiment_id=f"level-{level}",
            games=100,
            proxy_win_rate=win_rate,
            provenance="test",
            code_identity="same-code",
        )

    @staticmethod
    def _write_players(run_folder, win_credit=None):
        rows = []
        for game_index in range(1, 101):
            row = {
                "GameIndex": game_index,
                "StrategyName": "TST_CampaignPlayer_SafeBaseline",
                "IsWinner": game_index <= 60,
            }
            if win_credit is not None:
                row["WinCredit"] = win_credit
            rows.append(row)
        pd.DataFrame(rows).to_parquet(run_folder / "players.parquet", index=False)


if __name__ == "__main__":
    unittest.main()

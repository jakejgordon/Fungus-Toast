# AI P3.5 Frozen Reference Baseline v1

**Analysis version:** `p3.5.reference-baseline.v1`  
**Analyzer:** `FungusToast.Analytics/analyze_balance.py` SHA-256
`0837a9278f1c3b5650ea97faebf8ea7404cc00f0cf56937f0f908b3cafb7bcdc`  
**Result schema:** `fungus-toast.experiment-result.v1`  
**Status:** locked reference baseline; **not** a player-facing balance
classification or a promotion decision.

This is the first artifact-backed reference corpus for the AI Architecture and
Balance Overhaul. It fixes the inputs, raw outcomes, and offline-analysis
implementation against which subsequent controlled work can be compared. It
does not establish global or contextual difficulty bands, and it does not
attribute causation to mutations, strategies, or mycovariants.

## Frozen artifacts

Both artifacts are complete, have 50 games (200 player rows), and passed
`sha256sum -c resolved-manifest.sha256` on 2026-09-03. The frozen lineup is
`TST_Arch01_GrowthResilience`, `TST_Arch03_FungicideSurge`,
`TST_Arch04_DriftGrowth`, and `TST_Arch06_SurgeGrowth`; slot assignment is
`rotateByGame`, nutrients are disabled, and no starting Adaptations are
assigned.

| Corpus | Artifact folder | Manifest SHA-256 | Condition |
|---|---|---|---|
| A | `FungusToast.Simulation/bin/Debug/net8.0/SimulationParquet/p3_5_reference_a_v1` | `7bf40c778caab5bc101835df400874756f17c2d5c31112ac7f13ee1dd56d0d7e` | 50 games; seeds 310301–310350; 4-player 120x120 rectangle; mycovariants off |
| B | `FungusToast.Simulation/bin/Debug/net8.0/SimulationParquet/p3_5_reference_b_v1` | `4c3e9be2c6dcdae281046f501f1b5020ebc75d074b0323769075c66a9e3dd684` | 50 games; seeds 310302–310351; 4-player 140x100 rectangle; mycovariants on |

The Parquet row counts are 50 games and 200 players per corpus. Their
`players.parquet` SHA-256 values are respectively
`7d9c776e401e8ff9d5eef0d254c029f101e8e1d26eb64b3ac487237ee683bdf5`
and `e5ddd7e49a4f62de28765915597a48ec1d973836c991c90f66103a6dc7e622e2`.

## Combined reference estimate

The following combines the two artifacts (100 games and 100 observations per
strategy). Normalized board share has equal-share expectation 1.0; win surplus
has expectation 0.0; normalized rank ranges from 0.0 (last) to 1.0 (first).
Intervals are the analyzer's 95% intervals. Effect sizes are standardized
distance from equal-share (board) and mid-rank 0.5 (rank), not causal effects.

| Strategy | Board share [95% CI] | Win surplus [95% CI] | Normalized rank [95% CI] | Board / rank effect | Context share range |
|---|---:|---:|---:|---:|---:|
| GrowthResilience | 2.056 [1.999, 2.113] | +0.700 [+0.638, +0.728] | 0.983 [0.969, 0.998] | +3.613 / +6.620 | 0.132 |
| DriftGrowth | 0.996 [0.944, 1.047] | -0.200 [-0.228, -0.138] | 0.640 [0.613, 0.667] | -0.016 / +1.003 | 0.135 |
| SurgeGrowth | 0.733 [0.708, 0.758] | -0.250 [-0.250, -0.213] | 0.380 [0.357, 0.403] | -2.089 / -1.032 | 0.035 |
| FungicideSurge | 0.215 [0.206, 0.223] | -0.250 [-0.250, -0.213] | 0.000 [0.000, 0.000] | -17.765 / +0.000 | 0.033 |

The two observed context means are retained by the analyzer rather than hidden:
the worst normalized-share means are 1.990, 0.928, 0.716, and 0.198 in the
same strategy order. These values are descriptive reference measurements, not
an "overpowered" or "underpowered" conclusion. End-state correlations and
pick correlations in the generated recommendation files likewise cannot show
that a mutation, mycovariant, or strategy caused the observed result.

## Evidence limits and locked interpretation

- This is two 50-game, small-table, medium-area rectangular conditions only.
  It lacks duel, crowded, swarm, small/large board, masked-board, start-regime,
  nutrient-on, Adaptation, lineup, and holdout coverage.
- Corpus B changes both board aspect (square to wide) and mycovariant state
  relative to A. Those effects are confounded, so the two context means cannot
  establish a mycovariant effect, geometry effect, or contextual band.
- Each strategy has only two aggregate context observations. The reported
  context range is a diagnostic bound, not a robustness pass/fail result.
- The 100 combined games meet the Phase 3 holdout batch ceiling but are not a
  holdout: these are the frozen reference conditions. No strategy receives a
  player-facing label, roster promotion, mutation change, or balance treatment
  from this report.

For future comparison experiments, use a matched control/treatment manifest
diff, predeclare the hypothesis and materiality margin, and retain the Phase
3 smoke, calibration, comparison, and holdout gates. A contextual band needs
recurring evidence across at least two independent context axes.

## Reproduction

From the repository root, verify each frozen artifact and regenerate its
analysis without rerunning simulation:

```bash
for run in p3_5_reference_a_v1 p3_5_reference_b_v1; do
  artifact="FungusToast.Simulation/bin/Debug/net8.0/SimulationParquet/$run"
  (cd "$artifact" && sha256sum -c resolved-manifest.sha256)
  FungusToast.Analytics/.venv/bin/python FungusToast.Analytics/analyze_balance.py \
    --run-folder "$artifact"
done
```

Read each artifact's `post_simulation_player_summary.csv` for the per-condition
estimate. The combined table above is produced by applying the locked analyzer
to the concatenation of the two verified `players.parquet` files, grouping by
strategy and theme, exactly as `build_player_summary` does. Any change to the
analyzer SHA-256, manifest checksum, Parquet hash, or aggregation contract
requires a new analysis version rather than silently replacing this baseline.

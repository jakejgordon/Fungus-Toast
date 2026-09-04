# AI P3.5 Corrected Reference Baseline v2

**Analysis contract:** `fungus-toast.analysis.v2`  
**Analyzer SHA-256:**
`c1381030239e8b2357bd0b86524d06ca511afa2f36c724cb153a05eb3dd0cb6d`  
**Input / result / Parquet schemas:** `fungus-toast.experiment-input.v3` /
`fungus-toast.experiment-result.v7` / `v9`  
**Random-stream contract:** `fungus-toast.random-streams.v1`  
**AI corpus:** `fungus-toast.ai-corpus.pre-phase5.v1`  
**Balance identity (Core SHA-256):**
`5cf2b4faa7632164ebcad08cb812ce928bd44069c95b28c9727f3ba2bf3629a0`  
**Status:** corrected descriptive reference; no player-facing balance
classification, causal claim, or promotion decision.

This corpus supersedes
[AI_P3_5_REFERENCE_BASELINE_V1.md](AI_P3_5_REFERENCE_BASELINE_V1.md) for all
future comparisons. The v1 document remains unchanged as historical evidence,
but its shared RNG stream, slot-ordered tie handling, name/theme analytical key,
and earlier interval/robustness contracts make its numerical estimates
incompatible with v2. Do not splice v1 and v2 observations into one estimate.

## Frozen artifacts

Both artifacts completed 50 games (200 player rows), reported zero parity
invariant mismatches, passed `sha256sum -c resolved-manifest.sha256`, and were
analyzed on 2026-09-04. They use the same explicit four-strategy lineup and
historical conditions as v1: rotated slots, nutrients disabled, and no starting
Adaptations.

| Corpus | Artifact folder | Manifest SHA-256 | Players SHA-256 | Condition |
|---|---|---|---|---|
| A | `FungusToast.Simulation/bin/Debug/net8.0/SimulationParquet/p3_5_reference_a_v2` | `d2ff69c08a182d820ecd661ecbda2bac9ce4fb34466e2d3bf50c76e34619c483` | `bd355a2c9bbdf8bb5da19982728fa54419900cb6ff129dd6e68d1074a7a33985` | 50 games; seeds 310301–310350; 4-player 120x120 rectangle; mycovariants off |
| B | `FungusToast.Simulation/bin/Debug/net8.0/SimulationParquet/p3_5_reference_b_v2` | `9a30fbca5e39b5140071d79c6e52c669cb21402c4e62de9ecc51a1bdbd0ced09` | `467ca71ce9412f0c5d929b68dbe92c5c84c6d1f3723fb6c6abccee93b4cddf36` | 50 games; seeds 310302–310351; 4-player 140x100 rectangle; mycovariants on |

The frozen lineup is `TST_Arch01_GrowthResilience`,
`TST_Arch03_FungicideSurge`, `TST_Arch04_DriftGrowth`, and
`TST_Arch06_SurgeGrowth`. Analytics joins them by stable strategy ID plus
definition fingerprint; names and themes are labels only.

Both resolved plans declare the `comparison` evidence stage and a shared
100-game descriptive budget, but `analysis.hypothesis` is `null`. Consequently,
the analyzer cannot issue a decision verdict from either artifact. Corpus A
recorded 186.178 seconds of game runtime and corpus B recorded 210.461 seconds;
all 100 games ended by `board-occupancy-countdown`.

## Corrected combined reference estimate

The table combines 100 games and 100 game-level observations per strategy.
Normalized board share has equal-share expectation 1.0; normalized rank ranges
from 0.0 (last) to 1.0 (first). Win credit is tie-aware and sums to one within
each game. Intervals come from analysis v2. The robustness columns use a
20-game-prior shrunken context estimate, its 10th percentile, and its range
rather than the biased raw minimum over contexts.

| Strategy | Stable ID | Board share [95% CI] | Normalized rank [95% CI] | Win credit | Shrunken p10 / range |
|---|---|---:|---:|---:|---:|
| GrowthResilience | `legacy.testing.tst-arch01-growthresilience.v1` | 2.062 [2.002, 2.122] | 0.980 [0.964, 0.996] | 94/100 | 2.003 / 0.147 |
| DriftGrowth | `legacy.testing.tst-arch04-driftgrowth.v1` | 0.986 [0.920, 1.051] | 0.620 [0.588, 0.652] | 6/100 | 0.938 / 0.118 |
| SurgeGrowth | `legacy.testing.tst-arch06-surgegrowth.v1` | 0.728 [0.703, 0.753] | 0.400 [0.374, 0.426] | 0/100 | 0.725 / 0.008 |
| FungicideSurge | `legacy.testing.tst-arch03-fungicidesurge.v1` | 0.225 [0.213, 0.237] | 0.000 [0.000, 0.000] | 0/100 | 0.210 / 0.036 |

These are descriptive measurements of this lineup under two confounded
conditions. They are not evidence that a strategy, mutation, or mycovariant
caused an outcome, and they do not authorize an overpowered/underpowered label.
The large observed spread is useful for future characterization and for
detecting behavior drift, not for bypassing a paired preregistered experiment.

## Replay and correction-gate evidence

Corpus A was regenerated in a separate process as
`p3_5_reference_a_v2_replay`. Replay validated the exact canonical outcome
fingerprint:

`a77bbeac466e5b549324bc4b53e6779c0b5ece887a8ea189d442f674fda29918`

The source and replay each completed all 50 games with the same seed schedule,
lineup, strategy fingerprints, result schema, RNG contract, AI corpus, and
outcome fingerprint. Runtime and file hashes are intentionally not part of the
deterministic outcome fingerprint. The replay artifact's own manifest checksum
also passed.

The checked-in `verify-experiment-contract.sh` fixture subsequently passed its
build, matrix resume, separate-process replay, outcome equality, and manifest
checksum checks. Focused validation at the correction gate was:

- FungusToast.Core.Tests: 620 passed;
- FungusToast.Simulation.Tests: 18 passed;
- Python analytics tests: 8 passed in the preceding P3.R6 slice;
- both 50-game artifacts: complete, checksum-valid, zero parity mismatches;
- no `preregistered_verdict.json` or paired-comparison artifact was produced.

## Evidence limits and lifecycle

- Only two four-player, medium-area rectangular contexts are present. No duel,
  crowded, swarm, small/large, masked-board, nutrient-on, starting-Adaptation,
  position-pool, or holdout evidence is included.
- Corpus B changes both aspect ratio and mycovariant availability relative to
  A. Their difference cannot identify either effect.
- Fifty games per context supports a descriptive comparison-stage snapshot,
  not secondary or per-context promotion claims. Win credit remains tertiary;
  normalized board share is primary for future declared tests and normalized
  rank is secondary.
- Observational mutation, mycovariant, synergy, and interaction tables remain
  non-evidential screens. A power claim requires an intervention with paired
  seeds/seats and a preregistered primary estimand.
- Any behavior-changing Phase 5 migration must increment the AI corpus version
  and publish a new reference baseline. This pre-Phase-5 v2 corpus must remain
  identifiable rather than silently persisting as a current difficulty anchor.

## Reproduction

From the repository root, verify and regenerate each descriptive analysis:

```bash
for run in p3_5_reference_a_v2 p3_5_reference_b_v2; do
  artifact="FungusToast.Simulation/bin/Debug/net8.0/SimulationParquet/$run"
  (cd "$artifact" && sha256sum -c resolved-manifest.sha256)
  FungusToast.Analytics/.venv/bin/python FungusToast.Analytics/analyze_balance.py \
    --run-folder "$artifact"
done
```

Replay corpus A with the recorded contract:

```bash
dotnet run --project FungusToast.Simulation/FungusToast.Simulation.csproj -- \
  --replay-manifest \
  FungusToast.Simulation/bin/Debug/net8.0/SimulationParquet/p3_5_reference_a_v2/resolved-manifest.json
```

Any change to a recorded schema, version, binary/fingerprint, analyzer hash, or
artifact checksum requires a new versioned baseline rather than replacement of
this document.

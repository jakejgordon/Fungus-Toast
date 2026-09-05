# AI P3.5 Starting-Adaptation Reference Baseline v3

**Analysis contract:** `fungus-toast.analysis.v2`

**Analyzer SHA-256:**
`c1381030239e8b2357bd0b86524d06ca511afa2f36c724cb153a05eb3dd0cb6d`

**Input / result / Parquet schemas:** `fungus-toast.experiment-input.v3` /
`fungus-toast.experiment-result.v7` / `v9`

**Random-stream contract:** `fungus-toast.random-streams.v1`

**AI corpus:** `fungus-toast.ai-corpus.phase5-starting-adaptations.v2`

**Balance identity (Core SHA-256):**
`30b7bf3892fe0b774b986321ba68929ec6c392a63eed378c8cd3e46e7950e0ce`

**Source commit:** `b127502a12f73b139f437bdcaebbf1a3d70ec305`

**Status:** descriptive reference; no player-facing balance classification,
causal claim, difficulty-band change, or promotion decision.

This corpus supersedes
[AI_P3_5_REFERENCE_BASELINE_V2.md](AI_P3_5_REFERENCE_BASELINE_V2.md) for
behavior-drift comparisons after the Phase 5 starting-Adaptation change. The v2
document and artifacts remain unchanged as the pre-Phase-5 reference. Do not
combine v2 and v3 rows into one estimate: v3 grants every AI its mold-matched
starting Adaptation before starting-spore effects and gameplay begin.

## Frozen artifacts

Both artifacts completed 50 games (200 player rows), reported zero parity
invariant mismatches, passed `sha256sum -c resolved-manifest.sha256`, and were
analyzed on 2026-09-04. They preserve the v2 strategy lineup, seeds, slot
rotation, board conditions, and disabled nutrients. The manifest's empty
`systems.startingAdaptations` array means there are no explicit additions; the
actual `playerStarts[].adaptationIds` correctly records the automatic mold
loadout.

| Corpus | Artifact folder | Manifest SHA-256 | Players SHA-256 | Condition |
|---|---|---|---|---|
| A | `FungusToast.Simulation/bin/Debug/net8.0/SimulationParquet/p3_5_reference_a_v3` | `d53b668e197f1b404c2ec096909d66109c0604787ec4770689c171f0d133060b` | `44c7c8e1e54a90c5fdb6720f5fedac8ba76458996edb8bc5bce06fa086dbd7be` | 50 games; seeds 310301–310350; 4-player 120x120 rectangle; mycovariants off |
| B | `FungusToast.Simulation/bin/Debug/net8.0/SimulationParquet/p3_5_reference_b_v3` | `659353d193a59ccfe3d45f4dbe7ec7b6e10682ef79c0876984fb0e8c4a7afa71` | `110601274bed79b8c370bb63cd79a2832094c546cb61d59e6894d07ecbe8eeea` | 50 games; seeds 310302–310351; 4-player 140x100 rectangle; mycovariants on |

The frozen lineup is `TST_Arch01_GrowthResilience`,
`TST_Arch03_FungicideSurge`, `TST_Arch04_DriftGrowth`, and
`TST_Arch06_SurgeGrowth`. Analytics joins them by stable strategy ID plus
definition fingerprint; names and themes are labels only.

Slots 0–3 receive `adaptation_20` (Oblique Filament), `adaptation_21`
(Thanatrophic Rebound), `adaptation_22` (Toxin Primacy), and `adaptation_23`
(Centripetal Germination), respectively. Rotating slots therefore exposes every
strategy to each of these four molds across the corpus instead of permanently
confounding one strategy with one mold. This reference does not exercise molds
4–7 or campaign-difficulty extras.

Both plans declare the `comparison` evidence stage and a shared 100-game
descriptive budget, but `analysis.hypothesis` is `null`. The analyzer therefore
cannot issue a verdict. Corpus A recorded 191.463 seconds of game runtime and
corpus B recorded 213.390 seconds; all games ended by
`board-occupancy-countdown`.

## Combined reference estimate

The table combines 100 games and 100 game-level observations per strategy.
Normalized board share has equal-share expectation 1.0; normalized rank ranges
from 0.0 (last) to 1.0 (first). Win credit is tie-aware and sums to one per
game. Intervals and robustness values use analysis v2.

| Strategy | Stable ID | Board share [95% CI] | Normalized rank [95% CI] | Win credit | Shrunken p10 / range |
|---|---|---:|---:|---:|---:|
| GrowthResilience | `legacy.testing.tst-arch01-growthresilience.v1` | 1.961 [1.898, 2.023] | 0.980 [0.964, 0.996] | 94/100 | 1.900 / 0.152 |
| DriftGrowth | `legacy.testing.tst-arch04-driftgrowth.v1` | 1.060 [1.000, 1.121] | 0.640 [0.611, 0.669] | 6/100 | 1.010 / 0.125 |
| SurgeGrowth | `legacy.testing.tst-arch06-surgegrowth.v1` | 0.756 [0.726, 0.786] | 0.377 [0.353, 0.401] | 0/100 | 0.747 / 0.023 |
| FungicideSurge | `legacy.testing.tst-arch03-fungicidesurge.v1` | 0.223 [0.209, 0.237] | 0.003 [-0.003, 0.010] | 0/100 | 0.203 / 0.050 |

The estimates remain descriptive measurements of this lineup under two
confounded conditions. Relative to v2, the mold loadout moved the combined
board-share estimates but did not change the aggregate 94/6/0/0 win-credit
split. That observation is characterization evidence only; it does not isolate
the effect of any Adaptation or authorize balance changes.

## Replay and validation evidence

Corpus A was regenerated in a separate process as
`p3_5_reference_a_v3_replay`. Source and replay verified the same canonical
outcome fingerprint:

`30abceca5e0d543c9ddd4a2acc230b88ed5c599df3b0c4928be786f8a65667f3`

They completed the same 50-game seed schedule with matching lineup,
definitions, AI corpus, RNG contract, starting loadouts, and outcomes. Runtime
and physical Parquet hashes are intentionally excluded from the deterministic
outcome fingerprint. The replay manifest checksum also passed.

Validation at this gate was:

- FungusToast.Core.Tests: 644 passed;
- FungusToast.Simulation.Tests: 19 passed;
- Python analytics tests: 8 passed;
- experiment-contract build/resume/replay/checksum fixture: passed;
- both source artifacts: complete, checksum-valid, zero parity mismatches;
- corpus A separate-process replay: exact outcome fingerprint verified;
- no preregistered verdict or paired-comparison artifact was produced.

## Evidence limits and lifecycle

- Only two four-player rectangular contexts and molds 0–3 are represented. No
  duel, crowded, swarm, masked-board, nutrient-on, molds 4–7, explicit extra
  loadout, or campaign-difficulty evidence is included.
- Corpus B changes both aspect ratio and mycovariant availability relative to
  A. Their difference cannot identify either effect.
- Fifty games per context is a descriptive comparison-stage snapshot. Win
  credit remains tertiary; normalized board share is primary for future
  declared tests and normalized rank is secondary.
- Observational mutation, mycovariant, synergy, and interaction tables remain
  non-evidential screens. A power claim requires a paired, preregistered
  intervention.
- Any later AI behavior change must increment `ai_corpus_version` and publish a
  new versioned reference instead of silently retaining this one as current.

## Reproduction

From the repository root, verify and regenerate the descriptive analyses:

```bash
for run in p3_5_reference_a_v3 p3_5_reference_b_v3; do
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
  FungusToast.Simulation/bin/Debug/net8.0/SimulationParquet/p3_5_reference_a_v3/resolved-manifest.json \
  --replay-experiment-id p3_5_reference_a_v3_replay --no-keyboard
```

Any change to a recorded schema, version, binary/fingerprint, analyzer hash, or
artifact checksum requires a new versioned baseline rather than replacement of
this document.

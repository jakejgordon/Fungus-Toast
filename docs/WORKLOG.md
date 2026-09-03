# Fungus-Toast Worklog

## Current Status

The Substrate Ecology build is complete. Its implemented roster is Aerated
Frontier, Crustward Tropism, Compaction Pressure, Detrital Enzymes, Toxin
Margin, Necrophytic Bloom, and Toxinborne Seeding.

All previously listed Ecology balance probes and follow-up implementation tasks
are considered complete. The current Aerated Frontier calibration uses
`AeratedFrontierMinimumEligibleGrowthCycleAge = 5`.

## Remaining Validation

- Perform manual Unity Editor validation of the Substrate Ecology mutation tree,
  inspector, dependency routes, and visual flows at supported resolutions.

## Active Initiative — AI Architecture and Balance Overhaul

### Objective

Build a simpler, more effective AI strategy system and an autonomous,
reproducible evaluation workflow that can maintain a diverse roster of
campaign and single-player opponents across measurable difficulty levels.

### Scope and Decisions

- The current `ParameterizedSpendingStrategy` approach may be simplified,
  redesigned incrementally, or replaced entirely. Preserve it only where the
  investigation shows that it remains the best foundation.
- Cover the complete AI decision surface rather than mutation spending alone:
  mutation choices and ordering, mycovariant drafting, surge timing,
  adaptations, starting-position effects, and reactive board-state decisions.
- Every experimental variable must be independently controllable so tests can
  isolate causes. At minimum, this includes strategy logic, opponent lineup,
  player count, map size and geometry, starting slot, nutrient patches,
  mycovariants, adaptations, seeds, and other enabled gameplay systems.
- Target maximum safe autonomy. The workflow should generate candidate
  strategies or revisions, run controlled experiments, analyze results,
  iterate, and retest against holdout conditions with minimal human
  intervention. Promotion into player-facing pools remains evidence-based.
- Establish empirical AI performance bands rather than relying only on authored
  labels. Use parity-normalized final board share as a primary measure, where
  `1.0` means the strategy achieved the equal-share expectation for the active
  player count. Combine it with win-rate surplus, average rank, uncertainty,
  and robustness across seeds and opponent compositions.
- Treat map size and geometry as explicit performance dimensions. A strategy
  may have different small-, medium-, and large-map bands in addition to an
  overall classification; do not hide material contextual strengths or
  weaknesses inside one global difficulty label.
- Defining the target roster size and its distribution across difficulty bands
  and major strategic archetypes is part of this initiative, not a prerequisite
  supplied in advance.

### Planned Work

The canonical step-by-step plan, architecture hypothesis, experiment contract,
measurement model, phase gates, and open product decisions are in
`FungusToast.Core/docs/AI_ARCHITECTURE_BALANCE_OVERHAUL.md`.

1. Audit the current strategy representation, decision precedence, duplicated
   configuration, reactive capabilities, roster metadata, and authoring cost.
2. Audit the simulation and analytics pipeline for confounding variables,
   missing metrics, reproducibility gaps, weak statistical gates, and work that
   still requires manual interpretation.
3. Specify the replacement or revised AI architecture and a machine-readable
   experiment contract that exposes all isolation controls.
4. Implement an autonomous candidate-search and evaluation loop with staged
   smoke, calibration, comparison, and holdout validation gates.
5. Calibrate contextual performance bands across representative player counts,
   map sizes/geometries, seeds, and opponent pools.
6. Define the desired roster matrix by difficulty and archetype, identify gaps,
   and create or revise strategies to fill them.
7. Validate the resulting campaign and single-player pools, then document the
   promotion, regression, and periodic recalibration workflow.

### Current Queue

1. Phase 0 and both Phase 1 audit slices are complete. The evidence inventory,
   parity corpus, risk matrix, and Phase 2 ordering are in
   `FungusToast.Core/docs/second-level/AI_OVERHAUL_PHASE_0_1_AUDIT.md`.
2. P2-A is complete: Simulation now owns a versioned input-manifest model,
   strict structural and semantic validation, CLI-to-manifest translation, an
   enforced 100-game condition ceiling, an example JSON fixture, and a focused
   Simulation test project. Core AI behavior is unchanged.
3. P2.2 is complete: each Parquet export now includes a
   checksummed `resolved-manifest.json` with complete condition controls, exact
   selected and assigned lineups, actual starts/loadouts, code/config/geometry/
   strategy/outcome fingerprints, artifact hashes, and the seed schedule used
   by the runner. `--replay-manifest` performs strict code/strategy checks and
   fails unless the canonical outcome fingerprint matches; the separate-process
   replay smoke passed.
4. P2.3 is complete. `games.parquet` now carries the complete resolved causal
   controls and actual per-game assignments/starts/loadouts; `players.parquet`
   carries the player-specific realized context. A fixture with exact positions
   and two different Adaptations was read back successfully, and the existing
   offline analytics workflow completed against the expanded schema.
5. P2.4 is complete. `--compare-manifests <control> <treatment>` diffs causal
   replay inputs, while `--allow-differences` acts as the treatment hypothesis.
   Undeclared differences and declared paths that did not change both fail. A
   nutrient-toggle pair passed with the exact allowed path and failed without
   it.
6. P2.5 is complete. Each condition writes fingerprinted durable run state;
   `--resume` revalidates and skips a matching complete artifact, retries
   missing/failed/interrupted conditions, and refuses a completed artifact with
   a different execution fingerprint. The resume skip and mismatch refusal
   passed end to end. Offline analysis regeneration was byte-identical across
   repeated runs.
7. P2.6 is complete. The checked-in experiment-contract fixture proves a
   two-condition run can be selectively resumed after adding a third condition,
   then replays one completed condition in a separate process with an identical
   canonical outcome and a valid resolved-manifest checksum.
8. The legacy random mutation spender now uses its caller-provided seeded RNG;
   the unused base-strategy static RNG was removed. A regression test proves
   equal seeds produce equal portfolios and a different seed changes the
   result.
9. The resistant-cell Hyphal Resistance Transfer hook now requires an injected
   RNG, and its Unity caller uses `GameManager`'s gameplay stream. A focused test
   proves the supplied source controls the transfer rolls. Jake confirmed the
   Unity compile and affected flow work normally on 2026-09-02.
10. The Unity double-spending path is fixed at the source boundary. Core now
    exposes point-income-only mutation-phase setup for interactive front ends;
    normal Unity uses it and defers AI strategy execution until humans finish.
    Simulation and fast-forward retain the one-step income-plus-spend contract.
    Focused Core tests cover both paths, and the replay/resume fixture still
    passes.
11. Jake confirmed the Unity integration gate on 2026-09-02: Unity compiled
    cleanly, AI mutation purchases occurred once after human mutation turns,
    and Mycelial Bastion plus Hyphal Resistance Transfer worked normally.
12. P3.1 is complete. `players.parquet` records each game's total living-cell
    denominator, tie-aware competition rank, tie-group size, starting
    slot/coordinates, Adaptations, and raw condition ID. It also records
    structured condition dimensions for grouping without parsing the ID:
    input schema, strategy/selection and slot policies, start-position mode,
    player count, and board geometry fingerprint.
13. P3.2 is complete: offline analytics now computes normalized board share,
    win-rate surplus, tie-aware normalized rank, intervals, effect sizes, and
    context robustness summaries. P3.3 is complete: staged static, smoke
    (3–5), calibration (20), comparison (50), and holdout (100) gates prevent
    early promotion and retain the 100-game per-condition limit. Next: P3.4
    contextual map/player-count taxonomy.
14. P3.4 is complete. The manifest-derived taxonomy covers duel/small-table/
    crowded/swarm player counts; small/medium/large board area; square/wide/tall
    aspect; rectangle versus mask-fingerprint geometry; and generated/exact/
    preferred-pool starts.
15. P3.5 is complete. The locked artifact-backed reference baseline is in
    `FungusToast.Core/docs/second-level/AI_P3_5_REFERENCE_BASELINE_V1.md`.
    It verifies two complete 50-game, 4-player reference artifacts and locks
    `p3.5.reference-baseline.v1`; it explicitly withholds player-facing
    classification and causal balance claims. Next: P4.1 characterization
    tests for the current AI decision surfaces.

### Completion Criteria

- Strategy authoring and behavior are materially simpler or more capable than
  the current baseline, with the architectural choice supported by evidence.
- Controlled experiments can isolate every agreed gameplay and environment
  variable and reproduce their manifests and results.
- The autonomous workflow can propose, test, reject, and promote candidates
  without routine human steering while retaining explicit safety and evidence
  gates.
- Every player-facing AI has measured contextual performance metadata, and the
  final roster covers the initiative-defined difficulty/archetype matrix.
- Campaign and single-player validation artifacts support the final pool and
  difficulty assignments.

## Working Rules

- Keep new work as a small, independently validated and committed slice.
- Refresh the checked-in Unity Core DLL/PDB when Core behavior or API changes.
- Record only current work, decisions, validation results, and genuinely pending
  follow-ups here; retain detailed historical evidence in commits, test output,
  and simulation artifacts.

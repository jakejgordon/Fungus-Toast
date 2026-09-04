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
15. P3.5 v1 is preserved as a historical artifact-backed reference baseline in
    `FungusToast.Core/docs/second-level/AI_P3_5_REFERENCE_BASELINE_V1.md`.
    It verifies two complete 50-game, 4-player artifacts and explicitly
    withholds player-facing classification and causal balance claims.
16. An adversarial review accepted on 2026-09-03 reopened P3.2, P3.3, and P3.5
    before Phase 4. The approved correction program is documented in the
    canonical initiative plan as P3.R1–P3.R7.
17. Current queue: implement P3.R1 cheap correctness, P3.R2 analytical identity,
    P3.R3 hierarchical RNG, P3.R4 paired inference, P3.R5 enforceable gates,
    P3.R6 derived context/version/runtime evidence, and P3.R7 corrected baseline.
    Phase 4 characterization remains blocked until that correction gate passes.
18. P3.R1a is complete: Simulation manifest execution rejects insufficient
    registered rosters instead of synthesizing numbered `LegacyRandom` seats;
    player-summary analysis requires `condition_id`; mutation and mycovariant
    picked/not-picked denominators are corrected; the endogenous reconstructed
    mutation-eligibility gate is removed; and observational association,
    synergy, and interaction outputs are explicitly non-evidential. Focused
    Simulation and Python analytics tests pass. Next: P3.R1b tie-aware outcomes.
19. P3.R1b is complete: final ties no longer select the earliest player as the
    sole winner. Results retain all winner IDs, assign fractional win credit,
    export explicit winner/co-winner/loser status, and use credit in downstream
    Simulation and offline analytical scores. Legacy P3 artifacts derive the
    same credit from their already-exported tie-aware rank fields. Fifteen
    Simulation tests, three Python analytics tests, a one-game Parquet smoke,
    and the separate-process replay/resume contract fixture passed. Next: P3.R2
    stable analytical identity.
20. P3.R2 is complete: every registered strategy receives an additive stable
    `legacy.<set>.<slug>.v1` ID and a deterministic behavior-configuration
    fingerprint. Result schema v2 records both in resolved lineups and every
    strategy-bearing Parquet dataset; analytics groups on ID plus fingerprint
    and treats name/theme as labels. Legacy artifacts receive explicitly marked
    fallback identities during regeneration. Core's definition-schema version
    must be bumped for behavior changes not represented by fingerprinted fields.
    Full Core, Simulation, Python analytics, Parquet identity smoke, and replay
    contract validation passed. Next: P3.R3 hierarchical RNG streams.
21. P3.R3 is implemented as `fungus-toast.random-streams.v1`. The historical
    seeded gameplay sequence remains intact, while roster selection, mutation
    spending, and interactive/simulated/fast-forward mycovariant decisions use
    deterministic SHA-256-derived streams keyed by seed, player, round,
    decision kind, and occurrence. `AIRoster.GetStrategies` now requires an
    explicit RNG. Result schema v3 and Parquet manifest v6 stamp the contract.
    A regression test proves 1,000 extra AI draws cannot perturb the gameplay
    stream; 620 Core tests, 15 Simulation tests, and the separate-process
    replay/resume contract pass. Unity Editor compile and normal/fast-forward AI
    spending plus draft validation remain part of the manual integration gate.
    Next: P3.R4 paired inference.
22. P3.R4 is complete. Input schema v2 accepts a validated shared pairing-group
    ID; result schema v4 and Parquet manifest v7 emit pair IDs per game and
    assigned slot. The offline analyzer accepts a treatment artifact, refuses
    incomplete pairs and mismatched seeds/starts/board/RNG controls, and writes
    paired differences with 95% intervals, observed correlation, and measured
    paired-versus-unpaired variance ratios for normalized board share, rank,
    and fractional win credit. Six analytics tests, 16 Simulation tests, and a
    real three-game control/treatment Parquet round trip passed. The smoke
    variance ratio was reported empirically rather than assumed. Next: P3.R5
    enforceable preregistered gates.
23. P3.R5 is complete. Input schema v3 requires analysis version, evidence
    stage, total-game budget, and runtime budget; requested conditions cannot
    exceed the game budget, and execution stops between games when runtime is
    exhausted. Smoke/calibration/comparison/holdout counts are enforced as
    3–5/20/50/100. Decision-bearing plans are limited to comparison/holdout and
    preregister one pairing context, stable target strategy, metric, paired
    estimand, direction, and margin. The analyzer emits a verdict only with
    explicit `--emit-verdict` and matching complete plans. Eight analytics and
    18 Simulation tests pass. A real 50-pair non-inferiority run emitted the one
    declared board-share verdict; exploratory v4 artifacts were correctly
    refused. Next: P3.R6 geometry, robustness, version, termination, and runtime
    evidence.
24. P3.R6 is complete. Result schema v6 and Parquet manifest v9 stamp analysis
    v2 and the pre-Phase-5 AI corpus; game evidence records measured runtime and
    round-cap versus occupancy-countdown termination; player evidence records
    first elimination round and opponent-lineup fingerprint. Starting-position
    controls are derived from actual starts and the full playable mask as
    eight-neighbor geodesic distance to nearest opponent/edge and distance to
    playable centroid. Robustness now uses a 20-game-prior shrunken 10th
    percentile/range instead of raw min-of-K. Runtime is excluded from replay's
    deterministic outcome hash but is summed against the shared verdict budget.
    620 Core, 18 Simulation, and 8 analytics tests pass; a three-game Parquet
    readback and the replay/resume contract passed. Next: P3.R7 corrected
    reference baseline.
25. P3.R7 is complete. Result schema v7 stamps the AI corpus in the
    replay-authoritative resolved manifest and rejects stale corpus replays.
    The two historical 50-game P3.5 conditions were rerun under isolated RNG,
    tie-aware outcomes, stable strategy identity, analysis v2, and corrected
    robustness fields. Both v2 artifacts passed manifest checks; corpus A
    reproduced its canonical outcome in a separate process; and the canonical
    resume/replay fixture passed. The immutable corrected record is
    `FungusToast.Core/docs/second-level/AI_P3_5_REFERENCE_BASELINE_V2.md`.
    Phase 4 characterization is now unblocked; no balance verdict was issued.
26. The pre-architecture mycovariant correctness fix is complete.
    `IMutationSpendingStrategy` now requires explicit draft selection;
    parameterized and intentionally random implementations each own their
    behavior; and `MycovariantDraftManager` no longer downcasts or silently
    substitutes random selection. A non-parameterized dispatch regression,
    621 Core tests, and 18 Simulation tests pass. The checked-in Unity Core
    DLL/PDB were refreshed; Unity Editor compile and normal/fast-forward draft
    validation remain manual integration checks.
27. The additive strategy-definition registry foundation is complete. Each
    registered strategy now has one immutable record binding behavior, stable
    ID, definition fingerprint, and catalog metadata. Simulation manifests,
    replay validation, profiles, and live metadata exports consume that record.
    Duplicate bootstrap metadata keys and orphaned override names now fail
    loudly. The audit exposed and removed three orphan overrides and fixed a
    duplicate `AI13` role assignment that had silently overwritten `Boss` with
    `Training`; its strong hard/elite boss metadata is now regression-tested.
    All names remain unchanged, and roster selection now rejects any request
    that would require a fabricated numbered `LegacyRandom` seat. 624 Core
    tests, 18 Simulation tests, and a
    three-game checksum-valid export smoke pass; Unity plugin binaries were
    refreshed.
28. Phase 4's architecture spike is pre-registered before either prototype.
    Its fixed backlog fixtures are trailing-state defensive fallback, a
    board-aware preferred-surge window, and owned-mutation-category-aware
    mycovariant drafting. Both designs must pass identical cases, preserve
    1,000 disabled-behavior legacy fixtures, stay within a 5% runtime envelope,
    and meet a symmetric 20% authoring-cost or unrelated-path isolation rule
    for a decisive verdict. Throwaway spike code belongs under ignored `TEMP/`.
29. The Phase 4 bake-off is complete and the full composed controller is
    invalidated for the current backlog. Both throwaway designs passed all
    three behavior fixtures and 1,000 disabled-feature parity cases. The
    revised parameterized design used 56.3%/44.4% fewer logical lines for B1/B2
    and was 21–25% faster across five fresh-process, one-million-decision runs;
    composition's better path isolation did not overcome the frozen 5% runtime
    gate. Retain `ParameterizedSpendingStrategy`, add only focused pure helpers
    or demonstrated typed seams, and keep traces opt-in. Decision record:
    `FungusToast.Core/docs/second-level/AI_PHASE4_ARCHITECTURE_DECISION.md`.
30. P4.1 current-behavior characterization is complete. New public-path tests
    pin authored mycovariant preferences and the absolute always-pick override,
    scheduled-versus-last-resort surge ordering, pre-window surge banking,
    exclusion filtering, board-aware Tendril choice, and outward/inward start
    offsets. Existing target/prerequisite tests cover sequential mutation
    spending. The focused slice passes 16/16 and the full Core suite passes
    632/632; no production AI behavior changed.
31. The remaining concrete-type dependency in mutation acquisition is removed.
    Mutator Phenotype free upgrades now consume `ExcludedMutationIds` through
    `IMutationSpendingStrategy`; the strategy base supplies the empty default
    and parameterized strategies override it. This also fixes a default-interface
    dispatch trap that initially bypassed parameterized exclusions. Both the
    existing parameterized case and a new non-parameterized implementation are
    regression-tested. Core passes 633/633 and Simulation passes 18/18; the
    checked-in Unity Core DLL/PDB were refreshed.

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

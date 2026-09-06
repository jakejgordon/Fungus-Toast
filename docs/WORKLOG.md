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

### 2026-09-05 Hoardspore start-offset diagnosis

- Baseline: `elite_fixed8_no_adapt_500_c00` through `c09`; treatment:
  `elite_fixed8_hoard_offset0_500_c00` through `c09`.
- Fixed controls: the same eight-strategy Campaign Elite lineup, 160x160 board,
  rotating slots, contiguous seeds `2026090400..2026090899`, nutrient patches
  off, Mycovariants off, and starting Adaptations off. The sole treatment was
  Hoardspore Regent's strategy-owned starting-spore edge offset changing from
  `+6` to `0`.
- Across 500 matched games, Hoardspore's win credit increased from `74.5%` to
  `78.4%` (paired delta `+3.9` percentage points; bootstrap 95% interval
  `-1.2..+9.0`, sign-flip `p=0.141`). Its mean board share increased from
  `28.31%` to `29.74%` (delta `+1.43` points; bootstrap 95% interval
  `+0.72..+2.14`, `p<0.001`), and mean rank improved from `1.302` to `1.236`
  (delta `-0.066`; bootstrap 95% interval `-0.128..-0.004`, `p=0.043`).
- Conclusion: the `+6` offset does not explain Hoardspore's dominance; it is a
  measurable territorial handicap. Continue diagnosis inside its mutation
  sequencing/economy behavior rather than treating the offset as an advantage.

### Proposed — corpse-denial Mycelial Surge

- Working name: `Autolytic Clearance`. This name is provisional because the
  Mycelial Surges category already contains `Autolytic Surge`; choose a unique
  final first word before implementation.
- While active, before each Growth Phase, consume a limited number of the
  owner's dead cells, prioritizing corpses adjacent to enemy living cells.
- Intended counterplay: deny enemy Necrohyphal Infiltration targets while
  sacrificing the owner's opportunities to use Regenerative Hyphae, Catabolic
  Rebirth, or other corpse-reclamation effects on those cells.
- Keep it broadly useful against reclamation strategies rather than keying it
  specifically to Putrefactive Mycotoxin. Tier, prerequisites, duration,
  activation cost, per-level corpse limit, UI treatment, and analytics remain
  intentionally undecided until implementation is scheduled.

### 2026-09-05 Necrohyphal Infiltration corpse-age treatment

- Gameplay change: Necrohyphal Infiltration can reclaim an enemy corpse only
  after it has been dead for at least five completed Growth Cycles. The same
  eligibility rule applies to the initial reclaim and every cascade target.
- Implementation: each newly killed cell records one nullable absolute death
  Growth Cycle. Round-start snapshots persist it additively; legacy corpses
  without a timestamp remain immediately eligible to preserve old-save
  behavior.
- Control artifacts: `elite_fixed8_hoard_offset0_500_c00` and `c01` (the first
  100 games of the prior no-offset baseline). Treatment artifact:
  `elite_fixed8_infiltration_age5_100`.
- Fixed controls: the same eight-strategy Campaign Elite lineup, 160x160 board,
  rotating slots, seeds `2026090400..2026090499`, nutrient patches off,
  Mycovariants off, starting Adaptations off, and Hoardspore's start offset
  overridden to zero. Both runs completed all 100 paired games with no parity
  mismatches.
- Hoardspore's Necrohyphal Infiltration living-cell output fell from `451.41`
  to `312.28` cells/game: paired delta `-139.13` (`-30.8%`), bootstrap 95%
  interval `-173.81..-105.03`.
- Hoardspore's win credit fell from `81%` to `73%` (delta `-8` percentage
  points; interval `-17..+1`), mean board share fell from `29.59%` to `28.72%`
  (delta `-0.87` points; interval `-2.17..+0.44`), and mean rank changed from
  `1.21` to `1.33` (delta `+0.12`; interval `+0.01..+0.23`).
- Conclusion: the cooldown materially and specifically reduces Infiltration's
  cell production without disabling the mutation. Hoardspore remains dominant,
  so treat this as a successful first moderation rather than a complete balance
  solution; continue human-counterplay work before applying another numerical
  nerf.

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
32. Phase 5 registry metadata validation now includes suggested Adaptation-set
    keys, so an Adaptation override for an unregistered strategy fails during
    roster initialization like every other metadata override. The atomic
    registry invariant also validates non-empty sets, duplicate IDs, and every
    referenced Adaptation ID, and now initializes `AIRoster` explicitly instead
    of depending on test order. The canonical plan was reconciled with the
    Phase 4 verdict: no legacy controller adapter or universal trace graph, no
    name reset, and automatic Adaptation resolution requires an explicit
    scenario grant count. Core passes 633/633 and Simulation passes 18/18; the
    checked-in Unity Core DLL/PDB were refreshed.
33. Final post-Phase-4 experiment-contract validation passes on the integrated
    code state. `verify-experiment-contract.sh` produced a checksum-valid source
    artifact and exact replay for experiment
    `contract_20260904T222728_526758`. Remaining Phase 5/6 work changes product
    behavior and needs an explicit scenario Adaptation grant-count rule or a
    pre-registered AI treatment hypothesis before implementation.
34. P5.6 AI starting-Adaptation rules are approved and implemented. Every AI
    gets the Adaptation matching its assigned mold icon. Campaign difficulty
    adds Training/Easy/Medium/Hard/Elite/Boss = 0/1/2/3/4/5 extras; authored
    fixed/pool additions count toward the quota and may exceed it, then the
    resolver fills without replacement from themed suggestions before global
    non-starting fallback. Selection uses a purpose-scoped per-player RNG.
    Simulation applies mold identity by slot before spore placement and treats
    explicit loadouts as additive. The behavior change increments the corpus to
    `fungus-toast.ai-corpus.phase5-starting-adaptations.v2`. Core currently
    passes 644/644 and Simulation 19/19. The versioned
    `AI_P3_5_REFERENCE_BASELINE_V3.md` freezes two new 50-game corpora; both
    completed with zero parity mismatches and valid checksums, corpus A matched
    its separate-process replay outcome, and all 8 analytics tests pass. Jake
    confirmed the Unity Editor compile and affected game flows on 2026-09-05,
    closing the Phase 5 gate.
35. The preregistered Phase 6 economy-bias pilot compared otherwise matched
    Minor, Moderate, and Max Balanced Control definitions for 100 games with
    rotating slots and Adaptations/nutrients/Mycovariants disabled. Neither
    candidate advanced: paired normalized-share lift versus Moderate was
    `+0.0116` (adjusted 97.5% CI `-0.0922..+0.1154`) for Minor and `+0.0242`
    (`-0.0763..+0.1247`) for Max, below the `+0.05` threshold with both
    intervals crossing zero. The run completed in 305.200 seconds with zero
    parity failures, valid checksums, balanced slot exposure, and exact replay
    outcome fingerprint
    `99862d893ed77f5ba541a7517f7ad4ab92587c5b6953d1ce3c908b7a5cc1c67c`.
    Per the frozen gate, do not build the candidate framework or retune a
    player-facing strategy from this result; fallback economy bias alone was
    not a useful promotion lever.
36. The next preregistered screen isolated Balanced Control's first two goal
    positions. Across 100 two-player games, Anabolic-first beat Creeping-first
    by `+0.7509` normalized board share (95% CI `+0.6335..+0.8683`) and 88% to
    12% win credit. All games completed in 325.002 seconds with zero parity
    failures, valid checksums, empty starting loadouts, and exactly balanced
    slots. This passes the advancement threshold; an unseen 140x100,
    seed-`2026090503` holdout is frozen in the canonical plan before execution.
37. The frozen Balanced Control opener-order holdout confirmed the candidate in
    the unseen 140x100 context. Across 100 games, Anabolic-first beat
    Creeping-first by `+0.8590` normalized board share (95% CI
    `+0.7512..+0.9668`) and 94% to 6% win credit. All games completed in
    292.770 seconds with zero parity failures, empty starting loadouts, valid
    checksums, and exactly balanced slots. A separate-process replay exactly
    reproduced outcome fingerprint
    `6283eca7257865fc5b9ab837815170196ca288e2c32f2577627cca0c5a3dbacd`.
    The Phase 6 gate now permits bounded candidate tooling around proven safe
    dimensions such as mutation-goal order; the Testing candidate remains out
    of player-facing pools pending explicit review.
38. P6.1 is complete. `fungus-toast.ai-candidate-genome.v1` in
    `FungusToast.Simulation/Candidates` defines a bounded, serializable
    candidate genome over ten genes covering the entire
    `ParameterizedSpendingStrategy` behavior surface. It sits beside the Phase 2
    experiment contract rather than in Core, so no AI behavior changed and the
    AI corpus version is unaffected. Genomes state their gene set in full,
    derive `candidateId` from parent lineage plus a SHA-256 behavior
    fingerprint, and declare `variedGenes`; validation fails both undeclared
    differences and declared genes that did not change, making single-variable
    treatments structural rather than procedural. Semantic bounds reject
    unknown mutation/mycovariant/category IDs, non-surge surge entries,
    out-of-range target levels, goal/exclusion overlap, and preferences not in
    Core's evaluation order. A round trip over all 132 registered parameterized
    strategies proves an extracted gene set re-materializes to Core's exact
    definition fingerprint; that test caught two first-draft bounds that
    contradicted real roster content, and established that a repeated mutation
    goal is a legitimate rising-level ladder. 48 Simulation tests and 649 Core
    tests pass, and the experiment-contract script's selective resume and exact
    replay both passed. Next: P6.2 deterministic candidate generation,
    deduplication, and lineage.
39. P6.2 is complete. `fungus-toast.ai-candidate-plan.v1` adds a validated
    generation plan and a generator that enumerates candidates exhaustively
    rather than sampling, so it takes no seed: the same plan against the same
    registry always yields identical candidates, names, lineage notes, and
    rejections in identical order. A plan whose enumeration exceeds its own cap
    fails before generating. Seven single-gene operators cover goal order,
    economy bias, high-tier preference, surge frequency, edge offset, and max
    tier; the three sweeps require declared value lists, and a value list
    without its operator is refused. Deduplication is by behavior fingerprint
    and emits typed rejections for parent duplicates, earlier-candidate
    duplicates, registered-strategy duplicates, and validation failures. Real
    roster behavior exercises all four: swapping Balanced Control's first two
    goals reconstructs `TST_BalancedControl_AnabolicFirst` and is refused, and
    promoting goals in `TST_Arch06_SurgeGrowth` inverts its rising level ladder
    into three validation rejections. 68 Simulation tests and 651 Core tests
    pass. The slice also exposed a latent non-AI defect:
    `AnalyticsEventSubscriber` kept a process-wide non-concurrent dictionary
    that tore once a second Simulation test class ran in parallel, fixed
    separately in `b3e1807` with no effect on outcomes, replay fingerprints, or
    the corpus version. Next: P6.3 staged evaluation.
40. P6.3 is partially complete: both zero-game stages are done. Static
    validation is the existing plan/genome/rejection layer, and the new
    `CandidateCharacterizationGate` adds the unit/characterization stage — it
    materializes each candidate and checks that it constructs, spends without
    throwing, buys something, honors its own declared exclusions, drafts an
    offered mycovariant, and repeats all of it identically under a fixed seed.
    Generating from all 132 registered parameterized parents produced 1,559
    proposals and 1,247 accepted candidates, and all 1,247 passed
    characterization in 2.5 seconds with zero findings; that is both a
    no-false-positive result and independent confirmation of AI seed
    determinism across 1,247 configurations. Because every failure the gate
    detects is unreachable through real materialization, `Run` takes an
    optional materializer so tests can prove each of the seven checks fires.
    78 Simulation tests and 651 Core tests pass. Still outstanding for P6.3:
    the three game-consuming stages. Those need candidates addressable by the
    manifest pipeline, which resolves strategies through `StrategyRegistry` by
    set and name, so the next slice is the generated testing catalog, then
    per-stage manifest emission and progression rules.
41. The generated testing catalog the Phase 6 gate calls for now exists.
    `GeneratedCandidateCatalog` publishes candidates into a new
    `StrategySetEnum.Generated` that `AIRoster` never registers, so a manifest
    naming a published candidate passes the ordinary
    `ExperimentManifestValidator` and its evaluation inherits the existing
    resolved-manifest and replay guarantees instead of bypassing them. Two
    additive Core changes support it: `StrategyRegistry.Register` takes an
    optional stable-ID factory so a candidate keeps its own `candidate.*`
    identity rather than being minted a second `legacy.*` one, and
    `AIRoster.AuthoredStrategySets` names the four registered sets so
    authored-content invariants stop iterating every enum value — two Core
    tests had assumed those were the same thing. Every published candidate
    carries `StrategyPool.None`, no difficulty band, and no campaign
    difficulty, so no pool-filtered selection path can reach one. Publishing is
    wholesale rather than incremental. Making candidates registrable also
    exposed a P6.1 assumption: the display-name collision rule checked every
    set, so re-publishing a candidate saw itself as a collision; it now
    excludes the generated set. 86 Simulation tests and 651 Core tests pass,
    Unity compiles against the refreshed Core plugin, and the experiment
    contract's selective resume and exact replay both passed. Next: per-stage
    manifest emission and progression rules for the three game-consuming
    stages.
42. Jake chose the evaluation shape for the remaining P6.3 stages on
    2026-09-06: a paired two-artifact swap (control runs the parent, treatment
    runs the candidate, matched seeds/slots/board/RNG) against one fixed
    opponent in a two-player context, with a holdout that must change both
    board geometry and seed. That choice exposed a latent pipeline defect and
    it is now fixed. Per-condition artifact IDs were derived from the
    condition's *shape* (players, board, strategy set), which is unique only
    because the CLI happens to encode exactly that shape into the condition ID
    it generates. A hand-authored manifest whose two conditions differ only in
    lineup — precisely a paired control/treatment comparison — derived one
    artifact ID, so the second run overwrote the first's export, or under
    `--resume` failed with an execution fingerprint mismatch that pointed
    nowhere near the cause. `ExperimentArtifactId` now derives from the
    condition ID, which the manifest already validates as unique. Artifact
    folders are renamed accordingly (`__p2_w20_h20_sTesting` becomes
    `__p2_w20_h20_s_testing`), and `verify-experiment-contract.sh` was updated
    and passes end to end. 93 Simulation tests and 651 Core tests pass.
    Note: the code for this fix was swept into unrelated commit `9f76786` by a
    concurrent session sharing this working tree; only its tests and this
    record are in the commit that names it. Next: per-stage manifest emission
    and progression rules.
43. The second blocker for the approved evaluation shape is closed. A condition
    names exactly one strategy set, but a treatment arm needs a generated
    candidate beside an authored opponent, so `GeneratedCandidateCatalog` now
    publishes an evaluation cast: candidates plus the authored references they
    are measured against. Per-name set qualification in the manifest was
    considered and rejected — Parquet records the strategy set once per run, so
    it would turn a run-level invariant into a per-player one and ripple
    through the input schema, resolved manifest, replay runner, export schema,
    and every set-grouping analytic, with three schema bumps that would
    invalidate replay of existing artifacts. A republished reference keeps its
    authored stable ID and definition fingerprint, so `strategy_id` still
    identifies a control arm exactly; only the set label changes. Reference
    metadata is copied verbatim except pools, because
    `StrategyRegistry.GetDefinition(IMutationSpendingStrategy)` resolves by
    reference across every set and a faithful copy makes it irrelevant which
    registration a metadata lookup finds — regression-tested against
    `GetThemeForStrategy` and `GetFavoredAgainstForStrategy`. A paired
    control/treatment manifest with matched players, board, set, and pairing
    group now validates and derives two distinct artifact IDs. 98 Simulation
    tests and 651 Core tests pass, and the experiment contract verified end to
    end including its checksum. Next: per-stage manifest emission and
    progression rules.
44. P6.3 is complete and the candidate loop now runs end to end: generate,
    characterize, publish, emit, run both arms, verdict.
    `CandidateEvaluationEmitter` emits a stage as two single-condition runs
    rather than one two-condition manifest, because the batch runner derives
    conditions from config strata and cannot express two conditions of
    identical shape differing only in lineup, while the analyzer pairs two
    artifact folders. Progression is refused rather than warned about, and a
    holdout must change both board and seed. Three blockers surfaced only by
    trying to run it. The verdict could not describe a strategy swap at all:
    it matched its target row requiring the same strategy ID in both arms, so
    a swap found no rows; an optional `controlStrategyId` fixes it, and
    omitting it preserves the original behavior exactly. This corrects the
    claim made when the shape was chosen — the paired machinery did not
    already support a swap. The runner also rejected the control arm because
    it required the hypothesis target in the lineup, and candidates did not
    exist for the simulator at all, since it runs as its own process with an
    empty generated set; `--candidate-catalog` loads a serialized evaluation
    cast before any lineup resolves. Input schema is now
    `fungus-toast.experiment-input.v4`; `controlStrategyId` is omitted when
    unset so existing manifests serialize byte-identically and the result
    schema stays at v7. A real comparison ran through the new path: both
    50-game arms executed from emitted command lines, and the analyzer issued
    a preregistered verdict of `not_supported` (estimate `-0.0173`, 95%
    interval `[-0.0564, +0.0218]`, margin `0.05`, 50 complete pairs). 122
    Simulation, 651 Core, and 10 analytics tests pass; the experiment contract
    verified end to end. Next: P6.4 pruning, budgets, retry caps, and
    resumable queues.
45. P6.4 is complete. `CandidateEvaluationQueue` holds durable, resumable state
    for running a field of candidates through the ladder, breadth-first by
    stage so a field is thinned at 10 games a head rather than 200. Retry caps
    distinguish two failure kinds, which is the safeguard that matters most: an
    integrity failure says nothing about the candidate and may be retried to the
    cap, while an evidence failure answers a preregistered question and stops
    the candidate immediately with no retry at any cap — retrying a lost
    hypothesis until it passes would turn the staged gates into a search for a
    favorable sample. Budgets are checked before dispatch rather than after, so
    a stage that cannot fit is never started; the runtime budget projects from
    the queue's own measured throughput rather than an authored guess. Two
    interval-based pruning rules: futility (the optimistic bound is below the
    margin) and domination (another candidate's whole interval sits above
    theirs, compared only within one stage since intervals from different game
    counts are not comparable). The 100-game ceiling holds because it is per
    batch and each arm is its own batch, so a holdout is two runs of 100. 138
    Simulation, 651 Core, and 10 analytics tests pass. Next: P6.5 ranking
    strength and robustness separately from archetype fidelity.
46. P6.5 is complete. `CandidateRanking` reports four independent measures and
    deliberately publishes no composite score, since collapsing them would let a
    strong-but-fragile candidate outrank a steady one or hide that a field
    converged on one behavior. Strength orders by the interval's lower bound
    rather than the point estimate. Robustness reports the worst context and
    whether the estimate kept its sign, and treats one context as not
    demonstrating robustness at all. Lineage fidelity compares observed category
    profiles against the parent's actual behavior rather than an authored
    archetype label, because generated candidates carry placeholder metadata
    until Phase 7 assigns bands from evidence. Behavioral diversity uses the raw
    per-mutation build instead, because category granularity is right for
    fidelity but collapses genuinely different candidates onto identical
    vectors. Over a seven-candidate field fidelity ranged `0.836`-`1.000` and
    diversity `0.084`-`0.170`, with promote-to-front candidates correctly the
    most drifted (observed Growth 21 to 14, Cellular Resilience 5 to 17). The
    queue now keeps per-stage measurements rather than overwriting, which is
    what makes cross-context robustness computable. 147 Simulation and 651 Core
    tests pass. Next: P6.6 promotion packet.
47. P6.6 is complete, and Phase 6's gate is met except for one piece.
    `CandidatePromotionPacket` assembles the gene-level diff against the parent,
    lineage with the parent's definition fingerprint, per-stage artifact IDs and
    contexts, measurements with intervals, the four ranking measures, the
    observed category profile, and every recorded failure and retry;
    `CandidatePromotionPacketMarkdown` renders it for human review. The packet
    reports mechanical eligibility and stops there, since a packet that
    recommended promotion would be quietly doing the reviewing that the gate
    reserves for a person. Blockers cover a status short of passed, a missing
    holdout, robustness in fewer than two contexts, and an advantage that
    reversed sign between contexts. Gate assessment: generate, reject, evaluate,
    and add-to-catalog-without-manual-edits are all done, and no candidate can
    reach a player-facing pool because every generated entry carries
    `StrategyPool.None`. Outstanding is an unattended driver: every component
    composes and the loop has been run end to end, but launching each arm's
    process, running the analyzer, and feeding the verdict back into the queue
    were done by hand. That is process orchestration rather than new evaluation
    logic. 155 Simulation and 651 Core tests pass.

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

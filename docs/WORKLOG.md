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

### 2026-09-08 Mycovariant draft fix and campaign ladder measurement

- `MycovariantCategoryHelper.GetPreferredMycovariantIds` returned category ids in
  repository declaration order, and `ParameterizedSpendingStrategy` consumed that
  order as a strict ranking. Every strategy built that way preferred the weakest
  member of each tiered family — offered Mycelial Bastion I and III together it
  took I, 8 resistant cells instead of 16, and left III for an opponent.
  `AIDraftAlwaysPickScoreThreshold` never intervened: it is `99f` while the
  Bastion scores are 4/5/6. 82 roster entries were affected.
- Fixed by tagging the helper's output as `CategoryDerivedMycovariantIds` and
  resolving such a set by AI score at draft time. Hand-written id lists keep
  strict positional ranking, because those orderings are deliberate.
- Balance effect is not detectable. Proxy win rate across levels 0-6, 9 and 10
  (20 games each, seed `20260327`) moved by a mean of `-1.7` points, maximum
  single-level delta `10` points, which is two games. Every level's band verdict
  was identical before and after.
- The practical impact is smaller than the defect description implies. Tier-I
  variants are `IsUniversal` and permanently draftable, while tiers II and III
  are unique and leave the pool once anyone takes them, so the pathological
  comparison rarely arises. Treat this as a correctness fix, not a balance lever.
- Campaign levels 7 and 8 previously could not simulate at all: both presets
  listed `TST_CampaignPlayer_SafeBaseline` among their AI players while the
  harness also prepends it as the proxy, and the experiment contract rejects
  duplicate strategy names. Replaced with the nearest measured difficulty among
  strategies carrying a `FriendlyName` (`CMP_Defense_ReclaimShell_Easy` for
  Campaign7, `CMP_Bloom_NecrotoxinGauntlet_Elite` for Campaign8).
- Measured proxy win rate against the `CAMPAIGN_HELPER.md` bands: `L0 100%`
  (inside), `L1 80%` (below), `L2 95%` (inside), `L3 100%` (above), `L4 100%`
  (above), `L5 60%` (above), `L6 70%` (above), `L7 15%` (inside), `L8 10%`
  (inside), `L9 30%` (above), `L10 60%` (above).
- The ladder inverts after level 8. Levels 7 and 8 are the hardest measured
  rungs, and 9 and 10 are markedly easier than both. `validate_campaign_ai_rosters`
  passes because authored difficulty means rise monotonically, so that check does
  not currently predict measured proxy outcome at the top of the ladder.
- Confirmed on a single baseline. All eleven levels were rerun in a worktree
  pinned to `670bebc` (20 games each, seed `20260327`), and every proxy win rate
  reproduced the figure above exactly. That also shows `401eaee` and `d69d203`,
  which landed mid-measurement, did not move these outcomes. Artifacts are under
  `SimulationParquet/_ladder_singlebaseline_670bebc/`.
- Levels 7 and 8 land in band for a degenerate reason. In both, one strategy runs
  away with the level rather than the field being uniformly strong:
  `CMP_Reclaim_Scavenger_Easy` takes `65%` of Campaign7 and
  `CMP_Bloom_CreepingRegression_Elite` takes `70%` of Campaign8. Suppressing the
  proxy through a single dominant opponent is a different design than a level
  that is hard across the board, and it is worth deciding which was intended
  before treating those two rungs as correct.
- Remaining caveat: 20 games per level cannot resolve a small effect, so this
  ladder is reliable for its large shape and not for small differences between
  adjacent rungs.

### 2026-09-07 Necrotic Clearance

- Implemented `Necrotic Clearance`, a Tier-2 Mycelial Surge requiring
  Homeostatic Harmony 5. It has three levels, costs 5/6/7 mutation points, and
  lasts three rounds.
- Before Growth, each living cell may clear one adjacent own corpse: 5% per
  level normally, doubled when that corpse is adjacent to enemy living cells.
  Each source prioritizes contested corpses, and cleared corpses are removed
  directly without another death event or reactive death effect.
- This provides broadly applicable corpse denial while directly sacrificing
  nearby Regenerative Hyphae targets. Simulation exports track all cleared
  corpses and the subset cleared while contested.
- The resilience-oriented `CMP_Defense_ReclaimShell_Easy` AI is the initial
  named adopter. It uses the same visible eligibility rule as players and does
  not inspect enemy mutations.

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
48. The unattended driver is done, closing Phase 6. `CandidateEvaluationDriver`
    runs a queue to completion — emit, run both arms, analyze, record, prune,
    persist, repeat — with arm execution and analysis injected so the decision
    logic is testable without spawning processes;
    `ProcessCandidateExecution` supplies the process-backed defaults. Arms stay
    separate processes deliberately, which forces a candidate to arrive through
    the catalog file rather than surviving in memory. A failed arm or analysis
    is always an integrity failure; comparison and holdout pass or fail on their
    verdict, and a missing verdict there is integrity rather than a loss;
    calibration fails only on a clear regression; smoke never judges a
    candidate. Building it exposed a real defect in P6.4's pruning: futility was
    evaluated against the most recent interval including smoke's, whose few
    games per arm make it noise — the gates say never promote on smoke, and
    eliminating on it is the same mistake inverted. Pruning now ignores anything
    shallower than calibration. It also caught that a paired summary contains one
    row for the swapped slot and one for the unchanged opponent, so the reader
    now selects the row where the strategies actually differ rather than
    whichever came first. 166 Simulation and 651 Core tests pass.
    Jake set the near-term goal on 2026-09-06: use this machinery to identify
    over- and under-powered mutations, then tune ability strength,
    prerequisites, max-level bonuses, or add countering effects. The genome
    already carries the lever for that in `ExcludedMutationIds` — running a
    strategy with and without a mutation under matched seeds measures its
    contribution directly — but no generation operator produces ablations yet.
    Next: a mutation-ablation operator.
49. The mutation-ablation operator is done, and Jake clarified on 2026-09-06
    that there are two goals, not one: balancing mutations *and* identifying and
    tuning effective AI strategies. Ablation serves both, since a large drop
    when mutation X is removed says both that X is strong and that the strategy
    depends on it. `AblateTargetGoal` emits one candidate per distinct build
    goal, removing every occurrence of that mutation from the plan and blocking
    it outright; paired against the untouched parent, the difference is what
    that mutation contributed. Verified against the roster: exclusions hold on
    every acquisition path, and ablating Anabolic Inversion actually *grows*
    the observed build from 12 mutations to 14 as freed points go elsewhere,
    which is exactly why this needs measuring rather than reasoning about.
    Two correctness issues came out of it. Blocking a mutation also blocks
    everything gated behind it, so the measured effect is not that mutation
    alone; the operator now walks the prerequisite graph and records the
    downstream goals on the candidate — real chains were found, such as
    Necrosporulation gating Catabolic Rebirth. And pruning was direction-blind:
    an ablation hunts large *negative* differences, which increase-shaped
    futility pruning would have discarded first, silently deleting exactly the
    important mutations. Futility and domination pruning now require an explicit
    direction with no default, the calibration regression check applies only to
    increase hypotheses, and the driver passes the plan's direction through.
    176 Simulation, 651 Core, and 10 analytics tests pass. Next: Phase 7
    contextual band calibration, which is the roster-wide half of the strategy
    goal — measuring how strong each existing strategy is, rather than whether
    one variation beats its parent.
50. Jake confirmed Unity plays fine on 2026-09-07, closing the manual gate for
    everything through the ablation operator. P7.1 is complete.
    `ContextTaxonomy` implements the P3.4 class table, which had existed only as
    documentation: player-count, board-scale, aspect, geometry, and start-regime
    classes derived from resolved values rather than a preset list, with
    thresholds pinned to the anchors the plan names and deliberately not
    tunable. `CalibrationMatrix` freezes the context set before any measurement,
    split into a calibration half thresholds are fitted on and a holdout half
    reserved to check them. Validation refuses a context ID that does not start
    with its own derived rollup key, so a matrix cannot claim coverage it lacks;
    refuses a holdout reusing a calibration board, player count, or seed; and
    requires the calibration half to vary at least two player-count and two
    board-scale classes, since a matrix varying neither can only produce a
    global label with no evidence it generalizes. The checked-in
    `calibration-matrix.v1.example.json` freezes the reference panel Jake chose
    on 2026-09-07: the 19 `Proven` strategies solo play already draws from, so
    the yardstick is the opponents players actually meet. Sizing that panel
    exposed two problems the matrix now solves. One condition cannot supply a
    panel — 19 strategies sharing 100 two-player games get ten each — so a
    context declares `repeats` at consecutive seed blocks and
    `minimumGamesPerStrategy` is checked against panel size before anything
    runs, with the error naming the repeats needed. And even exposure is not
    fair opposition: `StratifiedCycle` slides a window over a fixed ordering, so
    in two-player games each strategy would face only its two neighbours and its
    rating would describe those matchups rather than general strength;
    `CoverageBalanced` over-samples rare themes for the same reason. The matrix
    requires `RandomUnique` whenever the panel exceeds a lineup, and checks that
    context seed spans do not overlap. Jake approved raising the per-condition
    game ceiling on 2026-09-07, having confirmed that agent token usage does not
    scale with games run — it is driven by how much run output is read back, and
    only summaries are read. The ceiling now follows the evidence stage: it is a
    promotion safeguard, so the four staged gates keep their frozen counts while
    an exploratory run, which cannot carry a hypothesis or emit a verdict, may
    run up to 1,000 games per condition. No schema bump, because the change only
    widens an accepted range and an older binary rejects an over-sized manifest
    rather than misreading it. The frozen matrix is 1,600 games across 7
    conditions with at least 30 games per strategy per context. 221 Simulation
    and 651 Core tests pass, and the experiment contract verifies end to end.
    Next: P7.2, running the matrix and quantifying slot, geometry, and lineup
    effects.
51. P7.2 scaffolding is in: `CalibrationConditionEmitter` renders runnable
    conditions with derived experiment IDs and `CalibrationDriver` runs them
    with durable resumable state, retrying a transient failure inside the pass
    rather than leaving a hole for analysis to find, refusing to resume against
    a different matrix, and costing only the added contexts when a matrix is
    widened. Running one condition for real proved the whole path — emit, run,
    analyze, read per-strategy shares with intervals — and exposed a wrong
    assumption in the P7.1 sizing. The artifact measured two strategies, not
    nineteen: the runner resolves its lineup once per run and rotates only slots
    within it, so a 300-game duel condition measures one pair rather than the
    panel. Every per-strategy sample calculation in P7.1 assumed the lineup was
    redrawn per game. The fix is per-game lineup selection from a purpose-scoped
    deterministic stream; enumerating all 171 duel pairs would work today with no
    code change and is statistically cleanest, but multiplies conditions by two
    orders of magnitude for the same evidence. 231 Simulation and 651 Core tests
    pass. Next: per-game lineup selection, without which the frozen matrix
    measures far less than it claims.
52. Per-game lineup selection is done, closing that gap. `PerGameLineupSelector`
    draws each game's lineup from a purpose-scoped stream keyed on that game's
    own seed, so a context samples the field, a replay reproduces the same
    lineups, and draw count cannot perturb gameplay. Enumerating all 171 duel
    pairs was considered — statistically cleanest, since every pair would meet
    equally often — but multiplies conditions by two orders of magnitude for the
    same evidence. Selection is opt-in via `--per-game-lineups`, added by the
    emitter only when the panel exceeds the table, so every existing run behaves
    exactly as before; when the panel is the table no selector is created, which
    is why the defect went unnoticed until a panel larger than a table was
    measured. Verified by running it: 40 two-player games over the 19-strategy
    panel measured all 19, with appearance counts spread 11 down to 1 around the
    expected mean of 4.2, where the same run previously measured two. 238
    Simulation and 651 Core tests pass and the experiment contract verifies end
    to end. Next: P7.2 proper — running the frozen matrix and quantifying slot,
    geometry, and lineup effects.
53. P7.2 through P7.4 are complete for strategy strength. The frozen matrix ran
    clean on 2026-09-07: 1,600 games across 7 conditions, zero failures, 96
    minutes, 145-195 games per strategy. `StrategyBandClassifier` places
    strategies into difficulty bands from parity-relative thresholds that were
    frozen before the campaign finished, so they could not be drawn around the
    results; placement uses the interval bound arguing against the band, and thin
    evidence is reported rather than guessed. Pooling averages interval bounds
    rather than inverse-variance weighting, which deliberately does not narrow
    the interval, because contexts genuinely differ and treating that as noise
    would claim confidence the design does not earn. Results are frozen in
    `AI_P7_REFERENCE_BANDS_V1.md`: a 12.5x spread across the strategies solo
    players face (`1.915` down to `0.153`), and six of nineteen contradicting
    their authored power tier — including one authored `Strong` and used as a
    Boss that measures Easy at `0.490`, and one authored `Spike` measuring
    `0.153`. Random per-game lineups gave uneven exposure (18-39 per cell against
    an expected 31.6), so six strategy/context cells fell under the 25-game floor
    and are reported unplaced; overall bands are unaffected. 251 Simulation and
    651 Core tests pass. Next: P7.5 regression alerts, then Phase 8 — or acting
    on these findings, which is a balance decision for Jake rather than an
    engineering one.
54. Jake approved relabelling from measurement on 2026-09-07. Seven metadata
    corrections applied to the Proven set: two authored `Strong` raised to
    `Spike` (measured Elite at 1.915/1.866), `Filament Regrowth` and
    `Power Mutations Max Econ` lowered to `Standard` (measured Normal), and
    `TST_CreepingNecroRegressionCascade` plus
    `Best_MaxEcon_Surge10_HyphalSurge` lowered to `Weak`. The Boss role on
    `TST_CreepingNecroRegressionCascade` became `Experimental`, since a strategy
    measuring Easy in all seven contexts cannot keep a Boss promise. One Core
    test pinned Filament Regrowth as Strong from authored intent and now asserts
    the measured value with the evidence cited.
    Campaign slotting was **not** changed, and should not be from this evidence:
    the panel measured is `Proven`, campaign presets reference only `CMP_*`
    strategies, and neither weak strategy appears in any preset. Both do affect
    solo play, which draws from `Proven`. Rearranging campaign difficulty needs
    the same matrix run against the Campaign set first. This also corrects an
    earlier characterisation: the Boss label was decorative metadata, not a
    campaign encounter players meet.
    Diagnosis is recorded in the reference doc. Fungicide investment correlates
    with strength at `r = 0.792` (buyers average 1.202, the five non-buyers
    0.409); the three `RegressionCascade` strategies sink 12-16 levels into
    Substrate Ecology instead and occupy three of the bottom five. Two causal
    tests on the weakest strategy both came back null: lifting its Tier4 cap
    gave `-0.0004` (CI `-0.011..+0.010`), and changing MaxEconomy to Neutral
    produced bit-identical games, so both genes are inert for it. Its weakness
    is its two-goal plan, which leaves it 5 Growth levels against 16-21 for
    every other strategy. The Fungicide correlation remains causally untested;
    an ablation sweep would settle it. 251 Simulation and 651 Core tests pass.
    Next: P7.5 regression alerts.
55. The Fungicide correlation was tested at mutation level and is **confounded,
    not causal**. All seven Fungicide levels in every top strategy are one
    Tier-1 mutation, `Mycotoxin Tracer`, reached through the fallback path and
    never declared as a goal. It cannot be ablated: it is a prerequisite of
    `Necrosporulation`, which the strong strategies target, and of
    `Catabolic Rebirth` through it. The generator marks the ablation
    unachievable and characterization rejects it as `ViolatedExclusions`. So
    `r = 0.792` measures "targets the Necrosporulation line" rather than any
    value of Fungicide itself; the `RegressionCascade` family targets
    `NecrophyticBloom` — a different mutation despite the name — and is never
    forced to buy the Tracer. No strategy in the panel buys it without needing
    it, so its own contribution cannot be isolated within this panel.
    Two enabling pieces landed. `AblateObservedPurchase` ablates any mutation a
    parent actually buys, not just declared goals, which is what mutation-level
    diagnosis needs since the most-bought Fungicide mutation is fallback-only.
    And a real defect was found and fixed: `PickBestTendrilMutation` substituted
    the best-direction tendril without re-applying the exclusion list, so an
    excluded tendril could be reacquired — the same bypass free upgrades once
    had. No registered strategy excludes a tendril so no existing result
    changes, but it would have silently broken any tendril ablation or
    controlled treatment. The unachievable-ablation detector was also corrected
    to walk the full prerequisite closure rather than only goal-to-goal links.
    652 Core and 254 Simulation tests pass.

56. A guard against the class of error item 55 represents. Jake's point was
    that the Fungicide conclusion was predictable from the prerequisite tree
    before any effort was spent on it, and that the pattern will recur — every
    root mutation and low-tier gateway produces the same spurious correlation,
    and any category dominated by one inherits it. The root cause is that a
    realized build is not a set of choices: it is declared goals plus whatever
    the prerequisite closure forced. `StructuralConfoundScreen` now partitions
    every mutation in an observed build into `Goal`, `RequiredByGoal`, or
    `Free`, and screens a panel so that only `FreeVariation` mutations may be
    reported as a purchase-level lead; `ExplainWhyNotEvidence` returns the
    sentence to write instead of the correlation. It is pure graph work over
    the prerequisite closure — no games, milliseconds — so the check is cheap
    enough to run when a lead is *formed* rather than when an experiment is
    launched, which was the actual sequencing failure. A regression test
    asserts `Mycotoxin Tracer` is never reportable across the Proven panel.
    Run over the panel the result is far larger than the case that prompted it:
    **17 of the 24 mutations the panel buys are not usable as purchase-level
    evidence**, and the most universal ones are the most confounded —
    `Mycelial Bloom`, `Homeostatic Harmony`, and `Mutator Phenotype` appear in
    all nineteen builds and were forced in 15–18 of them. Only four mutations
    vary freely across three or more owners, and only `Creeping Mold` has real
    sample. That is the honest size of the purchase-level evidence base.
    The deeper rule is recorded in the plan's *Structural confounding* section:
    correlate over genes rather than builds, because goals and biases are what
    an author chose and purchases are what the tree then forced.

57. The Campaign-set matrix ran on 2026-09-07: all 53 Campaign strategies,
    six contexts, 1,800 games, no failed or skipped conditions, 3,647s.
    Results are frozen in `AI_P7_CAMPAIGN_BANDS_V1.md`, and they say the
    campaign difficulty ladder does not hold. Median parity-normalized share by
    authored tier runs Training `0.415` < Easy `0.455` < **Elite `0.990`** <
    Medium `1.194` < Hard `1.902` — **`Elite` is the fourth-strongest tier of
    five and its median is below parity**, so a player promoted from Hard to
    Elite gets an easier fight. Six of the ten Elite bosses measure at or below
    parity and four band as `Easy`; the worst,
    `CMP_AnabolicBeaconRhizolith_Elite`, is authored `Strong`, slotted `Elite`,
    used as a Boss, and holds `0.498` — half an average opponent's board share.
    The middle tiers are not tiers: `Easy` and `Medium` each span ~14× and
    overlap almost totally, with `AI12` slotted `Easy` at `1.959` (second
    strongest in the panel) and a `Medium` entry at `0.141`. Twenty-five of 53
    strategies sit in `Medium`, so for half the roster the label carries almost
    no information. `Training` and `Hard` are internally tight at 1.9× and
    correctly ordered, so the ladder's ends work and its middle and top do not.
    Two strategies could not be placed at all:
    `CMP_Bloom_ToxinborneBallistospore_Hard` (2.244) and
    `CMP_Bloom_ToxinborneJetting_Medium` (2.062) return `IntervalTooWide` in
    every context on adequate games, which is the signature of boom-or-bust
    rather than reliable strength; they should not be slotted on pooled mean.
    **A reslotting is proposed but deliberately not applied** — how many
    encounters belong per tier and how the campaign paces them is a design
    call, and it needs a Unity validation pass.

58. The campaign reslotting was applied on 2026-09-07 at Jake's direction.
    **45 of the 53 strategies changed tier.** The band classifier's four-way
    thresholds leave the parity band nearly empty, so the campaign ladder got
    its own five, frozen in `AI_P7_CAMPAIGN_BANDS_V1.md`: `Elite` at `1.50`,
    `Hard` at `1.25`, `Medium` at `0.95`, `Easy` at `0.50`, `Training` below,
    giving 13/5/7/12/16. All six under-strength bosses left `Elite`;
    `CMP_AnabolicBeaconRhizolith_Elite` fell to `Training`. `AI12` went `Easy`
    to `Elite`, `AI13` `Hard` to `Elite` — which required updating the test
    pinning AI13's authored tier, though its `Boss` role and `Strong` power tier
    are untouched, since measurement says how strong a strategy is and authoring
    says what it is for. 652 Core and 259 Simulation tests pass; Unity Core
    DLL/PDB refreshed.

    **The reslotting is cosmetic on its own, and that is the more important
    finding.** A strategy's `CampaignDifficulty` is read in only two places — a
    label in the Board Preset inspector and an optional `StrategyCatalogFilter`
    predicate. It does *not* select which AIs a level fields: every level's
    roster is a hand-authored name list in its `BoardPreset` asset, either a
    fixed `aiPlayers` lineup or `aiStrategyPool` + `pooledAiPlayerCount`. The
    same enum's *other* use, `CampaignState.startDifficulty`, is unrelated to
    strategy metadata and does drive live levers: extra AI starting adaptations
    (0/1/2/3/4/5 by difficulty) and how far the human's allowed starting tiles
    slide toward worse positions.
    Joining each preset's roster with measured shares gives the real curve,
    which climbs `0.42` → `1.44` over sixteen levels and is far healthier than
    the metadata was, but dips at five levels. Three matter: **Campaign15, the
    finale, is the third-weakest of the last six** because it duplicates
    Economancer and HoardsporeRegent (four of seven seats on two strategies) and
    spends a fifth on `CMP_Surge_BeaconSprinter_Medium` at `0.358`;
    **Campaign10, named "4 AI Elite", is the softest of its neighbourhood** at
    `1.052` and is the only level fielding `CMP_AnabolicBeaconRhizolith_Elite`
    (`0.498`); and **Campaign2 is the sharpest drop**, leaving level 3 easier
    than level 2. Fixing these means editing BoardPreset assets, which is a
    pacing decision rather than a measurement one, so none were changed.

59. The campaign board presets were rerostered on 2026-09-07 at Jake's
    direction, so measured difficulty now rises at **every one of the sixteen
    levels**, `0.144` at Campaign0 to `1.805` at the finale. Board sizes, seat
    counts, human starting pools and level titles are untouched; only which
    opponents each level fields changed. The mean is a relative difficulty
    index, not a prediction of in-game share — shares were measured against a
    mixed panel and do not add up within one lineup.
    Four things worth remembering. **The finale now fields `AI13`**, which is
    authored `Role = Boss`, `PowerTier = Strong`, measures `2.076`, and
    previously appeared in no campaign level at all; the duplicated Economancer
    and HoardsporeRegent seats and the `0.358` filler are gone. **Campaign10's
    boss slot carried an Iron Shell loadout on the wrong strategy** —
    `adaptation_6/10/8` on `CMP_AnabolicBeaconRhizolith_Elite` (`0.498`, no
    suggested sets of its own), which is nearly
    `CMP_Defense_IronShell_Elite`'s own authored set `adaptation_6/10/7`, so
    Iron Shell (`1.284`) now holds the slot and Rhizolith moved down to
    Campaign4. **Authored `startingAdaptationIds` are a genuine difficulty
    lever**, not flavor: `AIStartingAdaptationResolver` adds them
    unconditionally and only tops up to the difficulty quota afterwards, so a
    boss with three authored adaptations still has three at `Training` where
    everyone else has none — Campaign10 is the only level using it. **Both
    Toxinborne variants are deliberately unfielded** because they are the only
    strategies the matrix could not place, so their effect on a level's
    difficulty is unpredictable; they suit the unused `bossBoardPresets` pool
    once their variance is measured. 51 of 53 strategies are fielded.
    `scripts/validate_campaign_ai_rosters.py` enforces the invariants: unknown
    names (which would silently fall back to a random lineup), duplicate seats,
    pools smaller than their seat count, and any level whose mean fails to
    exceed its predecessor. Negative-tested against a reintroduced duplicate and
    a typo.
60. Post-pull validation caught one stale test left by the campaign reroster:
    `Campaign_progression_board_presets_only_use_cmp_strategy_names` still
    treated the `CMP_` prefix as a runtime invariant, although the measured
    roster intentionally fields registered Campaign entries named `AI12`,
    `AI13`, and `TST_*`. The proposed naming standard below explicitly remains
    unapproved, and runtime selection is based on Campaign-set registration,
    not a prefix. The regression now verifies that every strategy referenced by
    a BoardPreset resolves in `CampaignStrategiesByName`, preserving the actual
    safety property without rejecting valid measured entries.

61. P7.5 regression-alert foundation is in. `StrategyRegressionAlerts` compares
    version-matched calibration snapshots and reports measured band movement,
    material-context/robustness changes, category-profile archetype drift,
    replay parity failures, and abnormal fallback or decision-failure rate
    increases. A missing profile or execution-health record is an explicit
    evidence gap, never silently interpreted as a zero rate; a classifier
    version change refuses all band comparisons. Versioned JSON snapshots and a
    Markdown report retain baseline/current matrix IDs, classifier version, and
    generation time beside the alert set. The detector is pure and tested;
    next, instrument/export the execution-health inputs from calibration
    artifacts and invoke the report as part of each recalibration run.

62. P7.5 execution-health export is in. Each funded AI mutation-spending turn
    now records a decision count and its actual points spent, while reaching a
    generic fallback branch records a separate fallback count; intentionally
    banking points is not mislabeled as a decision failure. Both counts are
    carried through game results into `players.parquet`, and the Unity Core
    plugin was refreshed. Next: aggregate those player rows into a calibration
    snapshot and invoke the persisted regression report after a recalibration.

63. The offline analyzer now writes `strategy_execution_health.csv`, grouped by
    stable strategy identity, with total funded decisions, fallback-branch
    decisions, and their observed rate. Legacy artifacts without the telemetry
    yield an empty health table rather than fabricated zeroes. Local Python
    unit execution is blocked because WSL lacks the repo's `numpy` dependency;
    the script passes syntax compilation. Next: have the calibration reader
    load this health artifact into a `StrategyRegressionSnapshot` and provide a
    command that writes the paired snapshot/report artifacts.

64. P7.5's artifact bridge is complete. `CalibrationMeasurementReader` now
    aggregates each completed condition's `strategy_execution_health.csv` into
    the durable regression snapshot; absent files and incomplete conditions are
    surfaced as evidence gaps, never healthy zeroes. The simulator's
    `--write-regression-snapshot` command writes the versioned snapshot from a
    calibration state plus analyzed artifact root, and with
    `--regression-baseline-snapshot` writes the paired Markdown alert report.
    Snapshot-building coverage includes aggregation and the missing-health gap;
    all 266 Simulation tests, plus Core and Simulation builds, pass. Category
    profiles and replay-parity telemetry remain intentionally absent until
    their own artifact sources exist, so the existing alert engine continues to
    report those as evidence gaps rather than guessing.

65. P7.5 category-profile evidence is now artifact-backed. The offline analyzer
    emits `strategy_category_profiles.csv`, summing each strategy's observed
    final mutation levels by category; the calibration reader aggregates those
    vectors across completed conditions into `StrategyRegressionSnapshot`.
    Therefore a strategy that shifts its realized build category mix can produce
    an archetype-drift alert instead of an unconditional missing-profile gap.
    Missing profile artifacts still remain explicit gaps. All 266 Simulation
    tests and Core/Simulation builds pass; Python unit execution is unavailable
    on this checkout because no documented `.venv` exists, so the analyzer was
    syntax-compiled only. Replay-parity telemetry remains the final P7.5 source
    gap.

66. P7.5 alert semantics are hardened: an unavailable decision-failure or
    replay-parity dimension is nullable in `StrategyExecutionHealth` and emits
    its own evidence-gap alert. Snapshot assembly no longer fabricates zero
    failures for dimensions that the health CSV does not observe. This preserves
    the valid fallback-rate comparison while preventing a clean-looking report
    from overstating evidence. 267 Simulation tests and the Simulation build
    pass. The next concrete source work is a bounded calibration replay verifier
    that records parity results without attempting to replay the full matrix in
    this session.

67. Replay validation now accepts the same 1,000-game exploratory ceiling as
    calibration artifacts. Previously `ResolvedExperimentReplayRunner` rejected
    valid exploratory conditions above the 100-game decision-stage cap, making
    the planned parity verifier unable to inspect the frozen calibration matrix.
    267 Simulation tests and the Simulation build pass.

68. The bounded calibration replay verifier is in. Running
    `--verify-calibration-replays` with the saved state and artifact root
    replays only completed conditions and writes
    `calibration-replay-parity.json` after each attempt. A failed or missing
    condition is recorded without abandoning later evidence, making the command
    safe to resume after a code-fingerprint mismatch or an outcome mismatch.
    268 Simulation tests and the Simulation build pass. Next: join this durable
    result into snapshots so per-strategy parity failures replace the current
    evidence gap.

69. Replay-parity results now flow into `StrategyRegressionSnapshot`: every
    strategy appearing in a verified condition receives the number of failed
    condition replays, while an absent or incomplete result file remains null
    and therefore alerts as an evidence gap. The snapshot-builder test proves a
    successful persisted replay result resolves to zero failures. 268 Simulation
    tests pass.

70. P7.5 decision-failure telemetry is now defined and exported: it records
    only an avoidable idle turn — funded points remain, the strategy did not
    declare banking, and at least one legal upgrade was affordable. Generic
    fallback use, unaffordable options, exhausted trees, prerequisites, surge
    plans, and failed simulation runs are not mislabeled as decision failures.
    The count flows through Core, Parquet, analyzer CSV, and regression snapshots;
    the Unity Core DLL/PDB was refreshed. 268 Simulation tests, Core and
    Simulation builds, and analyzer syntax compilation pass. Unity Editor
    compile plus normal and fast-forward AI spending remain the manual check.

71. P8.1 has its first reusable behavior-comparison primitive.
    `RosterBehaviorComparison` observes the complete roster with the same
    deterministic characterization script used for candidate evaluation, then
    compares each pair by raw mutation-build cosine similarity and the coarser
    category-profile similarity. Raw mutation similarity is deliberately the
    redundancy signal: two strategies can have identical category totals while
    pursuing different mutations. The result intentionally does not declare a
    retirement, counter, or player-value verdict; similarity only identifies
    candidates for review, which still need contextual matchup evidence. Three
    focused Simulation tests pass, including the whole Proven roster (19 profiles
    and 171 pairs). Next: render the comparison into a durable P8.1 evidence
    artifact and combine it with measured context and matchup evidence before
    proposing any roster changes.

72. P8.1's deterministic behavior evidence is complete. The new
    `--write-roster-behavior-report` command writes a provenance-stamped Markdown
    artifact with current code hashes, stable IDs, definition fingerprints, complete
    observed builds, and all pairwise raw/category similarities. The frozen Proven
    artifact is `AI_P8_ROSTER_BEHAVIOR_V1.md`: four of its 171 pairs have an exact
    observed raw build — Creeping>Necrosporulation/Filament Regrowth,
    Grow>Kill>Reclaim(Econ)/Grow>Kill>Reclaim(Econ/Reclaim), and the two Balanced
    Control/CampaignMirror pairs. They are not automatic retirement targets. The P7
    reference matrix measures different shares within every pair (0.965/1.058,
    1.372/1.373, 1.866/1.915, and 1.347/1.271 respectively), confirming that the
    scripted spender is only one behavioral surface. There is no matched matchup
    matrix, so counter claims and merge/retire recommendations remain evidence gaps.
    All 272 Simulation tests pass. Next: P8.2 target-matrix policy, with the static
    review candidates and matchup-evidence gap explicit rather than hidden.

73. P8.2's policy proposal is recorded in
    `AI_P8_TARGET_MATRIX_PROPOSAL_V1.md`; it makes no gameplay or roster change.
    The proposed solo ceiling is 15 behaviorally distinct strategies, distributed
    4 Easy / 4 Normal / 4 Hard / 3 Elite. The current 15 distinct observed builds
    are bottom-heavy (7/4/2/2), so the future action surface is three excess Easy
    slots and gaps of two Hard plus one Elite — not a justification to relabel any
    current strategy. Exact-build matches need controlled reactive/draft/surge or
    real-game evidence before being merged, and a counter claim now has an explicit
    50-game matched comparison plus 100-game holdout gate. Next: P8.3 inventory
    the review candidates and decide retain/merge/retire/replace only on that
    evidence.

74. P8.3's first exact-build review screen ran cleanly. With the two-player
    Proven lineup, rotating slots, seed `2026091001`, 160x160 board, and nutrient
    patches, Mycovariants, and starting Adaptations all disabled, Filament Regrowth
    earned 29/50 win credit (58%, mean normalized board share 1.053) against
    Creeping>Necrosporulation's 21/50 (42%, 0.947). Their raw scripted builds are
    identical, so the difference confirms a real-game behavioral surface outside
    that screen; it is exploratory and has no holdout, therefore blocks an
    automatic merge rather than proving a permanent retain decision. The new
    `--summarize-direct-match` command reads a completed two-strategy Parquet
    artifact without needing the optional Python environment. Next: either repeat
    this review under the preregistered holdout matrix or screen another exact-build
    pair; do not retire either member yet.

### Proposed — AI strategy naming and metadata standard

No convention currently governs AI strategy names, and the roster shows it:
`Grow>Kill>Reclaim(Econ/Reclaim)`, `Best_MaxEcon_Surge10_HyphalSurge`,
`TST_CampaignMirror_AI13_BalancedControl_MaxEconomy`, and `AI13` coexist. Names
mix prefix conventions (`TST_`, `CMP_`, none), embed implementation details
(`Surge10`), embed superlatives that measurement has since contradicted
(`Best_`), and use characters (`>`, `(`, `/`) that complicate CSV, path, and
report handling. `Best_MaxEcon_Surge10_HyphalSurge` measuring `0.153` is the
clearest case: the name asserts a claim the evidence refutes.

Scope for the proposal, to be agreed before any rename:

1. **A naming grammar.** The canonical plan already sketches
   `ai.<primary-archetype>.<identity>.v<major>` for machine IDs plus a short
   display name; settle both, and forbid superlatives, implementation details,
   and difficulty claims in either, since all three go stale.
2. **Migration.** Names appear in campaign preset assets, board presets, saved
   games, frozen baseline documents, and simulation artifacts. Stable strategy
   IDs already exist, so the rename should move display names while identity
   stays put; enumerate every reference site first and confirm save
   compatibility.
3. **Metadata fields to add.** Measured band, classifier version, and evidence
   date, so a label carries its provenance rather than an assertion. Consider a
   `MeasuredContext` note for contextual specialists.
4. **Metadata fields to reconsider.** `PowerTier` and `DifficultyBand` overlap
   now that bands are measured; decide whether the authored tier survives as
   intent, is derived from evidence, or goes. `StrategyTheme` versus
   `StrategyArchetype` similarly duplicate.
5. **A staleness rule.** Authored labels drifted from reality until Phase 7
   measured them. Decide what happens when a measurement contradicts a label:
   auto-derive, flag, or fail a test.

This is deliberately a proposal rather than a task list; the rename itself
should not start until the grammar and the migration surface are agreed.

### Proposed — Follow-ups from the 2026-09-08 mycovariant and ladder work

None of these were done in that session; they are the loose ends it exposed.

1. **Nothing checks that measured proxy outcome is monotonic.**
   `validate_campaign_ai_rosters` checks authored difficulty means, and the
   Campaign10 reroster note already records that the mean is a relative index
   rather than a share prediction. The gap is that no check covers the measured
   quantity the bands are written against: proxy win rate peaks at levels 7 and
   8 and then falls away, so the validator passes on a ladder that inverts.
   Decide whether the measured ladder gets a recorded expectation, a periodic
   artifact-backed check, or stays a manual review step.

2. **A category preference is a very weak draft identity.** 78 of the 80
   `GetPreferredMycovariantIds` call sites name only one or two categories, and
   the categories are large — Fungicide holds ten mycovariants, Growth and
   Economy eight each, Resistance seven, out of roughly three dozen total. After
   the ordering fix a two-category preference marks fifteen or more mycovariants
   as equally wanted and resolves purely by `AIScore`, so most strategies express
   almost no draft intent. Decide whether campaign strategies should declare
   narrower, authored preferences instead of whole categories.

3. **`AIScore` calibration now carries weight it did not before.** Category sets
   are resolved entirely by score, so a mis-scored mycovariant now directly
   causes a wrong pick where list position used to mask it. Worth an audit pass
   over the score constants, particularly across tiered families.

4. **Dead code found while tracing the draft path.**
   `MycovariantGameBalance.MycelialBastionSynergyBonusAIScore` is declared and
   never read — the Bastion `AIScore` lambdas return their flat base score, and
   only `ReclamationRhizomorphsBonusAIScore` has a live synergy path. Decide
   whether Bastion was meant to gain a synergy bonus or the constant should go.
   `ParameterizedSpendingStrategy.GetPreferredMycovariant(Player)` is public with
   no callers anywhere in the solution.

5. **`Player.AIType` is dead state.** All three construction sites pass
   `AITypeEnum.Random` and nothing varies it, so the field reads as a setting
   that does not exist. Either wire it to something real or remove it and the
   enum. It was dropped from the development-testing inspector block for exactly
   this reason.

6. **The player inspector was only built to step one.** The hover tooltip now
   renders shared sections from `PlayerInspectorContent`, but the docked
   `PlayerInspectorPanel` those sections were designed for does not exist yet.
   The dev block also carries only the strategy tuning parameters; the fields
   that actually catch AI misbehavior — unspent mutation points and banking
   intent, target-goal progress, the mutation ledger with first-acquired round,
   active surges, and effective growth/self-death rates — were deferred with it.

7. **Twenty games per level cannot resolve a small balance effect.** That is the
   `run_campaign_balance.py` default and what the mycovariant confirmation ran
   at; one game moves a level by five points, and the interval on a
   before/after difference is far wider than any effect worth shipping. The
   experiment contract caps a condition at 100 games. Agree a standard game
   count for balance confirmations rather than deciding per run.

8. **The analytics virtual environment does not exist where it is documented.**
   `FungusToast.Analytics/README.md` and the `validate-campaign-balance` skill
   both point at `FungusToast.Analytics/.venv`; on the current machine only the
   repository-root `.venv` exists, and it happens to carry pandas and pyarrow.
   Create the documented environment or change both documents to name the real
   one, so a validation run does not have to rediscover this.

### Proposed — Interactive HTML simulation results page

Reading a simulation run currently means opening several CSVs from
`FungusToast.Analytics/analyze_balance.py` side by side, or scrolling console
output. Neither supports the question actually being asked most of the time,
which is "why did this player win" — that requires pivoting between the player
summary, growth sources, mycovariant picks, and mutation timing, and each pivot
is a manual join.

Proposal: have `analyze_balance.py` emit a self-contained `results.html`
alongside the existing CSVs, explorable by clicking rather than by re-running
the analyzer with different flags.

Scope to agree before building:

1. **What the landing view answers.** Likely the per-player summary already in
   `post_simulation_player_summary.csv`, with win %, board share, and rank, plus
   the run's identity (experiment ID, seed, board, lineup) visible without a
   click.
2. **What drilling down opens.** Per-player growth sources, drafted
   mycovariants, and mutation purchase timing are the obvious three. Decide
   whether a game-by-game view is in scope or whether aggregate is enough.
3. **Multi-run comparison.** The recurring real task is before/after on one
   code change across a level sweep. Decide whether one page holds several runs
   or whether comparison stays a separate paired-analysis page.
4. **Self-containment.** The page should open from disk with no server and no
   network, which means inlining CSS/JS and embedding the data. Confirm that
   holds at the largest run sizes currently produced.
5. **Relationship to existing outputs.** The CSVs and `balance_recommendations.md`
   stay authoritative; the page is a reading surface over them, not a second
   source of truth, and should not re-derive numbers.

Prompted by the 2026-09-07 mycovariant-draft validation, where answering one
before/after question meant hand-assembling a table from eighteen separate
`post_simulation_player_summary.csv` files.

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

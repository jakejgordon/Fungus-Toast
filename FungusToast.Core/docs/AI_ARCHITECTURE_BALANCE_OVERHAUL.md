# AI Architecture and Balance Overhaul

This is the canonical design, execution plan, and decision log for the AI
Architecture and Balance Overhaul. Use [AI_STRATEGY_AUTHORING.md](AI_STRATEGY_AUTHORING.md)
for the current authoring workflow until this initiative explicitly replaces
it, and [SIMULATION_HELPER.md](SIMULATION_HELPER.md) for the current simulation
commands and artifact rules. The active session queue remains in
[`docs/WORKLOG.md`](../../docs/WORKLOG.md).

The completed Phase 0–1 evidence inventory is in
[AI_OVERHAUL_PHASE_0_1_AUDIT.md](second-level/AI_OVERHAUL_PHASE_0_1_AUDIT.md).

## 1. Initiative Status

- **State:** Phase 2 experiment infrastructure is complete. An adversarial
  review accepted on 2026-09-03 reopened the inferential portions of Phase 3
  before Phase 4: RNG ownership, tie handling, analytical identity, causal
  scoring, paired inference, and gate enforcement require correction. The
  original P3.5 reference remains immutable historical evidence and will be
  superseded by a versioned corrected baseline.
- **Implementation started:** Yes
- **Current gate:** Complete the adversarial-review correction program in
  section 8, reproduce the experiment contract under the new RNG/schema
  versions, and publish P3.5 v2 before beginning Phase 4 characterization.
- **Migration posture:** Incremental and compatibility-first. Existing campaign
  strategy names and board-preset references remain valid until an explicit,
  tested migration retires them.

## 2. Objective

Build a deterministic, comprehensible AI system that can express distinct
strategic identities across the complete AI decision surface, plus a resumable
evaluation system that can measure, generate, reject, and promote strategies
with minimal routine steering.

Success means more than producing stronger opponents. The final roster must
cover intentional difficulty levels and recognizable archetypes, expose
contextual strengths and weaknesses, and remain reproducibly measurable as
game content changes.

## 3. Guardrails and Non-Goals

- Core owns AI decisions, deterministic state inspection, and decision traces.
  Simulation invokes and measures Core behavior; Unity does not reimplement it.
- Do not combine gameplay-balance changes with architecture migration. A
  migration slice must prove behavioral parity unless its treatment is
  explicitly identified and tested as a behavior change.
- Do not optimize only for win rate. Strength, identity, robustness,
  comprehensibility, and campaign fit are separate promotion dimensions.
- Candidate generation may automatically add passing candidates to a generated,
  non-player-facing testing catalog. Promotion into campaign or single-player
  pools remains a deliberate evidence gate.
- Do not assume one global difficulty label is sufficient. Map size, geometry,
  player count, lineup, slot, enabled systems, and starting loadout remain
  visible dimensions.
- Do not require Unity to run autonomous evaluation. Unity validation is a
  final integration gate for player-facing roster or presentation changes.

## 4. Current-System Findings to Verify in the Formal Audit

These are planning observations, not yet the completed audit report.

1. `IMutationSpendingStrategy` exposes only mutation-spending behavior and
   coarse category metadata, but `ParameterizedSpendingStrategy` also owns
   mycovariant preferences and a starting-spore edge offset.
2. Mycovariant drafting discovers richer behavior through a concrete
   `ParameterizedSpendingStrategy` type check; other implementations fall back
   to random selection. Campaign difficulty can replace that path with separate
   simplified drafting behavior.
3. Mutation goals, prerequisite planning, early economy rules, banking,
   scheduled surges, catch-up surges, fallback categories, tier preference,
   economy weighting, tendril direction scoring, exclusions, and mycovariant
   scoring are interleaved in one large implementation. Several knobs affect
   only fallback behavior despite sounding global.
4. `AIRoster.cs` contains approximately 130 parameterized strategy
   constructions plus separate dictionaries for themes, status, power, role,
   lifecycle, campaign copy, and other catalog data. Strategy behavior and
   authored metadata can therefore drift independently.
5. Simulation already controls many important variables: explicit lineup,
   seeds, player count, dimensions, slot rotation, nutrient patches,
   mycovariant drafts, starting adaptations, explicit/preferred starting
   positions, and blocked-tile masks. The resolved run metadata does not yet
   record every one of those controls.
6. Exported player rows contain strong raw outcome/economy data but do not yet
   make normalized board share, rank, complete resolved context, or structured
   AI decision reasons first-class outputs.
7. Core and Unity both reach AI spending through orchestration paths that need
   an ownership/parity audit before the new boundary is selected.

Phase 1 must convert these observations into an evidence table of decision
owner, caller, inputs, random source, outputs, side effects, tests, and gaps.

## 5. Proposed Architecture Direction

The working hypothesis is a composed AI controller rather than a larger
parameter bag or one universal utility function. The exact names are deferred
until the architecture spike, but the intended responsibilities are:

- **AI strategy definition:** immutable standardized identity, behavior-module
  selection, tunable parameters, authored archetype intent, lifecycle,
  complementary Adaptation configuration, and human-readable summary in one
  registrable definition.
- **AI controller:** the single Core entry point used by Simulation and Unity.
  It routes each decision request to a typed policy.
- **Typed decision policies:** mutation acquisition, surge activation/banking,
  mycovariant drafting, and reactive-board choices. Adaptations are resolved as
  complementary starting loadout, not drafted by AI. Starting position remains
  scenario configuration rather than strategy intelligence.
- **Read-only decision context:** explicit player, board, round, available
  actions, scenario settings, and deterministic random source. Policies should
  not discover hidden global context.
- **Action/result contract:** a policy proposes a legal typed action plus a
  reason; the owning rules service validates and applies it.
- **Decision trace:** structured records of request, legal candidates, selected
  action, reason code, relevant scores, and outcome for debugging and analysis.
- **Migration adapter:** wraps the current mutation-spending strategy while
  behavior is characterized and definitions migrate. Stable IDs remain the
  machine identity and existing names remain compatibility aliases; display
  names may evolve independently without a breaking reference cutover.

The spike must compare this composition model against a minimally cleaned-up
`ParameterizedSpendingStrategy` using the pre-registered backlog behaviors and
measures in P4.2–P4.4. Replacement is justified only by a material measured win
without a regression in determinism or runtime cost.

### Decision-surface ownership target

| Surface | Strategy-controlled behavior | Scenario/environment control |
|---|---|---|
| Mutation acquisition | build priorities, prerequisites, fallback, banking | mutation availability and balance |
| Surge use | activation conditions, targets, timing | enabled content and starting loadout |
| Mycovariant draft | choice scoring and synergy intent | draft on/off, pool, offers, order |
| Adaptations | complementary loadout metadata; no in-game AI drafting | granted count, forced IDs, and campaign overrides |
| Starting position | none | validated random pool; campaign pool/coordinate override; geometry |
| Reactive play | board-state response rules | opponent lineup and game systems |

The Phase 1 audit must prevent environmental advantages from being mislabeled
as AI intelligence.

### Standard identity and summary model

The overhaul may reset all current strategy names. The proposed replacement
separates immutable machine identity from human presentation and measured
classification:

- **Strategy ID:** `ai.<primary-archetype>.<identity>.v<major>`, lowercase and
  stable, for example `ai.growth.frontier-rush.v1`. Do not encode difficulty,
  lifecycle, roster membership, or current performance in the ID because those
  can change after recalibration.
- **Display name:** short human-facing name, unique within active rosters.
- **Intent summary:** one or two plain-language sentences describing the opening
  priority, strategic transition, win condition, and any deliberate weakness.
- **Archetype tags:** structured primary and secondary identities, distinct from
  measured difficulty.
- **Teaching mistake:** optional explicit field used for Training opponents,
  such as over-investing in Growth without Resilience or buying too much
  Fungicide before establishing Growth. It must never be an undocumented
  parameter accident.
- **Definition version/fingerprint:** distinguishes behavior revisions without
  overloading the display name.

Candidate IDs use a generated namespace and fingerprint. Promotion assigns a
stable `ai.*` ID. The audit will verify every serialized, CLI, campaign, and UI
reference that must migrate when current names are reset.

Intent-summary style:

> Pushes for maximum Growth to claim board share quickly, then shifts toward
> Reclamation in the late game to recover dead cells. Its limited early
> Resilience makes the opening powerful but fragile.

Summaries describe observable intent rather than implementation parameters, so
they remain useful to humans, tools, campaign authors, and future candidate
generation.

### Starting-position model

- For each supported board geometry and player count, testing establishes a
  validated candidate-position pool and any fairness/difficulty annotations.
- Normal play assigns AI strategies randomly among the eligible positions; a
  strategy does not choose its own favorable slot.
- Campaign scenarios may override the normal pool with a curated pool or exact
  coordinates. Moving an AI toward the middle may intentionally raise
  difficulty; moving it toward the playable crust may lower difficulty.
- Irregular boards use a versioned geometry/mask fingerprint. A campaign level
  may lock its board mask and exact or curated starting configuration, including
  future disconnected or holed shapes such as torn bread or a bagel.
- Placement effects are measured and reported separately from strategy strength.

## 6. Machine-Readable Experiment Contract

Before autonomous search, introduce a versioned experiment manifest that is
both input and durable evidence. Each run writes the fully resolved manifest,
not just the user-supplied shorthand.

Required manifest groups:

1. **Identity:** schema version, experiment ID, purpose, treatment/control
   labels, creation time, code commit, balance/config fingerprint, and strategy
   definition fingerprints.
2. **Lineup:** exact strategy IDs and versions in selected order, any aliases
   resolved, opponent-pool identity, and selection policy if sampling is used.
3. **Board:** width, height, geometry/mask identity and fingerprint, player
   count, starting coordinates or position-pool identity, and slot assignment
   policy.
4. **Systems:** nutrient patches, mycovariant draft and pool, starting
   adaptations per player, and every other optional gameplay system with an
   explicit value. Defaults must be resolved into the output manifest.
5. **Randomness:** base seed, exact game-seed schedule, strategy-selection seed,
   and any independent random streams introduced later.
6. **Sampling:** games per condition, condition matrix, early-stop rules,
   confidence method, comparison baseline, calibration versus holdout role,
   and retry policy.
7. **Outputs:** artifact root, expected datasets, analysis version, completion
   status, failures, and manifest/result checksums.

Contract requirements:

- Reject unknown fields, invalid combinations, missing strategy IDs, illegal
  positions, and accidental treatment/control differences before a long run.
- Provide a manifest diff that highlights every differing variable.
- Support one-command replay from a resolved manifest.
- Resume only missing/failed conditions without silently rerunning successful
  samples.
- Preserve raw per-game artifacts; derived summaries are regenerable.
- Reject any individual simulation batch above 100 games. Autonomous searches
  must use staged batches and prune weak candidates before expensive gates.
- Reject an experiment whose requested player count exceeds the selected roster;
  analytical runs must never synthesize anonymous or numbered fallback
  strategies.
- Every decision-bearing experiment predeclares one primary hypothesis, metric,
  estimand, direction, context, and materiality margin. Undeclared secondary
  analyses are exploratory and cannot advance a candidate.
- Bound both games per condition and total games/runtime per decision. The
  per-condition ceiling is a local safety guard, not the compute budget.

## 7. Measurement and Classification Model

### Primary strength measures

- **Parity-normalized final board share:** player living-cell share divided by
  equal expected share (`player_count * player_living / total_living`). `1.0`
  is parity for the active player count.
- **Win-rate surplus:** tie-fractional win credit minus the equal-win
  expectation. It is a reported tertiary metric, not a promotion metric.
- **Average rank:** tie-aware final placement, normalized when comparisons span
  different player counts. It is the secondary promotion metric.
- **Uncertainty:** confidence/credible interval for primary measures and the
  sample count contributing to each condition.
- **Robustness:** distribution across seeds, slots, opponent compositions,
  player counts, map classes, and enabled-system conditions rather than only a
  pooled mean.

### Secondary diagnostic measures

- mutation acquisition order and round; points spent and banked
- surge attempts, activations, targets, and value realized
- mycovariant offers, scores, choices, and realized effects
- adaptation/loadout identity and relevant activations
- living/dead/toxin composition and growth-source mix
- decision failures, no-action reasons, illegal proposals, and fallback use
- runtime per game and candidate, for automation capacity planning

### Banding principles

- Compute empirical bands from a frozen reference corpus before assigning final
  player-facing labels.
- Keep contextual bands by at least map class and player-count class whenever
  the interval or effect-size threshold shows a material difference.
- Report both central tendency and a conservative robustness statistic. A
  strategy with a strong mean and catastrophic common matchup should not be
  labeled simply `Hard`.
- Separate measured strength from authored role. `Boss`, `Training`, `Spice`,
  and archetype are not synonyms for performance bands.
- Version every classification with the reference corpus, balance fingerprint,
  analysis version, and date.

The decisive primary metric is parity-normalized final board share. Exact
thresholds, interval method, tie policy, minimum samples, and materiality
cutoffs are versioned Phase 3 decisions informed by baseline data and declared
before treatment outcomes are analyzed.

## 8. Step-by-Step Execution Plan

Each numbered slice is intended to be independently reviewed, validated,
committed, and handed off. Do not start a later phase while its prerequisite
gate remains open.

### Phase 0 — Freeze scope and baseline vocabulary

- **P0.1:** Resolve the remaining section 12 product decisions and record answers
  here.
- **P0.2:** Define `strategy`, `controller`, `policy`, `candidate`, `condition`,
  `calibration`, `holdout`, `promotion`, and `performance band` precisely.
- **P0.3:** Inventory player-facing campaign and single-player pools, serialized
  and CLI references, and the compatibility aliases required when stable IDs
  become machine references.
- **P0.4:** Select a small frozen reference lineup and representative conditions
  for architecture parity tests; do not claim balance from this smoke corpus.
- **Gate:** Scope, compatibility posture, terminology, and baseline corpus are
  explicit enough that two sessions would construct the same experiment.

### Phase 1 — Complete the current-state audit

- **P1.1:** Build the decision-surface evidence table described in section 4.
- **P1.2:** Map `ParameterizedSpendingStrategy` precedence and every parameter to
  observed callers and tests; identify dead, misleading, implicit, or coupled
  controls.
- **P1.3:** Quantify authoring duplication and metadata drift in `AIRoster`;
  inventory aliases and strategy families that are exact or near duplicates.
- **P1.4:** Trace Unity/Core/Simulation invocation ownership and deterministic
  random sources. Add characterization tests for any ambiguous path before
  refactoring it.
- **P1.5:** Audit CLI controls, run metadata, exports, analytics, campaign
  harnesses, and manual steps against the experiment-contract requirements.
- **P1.6:** Produce a gap/risk matrix ranked by causal-confounding risk,
  migration risk, and automation payoff.
- **Gate:** The audit accounts for every decision surface and every experimental
  variable, with no unexplained execution path.

### Phase 2 — Establish reproducible experiment infrastructure

- **P2.1 — complete:** Defined and tested the original
  `fungus-toast.experiment-input.v1` contract (superseded by v2 for paired
  experiments), including CLI-to-condition translation, strict JSON
  unknown/missing-field handling, semantic validation, and the hard 100-game
  per-condition ceiling. The current checked-in example is
  `FungusToast.Simulation/Examples/experiment-input.v3.example.json`.
- **P2.2 — complete:** Added `fungus-toast.experiment-result.v1` output with
  code, condition, board, balance-binary, execution, and strategy-definition
  fingerprints; exact seed schedules; selected/assigned lineups; actual
  per-game starting coordinates and Adaptations; artifact hashes; completion
  status; a canonical outcome fingerprint; and a manifest checksum sidecar.
  `--replay-manifest` strictly checks the recorded code and strategy identities,
  executes the exact lineup/seed schedule, and fails on outcome-fingerprint
  inequality. A separate-process smoke replay passed on 2026-09-01.
- **P2.3 — complete:** `games.parquet` records condition/board fingerprints,
  selected and assigned lineups, full board-mask identity, all enabled-system
  toggles, configured and actual position/loadout values, and nutrient results.
  `players.parquet` records condition identity, board context, toggles, and each
  player's actual starting coordinate and Adaptations. The existing analytics
  workflow was smoke-tested successfully against the expanded schema.
- **P2.4 — complete:** `--compare-manifests` compares only causal replay inputs.
  `--allow-differences` declares the intended treatment paths; comparison fails
  for every undeclared difference and for every declared path that did not
  actually differ. Unit tests and a real nutrient-toggle control/treatment pair
  proved both the passing and contamination-rejection paths.
- **P2.5 — complete:** Parquet runs maintain a checksummed, execution-
  fingerprinted `run-state.json` with `running`, `complete`, `interrupted`, or
  `failed` status and failure details. `--resume` skips only a matching complete
  condition after rechecking the resolved-manifest checksum; failed,
  interrupted, and missing conditions rerun, while mismatched complete output
  is rejected. The offline analyzer regenerated twice with byte-identical
  outputs and no simulation rerun.
- **P2.6 — complete:** The checked-in
  `FungusToast.Simulation/Examples/verify-experiment-contract.sh` fixture builds
  the solution, proves selective resume across an expanded board-size matrix,
  replays a completed condition in a separate process, verifies canonical
  outcome equality, and validates the resolved-manifest checksum.
- **Gate:** Replaying a resolved manifest produces the same lineup, conditions,
  seed schedule, and deterministic outcomes; artifacts explain every input.

### Phase 3 — Add outcome metrics and statistical gates

- **P3.1 — complete:** Export total living share inputs, tie-aware rank, starting slot and
  coordinates, adaptations, and structured condition identifiers. The first
  independent slice exports the per-game total living-cell denominator,
  competition-style rank, and tie-group size in `players.parquet`; existing
  starts, Adaptations, and raw condition IDs remain exported. The second
  independent slice adds the explicit condition dimensions required for
  grouping without parsing the raw ID: schema, strategy/selection and slot
  policies, start-position mode, player count, and board geometry fingerprint.
- **P3.2 — complete:** Implement normalized board share, win surplus, normalized rank,
  intervals, effect sizes, and robustness summaries in offline analytics.
- **P3.3 — complete:** Define smoke, calibration, comparison, and holdout sample gates with
  early rejection for invalid/parity-failing candidates—not early promotion.
- **P3.4 — complete:** Define contextual map/player-count taxonomy from actual supported
  boards and measured sensitivity.
- **P3.5 v1 — historical:** Generated a descriptive baseline report for the
  frozen reference corpus. Its inferential role was reopened by the accepted
  adversarial review; P3.R7 will publish the corrected successor. The original
  artifact-backed report remains
  [AI_P3_5_REFERENCE_BASELINE_V1.md](second-level/AI_P3_5_REFERENCE_BASELINE_V1.md).
- **Gate:** The same artifacts always yield the same classification report, and
  weak evidence cannot pass a promotion gate.

#### P3.3 staged evidence gates

Every stage uses a versioned manifest, resolved artifact, exact seed schedule,
and at most 100 games in any condition. A candidate or balance treatment may
advance only when its current stage is complete and reproducible; failure stops
that candidate rather than consuming a larger batch.

| Stage | Per-condition games | Required evidence | Advancement rule |
|---|---:|---|---|
| Static | 0 | schema/semantic validation, legal lineup and positions, declared treatment paths | Reject invalid or confounded input. |
| Smoke | 3–5 | complete Parquet/manifest/checksum, parity invariants, no deterministic/replay failure | Reject on any integrity failure; never promote on smoke. |
| Calibration | 20 | normalized outcomes and intervals against the named control under matched seeds/slots | Reject clear regression outside the predeclared non-inferiority margin; retain inconclusive results only for comparison. |
| Comparison | 50 | manifest diff, effect sizes, intervals, and context summary across the declared representative conditions | Reject if the treatment fails its predeclared hypothesis or shows a material common-context weakness. |
| Holdout | 100 | the same measures on conditions, seeds, or lineups not used to select the candidate | A passing holdout can enter the generated testing catalog only; player-facing promotion still requires later review. |

Numerical margins and materiality thresholds are frozen with the P3.5 reference
corpus, not retrofitted after observing a treatment. Inconclusive intervals are
not evidence of parity or improvement.

#### P3.4 contextual taxonomy

The simulation accepts any positive width/height and the supported 2–8-player
range, so taxonomy derives from resolved manifest fields rather than a fragile
list of presets. Every report retains exact dimensions, geometry fingerprint,
and player count; the following classes provide comparable rollups.

| Dimension | Classes | Derivation |
|---|---|---|
| Player count | duel (2), small-table (3–4), crowded (5–6), swarm (7–8) | resolved `playerCount` |
| Board scale | small (area ≤ 6,400), medium (6,401–25,600), large (> 25,600) | `width × height`; 80×80 and 160×160 anchor the first two classes |
| Aspect | square (0.9–1.1 width/height), wide (> 1.1), tall (< 0.9) | resolved width/height ratio |
| Geometry | rectangle, masked | `geometryId` and mask fingerprint; masked results never pool across different fingerprints by default |
| Start regime | generated, exact, preferred-pool | resolved start-position mode and slot policy |

Reports must name one primary context for any decision-bearing hypothesis.
Other class estimates are exploratory and use a declared multiplicity policy
(FDR control or a suitable hierarchical model) before they can motivate a new
confirmatory experiment. Context axes are not called independent unless the
sampled design establishes that property. The taxonomy is not a required full
Cartesian matrix: begin with player count and board scale, then add aspect,
geometry, or start-regime coverage when evidence justifies it.

#### P3 correction program accepted after adversarial review

These slices reopen P3.2, P3.3, and P3.5. P2 provenance artifacts remain valid
records of the binaries that produced them, but the current pipeline must be
reproved after the corrected RNG and result contracts land.

1. **P3.R1 — cheap correctness:** make final outcomes tie-aware with fractional
   win credit and explicit outcome status; correct picked/not-picked
   denominators; remove authored appetite from mutation-power scores; mark all
   observational mutation, mycovariant, synergy, and interaction outputs as
   exploratory; reject missing condition identity and insufficient rosters.
2. **P3.R2 — analytical identity:** add stable strategy IDs and behavior-specific
   definition fingerprints to resolved manifests and row exports. Group and
   join on ID plus fingerprint; names and themes are labels only.
3. **P3.R3 — RNG contract:** replace the shared stream with a versioned,
   hierarchical deterministic stream contract. AI decisions and gameplay use
   separate domains; repeated decisions include occurrence identity. Record
   stream-contract identity in artifacts and prove that an internal AI draw-
   count change cannot perturb gameplay randomness when chosen actions match.
   Implemented contract v1 keeps the historical `Random(baseSeed)` gameplay
   sequence and derives AI streams from SHA-256 identities containing base seed,
   player, round, decision kind, and occurrence. Mutation spending, interactive
   and fast-forward drafting, and roster selection use scoped streams. Resolved
   result schema v6 and Parquet manifest v9 stamp the contract version.
4. **P3.R4 — paired inference:** add explicit pair IDs, paired estimators, and
   game/seed-aware uncertainty. Measure treatment correlation and variance
   reduction empirically; do not promise a universal common-random-number gain.
   Implemented input schema v2 adds an optional validated pairing-group ID;
   emitted pair IDs join each game and assigned slot across artifacts. The
   paired analyzer fails on missing pairs or mismatched controls and reports
   observed correlation and paired-versus-unpaired variance for each outcome.
5. **P3.R5 — enforceable gates:** extend the input manifest with a declared
   hypothesis, primary metric/context, estimand, direction, margin, analysis
   version, and total game/runtime budget. An analyzer may issue a verdict only
   for that declaration; all other results are exploratory and multiplicity-
   controlled where applicable.
   Implemented input schema v3 requires total game/runtime budgets, analysis
   version, and an evidence stage whose game count matches the approved ladder.
   Comparison/holdout hypotheses preregister one paired target/context/metric,
   estimand, direction, and margin. `--emit-verdict` refuses absent, mismatched,
   unsupported, incomplete, or under-stage declarations.
6. **P3.R6 — geometry and robustness:** derive opponent-lineup and shape-aware
   starting-position covariates from the preserved mask and actual starts. Add
   termination reason, elimination timing, corpus/analysis versions, and runtime
   evidence. Replace raw min-of-K robustness with a shrunken lower-tail measure
   once the corpus supports it.
   Implemented exports stamp analysis and pre-Phase-5 corpus versions; record
   runtime, termination, elimination, and opponent-lineup identity; and derive
   nearest-opponent/edge geodesics plus playable-centroid distance from the full
   mask. Robustness uses a 20-game-prior shrunken 10th percentile and shrunken
   range instead of the sample-count-biased raw minimum.
7. **P3.R7 — corrected reference:** rerun contract/replay validation and publish
   P3.5 v2 under the corrected schema, RNG, and analyzer. Keep v1 unchanged and
   clearly superseded. Budget another versioned reference corpus after any
   Phase 5 behavior-changing migration.
   Complete: the two historical 50-game conditions were rerun as the immutable
   [P3.5 corrected reference baseline v2](second-level/AI_P3_5_REFERENCE_BASELINE_V2.md).
   Both artifacts passed manifest checks, corpus A passed a separate-process
   outcome replay, and the contract fixture reproved resume/replay behavior.

**Correction gate:** focused and canonical tests pass; replay/resume is reproven;
P3.5 v2 identifies its RNG, analysis, balance, and AI-corpus versions; no
decision-bearing report can be produced from undeclared or underpowered evidence.

**Correction gate passed (2026-09-04).** Phase 4 characterization may proceed;
the v2 reference remains descriptive and carries no balance verdict.

### Phase 4 — Architecture spike and decision

- **P4.1:** Write characterization tests around current mutation spending,
  mycovariant choice, surge timing/banking, exclusions, tendril direction, and
  starting-offset behavior.
  Complete: focused public-path tests now pin authored mycovariant preference
  versus the always-pick threshold, scheduled and last-resort surge ordering,
  two-round surge-window banking, runtime exclusion filtering, board-aware
  tendril direction, and positive/outward versus negative/inward start offsets.
  Existing goal/prerequisite tests continue to pin sequential mutation spending.
- **P4.2:** Pre-register 3–5 concrete missing behaviors from the actual backlog,
  including at least one reactive rule, surge-timing condition, and draft
  heuristic that needs board state.
  The Phase 4 bake-off uses exactly these three behavior fixtures:

  | ID | Missing behavior | Acceptance fixture |
  |---|---|---|
  | B1 trailing defense | After round 10, when a player's normalized living-cell share is below 0.75, rank a legal Cellular Resilience fallback ahead of otherwise preferred fallback categories for that decision only. | Chooses the defensive option at 0.70 share and the authored normal option at 0.80; no persistent strategy state changes. |
  | B2 surge window | A scheduled preferred surge may fire only while the player is below equal living-cell share or global playable occupancy is at least 65%; catch-up-tag surge behavior remains separately expressible. | Defers at 1.10 share/50% occupancy, fires at 0.90/50%, and fires at 1.10/70%. |
  | B3 draft synergy | Add a deterministic bonus to a mycovariant whose category matches either of the player's two strongest owned mutation categories, after base score and before stable ID tie-breaking. | A one-point base-score deficit is overcome by category synergy; a larger deficit is not; equal final scores choose lower mycovariant ID. |

  Freeze the following comparison before implementation. Both throwaway designs
  receive identical immutable fixture inputs and must pass all B1–B3 cases.
  Disabled behaviors must preserve 1,000 generated legacy-decision fixtures
  exactly. Record behavior-only logical lines changed and files touched, whether
  each behavior is expressible without adding a parameter consumed by unrelated
  paths, and elapsed time for at least 1,000,000 mixed decisions after warm-up.
  Runtime must remain within 5% of the faster design. Composition wins only if
  it passes correctness/parity/runtime and reduces behavior-only logical lines
  by at least 20% for at least two behaviors, or avoids modifying at least one
  unrelated existing decision path for at least two behaviors. The revised
  parameterized design wins under the symmetric rule. Otherwise record a
  partial/hybrid verdict and retain the smaller production change.
- **P4.3:** Implement those behaviors in throwaway prototypes of both the
  composed controller and the smallest viable parameterized cleanup without
  migrating the roster.
- **P4.4:** Compare files and lines changed per behavior, expressibility,
  parameter leakage, behavior changes in existing strategies under matched
  runs, test burden, and runtime delta. Trace quality and search-space encoding
  remain descriptive secondary evidence.
- **P4.5:** Record an architecture decision: adopt composition, retain a revised
  parameterized design, or use a justified hybrid. Include rejected options.
- **Gate:** One design wins materially on the pre-registered backlog work, and
  an incremental migration with stable IDs, preserved reference aliases, and
  deterministic behavior is demonstrated. Otherwise retain the smaller change.

**Phase 4 complete:** P4.1 characterization passes with the full Core suite.
Both prototypes passed B1–B3 and 1,000 disabled-
behavior parity fixtures. The parameterized prototype used 56.3% and 44.4%
fewer behavior lines for B1 and B2. Composition isolated decision paths better
but was 21–25% slower across five one-million-decision runs, failing the 5%
runtime envelope. The recorded decision is to retain the revised parameterized
design and add narrow typed seams only for demonstrated needs. See
[AI_PHASE4_ARCHITECTURE_DECISION.md](second-level/AI_PHASE4_ARCHITECTURE_DECISION.md).

### Phase 5 — Implement the AI foundation incrementally

- **P5.1:** Add the unified strategy definition/registry and standardized identity
  model; co-locate behavior configuration, complementary Adaptation metadata,
  authored intent summary, archetypes, and lifecycle metadata.
  The additive registry foundation is complete: each roster entry is captured
  in one immutable `StrategyDefinition` containing its implementation, stable
  ID, behavior fingerprint, and full `StrategyCatalogEntry`. Manifest export,
  replay, profiles, and live metadata reads consume that record. Bootstrap
  override maps reject duplicate keys and orphaned names instead of silently
  drifting; no strategy names were reset.
- **P5.2:** Do not add the proposed general controller/context/action graph.
  Add immutable focused inputs or pure helpers only where production behavior
  requires them. Decision traces are opt-in and sampled by default.
- **P5.3:** Retain `IMutationSpendingStrategy` as the deterministic mutation-
  acquisition boundary; the rejected general controller needs no legacy
  adapter. Keep shared behavior contracts (including exclusions) on the
  interface and prove Core/Simulation/Unity parity before policy changes.
  The shared exclusion contract is now implemented by the strategy base and
  consumed through the interface by Mutator Phenotype auto-upgrades; a non-
  parameterized regression prevents concrete-type fallback.
- **P5.4:** Keep surge activation/banking in the revised parameterized design;
  extract a pure focused helper only when an approved surge behavior needs it,
  while preserving current behavior by default.
- **P5.5:** Route mycovariant drafting through a typed policy instead of a
  concrete strategy downcast; preserve campaign-specific rules explicitly.
  The pre-spike correctness slice is complete: draft selection now dispatches
  through `IMutationSpendingStrategy`, every implementation must declare its
  draft behavior, and the manager fails loudly when an AI has no strategy.
  The Phase 4 result keeps this as a focused required interface method rather
  than introducing a separate composed policy.
- **P5.6:** Resolve complementary starting Adaptations without an AI draft, and
  keep grant count and forced IDs under explicit scenario control. Complete:
  every AI receives its mold-matched starting Adaptation; campaign difficulty
  adds 0–5 extras from Training through Boss. Authored additions count toward
  the quota and may exceed it; deterministic selection prefers strategy-themed
  options before global fallback. Exact loadouts, position pools, and curated
  campaign coordinates/geometry remain scenario controls. The behavior change
  is frozen in the
  [P3.5 starting-Adaptation reference baseline v3](second-level/AI_P3_5_REFERENCE_BASELINE_V3.md).
- **P5.7:** Add reactive board-state policy primitives only for demonstrated
  decisions; avoid a catch-all scoring framework.
- **P5.8:** Migrate a small reference set, then each roster family in isolated
  slices. Stable IDs become machine references; retain existing names as
  compatibility aliases. Player-facing display names may change independently
  without a breaking reference cutover.
  Complete for the registry migration: every current roster family is registered
  under a stable ID and fingerprint while all existing names remain aliases.
- **Gate:** All player-facing strategies use explicit deterministic Core
  boundaries, registry metadata cannot drift silently, and current behavior or
  intentional deltas are covered by focused evidence. Traces remain opt-in and
  are required only for decisions under active diagnosis or experimentation.

**Phase 5 gate passed (2026-09-05).** The deterministic Core/Simulation suite,
reference replay, and Jake's Unity Editor compile and affected-flow validation
all pass. No unvalidated reactive behavior was added for P5.7.

### Phase 6 — Prove, then build autonomous candidate evaluation

Begin with a bounded deterministic parameter sweep over existing safe tunables
that writes a results table. Add the framework below only after that simple
sweep produces at least one candidate worth promoting.

#### Frozen Phase 6 pilot — Balanced Control economy bias

Before running the pilot, compare the three existing Testing definitions whose
behavior configuration differs only by `EconomyBias`:

- `TST_BalancedControl_MinorEconomy` (`MinorEconomy`);
- `TST_BalancedGeneralistControl` (`ModerateEconomy`, the control);
- `TST_BalancedControl_MaxEconomy` (`MaxEconomy`).

Run one 100-game, three-player, 120x120 rectangular condition with base seed
`2026090501`, rotating slots, nutrient patches off, Mycovariants off, and all
starting Adaptations off. The total budget is 100 games and 600 runtime seconds.
This is a bounded multi-arm screen, not a player-facing balance verdict.

Normalized board share is primary, normalized rank is secondary, and win credit
is descriptive. For each candidate, compute the per-game paired difference from
the Moderate control. Use a two-sided 97.5% interval for each of the two primary
comparisons (Bonferroni familywise alpha 0.05). A candidate is worth advancing
to a separately preregistered holdout only if it completes all 100 games with no
parity failure, its mean board-share lift is at least `0.05`, and the adjusted
interval excludes zero in the favorable direction. If both candidates pass,
advance only the larger primary lift. Secondary metrics cannot advance a
candidate. If neither passes, do not build the Phase 6 framework from this
pilot; record that fallback economy bias alone did not justify promotion.

**Pilot result (2026-09-05): no candidate advanced.** The smoke and 100-game
screen completed with zero parity mismatches, valid manifest checksums, empty
starting loadouts, balanced rotating-slot exposure, and a 305.200-second game
runtime. Relative to Moderate, Minor's paired normalized-board-share lift was
`+0.0116` with adjusted 97.5% interval `[-0.0922, +0.1154]`; Max's was
`+0.0242` with interval `[-0.0763, +0.1247]`. Both missed the `+0.05` effect
threshold and both intervals crossed zero. Mean normalized rank was `0.490`
for Moderate and `0.505` for both candidates. Descriptive win credit was
35%/28%/37% for Moderate/Minor/Max. The source artifact is
`p6_economy_bias_pilot_100`; its separate-process replay verified outcome
fingerprint
`99862d893ed77f5ba541a7517f7ad4ab92587c5b6953d1ce3c908b7a5cc1c67c`.
Fallback economy bias alone therefore does not justify building the candidate
framework or changing a player-facing strategy.

#### Frozen Phase 6 follow-up — Balanced Control opener order

Compare `TST_BalancedControl_AnabolicFirst` against
`TST_BalancedGeneralistControl`. Both use `ModerateEconomy`, high-tier
prioritization, the same Mycovariant preferences, and the same four goals; the
candidate moves `Anabolic Inversion` ahead of `Creeping Mold` while retaining
`Necrosporulation` and `Catabolic Rebirth` in third and fourth position.

Run one 100-game, two-player, 120x120 rectangular condition with base seed
`2026090502`, rotating slots, nutrient patches off, Mycovariants off, and all
starting Adaptations off. The total budget is 100 games and 600 runtime seconds.
Normalized board share is the sole advancement metric; normalized rank is
secondary and win credit descriptive. Compute candidate-minus-control share per
game and its two-sided 95% interval. Advance the candidate only if all games
complete with no parity failure, mean lift is at least `0.05`, and the interval
excludes zero in the favorable direction. A pass permits a new-context holdout,
not a player-facing roster change. A failure ends this line without framework
construction.

**Screen result (2026-09-05): candidate advanced to holdout.** All 100 games
completed in 325.002 seconds with zero parity mismatches, empty starting
loadouts, valid checksums, and exactly 50 games in each slot per strategy.
Anabolic-first produced normalized board share `1.3755` versus `0.6245` for the
control: paired lift `+0.7509`, 95% interval `[+0.6335, +0.8683]`. Its
normalized rank and descriptive win credit were both `0.88` versus `0.12`.
This passes the frozen advancement threshold but is not yet a player-facing
promotion. Source artifact: `p6_opener_order_100`; outcome fingerprint:
`07dec8cac82a77bf7f37cd457c3b33b40d091becc170eb297dc0ebab48143727`.

#### Frozen opener-order holdout

Repeat the same two definitions and sole goal-order contrast for 100 games on
an unseen 140x100 rectangular context with base seed `2026090503`. Keep rotating
slots, nutrient patches off, Mycovariants off, and all starting Adaptations off;
retain the 100-game and 600-second budgets. Normalized board share remains the
sole decision metric. The candidate confirms only if its paired mean lift is at
least `0.05` and the two-sided 95% interval excludes zero favorably, with full
completion and no parity failure. A pass establishes opener order as a useful
candidate-search dimension and permits bounded Phase 6 tooling; it does not by
itself place this strategy in a player-facing pool. A failure rejects the
candidate and stops this line.

**Holdout result (2026-09-05): candidate confirmed.** All 100 games completed
in 292.770 seconds with zero parity mismatches, empty starting loadouts, valid
checksums, and exactly 50 games in each slot per strategy. Anabolic-first
produced normalized board share `1.4295` versus `0.5705` for the control:
paired lift `+0.8590`, 95% interval `[+0.7512, +0.9668]`. Its normalized rank
and descriptive win credit were both `0.94` versus `0.06`. A separate-process
replay exactly reproduced outcome fingerprint
`6283eca7257865fc5b9ab837815170196ca288e2c32f2577627cca0c5a3dbacd`.
The frozen gate therefore passes: mutation-goal order is a useful bounded
candidate-search dimension and Phase 6 tooling may proceed. This result does
not promote the Testing candidate or change a player-facing strategy.

- **P6.1:** Define a bounded, serializable candidate genome from safe policy
  choices and tunables. Every dimension must be independently switchable.
  **Complete (2026-09-06)** — see the contract below.
- **P6.2:** Add deterministic candidate generation, deduplication, fingerprints,
  lineage, and invalid-candidate rejection. **Complete (2026-09-06)** — see the
  contract below.
- **P6.3:** Implement staged evaluation: static validation, unit/characterization
  checks, tiny smoke, calibration comparison, then unseen holdout conditions.
  **Complete (2026-09-06)** — see the contracts below.
- **P6.4:** Add dominated-candidate pruning, resource/time budgets, retry caps,
  resumable queues, and an enforced maximum of 100 games in any one batch.
  **Complete (2026-09-06)** — see the contract below.
- **P6.5:** Rank strength and robustness separately from archetype fidelity and
  behavioral diversity. **Complete (2026-09-06)** — see the contract below.
- **P6.6:** Produce a promotion packet containing definition diff, lineage,
  manifests, metrics, intervals, context bands, traces, failures, and holdouts.
  **Complete (2026-09-06)** — see the contract below.
- **Gate:** A clean run can generate, evaluate, reject, and add passing candidates
  to the generated testing catalog without manual file edits, while no candidate
  enters a player-facing pool without review.

#### P6.1 candidate genome contract (delivered 2026-09-06)

`fungus-toast.ai-candidate-genome.v1` lives in `FungusToast.Simulation/Candidates`,
alongside the Phase 2 experiment contract rather than in Core. It is search
tooling, not gameplay: Core is untouched, no AI behavior changed, and the AI
corpus version is unaffected, so the frozen P3.5 v3 baselines remain valid.
A materialized candidate is an ordinary `ParameterizedSpendingStrategy`.

Ten genes cover the whole behavior surface: `PrioritizeHighTier`, `MaxTier`,
`PriorityMutationCategories`, `TargetMutationGoals`, `SurgePriorityIds`,
`SurgeAttemptTurnFrequency`, `EconomyBias`, `MycovariantPreferences`,
`ExcludedMutationIds`, and `StartingSporeEdgeOffset`.

Four decisions define the contract:

- **The gene set is stated in full, not as a sparse diff.** A genome materializes
  without consulting the roster, and two genomes describing the same behavior
  always share one fingerprint, which is what P6.2 deduplication will need.
- **"Independently switchable" is enforced, not documented.** Each genome declares
  `variedGenes`; validation fails both when a gene actually differs but is not
  declared and when a declared gene did not change. This is the same
  declared-difference contract `--compare-manifests` already applies to
  experiment inputs, so a candidate is a single-variable treatment by
  construction rather than by reviewer diligence.
- **Identity is derived, never authored.** The behavior fingerprint is SHA-256 over
  a canonical per-gene text, and `candidateId` is
  `candidate.<parent-slug>.<fingerprint-prefix>`. Validation recomputes it, and
  lineage stores the parent's stable ID plus Core's definition fingerprint, so a
  changed parent invalidates the lineage claim instead of silently reparenting.
  Display names are namespaced `CAND_` and rejected if they collide with a
  registered strategy.
- **Bounds are semantic, not just structural.** Every mutation, mycovariant, and
  category ID must exist; surge entries must actually be Mycelial Surges; target
  levels must be within the mutation's own range; exclusions may not overlap
  goals; and preferences must be serialized in Core's evaluation order.

Two Core details the genome deliberately preserves rather than inherits. Core's
definition fingerprint flattens a null `priorityMutationCategories` and an empty
one to the same text even though it treats null as "every category"; the genome
keeps them distinct so deduplication cannot merge two different behaviors.
And repeating a mutation in the goal list is a real roster idiom — buy it to a
low level early, return for more later — so repeats are legal as a strictly
rising ladder and rejected when flat or falling, which is dead configuration.

Validation evidence: 48 Simulation tests pass, including a round trip over all
132 registered parameterized strategies proving that extracting a strategy's
genes and re-materializing it under its own name reproduces Core's definition
fingerprint exactly. That is what makes "the genome covers the safe surface" a
checked claim rather than an assertion; it also fixed the two bounds
(`startingSporeEdgeOffset`, the goal ladder) where the first draft contradicted
real roster content. Core passes 649/649. The checked-in example
`FungusToast.Simulation/Examples/candidate-genome.v1.example.json` encodes the
opener-order candidate the Phase 6 holdout already confirmed.

#### P6.2 candidate generation contract (delivered 2026-09-06)

`fungus-toast.ai-candidate-plan.v1` adds `CandidateGenerationPlan`,
`CandidateGenerationPlanValidator`, and `CandidateGenerator` beside the P6.1
genome. A plan names one parent, an ordered operator list, declared value lists,
and a candidate cap; generation returns accepted genomes plus every rejection.

**Generation is exhaustive, not sampled, and takes no seed.** Each operator
enumerates a finite ordered set of gene sets derived from the parent, so the
same plan against the same registry always yields the same candidates, display
names, lineage notes, and rejections in the same order. That is a stronger
reproducibility guarantee than a recorded seed, and it is affordable precisely
because Phase 6 batches cap at 100 games: a search wide enough to need sampling
could not be evaluated anyway. A plan whose enumeration exceeds its own
`maximumCandidates` fails before generating rather than silently truncating.

Seven operators, each varying exactly one gene: `GoalOrderAdjacentSwap`,
`GoalOrderPromoteToFront`, `EconomyBiasSweep`, `PrioritizeHighTierToggle`,
`SurgeAttemptTurnFrequencySweep`, `StartingSporeEdgeOffsetSweep`, and
`MaxTierSweep`. The three sweeps require a declared value list, and a value list
without its operator is refused — an ignored field in a search plan is a silent
lie about what was searched.

**Deduplication is by behavior fingerprint, in three tiers**, each recorded as a
typed rejection rather than a silent drop, which is the candidate-search half of
the Static evidence stage:

- `DuplicateOfParent` — sweeps deliberately include the parent's current value so
  the no-op is logged instead of skipped.
- `DuplicateOfEarlierCandidate` — two operators that converge on one build
  collapse to a single candidate.
- `DuplicateOfRegisteredStrategy` — a candidate that reproduces an existing
  roster build is rejected, because testing it spends batch budget re-proving a
  known result.

Real roster behavior exercises all four rejection paths. Swapping Balanced
Control's first two goals reconstructs `TST_BalancedControl_AnabolicFirst`, so
the generator refuses the build the Phase 6 holdout already confirmed. Promoting
goals in `TST_Arch06_SurgeGrowth` inverts its rising level ladder, producing
three `FailedValidation` rejections rather than unusable candidates.

Validation evidence: 68 Simulation tests and 651 Core tests pass. Building this
slice also exposed a latent defect unrelated to AI work — `AnalyticsEventSubscriber`
kept a process-wide non-concurrent dictionary that tore once a second Simulation
test class ran in parallel. It is fixed separately in commit `b3e1807`; the map
holds only handler references, so no simulation outcome, replay fingerprint, or
corpus version is affected.

#### P6.3 characterization gate (delivered 2026-09-06; stages 1–2 of 5)

P6.3's two zero-game stages are complete. Static validation is the P6.2 plan,
genome, and rejection layer. `CandidateCharacterizationGate` adds the
unit/characterization stage: it materializes each candidate and asks whether it
actually plays — constructs, spends without throwing, buys something, honors its
own declared exclusions, drafts a mycovariant that was actually offered, and does
all of it identically under a repeated seed.

The stage exists because a gene set can be structurally perfect and behaviorally
inert, and because catching that later is expensive: no games here, a batch at
smoke, fifty at comparison. Determinism is checked rather than assumed —
P3.R4 paired inference compares a candidate against its control under matched
seeds, so a candidate whose own decisions drifted under a fixed seed would
silently corrupt every downstream interval.

Evidence: generating from **all 132 registered parameterized parents** across
goal-order, economy, high-tier, and max-tier operators produced 1,559 proposals
and 1,247 accepted candidates, and **all 1,247 passed characterization in 2.5
seconds** with zero findings. That is a clean no-false-positive result, and it
independently confirms AI seed-determinism across 1,247 distinct configurations
rather than the handful covered by existing tests.

Because every failure the gate detects is unreachable through the real
materialization path, `Run` takes an optional materializer. That seam is what
lets tests hand the gate a strategy that throws, buys nothing, drifts under a
fixed seed, buys an excluded mutation, or drafts an unoffered mycovariant, so
each of the seven checks is shown to fire rather than assumed to work.

#### Generated testing catalog (delivered 2026-09-06)

The Phase 6 gate's "generated testing catalog" now exists.
`GeneratedCandidateCatalog` publishes candidates into a new
`StrategySetEnum.Generated`, which `AIRoster` never registers.

Registering rather than injecting is the point. Every reproducibility guarantee
built in Phases 2 and 3 — resolved manifests, stable strategy identity in
Parquet, replay outcome fingerprints — keys off a registered definition. A
candidate handed straight to the simulator would run, but outside all of that,
which is exactly the confounding this initiative exists to remove. A manifest
naming a published candidate now passes the ordinary
`ExperimentManifestValidator`; one naming an unpublished candidate fails.

Two small additive Core changes support it. `StrategyRegistry.Register` accepts
an optional stable-ID factory, so a candidate keeps its own
`candidate.<parent-slug>.<fingerprint>` identity instead of being minted a
second `legacy.*` one and ending up addressed two ways across artifacts. And
`AIRoster.AuthoredStrategySets` names the four sets the roster registers, so
invariants about authored content iterate that rather than every enum value —
two Core tests had assumed those were the same thing.

Nothing player-facing can reach these entries: every published candidate carries
`StrategyPool.None`, so pool-filtered campaign and single-player selection
returns nothing even by accident, and each carries no difficulty band and no
campaign difficulty rather than an authored guess. Phase 7 assigns real bands
from evidence. Publishing is wholesale, so the catalog always reflects one
search rather than an accumulation of untracked runs.

Making candidates registrable also exposed an assumption in the P6.1 validator:
its display-name collision rule checked every registered set, so re-publishing a
candidate saw itself as a collision. The rule now excludes the generated set,
which is the candidate's own home; duplicates within one publish are caught by
the catalog instead.

#### Frozen candidate evaluation shape (approved 2026-09-06)

Jake approved the shape for P6.3's remaining stages:

- **Paired two-artifact swap.** A control condition runs the parent and a
  treatment condition runs the candidate, matched on seeds, slots, board, and
  RNG controls and sharing one pairing group. This is what the P3.R4/P3.R5
  machinery was built for, so `--emit-verdict` and preregistered margins apply.
  It costs two conditions per comparison.
- **Two players against one fixed opponent.** Control is `[parent, opponent]`,
  treatment is `[candidate, opponent]`. Highest signal per game, and closest to
  the two-player context of the frozen opener-order holdout.
- **A holdout must change both board geometry and seed**, matching the frozen
  opener-order holdout that moved 120x120 to 140x100 on a new base seed.

Two infrastructure gaps blocked that shape, and both are now closed.

**Per-condition artifact IDs collided.** They were derived from a condition's
shape — players, board, strategy set — which is unique only by coincidence: the
CLI encodes exactly that shape into the condition ID it generates, so it can
never emit two colliding conditions. A hand-authored manifest can, and a paired
swap is precisely that case. Both arms derived one artifact ID, so the second
run overwrote the first's export, or under `--resume` failed with an execution
fingerprint mismatch pointing nowhere near the cause. `ExperimentArtifactId` now
derives from the condition ID, which the manifest already validates as unique.

**A condition names exactly one strategy set**, but the treatment arm needs a
generated candidate beside an authored opponent. The catalog now publishes an
evaluation cast: candidates plus the authored references they are measured
against. The alternative — per-name set qualification in the manifest — was
rejected because Parquet records the strategy set once per run, so it would turn
a run-level invariant into a per-player one and ripple through the input schema,
resolved manifest, replay runner, export schema, and every set-grouping
analytic, with three schema bumps that would invalidate replay of existing
artifacts. Republishing an authored strategy under a second set label costs
nothing that identifies behavior: a reference keeps its authored stable ID and
definition fingerprint. Read `strategy_id`, not `strategy_set`, to know what a
control arm was. Reference metadata is copied verbatim except for pools, because
`StrategyRegistry.GetDefinition(IMutationSpendingStrategy)` resolves by
reference across every set — a faithful copy makes it irrelevant which
registration a metadata lookup finds.

#### P6.3 complete — staged emission and the closed loop (2026-09-06)

`CandidateEvaluationEmitter` emits a stage's two arms, and the loop now runs end
to end: generate, characterize, publish, emit, run both arms, verdict.

**Two single-condition runs, not one two-condition manifest.** The batch runner
derives its conditions from config strata (player counts x boards x strategy
sets), so it cannot express two conditions of identical shape differing only in
lineup. The offline analyzer, meanwhile, pairs two artifact *folders*. Two runs
satisfy both. Each arm declares the same analysis plan and combined budget,
because the analyzer refuses a verdict whose arms disagree on either.

Progression is refused, not warned about: a stage cannot be emitted until every
earlier stage has passed, and a holdout must change both board and seed.
Comparison and holdout carry a preregistered hypothesis; smoke and calibration
carry none, because the frozen gates forbid a decision-bearing plan below
comparison.

Three blockers surfaced only by trying to run it, and each is fixed:

- **The verdict could not describe a strategy swap.** `build_preregistered_verdict`
  matched its target row with `strategy_id_control == target AND
  strategy_id_treatment == target`, which holds only when the treatment leaves
  strategy identity unchanged. A swap put a different strategy in the target slot
  per arm, so the filter found no rows. The hypothesis now takes an optional
  `controlStrategyId`; omitting it preserves the original behavior exactly. This
  corrects the claim made when the evaluation shape was chosen — the paired
  machinery did *not* already support a swap.
- **The runner rejected the control arm**, because it required the hypothesis
  target to be in the resolved lineup and the control arm contains the parent.
  Either declared identity now satisfies that check.
- **Candidates did not exist for the simulator.** It runs as its own process,
  where the generated set is empty, so candidates could be designed and screened
  but never played. `--candidate-catalog` loads a serialized evaluation cast
  before any lineup is resolved.

Input schema is `fungus-toast.experiment-input.v4`. `controlStrategyId` is
omitted from JSON when unset, so every manifest written before it existed still
serializes byte-identically and keeps its recorded checksum; the result schema
stays at v7 and existing artifacts remain replayable.

**End-to-end evidence.** A real comparison stage ran through the new path: 4
generated candidates characterized and published, both 50-game arms executed from
emitted command lines on a 40x40 board, and the analyzer issued a preregistered
verdict — estimate `-0.0173` on normalized board share, 95% interval
`[-0.0564, +0.0218]` against a `0.05` margin over 50 complete pairs, `not_supported`.
The candidate does not beat its parent, which is a real result rather than a
placeholder. 122 Simulation, 651 Core, and 10 analytics tests pass, and the
experiment contract verified end to end.

#### P6.4 evaluation queue (delivered 2026-09-06)

`CandidateEvaluationQueue` is the durable, resumable state for running a field of
candidates through the ladder. It is deliberately the one mutable model in the
candidate code: it exists to record progress, and persisting it after each
recorded result is what makes a long search resumable rather than restartable.

**Breadth-first by stage.** Every candidate clears a cheap stage before any
candidate starts an expensive one, so a field is thinned at 10 games a head
rather than 200.

**Retry caps distinguish two kinds of failure, and this is the safeguard that
matters most.** An integrity failure — a crash, a parity failure, an incomplete
run — says nothing about the candidate and may be retried up to the cap. An
evidence failure is the answer to a preregistered question and stops the
candidate immediately, with no retry at any cap. Retrying a lost hypothesis until
it passes would turn the staged gates into a search for a favorable sample,
which is precisely what they exist to prevent.

**Budgets are checked before dispatch, never after.** A stage that cannot fit the
remaining game budget is never started, so the queue never leaves a half-funded
comparison behind. The runtime budget is a real gate too, projected from the
queue's own measured throughput rather than an authored guess: before any games
have run there is nothing to project from, so only an already-exhausted budget
blocks dispatch.

**Two pruning rules, both interval-based.** Futility pruning drops a candidate
whose optimistic bound is below the margin, since more games can only spend
budget. Domination pruning drops a candidate another one confidently beats — its
whole interval sits above theirs — and only within a single stage, because
intervals from different game counts are not comparable.

The 100-game ceiling holds: it is per batch, and each arm is its own batch, so a
holdout costs 200 games as two runs of 100 rather than one run of 200.

138 Simulation, 651 Core, and 10 analytics tests pass.

#### P6.5 candidate ranking (delivered 2026-09-06)

`CandidateRanking` reports four independent measures and **deliberately publishes
no composite score**. They answer different questions, and collapsing them would
let a strong-but-fragile candidate outrank a steady one, or hide that a whole
field converged on one behavior. Rows are ordered by conservative strength purely
so the list reads sensibly; the other three travel alongside.

- **Strength** is the paired estimate at the deepest measured stage, ordered by
  the interval's *lower* bound. Ranking on the point estimate would put a noisy
  candidate above a steadier one that is better on the evidence.
- **Robustness** reports the worst lower bound across measured contexts and
  whether the estimate kept its sign. One context cannot demonstrate robustness
  at all, so that is reported rather than scored — a single-context candidate is
  never mistaken for a proven one.
- **Lineage fidelity** is cosine similarity between the candidate's observed
  category profile and its parent's. It is measured against the parent's actual
  behavior, not an authored archetype label, because generated candidates carry
  placeholder catalog metadata until Phase 7 assigns bands from evidence —
  scoring against that label would be scoring against a fabrication.
- **Behavioral diversity** is mean cosine distance from the rest of the field,
  measured over the raw per-mutation build rather than the category profile.
  Categories are right for fidelity, where two builds pursuing the same end by
  different means should read as similar, but that coarseness collapses genuinely
  different candidates onto identical vectors and understates coverage.

Both comparisons use the characterization script, so candidate and parent are
observed on identical terms. Measured over a seven-candidate field, fidelity
ranged `0.836`–`1.000` and diversity `0.084`–`0.170`, with the
promote-to-front candidates correctly reading as the most drifted and most
exploratory — they moved a later goal to the opening, shifting observed Growth
from 21 levels to 14 and Cellular Resilience from 5 to 17. Three candidates
produce identical scripted builds despite different genomes, which is a true
finding rather than an artifact: a goal reorder or economy bias need not change
what gets bought under a fixed-budget script.

147 Simulation and 651 Core tests pass.

#### P6.6 promotion packet (delivered 2026-09-06)

`CandidatePromotionPacket` assembles everything a person needs to decide on a
candidate: the gene-level diff against its parent, lineage with the parent's
definition fingerprint, per-stage artifact IDs and contexts, measurements with
intervals, the four ranking measures, the observed category profile, and every
failure and retry recorded along the way. `CandidatePromotionPacketMarkdown`
renders it for review, because promotion is a judgement a person makes and a JSON
blob is a poor thing to make one from.

**The packet reports mechanical eligibility and stops there.** Phase 6's gate is
that no candidate enters a player-facing pool without review, so a packet that
recommended promotion would be quietly doing the reviewing. An empty blocker list
means "nothing mechanical is in the way", not "promote this". Blockers cover a
status short of passed, a holdout that never ran, robustness measured in fewer
than two contexts, and — importantly — an advantage that reversed sign between
contexts, which blocks eligibility even though every stage technically passed.

155 Simulation and 651 Core tests pass.

#### Phase 6 gate assessment (2026-09-06)

> A clean run can generate, evaluate, reject, and add passing candidates to the
> generated testing catalog without manual file edits, while no candidate enters
> a player-facing pool without review.

Met, with one piece outstanding:

- **Generate, reject** — done, with four typed rejection reasons plus the
  zero-game characterization gate.
- **Evaluate** — done and proven by running it: a real 50-pair comparison
  executed both arms from emitted command lines and produced a preregistered
  verdict.
- **Add to the generated catalog without manual file edits** — done. The catalog
  is published programmatically and reloaded from a generated file; nothing is
  hand-edited.
- **No candidate enters a player-facing pool without review** — done
  structurally, not procedurally: every generated entry carries
  `StrategyPool.None`, so pool-filtered selection cannot return one even by
  accident.

**Closed (2026-09-06): the unattended driver.** `CandidateEvaluationDriver` runs a
queue to completion — emit a stage, run both arms, analyze the pair, record the
result, prune, persist, repeat. Arm execution and analysis are injected;
`ProcessCandidateExecution` supplies the process-backed defaults that shell out
to the simulator and the Python analyzer. Arms stay separate processes on
purpose: it is the isolation the replay contract relies on, and it forces a
candidate to arrive through the catalog file rather than surviving in memory, so
an unattended run exercises exactly the path a hand-run experiment does.

The outcome-to-result mapping is where the frozen gates live. A failed arm or
failed analysis is always an integrity failure, because it says nothing about the
candidate. Comparison and holdout pass or fail on their preregistered verdict,
and a *missing* verdict at those stages is an integrity failure rather than a
loss. Calibration has no verdict and exists to catch a clear regression, so it
fails only when the whole interval sits below the negative margin. Smoke checks
integrity alone and never judges a candidate.

Building it surfaced a real defect in P6.4's pruning: futility was evaluated
against whatever interval was most recent, including smoke's. Smoke runs a
handful of games per arm, so its interval is noise — the staged gates say never
to promote on smoke, and eliminating on it is the same mistake inverted. Pruning
now ignores any measurement shallower than calibration, the first stage with
enough games to be indicative. It also caught that a paired summary holds one row
for the swapped slot and one for the unchanged opponent, so the reader selects
the row where the strategies actually differ rather than whichever came first.

Phase 6 is therefore complete: a clean run generates, evaluates, rejects, prunes,
and records candidates without steering, and no candidate can reach a
player-facing pool because every generated entry carries `StrategyPool.None`.
166 Simulation and 651 Core tests pass.

### Phase 7 — Calibrate contextual performance bands

- **P7.1:** Freeze representative calibration and holdout matrices across map
  classes, geometries, player counts, seeds, slots, opponent pools, and system
  toggles. **Complete (2026-09-07)** — see the contract below.
- **P7.2:** Measure stable reference strategies and quantify slot, geometry,
  lineup, mycovariant, nutrient, and adaptation effects.
- **P7.3:** Choose empirical thresholds and material-context rules; version the
  classifier and reference corpus.
- **P7.4:** Classify every active strategy overall and by material context;
  flag insufficient evidence rather than guessing.
- **P7.5:** Add regression alerts for band movement, robustness loss, archetype
  drift, parity failures, and abnormal fallback/decision-failure rates.
- **Gate:** All retained player-facing strategies have reproducible measured
  metadata or an explicit evidence-gap status.

#### P7.1 frozen calibration matrix (delivered 2026-09-07)

Two pieces. `ContextTaxonomy` implements the P3.4 class table, which until now
existed only as documentation: player-count, board-scale, aspect, geometry, and
start-regime classes, all derived from resolved values rather than matched
against a preset list that would go stale the first time someone ran an unusual
board. Thresholds are pinned to the anchors the plan names (80x80 tops the small
class, 160x160 tops medium) and are deliberately not tunable — redrawing class
boundaries after seeing results is how a contextual specialist gets relabelled a
generalist.

`CalibrationMatrix` is the frozen context set, split into a calibration half that
thresholds are fitted on and a holdout half reserved to check them. Freezing it
before measurement is the entire point: choosing contexts after seeing results
lets a strategy be called strong by picking the boards it likes, and choosing
thresholds afterwards does the same thing one step later.

Three validation rules carry the weight:

- **A context ID must start with its own derived rollup key**, so a matrix cannot
  claim coverage it does not have. An entry calling itself a duel while running
  four players is refused.
- **Holdout contexts must differ from every calibration context in board, player
  count, and seed.** A holdout that reuses a calibration board is not independent
  evidence; it re-measures what the thresholds were fitted on and reports it as
  confirmation.
- **The calibration half must vary at least two player-count classes and two
  board-scale classes.** The taxonomy says to begin with those two axes, and a
  matrix varying neither can only produce a global label with no evidence it
  generalizes — the failure this initiative exists to stop.

**The reference panel is the 19 `Proven` strategies solo play already draws
from** (approved 2026-09-07), which makes the yardstick the opponents players
actually meet rather than an invented set.

Sizing a panel that large exposed two problems the matrix now solves.

**One condition does not give a panel enough games.** Nineteen strategies sharing
100 two-player games get ten games each, which cannot support a band.
`minimumGamesPerStrategy` is therefore checked against the panel size before
anything runs, with the error naming the repeats the context actually needs, so
an under-powered matrix fails in milliseconds instead of after hours of
simulation. A context may also declare `repeats` — several conditions at
consecutive seed blocks — which stays useful for very long contexts because each
condition is a separate artifact and a failure then costs one block rather than
all of them.

**The 100-game ceiling now follows the evidence stage** (approved 2026-09-07).
That limit is a promotion safeguard: the staged gates exist so a candidate cannot
buy significance with an ever-larger batch, which is why P3.3 caps them. An
exploratory run cannot carry a hypothesis and cannot emit a verdict, so nothing
advances on its evidence and the reason for the cap does not apply. Exploratory
conditions may now run up to 1,000 games; smoke, calibration, comparison, and
holdout keep their exact frozen counts, and a regression test pins that. No
schema bump: the change only widens an accepted range, an older binary rejects an
over-sized manifest rather than misreading it, and a newer binary treats every
existing manifest identically.

**Even exposure is not the same as fair opposition.** `StratifiedCycle` gives
perfectly even appearance counts, but it slides a window over a fixed ordering,
so in two-player games each strategy would only ever face its two neighbours —
a rating that measures particular matchups and reports them as general strength.
`CoverageBalanced` picks one strategy per theme and over-samples rare themes for
the same reason. The matrix therefore requires `RandomUnique` whenever the panel
is larger than a lineup, trading exact exposure counts for an unbiased
cross-section of the field. Seed spans are also checked for overlap, since
per-game seeds run from a condition's base and two overlapping contexts would
replay the same games.

The checked-in `calibration-matrix.v1.example.json` freezes five calibration
contexts (duel and small-table across small, medium, and large boards) and two
unseen holdout contexts that also change aspect: 1,600 games across 7
conditions, at least 30 games per strategy in every context, systems off so a
band reflects strategy strength rather than draft or nutrient luck.
221 Simulation and 651 Core tests pass, and the experiment contract still
verifies end to end.

### Phase 8 — Define and fill the target roster matrix

- **P8.1:** Cluster or compare existing behavior to find real archetype coverage,
  redundancy, counters, and gaps; do not trust authored labels alone.
- **P8.2:** Define target counts by difficulty, archetype, role, and contextual
  niche, with a maximum roster size justified by player value and maintenance
  cost.
- **P8.3:** Retire redundant/opaque strategies, complete the standardized-name
  cutover, migrate campaign references, and record replacements.
- **P8.4:** Search for candidates in each gap, with archetype constraints and
  diversity penalties in addition to performance targets.
- **P8.5:** Review promotion packets and promote only candidates that pass
  calibration, holdout, identity, and comprehensibility gates.
- **Gate:** The roster satisfies the approved matrix without relying on near
  duplicates or mislabeled contextual specialists.

### Phase 9 — Integrate campaign and single-player pools

- **P9.1:** Map measured strategies to campaign difficulty progression, board
  contexts, adaptations, and intended teaching/boss roles.
- **P9.2:** Rebuild single-player sampling pools from measured metadata and
  explicit diversity rules.
- **P9.3:** Run artifact-backed campaign validation with the safe proxy and any
  required human-model variants; compare every level to its intended band.
- **P9.4:** Run single-player pool validation for lineup diversity, context
  robustness, and repeated-strategy frequency.
- **P9.5:** Validate preset references, aliases, tooltips, Unity compile, and
  affected player-facing flows.
- **Gate:** Campaign and single-player artifacts support every assignment, and
  Unity manual checks pass.

### Phase 10 — Operationalize maintenance

- **P10.1:** Update `AI_STRATEGY_AUTHORING.md` to the final architecture and
  archive superseded instructions rather than maintaining two workflows.
- **P10.2:** Document candidate authoring, experiment replay, promotion,
  retirement, regression response, and classifier-version migration.
- **P10.3:** Add a periodic recalibration command/checklist with explicit compute
  budget and triggers for balance/content changes.
- **P10.4:** Add a concise roster health report: coverage, contextual bands,
  evidence age, regressions, gaps, and pending promotion packets.
- **Gate:** A fresh session can reproduce the current classifications, add or
  evaluate a candidate, and understand the next action from canonical docs.

## 9. Validation Ladder

Use the smallest applicable gate for each slice, then escalate:

1. static manifest/definition validation
2. focused Core unit and characterization tests
3. canonical Core tests
4. Core and Simulation builds
5. deterministic tiny simulation and replay comparison
6. artifact-backed calibration/comparison analysis
7. independent holdout analysis
8. affected campaign and single-player validation
9. Core DLL/PDB refresh when Core changes affect Unity
10. Jake's manual Unity compile and affected-flow checks

Any parity invariant failure, manifest contamination, unresolved strategy ID,
or nondeterministic replay blocks statistical interpretation.

## 10. Durable Work Products

- This document: architecture, phases, decisions, and initiative-level gates.
- `docs/WORKLOG.md`: only the current slice, checkpoint, blockers, and exact next
  actions.
- Versioned experiment manifests and schemas: reproducible run inputs.
- Artifact directories: raw outcomes, resolved manifests, analysis outputs, and
  promotion packets. Scratch runs remain under `TEMP/`.
- Architecture decision record created in Phase 4, linked from this document.
- Final roster matrix and measured catalog produced in Phases 7–8.
- Daily memory: session checkpoint only; it must not replace repository docs.

At each session end, update the worklog with completed task IDs, commit/artifact
identifiers, decisions, failed attempts, manual checks, and the next unblocked
task. Update this document only when the plan, architecture, or durable decision
changes.

## 11. Recommended First Implementation Sessions

1. **Session A — Audit inventory:** P0.2–P0.4 and P1.1–P1.3. Deliver the
   vocabulary, compatibility inventory, decision-surface table, and roster
   duplication report. No behavior changes.
2. **Session B — Execution and experiment audit:** P1.4–P1.6. Deliver invocation
   characterization tests where needed, pipeline gap matrix, and Phase 2 slice
   ordering. No balance changes.
3. **Session C — Manifest core:** P2.1–P2.2. Introduce model, validator, resolved
   output, fingerprints, and replay for a minimal scenario.
4. **Session D — Full isolation controls:** P2.3–P2.4. Close metadata gaps and
   add treatment/control diff enforcement.
5. **Session E — Resume and replay proof:** P2.5–P2.6. Add resumability and prove
   deterministic equality before architecture experimentation.

This order deliberately makes later architecture bakeoffs and balance results
trustworthy before they become expensive.

## 12. Product Decisions

Approved on 2026-09-01:

1. **Difficulty philosophy:** Use coherent strategies across difficulty levels.
   Training opponents may have explicit, legible teaching mistakes such as too
   much Growth without Resilience or too much Fungicide without enough Growth.
2. **Promotion authority:** Automation should have maximum safe autonomy and may
   add passing candidates directly to a generated testing catalog. Player-facing
   campaign and single-player promotion retains an evidence/review gate.
3. **Adaptations:** Assign complementary Adaptations up front. AI does not draft
   Adaptations during play.
4. **Starting positions:** Test and validate candidate pools by board and player
   count, then assign normal AI positions randomly. Campaign levels may curate
   easier crustward or harder centerward offsets and may lock special board
   geometry and positions.
5. **Naming and summaries (revised after adversarial review):** Add standardized
   stable IDs and fingerprints without resetting existing machine-facing names.
   Human-friendly display names and intent summaries evolve independently.
6. **Compute envelope:** No individual simulation batch may exceed 100 games.
   Candidate evaluation should use staged, prunable batches rather than
   thousand-game runs.
7. **Human models:** Retain the current safe proxy as the campaign safety anchor
   and add at least one less-optimized human model before final assignments.
8. **Contextual specialists:** Distinctive volatile strategies may remain as
   `Spice` opponents when their weaknesses are measured, surfaced, and excluded
   from misleading contexts.

9. **Adaptation quantity and resolution:** Each strategy owns ranked compatible
   Adaptation options. Single-player and campaign scenario rules decide how many
   to grant and may force exact IDs when required by an authored scenario.
10. **Summary visibility:** Author display names and intent summaries as
    player-safe text from the start. Expose them through existing campaign and
    single-player opponent-preview surfaces when the new catalog is integrated;
    do not create an otherwise unnecessary parallel UI.
11. **Search breadth:** Begin with at most 20 new candidates per autonomous
    sweep. Use approximately 3–5 smoke games, 20 screening games, 50 comparison
    games, and at most 100 holdout games per condition, pruning at every gate.

## 13. Initiative Completion Criteria

- Explicit deterministic Core boundaries cover every agreed AI decision surface.
- Strategy definitions are substantially simpler to author and cannot silently
  drift from their catalog metadata.
- Every experimental variable is explicit, validated, fingerprinted, replayable,
  and present in resolved artifacts.
- Candidate evaluation is resumable, bounded, staged, and capable of automatic
  rejection and recommendation without routine hand editing.
- Every player-facing AI has versioned overall and material-context performance
  metadata with uncertainty and robustness evidence.
- The approved roster matrix covers the required difficulty/archetype/role
  combinations without unnecessary duplicates.
- Campaign and single-player assignments are supported by artifact-backed
  validation, and affected Unity flows have passed manual checks.
- Authoring, promotion, regression, retirement, and recalibration workflows are
  documented and reproducible by a fresh session.

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

- **State:** Phase 2 experiment infrastructure in progress; P2-A complete
- **Implementation started:** Yes
- **Current gate:** Implement P2.4 manifest diffing and treatment/control
  contamination checks; P2.1–P2.3 are complete.
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
  behavior is characterized and definitions migrate. Existing names may be
  reset; compatibility lasts only as long as needed to migrate every reference
  safely.

The spike must compare this composition model against a minimally cleaned-up
`ParameterizedSpendingStrategy`. Replacement is justified only if composition
improves at least two of: independent control, authoring cost, testability,
decision capability, traceability, or automated searchability without a
material regression in determinism or runtime cost.

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

## 7. Measurement and Classification Model

### Primary strength measures

- **Parity-normalized final board share:** player living-cell share divided by
  equal expected share (`player_count * player_living / total_living`). `1.0`
  is parity for the active player count.
- **Win-rate surplus:** observed win rate minus the equal-win expectation,
  including an explicit tie policy.
- **Average rank:** tie-aware final placement, normalized when comparisons span
  different player counts.
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

Exact thresholds, interval method, tie policy, minimum samples, and materiality
cutoffs are Phase 3 decisions informed by baseline data.

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
  and CLI references, and the exact migration work required to reset names.
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

- **P2.1 — complete:** Defined and tested
  `fungus-toast.experiment-input.v1`, including the existing CLI-to-condition
  translation, strict JSON unknown/missing-field handling, semantic validation,
  and the hard 100-game per-condition ceiling. The checked-in example is
  `FungusToast.Simulation/Examples/experiment-input.v1.example.json`.
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
- **P2.4:** Add treatment/control manifest diffing and a contamination check that
  rejects unintended differences.
- **P2.5:** Add resumable condition execution, completion markers, failure
  records, and idempotent analysis regeneration.
- **P2.6:** Create small contract fixtures proving replay equality and selective
  resume behavior.
- **Gate:** Replaying a resolved manifest produces the same lineup, conditions,
  seed schedule, and deterministic outcomes; artifacts explain every input.

### Phase 3 — Add outcome metrics and statistical gates

- **P3.1:** Export total living share inputs, tie-aware rank, starting slot and
  coordinates, adaptations, and structured condition identifiers.
- **P3.2:** Implement normalized board share, win surplus, normalized rank,
  intervals, effect sizes, and robustness summaries in offline analytics.
- **P3.3:** Define smoke, calibration, comparison, and holdout sample gates with
  early rejection for invalid/parity-failing candidates—not early promotion.
- **P3.4:** Define contextual map/player-count taxonomy from actual supported
  boards and measured sensitivity.
- **P3.5:** Generate a baseline report for the frozen reference corpus and lock
  the first analysis version.
- **Gate:** The same artifacts always yield the same classification report, and
  weak evidence cannot pass a promotion gate.

### Phase 4 — Architecture spike and decision

- **P4.1:** Write characterization tests around current mutation spending,
  mycovariant choice, surge timing/banking, exclusions, tendril direction, and
  starting-offset behavior.
- **P4.2:** Prototype the composed controller/policy boundary for two contrasting
  strategies without migrating the roster.
- **P4.3:** Prototype the smallest viable cleanup of the existing parameterized
  design as the comparison.
- **P4.4:** Compare behavior parity, code/config size, independent overrides,
  trace quality, authoring steps, test complexity, and search-space encoding.
- **P4.5:** Record an architecture decision: adopt composition, retain a revised
  parameterized design, or use a justified hybrid. Include rejected options.
- **Gate:** Evidence supports the chosen boundary, and an incremental migration
  with preserved names and deterministic behavior is demonstrated.

### Phase 5 — Implement the AI foundation incrementally

- **P5.1:** Add the unified strategy definition/registry and standardized identity
  model; co-locate behavior configuration, complementary Adaptation metadata,
  authored intent summary, archetypes, and lifecycle metadata.
- **P5.2:** Add the single AI controller entry point, typed contexts/actions,
  legality boundary, and structured decision trace.
- **P5.3:** Route mutation acquisition through the controller using a legacy
  adapter; prove Core/Simulation/Unity parity before changing policy behavior.
- **P5.4:** Extract surge activation and banking as an independently replaceable
  policy while preserving current behavior.
- **P5.5:** Route mycovariant drafting through a typed policy instead of a
  concrete strategy downcast; preserve campaign-specific rules explicitly.
- **P5.6:** Resolve complementary starting Adaptations without an AI draft, and
  implement board/player-count position pools plus curated campaign
  pool/coordinate/geometry overrides as scenario controls.
- **P5.7:** Add reactive board-state policy primitives only for demonstrated
  decisions; avoid a catch-all scoring framework.
- **P5.8:** Migrate a small reference set, then each roster family in isolated
  slices. Reset names using the standardized IDs and migrate all verified
  serialized, campaign, CLI, test, and UI references in a controlled cutover.
- **Gate:** All player-facing strategies use one deterministic boundary, traces
  cover every decision, and legacy behavior or intentional deltas are proven.

### Phase 6 — Build autonomous candidate evaluation

- **P6.1:** Define a bounded, serializable candidate genome from safe policy
  choices and tunables. Every dimension must be independently switchable.
- **P6.2:** Add deterministic candidate generation, deduplication, fingerprints,
  lineage, and invalid-candidate rejection.
- **P6.3:** Implement staged evaluation: static validation, unit/characterization
  checks, tiny smoke, calibration comparison, then unseen holdout conditions.
- **P6.4:** Add dominated-candidate pruning, resource/time budgets, retry caps,
  resumable queues, and an enforced maximum of 100 games in any one batch.
- **P6.5:** Rank strength and robustness separately from archetype fidelity and
  behavioral diversity.
- **P6.6:** Produce a promotion packet containing definition diff, lineage,
  manifests, metrics, intervals, context bands, traces, failures, and holdouts.
- **Gate:** A clean run can generate, evaluate, reject, and add passing candidates
  to the generated testing catalog without manual file edits, while no candidate
  enters a player-facing pool without review.

### Phase 7 — Calibrate contextual performance bands

- **P7.1:** Freeze representative calibration and holdout matrices across map
  classes, geometries, player counts, seeds, slots, opponent pools, and system
  toggles.
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
5. **Naming and summaries:** Resetting all current AI names is acceptable. The
   overhaul will introduce standardized stable IDs plus human-friendly display
   names and intent summaries using the model in section 5.
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

- One deterministic AI boundary covers every agreed decision surface in Core.
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

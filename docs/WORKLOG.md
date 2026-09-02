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

1. Execute Phase 0 without behavior changes: lock terminology, name/reference
   migration obligations, and the small architecture-parity reference corpus.
2. Include standardized machine IDs, human display names, intent summaries,
   explicit Training mistakes, complementary starting Adaptations, and
   board/player-count position pools in the Phase 0 contracts.
3. Execute Phase 1 in two audit slices: strategy/roster representation first,
   then invocation ownership and experiment/analytics gaps.
4. Do not begin architecture migration or balance tuning until the audit and
   reproducible experiment-contract foundation are complete.

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

# Fungus Toast - Architecture Overview

> **Related Documentation**: For canonical gameplay verbs and state-transition terminology, see [GAMEPLAY_TERMINOLOGY.md](GAMEPLAY_TERMINOLOGY.md). For simulation commands and automation workflows, see [SIMULATION_HELPER.md](SIMULATION_HELPER.md). For Unity UI service and presentation patterns, see [UI_ARCHITECTURE_HELPER.md](UI_ARCHITECTURE_HELPER.md). For the full documentation hierarchy, see [README.md](README.md).

## Purpose

This document is the technical architecture overview for Fungus Toast. Use it to understand layer ownership, deterministic-core rules, game flow, and the event-driven patterns that keep Core, Simulation, and Unity aligned.

This document is not the source of truth for simulation commands, UI styling, or mutation authoring workflow. Those topics live in their dedicated helper documents.

## Repository Architecture

Fungus Toast is split into three primary runtime layers:

- **FungusToast.Core** - deterministic game rules, board state, mutations, events, metrics, and shared gameplay logic
- **FungusToast.Simulation** - headless execution, AI-driven balance runs, batch exports, and analytics-facing tracking
- **FungusToast.Unity** - presentation, interaction flow, animation, and UI orchestration

### Ownership Rules

- Keep gameplay rules deterministic and Unity-free inside `FungusToast.Core`.
- Treat `FungusToast.Core` as the source of truth for board state transitions, mutation effects, and round sequencing.
- Use `FungusToast.Simulation` to observe and export gameplay behavior, not to redefine gameplay rules.
- Keep `FungusToast.Unity` focused on view/controller concerns, player input, and presentation-specific timing.

## Deterministic Core Boundary

The Core project exists so the same gameplay rules can drive both simulation and the Unity front end.

### Why this boundary matters

- Simulation balance runs must execute the same gameplay rules as the playable build.
- Core-only rules are easier to test with deterministic inputs and narrow fixtures.
- Shared event semantics let analytics, replay, and UI respond to the same state transitions.
- Unity-facing code can evolve its presentation without changing gameplay correctness.

### Practical implications

- Do not move gameplay resolution into MonoBehaviours or Unity-only utility classes.
- Prefer new game-state hooks through Core events and observer interfaces when behavior must be visible in both Simulation and Unity.
- Keep tunable gameplay values in balance/configuration classes rather than scattering literals across layers.

## Round Flow and Phase Structure

Fungus Toast runs as a round-based game with a stable phase sequence.

The main Core entry points are in `FungusToast.Core.Phases.TurnEngine`:

- `AssignMutationPoints(...)`
- `RunGrowthPhase(...)`
- `RunDecayPhase(...)`

### Round Structure

1. **Mutation Phase**
   `TurnEngine.AssignMutationPoints(...)` fires `GameBoard.OnMutationPhaseStart()` and then lets each player assign points, trigger mutation-phase effects, and execute spending strategy behavior.
2. **Growth Phase**
   `TurnEngine.RunGrowthPhase(...)` fires `GameBoard.OnPreGrowthPhase()`, executes `GrowthPhaseProcessor` cycles, then completes the phase through `GameBoard.OnPostGrowthPhase()` and `GameBoard.OnPostGrowthPhaseCompleted()`.
3. **Decay Phase**
   `TurnEngine.RunDecayPhase(...)` delegates to `DeathEngine.ExecuteDeathCycle(...)` and then fires `GameBoard.OnPostDecayPhase()` for downstream consumers such as UI/log aggregation.
4. **Optional Mycovariant Draft Phase**
   On configured rounds, players draft mycovariants before normal round flow resumes.
5. **Round End**
   Endgame conditions are checked before the next round begins.

### Phase Notes

- Growth uses multiple cycles per round, configured through `GameBalance.TotalGrowthCycles`.
- Each growth cycle is executed by `GrowthEngine.ExecuteGrowthCycle(...)`, which fires `GameBoard.OnPreGrowthCycle()`, attempts player growth in living-cell order, increments the growth-cycle counter, ages living and toxin cells, and expires toxin tiles.
- Draft timing and size are controlled through mycovariant balance/configuration values.
- Endgame evaluation should consume finalized round state rather than partially resolved phase state.

## Runtime Orchestration Path

Several collaborating classes define the runtime flow in Core:

- `TurnEngine` owns high-level mutation, growth, and decay phase sequencing.
- `GrowthPhaseProcessor` and `GrowthEngine` execute multi-cycle board expansion.
- `DeathEngine` resolves decay, death, and decay-phase caching.
- `MutationEffectCoordinator` centralizes mutation-trigger ordering for phase hooks and special events.
- `GameBoard` owns authoritative state plus the event surface that other systems subscribe to.

This split is intentional: phase sequencing stays in one place, while mutation-specific logic remains in category processors coordinated through `MutationEffectCoordinator`.

## Event-Driven Runtime Pattern

Core gameplay state changes are exposed through events and observer hooks so multiple consumers can stay in sync.

### Event model

- **Phase hooks on `GameBoard`** expose round-level orchestration points such as `MutationPhaseStart`, `PreGrowthPhase`, `PreGrowthCycle`, `PostGrowthPhase`, `PostGrowthPhaseCompleted`, `DecayPhase`, and `PostDecayPhase`.
- **State-transition events on `GameBoard`** expose board-level actions such as `CellColonized`, `CellReclaimed`, `CellInfested`, `CellToxified`, `CellPoisoned`, `CellOvergrown`, `CellDeath`, `ToxinPlaced`, and `ToxinExpired`.
- **Attempt lifecycle events** such as `BeforeGrowthAttempt` and `AfterGrowthAttempt` allow growth to be intercepted or annotated without moving resolution out of Core.
- **Observers** record simulation-facing metrics through `ISimulationObserver` while board events continue to serve UI and other consumers.

### Coordination path

- `TurnEngine` triggers board phase hooks.
- `MutationEffectCoordinator` executes mutation-category handlers in a defined order for pre-growth, post-growth, decay-phase, mutation-phase, and special-event processing.
- `DeathEngine` and growth/death helpers route tile-state changes back through `GameBoard` methods so event firing stays consistent.
- `AnalyticsEventSubscriber` wires selected `GameBoard` events into `ISimulationObserver` recording.

### Why this pattern matters

- Simulation and Unity can react to the same state transitions without duplicating game logic.
- Metrics and analytics can accumulate effect counts without reaching into view code.
- New mechanics can expose shared hooks instead of creating layer-specific side channels.

### Cross-layer tracking rule

When a new mechanic needs to be visible outside Core, prefer a Core-side event or `ISimulationObserver` extension point over Unity-only tracking.

In practice this usually means one or both of the following:

- Add or reuse a `GameBoard` event when Unity, replay, or generalized listeners need to react to the state transition.
- Add an `ISimulationObserver` method when Simulation exports or analytics need an explicit per-effect counter.

## Unity Integration Patterns

Unity should consume Core state through stable facades and focused services rather than direct singleton sprawl.

### Established patterns

- Use `GameUIManager` as the main UI-facing facade instead of widespread direct `GameManager.Instance` access.
- When `GameManager` grows, extract cohesive logic into lightweight services with explicit dependencies.
- Reuse the shared tooltip system and pooled UI-element patterns documented in [UI_ARCHITECTURE_HELPER.md](UI_ARCHITECTURE_HELPER.md).

### Scope boundary

This document defines why those patterns exist. The exact implementation recipes remain in [UI_ARCHITECTURE_HELPER.md](UI_ARCHITECTURE_HELPER.md).

## State and Configuration Model

### State ownership

- **GameBoard** owns authoritative board state.
- **Player** state owns mutation levels, point totals, surge activation, AI strategy execution, and player-scoped mutation-change notifications such as `MutationsChanged`.
- **Round context** tracks round number and growth-cycle context and is passed through growth/decay processing.
- **Metrics/observers** track counts and summaries for later analysis.

### Board responsibilities

`GameBoard` is more than a tile container. It also owns:

- the authoritative occupied-tile index
- current round and growth-cycle counters
- cached decay-phase context used by mutation processors
- chemobeacon and other board-scoped transient state
- event dispatch for cell, toxin, nutrient, surge, and phase-level transitions

### Configuration ownership

- Gameplay tuning belongs in Core balance/configuration classes.
- UI timing and presentation tuning belongs in Unity-side configuration or serialized fields.
- Documentation about specific balance levers belongs in `GAME_BALANCE_CONSTANTS.md`, not here.

## Code References

### Core engine references

- `FungusToast.Core.Phases.TurnEngine`
- `FungusToast.Core.Phases.GrowthPhaseProcessor`
- `FungusToast.Core.Growth.GrowthEngine`
- `FungusToast.Core.Death.DeathEngine`
- `FungusToast.Core.Phases.MutationEffectCoordinator`
- `FungusToast.Core.Board.GameBoard`
- `FungusToast.Core.Events.AnalyticsEventSubscriber`
- `FungusToast.Core.Metrics.ISimulationObserver`

### Unity-side references

- `FungusToast.Unity.Phases.GrowthPhaseRunner`
- `FungusToast.Unity.Phases.DecayPhaseRunner`
- `FungusToast.Unity.UI.MycovariantDraftController`

### Related docs for adjacent concerns

- [GAMEPLAY_TERMINOLOGY.md](GAMEPLAY_TERMINOLOGY.md) for canonical gameplay verbs and state names
- [UI_ARCHITECTURE_HELPER.md](UI_ARCHITECTURE_HELPER.md) for Unity UI patterns and service extraction examples
- [SIMULATION_HELPER.md](SIMULATION_HELPER.md) for commands, automation-safe runs, and output conventions
- [second-level/MUTATION_PREREQUISITE_GUIDELINES.md](second-level/MUTATION_PREREQUISITE_GUIDELINES.md) for mutation-category philosophy and prerequisite design guidance

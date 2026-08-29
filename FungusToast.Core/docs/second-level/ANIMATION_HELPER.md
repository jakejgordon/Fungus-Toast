# Animation Helper (Concise)

This file lists WHEN gameplay animations run during normal play (excluding mycovariant draft UI), the METHOD that triggers each batch, and the PRIMARY CONSTANT that determines total batch duration (or the value you pass in).

Any new gameplay animation entry point should be added to this file when introduced so it stays discoverable for later reuse, tuning, and sequencing work.

## Core (Normal Round Flow)

| Sequence Point (Order of Appearance) | What Triggers It (public entry or immediate caller) | Per‑Tile Coroutine | Duration Input / Governing Constant |
|-------------------------------------|------------------------------------------------------|--------------------|--------------------------------------|
| New growth cells appear after each board render (during growth cycles & post-growth re-render) | Default: `GridVisualizer.RenderBoard()` → `StartFadeInAnimations()`; orthogonal hyphal outgrowth: `GameBoard.HyphalGrowthVisualized` → `GridVisualizer.RenderBoard()` → `StartDirectionalGrowthAnimations()` | `FadeInCell` or `PlayDirectionalGrowth` | Default: `CellGrowthFadeInDurationSeconds` (fade) + bright ownership-neutral flash (`NewGrowthFlashDurationSeconds`); directional: `HyphalGrowthSourceStretchDurationSeconds` + `HyphalGrowthTravelDurationSeconds` + `HyphalGrowthSettleDurationSeconds`. Non-initial newly created cells use the normal mold sprite at 50% of their normal state scale, gaining 10 percentage points per subsequent growth cycle from `GrowthCycleAge` until normal size; this continues after the next growth phase clears the transient animation flag. |
| Creeping Mold move resolves after board render for Creeping Mold destinations | `GameBoard.CreepingMoldMove` → `GridVisualizer.RenderBoard()` → `PlayCreepingMoldAnimationBatch()` | `PlayCreepingMoldHopBatch` | `CreepingMoldSourceEmphasisDurationSeconds` + `CreepingMoldHopDurationSeconds` + `CreepingMoldLandingDurationSeconds` |
| Toxin placed (any phase when toxin tiles are added) | `RenderBoard()` → `StartToxinDropAnimations()` | Capped `ToxinLaunchBatch` per placing colony, with a starting-cell source ping | `ToxinLaunchVisibleProjectileCapPerPlayer` (8) + `ToxinLaunchVolleyDurationSeconds`; all unrepresented targets resolve immediately and time-lapse suppresses the batch |
| Toxin expires during growth-start cleanup | `GameBoard.ToxinExpired` → `GridVisualizer.HandleToxinExpired()` → next `RenderBoard()` → `StartPendingToxinExpiryAnimations()` | `ToxinExpiryDissolveAnimation` | `ToxinExpiryDissolveDurationSeconds` |
| Cell marked dying (mainly after decay phase) | `TriggerDeathAnimation(int)` or `RenderBoard()` → `StartDeathAnimations()` | `DeathAnimation` | `CellDeathAnimationDurationSeconds` (first 15% is flash) |
| Passive alive-mold idle drift | `GridVisualizer.LateUpdate()` → `UpdateMoldIdleVisuals()` | None (per-frame tilemap transform) | `MoldIdleDriftAmplitudeXCellFraction`, `MoldIdleDriftAmplitudeYCellFraction`, `MoldIdleDriftPrimarySpeed`, `MoldIdleDriftSecondarySpeed` |
| Post‑Growth: Regenerative Hyphae reclaim batch | `PlayRegenerativeHyphaeReclaimBatch(tileIds, simplified, scaleMult [, explicitTotalSeconds])` | `RegenerativeHyphaeReclaimFull` / `RegenerativeHyphaeReclaimLite` | If provided: `explicitTotalSeconds`; else base sum `RegenerativeHyphaeTotalBaseDurationSeconds` scaled by `postGrowthPhaseDurationMultiplier` & `regenerativeHyphaeDurationMultiplier` |
| Post‑Growth: Directed vector surge presentation | `GameBoard.DirectedVectorSurge` → `PostGrowthVisualSequence` → `PlayDirectedVectorSurgePresentation(playerId, originTileId, tileIds)` | `RunDirectedVectorSurgePresentation` with chunk pulses + floating toast | `HyphalVectoringOriginPulseDurationSeconds` + chunk cadence (`HyphalVectoringChunkPulseDurationSeconds`, `HyphalVectoringChunkStaggerSeconds`) + `HyphalVectoringToastDurationSeconds` |
| Post‑Growth: Resistance pulses (Bastion / HRT spread) | `PlayResistancePulseBatchScaled(tileIds, scaleMultiplier)` | `BastionResistantPulseAnimation` | `MycelialBastionPulseDurationSeconds` (or `_timingContext.ResistancePulseTotal` if set) |
| Starting tile ping highlight (occasionally shown) | `TriggerStartingTilePing(playerId)` / hover path `StartStartingTileHoverPing(playerId)` → `RunStartingTilePing()` / `RunLoopingStartingTilePing()` | (helper in `RingHighlightHelper`) | `StartingTilePingDurationSeconds` with `StartingTilePing*` radius, fade, and band-color constants |
| Starting spores establish at game entry | `GameManager.PlayGameplayEntryFlow()` → `GridVisualizer.PlayStartingSporeArrivalAnimation()` | `StartingSporeArrivalAnimator.AnimateSingleArrival()` | Surgical Inoculation arc constants; each mold sprite takes a non-spinning parabolic path from the closest board edge |
| Growth-cycle progress number advances | `GrowthPhaseRunner.RunNextCycle()` → `UI_PhaseProgressTracker.AdvanceToNextGrowthCycle()` | `UI_PhaseProgressTracker.UpdatePulse()` (unscaled, non-blocking) | `GrowthCycleProgressPulseDurationSeconds` + `GrowthCycleProgressPulsePeakScaleMultiplier` |

## Notes
- Phase timing diagnostics are compiled out by default; define `FT_PHASE_TIMING` in Core and Unity to re-enable them.
- Reclaim FULL sub‑phases (rise / hold / swap / settle) proportions come from base constants (`RegenerativeHyphae*DurationSeconds` + `RegenerativeHyphaeHoldBaseSeconds`). When you pass `explicitTotalSeconds`, those portions are applied linearly.
- Lite reclaim uses only rise + swap (same proportional logic limited to those two components).
- Timing context (`SetPostGrowthTiming`) can override reclaim (rise / hold / swap / settle / lite total) and resistance pulse totals directly.
- The passive alive-mold drift only applies to eligible living mold tiles with no overlay present, and it suspends while higher-priority board animations or player-hover emphasis are active.
- Source-aware directional normal growth currently applies only to standard orthogonal `HyphalOutgrowth` placements. Any newly grown tile without a buffered source/destination pair falls back to the existing fade-in behavior.
- When adding a new reusable animation or board-FX entry point, register it here with its trigger, main method, and governing constants.
- Click-driven UI micro-interactions (mutation-node attention pulses, panel-open shimmer, etc.) are not part of round-flow sequencing and live in [../UI_ARCHITECTURE_HELPER.md](../UI_ARCHITECTURE_HELPER.md) under **Attention Pulses**, not in the tables above.

## Minimal Mycovariant (Active Ability) Animations
Use the same GridVisualizer entry points; only the triggering context differs.
- Mycelial Bastion: calls `BastionResistantPulseAnimation` (batch via `PlayResistancePulseBatchScaled`) → duration: `MycelialBastionPulseDurationSeconds`.
- Surgical Inoculation: `SurgicalInoculationArcAnimation` (duration: `SurgicalInoculationArcDurationSeconds`). If the arc cannot be staged, it falls back to `ResistantDropAnimation` (duration: `SurgicalInoculationDropDurationSeconds`).
- Perispore Crown: `PlayPerisporeCrownToxinVolleyAnimation` → `RunPerisporeCrownArcVolley`; its AI delays, arc stagger/duration, and post-volley hold are each 60% of the shared Jetting Mycelium baselines.
- Regenerative Hyphae reclaim already covered (triggered during post-growth when tiles reclaimed by effect logic).
- To uniformly slow a new active effect: add a single total duration constant to `UIEffectConstants` and multiply internal sub‑phase portions.

_End of concise helper._

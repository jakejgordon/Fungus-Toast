# Animation Helper (Concise)

This file lists WHEN gameplay animations run during normal play (excluding mycovariant draft UI), the METHOD that triggers each batch, and the PRIMARY CONSTANT that determines total batch duration (or the value you pass in).

Any new gameplay animation entry point should be added to this file when introduced so it stays discoverable for later reuse, tuning, and sequencing work.

## Core (Normal Round Flow)

| Sequence Point (Order of Appearance) | What Triggers It (public entry or immediate caller) | Per‑Tile Coroutine | Duration Input / Governing Constant |
|-------------------------------------|------------------------------------------------------|--------------------|--------------------------------------|
| New growth cells appear after each board render (during growth cycles & post-growth re-render) | Default: `GridVisualizer.RenderBoard()` → `StartFadeInAnimations()`; orthogonal hyphal outgrowth: `GameBoard.HyphalGrowthVisualized` → `GridVisualizer.RenderBoard()` → `StartDirectionalGrowthAnimations()` | `FadeInCell` or `PlayDirectionalGrowth` | Default: `CellGrowthFadeInDurationSeconds` (fade) + minor flash (`NewGrowthFlashDurationSeconds`); directional: `HyphalGrowthSourceStretchDurationSeconds` + `HyphalGrowthTravelDurationSeconds` + `HyphalGrowthSettleDurationSeconds` |
| Creeping Mold move resolves after board render for Creeping Mold destinations | `GameBoard.CreepingMoldMove` → `GridVisualizer.RenderBoard()` → `PlayCreepingMoldAnimationBatch()` | `PlayCreepingMoldHopBatch` | `CreepingMoldSourceEmphasisDurationSeconds` + `CreepingMoldHopDurationSeconds` + `CreepingMoldLandingDurationSeconds` |
| Toxin placed (any phase when toxin tiles are added) | `TriggerToxinDropAnimation(int)` or `RenderBoard()` → `StartToxinDropAnimations()` | `ToxinDropAnimation` | `ToxinDropAnimationDurationSeconds` |
| Toxin expires during growth-start cleanup | `GameBoard.ToxinExpired` → `GridVisualizer.HandleToxinExpired()` → next `RenderBoard()` → `StartPendingToxinExpiryAnimations()` | `ToxinExpiryDissolveAnimation` | `ToxinExpiryDissolveDurationSeconds` |
| Cell marked dying (mainly after decay phase) | `TriggerDeathAnimation(int)` or `RenderBoard()` → `StartDeathAnimations()` | `DeathAnimation` | `CellDeathAnimationDurationSeconds` (first 15% is flash) |
| Passive alive-mold idle drift | `GridVisualizer.LateUpdate()` → `UpdateMoldIdleVisuals()` | None (per-frame tilemap transform) | `MoldIdleDriftAmplitudeXCellFraction`, `MoldIdleDriftAmplitudeYCellFraction`, `MoldIdleDriftPrimarySpeed`, `MoldIdleDriftSecondarySpeed` |
| Post‑Growth: Regenerative Hyphae reclaim batch | `PlayRegenerativeHyphaeReclaimBatch(tileIds, simplified, scaleMult [, explicitTotalSeconds])` | `RegenerativeHyphaeReclaimFull` / `RegenerativeHyphaeReclaimLite` | If provided: `explicitTotalSeconds`; else base sum `RegenerativeHyphaeTotalBaseDurationSeconds` scaled by `postGrowthPhaseDurationMultiplier` & `regenerativeHyphaeDurationMultiplier` |
| Post‑Growth: Directed vector surge presentation | `GameBoard.DirectedVectorSurge` → `PostGrowthVisualSequence` → `PlayDirectedVectorSurgePresentation(playerId, originTileId, tileIds)` | `RunDirectedVectorSurgePresentation` with chunk pulses + floating toast | `HyphalVectoringOriginPulseDurationSeconds` + chunk cadence (`HyphalVectoringChunkPulseDurationSeconds`, `HyphalVectoringChunkStaggerSeconds`) + `HyphalVectoringToastDurationSeconds` |
| Post‑Growth: Resistance pulses (Bastion / HRT spread) | `PlayResistancePulseBatchScaled(tileIds, scaleMultiplier)` | `BastionResistantPulseAnimation` | `MycelialBastionPulseDurationSeconds` (or `_timingContext.ResistancePulseTotal` if set) |
| Starting tile ping highlight (occasionally shown) | `TriggerStartingTilePing(playerId)` → `RunStartingTilePing()` | (helper in `RingHighlightHelper`) | Fixed 1.0s internal (no constant yet) |

## Notes
- Reclaim FULL sub‑phases (rise / hold / swap / settle) proportions come from base constants (`RegenerativeHyphae*DurationSeconds` + `RegenerativeHyphaeHoldBaseSeconds`). When you pass `explicitTotalSeconds`, those portions are applied linearly.
- Lite reclaim uses only rise + swap (same proportional logic limited to those two components).
- Timing context (`SetPostGrowthTiming`) can override reclaim (rise / hold / swap / settle / lite total) and resistance pulse totals directly.
- The passive alive-mold drift only applies to eligible living mold tiles with no overlay present, and it suspends while higher-priority board animations or player-hover emphasis are active.
- Source-aware directional normal growth currently applies only to standard orthogonal `HyphalOutgrowth` placements. Any newly grown tile without a buffered source/destination pair falls back to the existing fade-in behavior.
- When adding a new reusable animation or board-FX entry point, register it here with its trigger, main method, and governing constants.

## Minimal Mycovariant (Active Ability) Animations
Use the same GridVisualizer entry points; only the triggering context differs.
- Mycelial Bastion: calls `BastionResistantPulseAnimation` (batch via `PlayResistancePulseBatchScaled`) → duration: `MycelialBastionPulseDurationSeconds`.
- Surgical Inoculation: `SurgicalInoculationArcAnimation` (duration: `SurgicalInoculationArcDurationSeconds`). If the arc cannot be staged, it falls back to `ResistantDropAnimation` (duration: `SurgicalInoculationDropDurationSeconds`).
- Regenerative Hyphae reclaim already covered (triggered during post-growth when tiles reclaimed by effect logic).
- To uniformly slow a new active effect: add a single total duration constant to `UIEffectConstants` and multiply internal sub‑phase portions.

_End of concise helper._

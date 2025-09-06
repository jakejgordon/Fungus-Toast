# Animation Helper (Concise)

This file lists WHEN gameplay animations run during normal play (excluding mycovariant draft UI), the METHOD that triggers each batch, and the PRIMARY CONSTANT that determines total batch duration (or the value you pass in).

## Core (Normal Round Flow)

| Sequence Point (Order of Appearance) | What Triggers It (public entry or immediate caller) | Per‑Tile Coroutine | Duration Input / Governing Constant |
|-------------------------------------|------------------------------------------------------|--------------------|--------------------------------------|
| New growth cells appear after each board render (during growth cycles & post-growth re-render) | `GridVisualizer.RenderBoard()` → `StartFadeInAnimations()` | `FadeInCell` | `CellGrowthFadeInDurationSeconds` (fade) + minor flash (`NewGrowthFlashDurationSeconds`) |
| Toxin placed (any phase when toxin tiles are added) | `TriggerToxinDropAnimation(int)` or `RenderBoard()` → `StartToxinDropAnimations()` | `ToxinDropAnimation` | `ToxinDropAnimationDurationSeconds` |
| Cell marked dying (mainly after decay phase) | `TriggerDeathAnimation(int)` or `RenderBoard()` → `StartDeathAnimations()` | `DeathAnimation` | `CellDeathAnimationDurationSeconds` (first 15% is flash) |
| Post‑Growth: Regenerative Hyphae reclaim batch | `PlayRegenerativeHyphaeReclaimBatch(tileIds, simplified, scaleMult [, explicitTotalSeconds])` | `RegenerativeHyphaeReclaimFull` / `RegenerativeHyphaeReclaimLite` | If provided: `explicitTotalSeconds`; else base sum `RegenerativeHyphaeTotalBaseDurationSeconds` scaled by `postGrowthPhaseDurationMultiplier` & `regenerativeHyphaeDurationMultiplier` |
| Post‑Growth: Resistance pulses (Bastion / HRT spread) | `PlayResistancePulseBatchScaled(tileIds, scaleMultiplier)` | `BastionResistantPulseAnimation` | `MycelialBastionPulseDurationSeconds` (or `_timingContext.ResistancePulseTotal` if set) |
| Starting tile ping highlight (occasionally shown) | `TriggerStartingTilePing(playerId)` → `RunStartingTilePing()` | (helper in `RingHighlightHelper`) | Fixed 1.0s internal (no constant yet) |

## Notes
- Reclaim FULL sub‑phases (rise / hold / swap / settle) proportions come from base constants (`RegenerativeHyphae*DurationSeconds` + `RegenerativeHyphaeHoldBaseSeconds`). When you pass `explicitTotalSeconds`, those portions are applied linearly.
- Lite reclaim uses only rise + swap (same proportional logic limited to those two components).
- Timing context (`SetPostGrowthTiming`) can override reclaim (rise / hold / swap / settle / lite total) and resistance pulse totals directly.

## Minimal Mycovariant (Active Ability) Animations
Use the same GridVisualizer entry points; only the triggering context differs.
- Mycelial Bastion: calls `BastionResistantPulseAnimation` (batch via `PlayResistancePulseBatchScaled`) → duration: `MycelialBastionPulseDurationSeconds`.
- Surgical Inoculation: `SurgicalInoculationArcAnimation` (duration: `SurgicalInoculationArcDurationSeconds`) then `ResistantDropAnimation` (duration: `SurgicalInoculationDropDurationSeconds`).
- Regenerative Hyphae reclaim already covered (triggered during post-growth when tiles reclaimed by effect logic).
- To uniformly slow a new active effect: add a single total duration constant to `UIEffectConstants` and multiply internal sub‑phase portions.

_End of concise helper._

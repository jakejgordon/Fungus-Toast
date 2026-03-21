# Player Activity Log Helper

This document summarizes how the human-focused Player Activity Log aggregates and displays gameplay events. It is implementation-oriented and intentionally concise.

## Purpose

The Player Activity Log shows per-human-player summaries of what changed, grouped at meaningful gameplay checkpoints (log segments). It reduces spam while preserving tactical feedback.

The Global Game Log remains responsible for round/phase/system announcements for all players.

## Core Concepts

### Log Segment

A log segment is a contiguous gameplay window during which events are collected for later summarization. When a new segment starts, the previous segment is flushed (if non-empty) and aggregation state resets.

Planned segment types (`LogSegmentType`):

- `MutationPhaseStart`
- `GrowthPhase`
- `DecayPhase`
- `DraftPhase`

### Aggregation

Per human player, a `PlayerLogAggregation` tracks:

- Cell transformation totals (colonize, reclaim, infest, overgrow, toxify, poison), including per-`GrowthSource` breakdowns
- Death totals by `DeathReason`
- Free mutation points by source label
- Automatic/free upgrades by source and mutation name
- Optional draft snapshot (living/dead/toxin counts) for delta reporting

Aggregation is phase-agnostic; segment boundaries control presentation timing.

### Flush Timing

A segment flush happens when a new segment starts. Empty segments produce no summary lines.

### Display Timing

Queued segment summaries are emitted in chronological order when the active human player reaches the next actionable UI window.

## Formatting Rules

### Mutation Phase Start Summary

Prefix: `Mutation Phase Start:`

- Free points clause: `Free Points: X from SourceA, Y from SourceB`
- Upgrades clause: `Upgrades: SourceA upgraded N level(s) of Mutation1, ...`
- If both exist, join with `; ` and place points first

### Growth / Decay / Draft Summaries

Prefix by segment name:

- `Growth Phase:`
- `Decay Phase:`
- `Draft Phase:`

Display category order (non-zero only):

1. Colonized
2. Infested
3. Reclaimed
4. Overgrown
5. Toxified
6. Poisoned
7. Deaths

Example with source breakdown:

`Colonized 5 (3 from Hyphal Outgrowth, 2 from Manual placement)`

Deaths example:

`Deaths 4 (Infested 2, Poisoned 1, RandomDecay 1)`

Draft delta example:

`Draft Phase: Living +2, Dead -1`

### Round Summary (Special Case)

Round summary is always emitted at round end.

- If changes: `Round X Summary: Added 3 living cells, removed 1 dead cell`
- If no changes: `Round X Summary: No changes`

### Categories and Colors

- Segment summaries use `GameLogCategory.Normal`
- Instantaneous event lines may still use Lucky/Unlucky categories when appropriate

### Suppression Policy

Only round summary shows an explicit no-change line. Other segment summaries suppress empty output.

## Data Structures (High Level)

```text
PlayerLogAggregation
  Dictionary<CellEventKind, int> Totals
  Dictionary<CellEventKind, Dictionary<GrowthSource,int>> PerSource
  Dictionary<DeathReason,int> Deaths
  Dictionary<string,int> FreePointsBySource
  Dictionary<string, Dictionary<string,int>> FreeUpgradesBySource
  DraftStartSnapshot (living, dead, toxins) // optional during draft
```

## Lifecycle Summary

1. Initial state is empty.
2. `OnLogSegmentStart(...)` flushes prior segment (if non-empty), resets aggregation, and sets current segment type.
3. Event handlers update aggregation immediately.
4. Human action window displays queued summaries.
5. Repeat.

## Adding a New Event Type

1. Decide whether this is a `CellEventKind` addition or another category.
2. If `CellEventKind`, add enum member and update display ordering.
3. If `GrowthSource` applies, increment both total and per-source counters.
4. For death variants, prefer existing `DeathReason` taxonomy.
5. Validate by forcing one-round execution and checking flush/reset behavior.

## Extending Free Points and Upgrades

When adding passives that grant points or auto-levels:

- Call existing observer methods where possible.
- If new callbacks are needed, map them into `FreePointsBySource` / `FreeUpgradesBySource`.
- Use stable human-readable source labels.

## Common Pitfalls

- Missing segment summary: ensure boundary is called after intended effects.
- Duplicate counts: avoid double-subscribing event handlers.
- Missing source breakdown: preserve original `GrowthSource` values.
- Draft plus-zero noise: filter out zero deltas before formatting.
- No-change spam: keep suppression policy intact except round summary.

## Minimal Checklist

- [ ] Added/updated `CellEventKind` when needed
- [ ] Aggregation updates totals and per-source correctly
- [ ] Segment boundaries are still ordered correctly
- [ ] Summary ordering remains sensible
- [ ] Round summary behavior unchanged
- [ ] No unintended no-change lines

## Philosophy

Keep aggregation phase-agnostic and let segment boundaries decide presentation. This minimizes coupling when new abilities trigger outside canonical phases.

## When Not to Use Activity Log

- Global announcements: use Global Game Log
- Debug traces and analytics internals: use core logging or simulation output

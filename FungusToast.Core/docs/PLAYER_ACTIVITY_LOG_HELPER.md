# Player Activity Log Helper

This document summarizes how the **Player Activity Log** (human player–focused log) aggregates and displays gameplay events. It is intentionally concise and implementation-oriented.

---
## Purpose
The Player Activity Log shows per-human player summaries of what *changed* for them, grouped at meaningful gameplay checkpoints ("Log Segments"). It reduces spam while preserving important tactical feedback.

The Global Game Log (separate file/panel) continues to show round/phase/game system messages for everyone. This helper covers only the player-centric activity log.

---
## Core Concepts
### Log Segment
A contiguous window of gameplay during which events are collected for later summarization. When a new segment starts, the previous segment (if it recorded anything) is flushed into one summary entry per player and the aggregation state is reset.

Planned segment types (`LogSegmentType`):
- `MutationPhaseStart`
- `GrowthPhase`
- `DecayPhase`
- `DraftPhase`

(Additional types can be added without changing existing behavior.)

### Aggregation
Per human player we maintain a `PlayerLogAggregation` object that tracks:
- Cell acquisition / transformation events (colonize, reclaim, infest, overgrow, toxify, poison) by total and by `GrowthSource`
- Deaths by `DeathReason`
- Free mutation points earned by source (e.g. Mutator Phenotype, Hyperadaptive Drift)
- Free/automatic upgrades: source - mutation name - levels gained
- (Draft only) A snapshot of living / dead / toxin counts to report deltas

All of these are *phase-agnostic*; they accumulate regardless of when events fire. The segment boundary simply defines when they are summarized and reset.

### Flush Timing
A segment is *flushed* (summarized) only when a **new** segment begins. (Round Summary is separate; see below.) No "No changes" summary lines are emitted—silence means nothing notable happened.

### Display Timing
When the active human player's mutation turn (or next actionable UI window) begins, any queued segment summaries are emitted to the UI in chronological order.

---
## Formatting Rules
### Mutation Phase Start Summary (single line)
Prefix: `Mutation Phase Start:`
- If free points exist: `Free Points: X from SourceA, Y from SourceB`
- If upgrades exist: `Upgrades: SourceA upgraded N level(s) of Mutation1, SourceB upgraded M level(s) of Mutation2`
- If both exist: join with `; ` (points first). Omit any empty clause.

### Growth / Decay / Draft Summaries
Prefix matches segment name (`Growth Phase:`, `Decay Phase:`, `Draft Phase:`)
Order of event categories (only those with non-zero totals are shown):
1. Colonized
2. Infested
3. Reclaimed
4. Overgrown
5. Toxified
6. Poisoned
7. Deaths (always last, if any)

Per category with per-source breakdown:
`Colonized 5 (3 from Hyphal Outgrowth, 2 from Manual placement)`

Deaths:
`Deaths 4 (Infested 2, Poisoned 1, RandomDecay 1)` — sorted by descending count then name.

Draft Phase delta example:
`Draft Phase: Living +2, Dead -1` (omit any +0 category)

### Round Summary (special case)
Always emitted at round end:
- If changes: `Round X Summary: Added 3 living cells, removed 1 dead cell` (wording handled by existing formatter)
- If no changes: `Round X Summary: No changes`

### Categories / Colors
All segment summaries use `GameLogCategory.Normal` (neutral). Individual instantaneous events (e.g. a free upgrade line) may still use Lucky/Unlucky.

### Suppression Policy
Only Round Summary shows a "no changes" line. All other segment summaries are omitted if empty.

---
## Data Structures (High Level)
```
PlayerLogAggregation
  Dictionary<CellEventKind, int> Totals
  Dictionary<CellEventKind, Dictionary<GrowthSource,int>> PerSource
  Dictionary<DeathReason,int> Deaths
  Dictionary<string,int> FreePointsBySource
  Dictionary<string, Dictionary<string,int>> FreeUpgradesBySource
  DraftStartSnapshot- (living, dead, toxins)  // optional during draft
```
`CellEventKind` lives in Core for reuse. Deaths are kept separate to mirror existing `DeathReason` taxonomy.

---
## Lifecycle Summary
1. Game starts - initial segment implicitly empty.
2. Segment boundary method called (e.g. `OnLogSegmentStart(GrowthPhase)`):
   - Flush previous aggregation (if non-empty) into queue.
   - Reset aggregation.
   - Set current segment type.
3. Events fire - handlers update aggregation immediately.
4. Human turn begins - queued summaries for that player emitted to UI.
5. Repeat for each boundary.

---
## Adding a New Event Type
1. Decide if it is a cell acquisition / transformation (add new `CellEventKind`) or a distinct category.
2. If new `CellEventKind`:
   - Add enum member in Core.
   - Update formatting order list (if needed) in the activity log manager.
3. If it has a `GrowthSource`, extend the handler to increment both `Totals` and `PerSource`.
4. If it’s a death variant, reuse `DeathReason` rather than adding a new event kind.
5. Test by forcing the event in a single round and confirming:
   - It appears in the next segment summary
   - It clears after boundary

---
## Extending Free Points / Upgrades
When adding a new passive that awards points or levels automatically:
- Call the existing observer methods so the tracker updates (or add a new observer callback and map it into `FreePointsBySource` / `FreeUpgradesBySource`).
- Use a stable, human?readable source label (e.g. `Adaptive Expression`).

---
## Common Pitfalls
| Pitfall | Fix |
|---------|-----|
| Segment summary missing | Ensure boundary method called *after* all phase effects you want included. |
| Duplicate counting | Only register one event handler per GameBoard event. Guard with initialization flag. |
| Missing source breakdown | Make sure to pass the original `GrowthSource` through to the handler (do not overwrite with `Unknown`). |
| Empty Draft deltas show +0 | Filter zero categories before formatting. |
| Unwanted "No changes" spam | Summary suppression logic should skip empties (except round summary). |

---
## Minimal Checklist for Future Changes
- [ ] Added/updated `CellEventKind` (if needed)
- [ ] Handler increments aggregation (Totals + PerSource)
- [ ] Boundary call order still correct
- [ ] Summary formatting order still sensible
- [ ] Round Summary unaffected
- [ ] No unintended "No changes" lines

---
## Quick Example Flow
```
(Mutation auto-upgrades fire) -> aggregation collects points/upgrades
OnLogSegmentStart(MutationPhaseStart)   // flush previous, start new
Human player turn begins -> summary line emitted:
  Mutation Phase Start: Free Points: 2 from Mutator Phenotype; Upgrades: Mutator Phenotype upgraded 1 level(s) of Hyphal Outgrowth
Growth events occur (colonize, poison...) -> aggregation updates
OnLogSegmentStart(DecayPhase) -> emits Growth Phase summary
```

---
## Philosophy
Keep the aggregation *phase agnostic* and let segment boundaries decide presentation. This reduces future coupling when new abilities trigger outside canonical phases.

---
## When NOT to Use the Activity Log
- Global announcements (endgame, round start) - Global log
- Debug or analytics traces - Core logging / simulation output

---
## Future Ideas (Optional)
- Optional severity coloring per category
- Collapsible per-source breakdowns (UI enhancement)
- Export of per-segment metrics for analytics

---
**End of document.**

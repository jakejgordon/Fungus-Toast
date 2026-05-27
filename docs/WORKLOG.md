# Fungus-Toast Worklog

This file is the lightweight continuity anchor for OpenClaw-assisted Fungus-Toast work.

## Modus Operandi

Use the following minimal workflow to preserve working memory across sessions:

1. **Session anchor**
   - At the start of a Fungus-Toast session, explicitly say to work in:
     - `/home/jakejgordon/Fungus-Toast`
   - Also name the current thread of work when helpful.

2. **Canonical task list**
   - Use this file as the canonical in-repo task list and handoff for active Fungus-Toast work.
   - At the start of a new task, check `Pending Tasks` here and ask whether one of those should be completed first.

3. **Durable project record**
   - Keep durable project context in the repo only when it is still actively useful.
   - Do not keep transient simulation findings or stale task history here once they stop helping current decisions.

4. **End-of-session checkpoint**
   - Put end-of-session checkpoints in OpenClaw daily memory (`memory/YYYY-MM-DD.md`).
   - Only keep partial-progress resume notes here when they are needed to continue an unfinished task.

## Current Notes

- This file should stay concise and current.
- Detailed balance or simulation findings should live in the most relevant project docs, while this file tracks the active thread, pending tasks, and the next handoff.
- `docs/WORKLOG.md` is the canonical in-repo task and handoff file for active Fungus-Toast work.
- When starting a new Fungus-Toast task, first check the pending tasks here and ask whether one of them should be completed before starting something new.

## Active Thread

- **Repo:** `/home/jakejgordon/Fungus-Toast`
- **Current focus:** explicit board-shape metadata now exists for pita so the playable footprint no longer has to be inferred from pita sprite alpha.
- **Current state:** bread photo backgrounds still default to the shared visible-alpha-fit path, and the repo now also has a repeatable validator at `scripts/validate_board_backgrounds.py` that checks serialized metadata against the source PNGs and sweeps current size bands for degenerate masks. The earlier alpha-only pita path kept drifting between three geometries: the bread art, the square gameplay board, and the runtime clip. The current pita override now instead uses explicit playable-ellipse metadata in `BoardMediumConfig`, with blocked-tile derivation and background placement both reading the same stored circle approximation before the runtime `SpriteMask` is generated from the live blocked-tile footprint. Cheese now uses an explicit authored profile shape across the whole `41x41..80x80` band: it keeps the larger square `boardBoundsNormalized` footprint for placement, while blocked-tile derivation evaluates a normalized per-row left/right span profile plus explicit vertical min/max bounds so the top-left notch, side trims, and top/bottom blocking bands can be shaped deliberately without any debug-only overlay. Plain-cracker still uses measured visible-alpha bounds with tuned 2.5% insets and no board-bounds metadata or extra scale multiplier, and small seeded-cracker boards up to `20x20` still compose a light vertical safe-area margin on top of their sprite metadata so the top edge is less aggressive without redefining the sprite-wide footprint. Board-edge fade, the restored faint playable-area overlay, hover hit-testing, and highlight/ping overlays all continue to follow the live blocked-tile silhouette instead of the raw rectangular board bounds, bread-photo boards still use a generated runtime `SpriteMask` built from the live blocked-tile footprint, and the board renderer now suppresses toxin overlays when no owner mold tile can be resolved so a toxin icon should no longer render by itself if owner resolution ever fails. The current asset validator passes with pita reported through the new `ellipse-shape` path and cheese reported through the new `profile-shape` path.

## Current Plan

1. Keep the visible-alpha-fit + clip-budget path as the default shape solution for bread-photo backgrounds, but use explicit stored geometry when a shape keeps drifting under alpha inference.
2. Treat the current tuning baseline as `backgroundMaxTileClipFraction: 0.0`, `backgroundTileClipSampleResolution: 5`, and `backgroundScaleMultiplier: 1.0`; only keep per-background scale multipliers or hard-coded board-bounds metadata when they have been visually verified not to shrink the playable footprint.
3. Run `python3 scripts/validate_board_backgrounds.py` after board-background asset edits before doing the Unity-side visual pass.
4. For cheese specifically, keep future tuning asset-driven unless the authored profile model proves insufficient again; the runtime overlay probe has been removed for production.

## Pending Tasks

- Do an in-Unity visual pass across white bread, cheese, both crackers, and pita to confirm the silhouette-aligned faint playable overlay, edge fade, masked hover/highlight layers, and the new pita ellipse footprint all visually agree with the bread art.

## Compatibility Follow-up

- **Focus:** board-size / board-shape metadata compatibility when a new published build ships with changed board background metadata.
- **Evaluation:** current campaign checkpoint validation is too shallow for this risk. `CampaignController.TryGetGameplayCheckpoint(...)` only rejects missing snapshots, non-positive dimensions, or snapshots with no human player; it does not validate saved tile ids, blocked-tile ranges, or board-metadata compatibility before resume. `RoundStartRuntimeSnapshotFactory.Restore(...)` then rebuilds the board directly from the saved snapshot, and `GameBoard.PlaceFungalCell(...)` indexes `Grid[x, y]` from the saved `cell.TileId` without a bounds guard. That means a malformed or incompatible resumed snapshot can still hard-fail instead of degrading cleanly. By contrast, campaign-level preset lookup already has a soft fallback for missing `boardPresetId` values, so the highest crash risk is the in-level resume path, not the higher-level campaign meta save. There is already a startup reset precedent in `AlphaDataResetService`, but it is a hard-coded silent wipe with no player-facing post-restart explanation, which is not the behavior we want here.
- **Plan:**
  1. Add an explicit board-layout compatibility token for save/resume, scoped to board metadata compatibility rather than general app versioning. Bump it whenever board background metadata changes can invalidate old blocked-tile or tile-id assumptions.
  2. Apply that token during startup before resume UI is shown. On mismatch, invalidate only affected in-progress gameplay state: clear the campaign in-level checkpoint and delete the solo/hotseat save, while preserving durable campaign meta progression when it remains honest to do so.
  3. Persist a one-shot post-restart notice payload alongside the invalidation so the next boot can show a dismissable modal explaining that an update changed board layout data and the in-progress run had to restart from a safe state.
  4. Strengthen resume validation beyond the current width/height/player check: reject snapshots with out-of-range tile ids, impossible player references, or other structural inconsistencies before they reach `RoundStartRuntimeSnapshotFactory.Restore(...)`.
  5. Wrap actual restore entry points so any remaining restore exception degrades to deliberate invalidation + restart/notice instead of crashing the build.
  6. Add compatibility smoke coverage for both campaign and solo resume so future board metadata changes have an explicit checklist instead of relying on memory.
- **Pending implementation tasks:**
  - implement the board-metadata compatibility token + startup invalidation flow
  - add a dismissable post-restart popup on the start/menu surface for compatibility-triggered restarts
  - harden campaign and solo resume validation around saved snapshot tile ids and restore exceptions
- **Update:** those implementation tasks are now complete in code. Remaining follow-up is an in-Unity validation pass for the compatibility-restart UX and the new popup/menu flow.

## Current Handoff

- `WORKLOG.md` is intentionally present-tense only. Historical task logs and stale tuning notes have been removed.
- Moldiness was chosen as the campaign meta-currency name.
- Moldiness progression foundation is committed in `90b81d3` and the first unlock-plumbing prototype is committed in `32b2b44`.
- Current moldiness prototype in repo:
  - level clears award persistent moldiness using rewards `1,1,2,2,3,3,4,4,5,5,6,6,7,7`, and the final campaign clear now grants a dedicated victory bonus that currently lands at `14`
  - threshold tiers are `6,9,12,15,18,21,24,27,30,34`, then continue with `+4` growth beyond the table
  - overflow carries over and multiple unlock thresholds can trigger from one award
  - moldiness rewards now block the normal adaptation draft until resolved instead of auto-applying silently
  - defeat carryover selection now blocks defeat reset when the player has carryover capacity
  - pending-threshold reward generation now correctly considers newly triggered unlock levels, so level-appropriate rewards appear immediately on threshold reach
  - victorious full campaign clears now unlock the next campaign start difficulty, and the campaign start screen exposes the unlocked difficulty starts directly on the mold-selection step
- Completed moldiness UI/presentation work now in repo:
  - moldiness reward cards render visibly and can be selected on the endgame panel (`fcad81d`)
  - campaign menu moldiness summary card exists as the primary persistent home for moldiness state outside gameplay (`d71f2c3`)
  - end-of-run moldiness summaries exist on campaign end panels (`619305e`)
  - reward generation bug for pending thresholds is fixed (`cc56710`)
  - moldiness reward cards now support icon/category presentation, and permanent campaign upgrades have distinct tags and campaign-menu visibility (`2dbb202`)
  - on 2026-04-19, substantial follow-up polish landed for campaign testing overrides, pending moldiness reward presentation, reward-card sizing/layout, mode-select pending reward routing, and resume/menu flow cleanup; inspect current files before assuming any earlier UI layout details
- Recovered design intent from the earlier moldiness transcript:
  - moldiness should be a single meta-progression currency first, with more milestone-based systems added later if needed
  - reward gain should come from campaign progress, especially cleared campaign levels, and scale with progression depth
  - threshold crossings should surface multiple unlock choices, not a single forced result
  - current preference is that moldiness unlock resolution happens before the normal post-victory adaptation choice
  - some unlocks may eventually affect the current run immediately rather than only future runs
- Design correction from Jake:
  - ordinary existing adaptations should not be treated as automatic permanent moldiness unlocks
  - moldiness should instead drive a separate moldiness draft that can unlock explicit locked content and repeatable meta rewards
  - `MoldinessUnlockLevel` controls what can appear in the moldiness draft, not what automatically appears in the normal adaptation draft
  - the system should be extensible enough to support Adaptations first, then later Mycovariants, Mutations, or other reward types
  - moldiness progression should not be shown during live gameplay; it should live on campaign-layer surfaces instead
- MVP content direction:
  - start with a hybrid model
  - moldiness draft offers should include both locked-content unlock rewards and repeatable universal meta rewards
  - first repeatable universal reward should permanently increase failed-run adaptation carryover capacity by +1 per draft
  - first locked-content rewards include at least `Spore Salvo`, `Hyphal Bridge`, `Vesicle Burst`, `Hyphal Priming`, and `Prime Pulse` as level-1 adaptation unlock rewards
- Longer-term unlock categories discussed in the recovered transcript and follow-up clarification:
  - additional draftable adaptations
  - additional draftable mycovariants
  - mutation-related unlocks
  - failed-run carryover adaptation systems
- UI direction discussed and currently adopted:
  - moldiness should feel organic, atmospheric, scientific, fungal, and slightly quirky
  - a toast-corruption / colonized-tiles mini-toast is the primary moldiness visualization
  - exact numbers support the visualization rather than replace it
  - the visualization belongs in campaign menus, win/loss result panels, and moldiness reward context, not in the live match HUD
  - permanent campaign upgrades should keep the same overall card footprint as other moldiness rewards while using distinct iconography, accent styling, and category labels rather than different geometry
  - for pending moldiness reward resume flows, a simple opaque backdrop is currently preferred over spending more time on special bread-background rendering
- Recent completed work from 2026-04-24:
  - campaign-only `Strain Profiling` moldiness unlock was added so owned campaign runs reveal AI `FriendlyName` and `AIPlayerIntentions` in player-summary tooltips
  - campaign-visible AI strategy ids were normalized to canonical `CMP_*` names with legacy alias resolution kept for compatibility
  - campaign AI `AIPlayerIntentions` now come from deterministic generated text rather than relying only on hand-authored blurbs
  - tooltip display was adjusted so unlocked AI profiles show `Opponent: {FriendlyName}` and `Strategy: {AIPlayerIntentions}` rather than exposing the technical strategy id
- Latest completed polish/fix work from 2026-04-19:
  - campaign testing level override and temporary forced-adaptation flow were completed and pushed earlier in the day
  - forced-adaptation checklist scrolling, hit area, persistence, and stale-state clearing were fixed in Unity UI
  - moldiness reward cards were rebuilt into compact icon + title + description + badge tiles with tuned widths and larger readable text
  - pending moldiness reward overlays were narrowed and centered, with right-sidebar suppression for the resume flow
  - mode select now surfaces `Campaign (Pending Reward)` and routes into pending reward resolution
  - pending reward claims entered from mode select now return to the campaign menu instead of jumping straight into gameplay
- Latest completed campaign-victory payoff work from 2026-05-08:
  - final campaign clear now uses dedicated victory copy on the campaign end panel
  - final campaign clear now awards a larger moldiness payout tuned to about `2x` the penultimate reward
  - victorious full clears now unlock the next campaign start difficulty, and the campaign panel exposes unlocked start difficulties on the new-run flow
- Spore-sifting planning and campaign-update follow-ups are considered complete for handoff purposes; leave them out of future pending-task lists unless they re-open.

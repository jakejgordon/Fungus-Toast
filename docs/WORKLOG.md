# Fungus-Toast Worklog

This file is the lightweight continuity anchor for OpenClaw-assisted Fungus-Toast work.

## Modus Operandi

Use the following minimal workflow to preserve working memory across sessions:

1. **Session anchor**
   - At the start of a Fungus-Toast session, explicitly say to work in:
       - `c:/Users/jakej/FungusToast`
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

- **Repo:** `c:/Users/jakej/FungusToast`
- **Current focus:** realistic mold icon overhaul rollout for the remaining 6 molds
- **Current state:**
   - Red mold slot 0 is the completed baseline.
   - Yellow mold slot 1 is wired, visually accepted, and now part of the completed baseline set.
   - Board rendering now supports `isolatedTile`, `clusteredTile`, `clusteredAlternateTile`, `denseTile`, and `denseAlternateTile`.
   - Shared non-board UI icons now use the representative mold icon path in `GridVisualizer` rather than reading only the old base tile.
   - `FungusToast.Core/docs/MOLD_ICON_HELPER.md` is the canonical workflow doc for prompts, filenames, wiring, and evaluation.
   - New prompt rule: each mold must have its own species identity and macro-structure motif, not just a palette shift from red.

## Pending Tasks

- Roll out the remaining 6 mold slots one at a time using the red and yellow completed molds as the current reference bar.
- Slot 2: `cyan_mold_64x64.asset` -> use prefix `cyan_mold_pilot`
- Slot 3: `aqua_mold_64x64_0.asset` -> use prefix `aqua_mold_pilot`
- Slot 4: `green_mold_64x64.asset` -> use prefix `green_mold_pilot`
- Slot 5: `dark_blue_mold_64x64_0.asset` -> use prefix `dark_blue_mold_pilot`
- Slot 6: `purple_mold_new_64x64_0.asset` -> use prefix `purple_mold_pilot`
- Slot 7: `orange_red_mold_64x64_0.asset` -> use prefix `orange_red_mold_pilot`
- For each remaining slot, target this asset set unless playtest evidence says a simpler set is sufficient:
   - `{prefix}_isolated_64x64.png`
   - `{prefix}_clustered_64x64.png`
   - `{prefix}_clustered_alt_64x64.png`
   - `{prefix}_dense_64x64.png`
   - `{prefix}_dense_alt_64x64.png`
- For each slot, follow the same execution loop:
   - generate PNGs with the real-alpha prompt requirements from `MOLD_ICON_HELPER.md`
   - create matching Tile assets in `Assets/Tiles/Mold`
   - wire the slot in `SampleScene` under `playerMoldAliveVariantTiles`
   - build `FungusToast.Unity/Assembly-CSharp.csproj --no-restore`
   - review in Unity on both the board and the shared UI icon surfaces

## Next Handoff

- Start the next mold with slot 2 `cyan_mold_pilot`.
- Use red and yellow as quality baselines for readability, but do not reuse their rounded rosette silhouette language.
- Push cyan toward a more distinctive branching or frond-based species identity so it reads as a different organism family at a glance.
- Suggested next scope: generate all five cyan prompts with explicit same-species language, distinct macro-structure guidance, and the standard alpha/channel constraints before moving into wiring.

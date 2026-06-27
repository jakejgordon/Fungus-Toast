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
- **Current focus:** campaign start difficulty now biases the human starting slot by authored board-position strength
- **Current state:**
   - `CampaignBoardStartingPositionCatalog.g.cs` and `FungusToast.Core/Board/Generated/campaign_board_starting_positions.json` now carry generated per-preset slot metadata keyed by preset and player count.
   - `CampaignStartingPositionDifficultyResolver` maps `CampaignDifficulty` to a sliding half-window of allowed human spawn coordinates, from strongest slots at `Training` toward weakest slots at `Boss`.
   - New campaign runs now persist the selected `startDifficulty`, and the Unity campaign start flow forwards that selection into gameplay setup.
   - `GameManager` now prefers metadata-driven spawn coordinates for campaign humans before falling back to the preset pool.
   - Targeted resolver tests pass, and Core/Simulation builds are clean in the current checkout.

## Pending Tasks

- Verify the new spawn-bias flow in Unity Editor because this checkout does not currently include the generated Unity `.csproj` compile proxy.
- Decide whether `docs/generated/hotdog_115_slot_validation.csv` should be kept as a committed validation artifact for this feature.
- If campaign-start difficulty behavior feels too subtle in playtests, tune the resolver windowing strategy rather than adding flat stat modifiers first.
- The mold icon overhaul thread is still parked after the yellow slot baseline; resume from cyan when this campaign work is out of the way.

## Next Handoff

- In Unity, start new campaign runs at `Training` and `Boss` on the same preset and confirm the human spawn shifts from the strongest half of slots toward the weakest half.
- If the in-editor behavior matches expectations, keep the feature slice scoped to the current metadata/resolver/start-flow files and exclude local temp outputs such as `tmp/campaign_starting_position_metadata` and the touched `.pyc`.
- After Unity verification, the next dormant project thread is still the cyan mold rollout from the earlier art pass.

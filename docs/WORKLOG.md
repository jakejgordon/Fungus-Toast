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
- **Current focus:** none
- **Current state:**
   - No active Fungus-Toast task is currently in progress.
   - The recent main-menu polish pass is complete, including ambient mold growth, brighter button contrast, translucent overlay cards, submenu background continuity, and cleanup of the stray draggable toast-colored line at the top of the screen.
   - Use daily memory notes for the detailed change history; keep this file focused on genuinely active work.

## Pending Tasks

- Expand each of the eight player molds from five to eight alive-state sprites: add one isolated, one clustered, and one dense variant per mold. The durable implementation and generation handoff is in `FungusToast.Core/docs/MOLD_ICON_HELPER.md` under **Eight Images Per Mold Expansion Plan**. Image generation is waiting on the explicit decisions listed in that plan.

## Next Handoff

- Resume from `FungusToast.Core/docs/MOLD_ICON_HELPER.md` → **Eight Images Per Mold Expansion Plan**. Resolve the five inputs from Jake before generating assets; do not silently choose the alpha workflow or normalize existing outlier sprites.

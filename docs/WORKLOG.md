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
- **Current focus:** main-menu polish pass with subtle ambient mold growth around the mode-select screen
- **Current state:**
   - `Solo / Hotseat` and `Campaign` should remain equal-weight entry paths, so the menu treatment should emphasize atmosphere and readability rather than a single recommended CTA.
   - Existing player mold icon art under `FungusToast.Unity/Assets/Sprites/Tiles/Mold/` is the preferred source material for this pass; avoid turning the task into a new art pipeline.
   - The current target is a lightweight first pass inside the existing mode-select flow, aiming for "better" rather than a bespoke background-animation system.

## Pending Tasks

- Implement a runtime ambient mold layer for the main menu using reused player mold icon sprites, with colonies staged around the screen perimeter and kept off the button labels.
- Make the mold presentation fully ambient from frame one: slow breathing, slight drift, and staggered timings instead of explicit center-racing growth.
- Update the mode-select button semantics so `Solo / Hotseat` and `Campaign` read as equal peer choices instead of dual primary CTAs.
- Verify the menu in Unity Editor for subtlety, readability, and whether the ambient layer should remain visible behind `Settings` and `Credits`.

## Next Handoff

- First implementation slice: keep the change scoped to `UI_ModeSelectPanelController` plus this worklog update so the menu ambience can be tuned without broad scene refactors.
- In Unity, check the home screen at a few aspect ratios and confirm the perimeter mold feels alive but does not compete with the central menu stack.
- If the first pass reads too static, consider a tiny spore layer only after testing the sprite-only ambience; do not add particles by default.

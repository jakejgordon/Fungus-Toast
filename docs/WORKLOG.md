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
   - The ambient mold layer is now in place inside `UI_ModeSelectPanelController` and has been tuned once to feel less like repeated stickers: colonies now sample multiple sprites from one random mold family, sit more offscreen at the edges, and animate with lighter motion/opacity.
   - A lightweight backdrop depth layer now sits under the mold: subtle center glows plus soft edge vignette bands to give the menu stack more separation from the flat canvas color without adding bespoke art assets.
   - The current target remains a lightweight polish path inside the existing mode-select flow, aiming for "better" rather than a bespoke background-animation system.

## Pending Tasks

- Verify the menu in Unity Editor for subtlety, readability, and whether the ambient layer should remain visible behind `Settings` and `Credits`.
- If the perimeter still reads too “placed,” try a second tuning pass with either 1-2 smaller secondary colonies or per-colony tint softening before considering any particle/spore work.
- If the backdrop pass feels too rectangular in motion, reduce it to mostly static edge shading or replace the larger glow plates with a more organic asset-driven treatment later.

## Next Handoff

- The implementation remains intentionally scoped to `UI_ModeSelectPanelController`, so more tuning can happen without scene or asset-pipeline churn.
- In Unity, check the home screen at a few aspect ratios and confirm the new offscreen-biased placement, mixed mold-family sprites, and backdrop glow/vignette layer still feel organic and stay out of the button lane.
- Only consider spores or particles if the sprite-only pass still feels dead after the current variation work; they are not the default next step.

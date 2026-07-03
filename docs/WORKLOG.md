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
   - The backdrop pass has been softened back down to mostly faint edge shading after the larger glow plates read too boxy on-screen.
   - A second ultra-subtle encroachment ring now uses the same mold family art nearer the center margins with very low alpha, slight drift, and slow fade/scale breathing so the menu feels alive without putting mold over the main buttons.
   - The current target remains a lightweight polish path inside the existing mode-select flow, aiming for "better" rather than a bespoke background-animation system.

## Pending Tasks

- Verify the menu in Unity Editor for subtlety, readability, and whether the ambient layer should remain visible behind `Settings` and `Credits`.
- If the center encroachment reads too noticeable, reduce its alpha/growth swing before adding any new effect type.
- If the menu still feels dead after this pass, the next candidate is a very sparse spore drift layer rather than stronger central mold coverage.

## Next Handoff

- The implementation remains intentionally scoped to `UI_ModeSelectPanelController`, so more tuning can happen without scene or asset-pipeline churn.
- In Unity, check the home screen at a few aspect ratios and confirm the restored perimeter motion plus faint center-encroachment layer feel alive while still staying out of the button lane.
- Only consider spores or particles if the sprite-only pass still feels dead after the current variation work; they are not the default next step.

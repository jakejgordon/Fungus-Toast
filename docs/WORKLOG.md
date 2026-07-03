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
   - Those inner colonies now stagger their fade-in over the opening several seconds so the empty middle margins gradually feel more claimed instead of fully static from frame one.
   - The latest follow-up lightens the menu base further, softens the vignette again, and gives the compact lower action buttons a lighter menu-specific dark treatment so they stand apart from the background without the harsh hover contrast.
   - The ambient mold layer now stages a much larger growth wave over roughly the first 30 seconds, with additional top, bottom, and inner-approach colonies fading in so the screen fills in from all sides instead of staying sparse.
   - Ambient motion/pulsing has been strengthened again by increasing the drift, alpha swing, scale breathing, and animation speeds so the mold reads as gently alive instead of static.
   - A follow-up tuning pass adds another set of center-side and upper/lower gap colonies so the remaining empty pockets fill in more naturally over time instead of leaving large dead zones.
- The latest pass specifically targets the left/right side lanes with another set of staged colonies so the wide side gutters fill in more evenly instead of leaving obvious vertical empty bands.
- The latest opacity tuning makes the ambient mold fade in faster and hold at a noticeably higher alpha so the early colonies no longer disappear into the background after the first 10-15 seconds.
   - Credits and Settings now use translucent panel cards rather than opaque slabs, so the mold backdrop still reads behind them while the text stays comfortably legible.
   - The `Solo / Hotseat` and `Campaign` submenu roots now use transparent full-screen surfaces so the shared animated mold backdrop can remain visible behind those screens instead of repainting a flat canvas over it.
   - The current target remains a lightweight polish path inside the existing mode-select flow, aiming for "better" rather than a bespoke background-animation system.

## Pending Tasks

- Verify in Unity Editor that the brightened backdrop restores enough contrast for the lower action buttons without washing out the mold atmosphere.
- Verify that entering `Solo / Hotseat` and `Campaign` now visibly preserves the animated mold background, and that backing out of those screens restores the normal main menu state.
- Verify that the first 20-30 seconds now show enough extra mold arrivals from the top/bottom and not just the left/right edges.
- Verify that the left and right gutters now feel filled enough without making the side lanes look too evenly patterned.
- Verify that the translucent Credits/Settings cards feel intentional rather than washed out, and keep enough contrast for the copy on all tested monitors.
- If the restored motion now reads too busy, reduce drift distance first before trimming the new colony count.

## Next Handoff

- The implementation remains intentionally scoped to the menu controllers, so more tuning can happen without scene or asset-pipeline churn.
- In Unity, check the main menu plus the `Solo / Hotseat` and `Campaign` submenu transitions at a few aspect ratios and confirm the shared animated backdrop sits behind those screens without leaking into actual gameplay.
- Only consider spores or particles if the sprite-only pass still feels dead after this submenu-background pass; they are not the default next step.

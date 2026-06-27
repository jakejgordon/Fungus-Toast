---
name: add-board-background
description: Guide for adding or reauthoring a Fungus Toast board background, especially when playable footprint, blocked tiles, irregular silhouettes, baked masks, or placement metadata are involved. Use when asked to add a new background or fix one whose art and gameplay shape must align.
---

# Add Board Background

Read these docs first:

1. `FungusToast.Core/docs/NEW_BACKGROUND_HELPER.md`
2. `FungusToast.Core/docs/UI_ARCHITECTURE_HELPER.md`
3. `FungusToast.Core/docs/second-level/UNIT_ASSET_NAMING_CONVENTIONS.md` when importing new source assets

## Workflow

1. Choose the intended shape source before tuning anything:
   - plain alpha
   - ellipse metadata
   - horizontal span profile
   - baked blocked-tile masks
2. Treat `visibleAlphaBoundsNormalized` as measured input, not as a tuning knob.
3. If the board art is irregular but gameplay should still be square, use the contour-to-square baked-mask workflow instead of piling on inset and scale tweaks.
4. Update the owning metadata in:
   - `FungusToast.Unity/Assets/Scripts/Unity/Grid/BoardMediumConfig.cs`
   - `FungusToast.Unity/Assets/Configs/Toast Configs/ToastBoardMedium.asset`
5. Use `scripts/validate_board_backgrounds.py` to measure, validate, and emit baked masks when needed.
6. Do not hand-author blocked-tile ID lists.
7. Keep placement, blocked tiles, mask rendering, and overlay alignment tied to one canonical footprint model.

## Validation

1. Run the validator from the repo root.
2. Confirm the reported metadata matches the intended board bounds and baked sizes.
3. For baked sizes, confirm the validator reports `baked-mask` rather than fallback `alpha-shape`.
4. Perform the Unity visual pass for placement, blocked tiles, hover alignment, highlight clipping, and edge fades.
5. If the sprite pixels or canonical board bounds change, rebake instead of preserving stale masks.

## Output

Report:

1. Which shape model was chosen and why.
2. The metadata and asset files changed.
3. The validator command that was run.
4. Any Unity-side manual follow-up still required.

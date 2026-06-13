# Board Background Authoring Helper

This document is the canonical workflow for adding or reauthoring board background silhouettes in `FungusToast.Unity`.

Use this helper when a background image affects playable footprint, blocked tiles, or board placement. For broader Unity UI/service patterns, see `UI_ARCHITECTURE_HELPER.md`.

## When To Use Which Shape Source

Pick the simplest shape model that matches the intended playable silhouette.

1. Use plain alpha-derived masking when the visible sprite alpha already cleanly matches the intended board footprint.
2. Use `hasPlayableEllipse` when the intended playable area is genuinely ellipse-like, such as pita.
3. Use `hasPlayableHorizontalSpanProfile` when the silhouette needs authored asymmetric per-row trimming and the board-space shape is still easy to describe row by row.
4. Use `bakedBlockedTileMasks` when the intended silhouette is irregular enough that row spans are brittle or when non-square source art needs a deliberately centered square gameplay envelope with conservative trimming.

The baked-mask path is the preferred workflow for irregular bread-photo boards like Kaiser Bun.

## Owning Files

- Runtime/background metadata owner: `FungusToast.Unity/Assets/Scripts/Unity/Grid/BoardMediumConfig.cs`
- Toast medium asset: `FungusToast.Unity/Assets/Configs/Toast Configs/ToastBoardMedium.asset`
- Validation and bake tooling: `scripts/validate_board_backgrounds.py`

## Core Rule

Keep one canonical playable footprint model for the sprite, then make placement, blocked-tile derivation, and mask rendering all agree with that same model.

Do not keep widening insets, scale multipliers, or clip budgets to compensate for a silhouette that should instead have explicit shape metadata.

## Contour-To-Square Baked-Mask Workflow

Use this for irregular backgrounds whose intended gameplay board is square but whose art alpha is not.

### 1. Measure The Visible Perimeter

Measure the sprite's raw visible alpha envelope and record it as `visibleAlphaBoundsNormalized`.

Treat this as source-of-truth input data, not the final gameplay footprint.

### 2. Build The Square Gameplay Envelope

Create a centered square from the visible bounds:

1. Compute `squareSize = max(visibleWidth, visibleHeight)`.
2. Keep the square centered on the visible-bounds midpoint.
3. Clamp only if needed to stay within the sprite rect.
4. Write that square to `boardBoundsNormalized`.

This square is the canonical gameplay envelope for the bake. It intentionally may extend beyond visible alpha when the art is shorter or narrower than the requested board shape.

### 3. Decide Which Board Sizes Need Exact Masks

Choose the exact board sizes that should use the baked footprint, usually the sizes covered by the sprite's override bands.

Bake exact masks for each of those target sizes instead of relying on interpolation.

### 4. Emit The Baked Masks

Run the validator with the workspace virtual environment on Windows:

```powershell
.\.venv\Scripts\python.exe scripts/validate_board_backgrounds.py --emit-baked-mask-sprite <sprite-name> --emit-baked-mask-sizes 85x85,90x90,95x95 --emit-baked-mask-version contour-square-v1
```

The emitted snippet includes:

- the recommended square `boardBoundsNormalized`
- `spriteContentHash`
- `bakedBlockedTileMasks` entries for each requested board size

Do not hand-author blocked-tile ID lists. Regenerate them from the tool whenever the sprite pixels change.

### 5. Update The Asset Metadata

In `ToastBoardMedium.asset` or the relevant medium asset:

1. Keep `visibleAlphaBoundsNormalized` as the measured visible bounds.
2. Set `boardBoundsNormalized` to the square gameplay envelope from the tool.
3. Disable incompatible explicit-shape metadata if the baked mask is now authoritative:
   - `hasPlayableEllipse: 0`
   - `hasPlayableHorizontalSpanProfile: 0`
   - `playableHorizontalSpanProfile: []`
4. Add the emitted `bakedBlockedTileMasks` entries.
5. Keep `deriveBlockedTilesFromBackgroundAlpha` enabled unless there is a reason to stop using alpha for non-baked fallback sizes.

### 6. Align Override Placement

Make sure size-band overrides do not reintroduce offset drift against the canonical square envelope.

In practice:

1. Start with `backgroundInset*Normalized: 0` for the baked-mask size bands when the square `boardBoundsNormalized` should fully own placement.
2. Keep `composeSafeAreaWithBoardBoundsMetadata: 0` unless the band truly needs extra inset inside the authored square.
3. Keep `backgroundScaleMultiplier: 1.0` unless visual verification proves the sprite still needs framing adjustment.

If you need band-specific extra inset after baking, compose it deliberately inside the square envelope instead of changing the square itself.

## Metadata Intent

- `visibleAlphaBoundsNormalized`: measured visible-pixel envelope from the source sprite.
- `boardBoundsNormalized`: canonical gameplay envelope inside the sprite; for contour-square bakes this is usually the centered square derived from visible bounds.
- `bakedBlockedTileMasks`: exact blocked-tile sets keyed by `boardWidth`, `boardHeight`, bake version, and sprite content hash.
- `spriteContentHash`: guardrail that ties a baked mask to the exact sprite pixels used to generate it.

## Validation Checklist

After editing the asset, run:

```powershell
.\.venv\Scripts\python.exe scripts/validate_board_backgrounds.py
```

Do not stop at a green exit code. Confirm the output also says all of the following:

1. The sprite metadata summary shows the expected `boardBoundsNormalized` square.
2. The sprite metadata summary lists the expected baked sizes, for example `baked=85x85, 90x90, 95x95`.
3. The probe summary reports `baked-mask` for those exact target sizes rather than `alpha-shape`.

Then do an in-Unity visual pass at the affected board sizes and verify:

1. background placement
2. blocked-tile footprint
3. hover / inspection / magnifier alignment
4. highlight and overlay clipping
5. board-edge fade and playable-area tint alignment

## Regeneration Rules

Rebake the masks if any of these change:

1. the source sprite pixels
2. the intended square `boardBoundsNormalized`
3. blocker sampling rules in the validator/runtime
4. the exact board sizes covered by the authored override band

Do not preserve old blocked-tile lists after a sprite-content change just because the silhouette looks similar.

## Current Example

Kaiser Bun is the reference implementation for this workflow:

1. visible alpha stays recorded as the measured bun silhouette
2. `boardBoundsNormalized` is a centered square derived from `max(width, height)`
3. the size bands `85x85`, `90x90`, and `95x95` use `bakedBlockedTileMasks`
4. band insets are zeroed so the square envelope, blocked tiles, and rendered art all line up
# Board Background Authoring Helper

This document is the canonical workflow for adding or reauthoring board background silhouettes in `FungusToast.Unity`.

Use this helper when a background image affects playable footprint, blocked tiles, or board placement. For broader Unity UI/service patterns, see `UI_ARCHITECTURE_HELPER.md`.

## Quick Start

Use this checklist when adding a brand-new board background:

1. Pick the intended shape source before tuning anything:
   - plain alpha when the sprite alpha already matches the playable shape
   - ellipse metadata for genuinely round/oval boards such as pita
   - horizontal span profile for stable row-by-row authored trims such as cheese
   - baked masks for irregular photo silhouettes that need an exact square gameplay envelope such as Kaiser Bun
2. Import the sprite and add or update the background entry in `ToastBoardMedium.asset`.
3. Add a matching `boardBackgroundSpriteMetadata` entry for the sprite.
4. Start from the conservative baseline:
   - `deriveBlockedTilesFromBackgroundAlpha: 1` when alpha is still part of the fallback path
   - `backgroundScaleMultiplier: 1.0`
   - `backgroundMaxTileClipFraction: 0.0`
   - `backgroundTileClipSampleResolution: 5`
5. If the shape is irregular and should still play as a square board, follow the baked-mask workflow below instead of widening insets until it "looks close enough".
6. Run the validator, then do the Unity visual pass. Do not treat validator success as sufficient by itself.

## When To Use Which Shape Source

Pick the simplest shape model that matches the intended playable silhouette.

1. Use plain alpha-derived masking when the visible sprite alpha already cleanly matches the intended board footprint.
2. Use `hasPlayableEllipse` when the intended playable area is genuinely ellipse-like, such as pita.
3. Use `hasPlayableHorizontalSpanProfile` when the silhouette needs authored asymmetric per-row trimming and the board-space shape is still easy to describe row by row.
4. Use `bakedBlockedTileMasks` when the intended silhouette is irregular enough that row spans are brittle or when non-square source art needs a deliberately centered square gameplay envelope with conservative trimming.

The baked-mask path is the preferred workflow for irregular bread-photo boards like Kaiser Bun.

## Before You Start Tuning

Use these guardrails up front:

1. Treat `visibleAlphaBoundsNormalized` as measured input, not as a creative tuning knob.
2. Treat `boardBoundsNormalized` as canonical gameplay geometry once authored. Do not casually add it "just to fix framing".
3. Keep `backgroundScaleMultiplier` at `1.0` unless a validated shape still needs a render-only framing adjustment after the footprint is already correct.
4. Prefer changing the shape model over stacking compensating tweaks. Repeated inset/scale/clip adjustments usually mean the chosen model is wrong.

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

Create a centered square from the visible bounds in source-image pixels:

1. Compute `squareSize = max(visibleWidth, visibleHeight)`.
2. Keep the square centered on the visible-bounds midpoint.
3. Clamp only if needed to stay within the sprite rect.
4. Convert that pixel square back into normalized sprite coordinates and write the resulting rect to `boardBoundsNormalized`.

This square is the canonical gameplay envelope for the bake. It intentionally may extend beyond visible alpha when the art is shorter or narrower than the requested board shape.
On non-square sprites, the serialized `boardBoundsNormalized.width` and `.height` will usually differ even though the underlying pixel-space gameplay envelope is square.

### 3. Decide Which Board Sizes Need Exact Masks

Choose the exact board sizes that should use the baked footprint, usually the sizes covered by the sprite's override bands.

Bake exact masks for each of those target sizes instead of relying on interpolation.

### 4. Emit The Baked Masks

Run the validator from the repo root:

Windows:

```powershell
.\.venv\Scripts\python.exe scripts/validate_board_backgrounds.py --emit-baked-mask-sprite <sprite-name> --emit-baked-mask-sizes 85x85,90x90,95x95 --emit-baked-mask-version contour-square-v1
```

POSIX:

```bash
python3 scripts/validate_board_backgrounds.py --emit-baked-mask-sprite <sprite-name> --emit-baked-mask-sizes 85x85,90x90,95x95 --emit-baked-mask-version contour-square-v1
```

The emitted snippet includes:

- the recommended pixel-square `boardBoundsNormalized`
- `spriteContentHash`
- `bakedBlockedTileMasks` entries for each requested board size

Do not hand-author blocked-tile ID lists. Regenerate them from the tool whenever the sprite pixels change.

Coordinate-system rule: the validator must interpret source PNG rows in the same bottom-origin orientation Unity uses when sampling textures at runtime. If the tool treats image rows as top-origin, asymmetric bread silhouettes can look almost right but mirror their top contour onto the bottom, which is exactly how the Kaiser Bun bottom-right overhang regression happened.

### 5. Update The Asset Metadata

In `ToastBoardMedium.asset` or the relevant medium asset:

1. Keep `visibleAlphaBoundsNormalized` as the measured visible bounds.
2. Set `boardBoundsNormalized` to the normalized rect emitted from the tool's pixel-square gameplay envelope.
3. Disable incompatible explicit-shape metadata if the baked mask is now authoritative:
   - `hasPlayableEllipse: 0`
   - `hasPlayableHorizontalSpanProfile: 0`
   - `playableHorizontalSpanProfile: []`
4. Add the emitted `bakedBlockedTileMasks` entries.
5. Keep `deriveBlockedTilesFromBackgroundAlpha` enabled unless there is a reason to stop using alpha for non-baked fallback sizes.
6. Keep `blockedTileIds` in Unity's normal block-list YAML form:
   - `blockedTileIds:`
   - `  - 123`
   - not a single inline flow list wrapped across lines

### 6. Align Override Placement

Make sure size-band overrides do not reintroduce offset drift against the canonical square envelope.

In practice:

1. Start with `backgroundInset*Normalized: 0` for the baked-mask size bands when the square `boardBoundsNormalized` should fully own placement.
2. Keep `composeSafeAreaWithBoardBoundsMetadata: 0` unless the band truly needs extra inset inside the authored square.
3. Keep `backgroundScaleMultiplier: 1.0` unless visual verification proves the sprite still needs framing adjustment.

If you need band-specific extra inset after baking, compose it deliberately inside the square envelope instead of changing the square itself.

## Metadata Intent

- `visibleAlphaBoundsNormalized`: measured visible-pixel envelope from the source sprite.
- `boardBoundsNormalized`: canonical gameplay envelope inside the sprite; for contour-square bakes this is the normalized rect produced from the centered pixel-space square derived from visible bounds.
- `bakedBlockedTileMasks`: exact blocked-tile sets keyed by `boardWidth`, `boardHeight`, bake version, and sprite content hash.
- `spriteContentHash`: guardrail that ties a baked mask to the exact sprite pixels used to generate it.

## Validation Checklist

After editing the asset, run:

Windows:

```powershell
.\.venv\Scripts\python.exe scripts/validate_board_backgrounds.py
```

POSIX:

```bash
python3 scripts/validate_board_backgrounds.py
```

Do not stop at a green exit code. Confirm the output also says all of the following:

1. The sprite metadata summary shows the expected pixel-square `boardBoundsNormalized`.
2. The sprite metadata summary lists the expected baked sizes, for example `baked=85x85, 90x90, 95x95`.
3. The probe summary reports `baked-mask` for those exact target sizes rather than `alpha-shape`.
4. For asymmetric silhouettes, inspect the validator preview or probe data with row orientation in mind. A top/bottom mirror bug usually shows up as a bottom overhang shaped suspiciously like the sprite's top contour.

Then do an in-Unity visual pass at the affected board sizes and verify:

1. background placement
2. blocked-tile footprint
3. hover / inspection / magnifier alignment
4. highlight and overlay clipping
5. board-edge fade and playable-area tint alignment

## Common Failure Modes

If a new background still looks wrong, check these before inventing more tuning:

1. Square-inside-the-shape look:
   - usually means `boardBoundsNormalized` or safe-area insets are keeping the board inside a conservative inner box instead of letting the intended shape own the footprint
2. Shape looks vertically mirrored on one side:
   - suspect a Y-axis mismatch between validator output and Unity runtime sampling
3. Baked sizes validate, but Unity still uses the wrong contour:
   - confirm the target board size exactly matches a baked `boardWidth`/`boardHeight` entry
   - confirm the sprite pixels and `spriteContentHash` still match the baked data
4. Visual framing improves but playable tiles regress:
   - a scale multiplier or inset tweak may have moved rendering without fixing the canonical footprint model
5. One override band looks correct while neighboring sizes drift:
   - check override ordering and whether only some sizes got baked masks while adjacent sizes still fall back to alpha

## Regeneration Rules

Rebake the masks if any of these change:

1. the source sprite pixels
2. the intended square `boardBoundsNormalized`
3. blocker sampling rules in the validator/runtime
4. the exact board sizes covered by the authored override band

Do not preserve old blocked-tile lists after a sprite-content change just because the silhouette looks similar.
Do not preserve old baked masks after a validator coordinate-system fix either; regenerate the masks and re-check `visibleAlphaBoundsNormalized`/`boardBoundsNormalized` so the stored metadata stays aligned with the corrected bake.

## Current Example

Kaiser Bun is the reference implementation for this workflow:

1. visible alpha stays recorded as the measured bun silhouette
2. `boardBoundsNormalized` is the normalized rect emitted from the centered pixel-space square derived from `max(width, height)`
3. the size bands `85x85`, `90x90`, and `95x95` use `bakedBlockedTileMasks`
4. band insets are zeroed so the square envelope, blocked tiles, and rendered art all line up
5. the validator/runtime row orientation must stay bottom-origin end-to-end or the bun's top contour will be baked onto the lower rows

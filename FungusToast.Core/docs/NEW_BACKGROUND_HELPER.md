# Board Background Authoring Helper

This document is the canonical workflow for adding or reauthoring board background silhouettes in `FungusToast.Unity`.

Use this helper when a background image affects playable footprint, blocked tiles, or board placement. For broader Unity UI/service patterns, see `UI_ARCHITECTURE_HELPER.md`.

## Quick Start

Use this checklist when adding a brand-new board background:

1. Pick the intended shape source before tuning anything:
   - plain alpha when the sprite alpha already matches the playable shape
   - ellipse metadata for boards whose playable area should be a true geometric circle or oval rather than follow the photo edge
   - horizontal span profile for stable row-by-row authored trims such as cheese
   - a traced outline for photo silhouettes that should play exactly to their edge inside a square board, such as pita and the toenail
   - baked masks only for a footprint the outline can't express, such as hand-picked per-size exceptions
2. Import the sprite and add or update the background entry in `ToastBoardMedium.asset`.
3. Add a matching `boardBackgroundSpriteMetadata` entry for the sprite.
4. Start from the conservative baseline:
   - `deriveBlockedTilesFromBackgroundAlpha: 1` when alpha is still part of the fallback path
   - `backgroundScaleMultiplier: 1.0`
   - `backgroundMaxTileClipFraction: 0.0`
   - `backgroundTileClipSampleResolution: 5`
5. If the shape is irregular and should still play as a square board, follow the traced-outline workflow below instead of widening insets until it "looks close enough".
6. Run the validator, then do the Unity visual pass. Do not treat validator success as sufficient by itself.

## When To Use Which Shape Source

Pick the simplest shape model that matches the intended playable silhouette.

1. Use plain alpha-derived masking when the visible sprite alpha already cleanly matches the intended board footprint.
2. Use `hasPlayableEllipse` only when the playable area should be a geometric ellipse. A real round food photo is rarely a true circle: pita used an ellipse until 2026-09-30, and it overhung the bread at the corners.
3. Use `hasPlayableHorizontalSpanProfile` when the silhouette needs authored asymmetric per-row trimming and the board-space shape is still easy to describe row by row.
4. Use `hasPlayableOutline` when the playable area should follow the photo's edge. The outline is a closed polygon of a few dozen to a few hundred points traced from the sprite's alpha. The runtime tests each tile against it with the same sampling the alpha mask uses, so one outline gives an exact footprint at every board size.
5. Use `bakedBlockedTileMasks` only when a footprint can't be described by the sprite's edge. Baked masks store every blocked tile for one exact board size, so a medium offered at many sizes costs tens of thousands of asset lines; pita alone was about 90k before it moved to an outline.

The traced outline is the preferred workflow for irregular photo boards.

## Traced-Outline Workflow

Use this for irregular backgrounds whose gameplay board is square but whose playable area should follow the art's edge.

1. Make the sprite's alpha the intended playable shape. A clean cutout already is one. When only part of a photo should be playable (the nail in a toe photo), cut that part out as its own sprite and put the rest of the photo in the backdrop surface.
2. Emit the outline. The tool traces the alpha contour at the sprite's `backgroundAlphaPlayableThreshold`, simplifies it, and reports how many tiles would differ from any existing baked masks:

   ```powershell
   .\.venv\Scripts\python.exe scripts/validate_board_backgrounds.py --emit-outline-sprite <sprite-name> --emit-outline-tolerance-px 0.75
   ```

   0.75 px keeps the polygon within a fraction of a tile at every board size. A hard-edged cutout is a pixel staircase and needs a few hundred points; an antialiased edge needs far fewer (the toenail needs 58).
3. In the sprite's `boardBackgroundSpriteMetadata` entry, set `hasBoardBounds: 1` with the square `boardBoundsNormalized` from `--emit-baked-mask-sprite` (step 2 of the baked workflow below), paste the `hasPlayableOutline` / `playableOutlineNormalized` snippet, and leave `bakedBlockedTileMasks: []`. Baked masks take priority over the outline, so remove any stale ones.
4. Zero the size band's insets and set `composeSafeAreaWithBoardBoundsMetadata: 0` so the square owns placement.
5. Run the validator and confirm the probes report `outline-shape` for that sprite's sizes, then do the Unity visual pass at a few sizes across the band.

Re-emit the outline whenever the sprite's pixels change.

## Before You Start Tuning

Use these guardrails up front:

1. Treat `visibleAlphaBoundsNormalized` as measured input, not as a creative tuning knob.
2. Treat `boardBoundsNormalized` as canonical gameplay geometry once authored. Do not casually add it "just to fix framing".
3. Keep `backgroundScaleMultiplier` at `1.0` unless a validated shape still needs a render-only framing adjustment after the footprint is already correct.
4. Prefer changing the shape model over stacking compensating tweaks. Repeated inset/scale/clip adjustments usually mean the chosen model is wrong.

## Mediums And Backdrops

The thing the mold grows on is the **medium** (`BoardMediumConfig`). "Toast" is the game's name and the original medium, but it is only one of many: toast, cracker, cheese, baguette, pita, Kaiser bun today, and later non-food mediums such as a toenail or tree bark. Do not assume bread when writing code, docs, or art direction for the medium.

The medium will eventually sit in front of a **backdrop** chosen per medium, built in layers from the medium outward:

1. **Medium** - the board sprite itself, the only thing mold grows on.
2. **Surface** - what the medium rests on. Breads will usually sit on a plate or a cutting board.
3. **Setting** - what the surface rests on, such as a table. Non-food mediums get their own natural context rather than a kitchen.

Until backdrop art exists, a flat color (the Main Camera clear color in `SampleScene.unity`) fills the space around the medium. Treat that as a placeholder, not a design surface.

### Real-world sizes

Placing mediums at true scale needs each one's physical size. The runtime source of truth is `BoardBackdropCatalog.cs`, which also stores each sprite's measured visible rect; keep this table in sync with it. Mediums use typical sizes for their food type (not measured) and can be tuned by eye. Surfaces are measured. "Width" is the sprite's visible (opaque) width; height follows from the sprite's proportions.

| Asset | Kind | Width | Density | Default surface |
|-------|------|-------|---------|-----------------|
| `white_bread_1024x1024.png` | Sandwich bread slice | 11.0 cm | ~85 px/cm | Plate |
| `kaiser_bun_1086x1448.png` | Kaiser bun, cut face | 10.5 cm | ~95 px/cm | Plate |
| `pita_900x900.png` | Pita round | 16.5 cm | ~52 px/cm | Plate |
| `hotdog_bun_900x900.png` | Hot dog bun, top view | 16.0 cm | ~45 px/cm | Plate |
| `cheese_800x800.png` | Pale cheese slice from a block | 8.0 cm | ~85 px/cm | Cutting board |
| `cracker_final_600x600.png` | Saltine-style square cracker | 5.0 cm | ~117 px/cm | Small plate |
| `seed_cracker_550x550.png` | Hexagonal seed cracker | 6.0 cm | ~91 px/cm | Small plate |
| `toenail_709x709.png` | Big-toe nail, cut from the toe photo | 1.7 cm | ~406 px/cm | Toe |
| `plate_surface_1476x1476.png` (art-source) | Plate | 21.6 cm (8.5 in) | ~67 px/cm | Settings: cutting-board material or countertop |
| Same plate photo, drawn smaller | Small (bread) plate | 16.5 cm (6.5 in) | ~87 px/cm | Settings: cutting-board material or countertop |
| `cutting_board_surface_2048x1504.png` (art-source) | Cutting board | 43.8 cm (17.25 in) | ~45 px/cm | Settings: countertop only |
| `toe_surface_1224x1285.png` (art-source) | Big toe, cropped above the foot | ~3.0 cm (derived: 1.7 cm x 1213 / 691 px) | ~406 px/cm | Settings: countertop placeholder until a bath mat photo exists |

Crackers sit on the small plate rather than the cutting board: a 5-6 cm cracker on a 44 cm board shows only plain board until zoomed out about 7x, the board and crackers are nearly the same tan, and the board photo looks soft that close up. The plate's rim appears after about 2.7x, and its smooth white center stays sharp.

**Mediums cut from their surface.** The toenail is the first medium that is part of its surface rather than resting on it. It is cut from the same photo as the toe, and three surface options in `BoardBackdropCatalog` keep them aligned:

- `mediumAnchorNormalized` places the medium's visible center at the nail's position in the toe photo, not at the photo's center.
- `showsMediumShadow: false` skips the medium's contact shadow, which would draw a dark band on the skin.
- `viewFloorNormalized` marks the flat crop at the bottom of the toe photo. `CameraControls` caps zoom-out and blocks panning below it, so no foot photo is needed.

The toenail is currently only on the 165x165 size band, which no campaign level uses, so it can be tried from the solo board-size dropdown.

**How a game's backdrop is chosen.** The medium fixes the surface (for realism). The surface lists the settings it contrasts with, and one is picked from the level's gameplay seed (`BoardBackdropCatalog.TryPickSetting`). The pick is stable when a game is resumed or a checkpoint reloads, but varies between games. Add a new setting by giving it an entry in `SettingsById` and adding its id to every surface it contrasts with; no other rule is needed.

`yellow_cheese_600x600.png` (cheddar, typical width 7.0 cm) and `hotdog_bun_900x600.png` exist but are not referenced by `ToastBoardMedium.asset`.

At default zoom the medium fills most of the screen height, so a smaller medium is magnified more. The pita and hot dog bun sprites are the least detailed per centimeter, but they are also the largest mediums and are magnified least, so all of them display at similar sharpness.

### Backdrop photo guidelines

Backdrops are real photos (plates, cutting boards, napkins, tables), shot so they composite cleanly under a separately photographed medium:

- **Shoot straight down.** Hold the camera level over the center. Step back and zoom in rather than shooting wide up close, so edges do not bow with perspective.
- **Soft, even light.** Use overcast window light or diffuse shade, with no flash and no hard shadows. Match the light direction of the medium sprites. Keep white balance fixed across a set, and shoot RAW if the camera allows it.
- **Shoot each layer empty.** Photograph the surface with nothing on it (the medium is composited on top), and the setting with nothing on it where you can, so layers can be reused across mediums.
- **Realistic physical scale.** Mediums sit on their backdrops at true relative size: a cheese slice covers a small fraction of a cutting board, and most of the board is only visible when zoomed out. Every medium, surface, and setting asset therefore records its real-world size in centimeters, and the game scales layers by those sizes, never to fit the screen.
- **Match pixel density, not screen size.** Backdrop layers should carry roughly the same detail per centimeter as the mediums (the existing medium sprites work out to roughly 90-120 px/cm), so the surface right around the medium looks as sharp as the medium itself. Keep surfaces at their native photo resolution, capped at 4096 px on the long side, and never upscale. Only the area right around the medium needs full density, and that area is always plain surface material, so a seamless material tile of the surface supplies close-zoom detail. Make that tile from a dedicated close-up photo of the surface (roughly a 10-15 cm patch filling the frame); a tile cut from a whole-object shot only has that shot's density. Check a tile's true scale by matching its grain to the measured whole-object photo. The whole-object surface image and the setting are only seen zoomed out, where about 30 px/cm is enough.
- **Contrast each surface with its setting.** A surface on a setting of the same material (a cutting board on a larger cutting board) disappears when zoomed out. Pair each surface with a setting of a different material or tone, such as a wooden table or stone counter.
- **Settings are tileable materials.** A table or large board at matched density is far too big for one image, so settings are delivered as seamless, tileable material textures with a known real-world size per tile, not as fixed screen-sized images.
- **Quiet, neutral materials.** Natural wood, white or cream ceramic, undyed linen. Avoid saturated colors, especially hues close to a player mold color (red, orange, blue, purple, teal, yellow-green), and avoid busy patterns such as gingham near the medium. The backdrop should never out-detail the medium; natural depth of field is fine.
- **Own the photos.** Self-shot photos avoid licensing questions.

**Realism rule.** The medium and its backdrop are photographic and must stay realistic. Do not tint, glow, stylize, recolor, or add effects to them to fit a UI palette or a mood. Stylization belongs to the UI chrome (menus, panels, buttons), which must be designed to sit well against a range of real backdrops: warm wood, white ceramic, bark, skin.

## Source Art Expectations

The workflow below assumes the bread/bun image itself is already fit for a game board. Check these before measuring anything:

- The sprite has a real alpha channel with fully transparent surroundings, not a rendered checkerboard or a flat color that gets keyed later. Everything outside the loaf is `0%` opacity.
- Surface shading is controlled enough to read cleanly under mold tiles. Harsh dark pockets, deep crevices, and burnt patches compete with cell sprites and have repeatedly been softened during iteration; prefer even, gently textured crumb.
- The silhouette has no unnaturally sharp border points or thin spikes. Those become one-tile peninsulas after masking and look like clipping bugs.
- The loaf is the only subject. No plate, table, kitchen, crumbs scattered outside the silhouette, or drop shadow. Plates, cutting boards, and tables belong to the separate backdrop layers described above, never baked into the medium sprite.

## Owning Files

- Runtime/background metadata owner: `FungusToast.Unity/Assets/Scripts/Unity/Grid/BoardMediumConfig.cs`
- Toast medium asset: `FungusToast.Unity/Assets/Configs/Toast Configs/ToastBoardMedium.asset`
- Validation and bake tooling: `scripts/validate_board_backgrounds.py`
- Real-scale backdrop data (medium sizes, surfaces, settings): `FungusToast.Unity/Assets/Scripts/Unity/Grid/BoardBackdropCatalog.cs`
- Backdrop rendering (surface, setting tile, contact shadows): `FungusToast.Unity/Assets/Scripts/Unity/Grid/Helpers/GridBoardBackdropRenderer.cs`
- Backdrop textures loaded at runtime: `FungusToast.Unity/Assets/Resources/Backdrops/` (sources and notes in `art-source/backdrops/`)

## Core Rule

Keep one canonical playable footprint model for the sprite, then make placement, blocked-tile derivation, and mask rendering all agree with that same model.

Do not keep widening insets, scale multipliers, or clip budgets to compensate for a silhouette that should instead have explicit shape metadata.

## Contour-To-Square Baked-Mask Workflow

Prefer the traced-outline workflow above. This one stores a full blocked-tile list per board size, so use it only for footprints an outline can't describe. Its square-envelope step still applies to outlines.

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
5. board-edge fade and playable-area tint alignment. Both follow a smoothed version of the blocked-tile silhouette (a Gaussian blur of the playable grid, `SmoothPlayableField` in `GridVisualizer.Reclaim.cs`), so they trace a rounded outline rather than one-tile stair-steps; only the mold and highlight tilemaps clip to the exact tile mask

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

Re-emit the outline (or rebake any baked masks) if any of these change:

1. the source sprite pixels
2. the intended square `boardBoundsNormalized`
3. blocker sampling rules in the validator/runtime
4. the exact board sizes covered by the authored override band

Do not preserve old blocked-tile lists after a sprite-content change just because the silhouette looks similar.
Do not preserve old baked masks after a validator coordinate-system fix either; regenerate the masks and re-check `visibleAlphaBoundsNormalized`/`boardBoundsNormalized` so the stored metadata stays aligned with the corrected bake.

## Current Example

Kaiser Bun shows the current setup, and every other medium follows it:

1. visible alpha stays recorded as the measured bun silhouette
2. `boardBoundsNormalized` is the normalized rect emitted from the centered pixel-space square derived from `max(width, height)`
3. `playableOutlineNormalized` holds the traced edge (264 points), and `bakedBlockedTileMasks` is empty
4. band insets are zeroed so the square envelope, blocked tiles, and rendered art all line up
5. the validator/runtime row orientation must stay bottom-origin end-to-end or the bun's top contour will land on the lower rows

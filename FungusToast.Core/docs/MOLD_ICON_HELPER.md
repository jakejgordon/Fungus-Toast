# Mold Icon Helper

Use this guide when creating or iterating on board mold sprites for gameplay tiles.

This helper covers:
- how to generate new mold tile art with AI
- how to name and place the PNG files
- how to evaluate whether a sprite survives the jump to 64x64 gameplay use
- how the current pilot runtime wiring works in Unity
- how to iterate from one mold to the next without re-learning the workflow

## Per-Mold Species Identity Rule

Do not treat the remaining molds as palette swaps of the red pilot.

For each new mold set, decide one species identity before writing prompts and keep that identity consistent across all five states.

That species identity should define:
- the dominant macro silhouette family
- the typical edge rhythm
- the interior texture language
- the growth habit that distinguishes isolated, clustered, and dense forms

Examples of acceptable species-identity differences:
- lobed velvet rosettes
- branching fronds or fern-like tendrils
- coral-like lace fans
- chunky blistered pads
- threadlike radial mats

Examples of unacceptable differences:
- same red-pilot rosette silhouette with only a different color
- same clustered stamp shape with minor texture swaps
- same dense medallion perimeter repeated across colors

Important rule for prompts:
- explicitly tell the model that all five images for the current mold must look like the same species
- explicitly tell the model what makes this species different from the previously completed molds

Current lesson from rollout:
- yellow passed gameplay readability, but it stayed somewhat close to the red mold in silhouette language
- future prompts should push harder on species-specific structure so each mold reads as its own organism family at a glance

## Current Eight-Mold Status

The red mold in mold slot 0 was the original pilot. All eight mold slots now have the same five-image alive-state set, and the red set remains the baseline quality reference.

The runtime currently supports these alive-state variants for every mold:
- `isolatedTile`
- `clusteredTile`
- `clusteredAlternateTile`
- `denseTile`
- `denseAlternateTile`

Current dense/spacing tuning:
- isolated scale: `1.00`
- clustered scale: `1.10`
- dense scale: `1.20`

Current state classification:
- `0` friendly orthogonal neighbors -> isolated
- `1-2` friendly orthogonal neighbors -> clustered
- `3+` friendly orthogonal neighbors -> dense

Current variation behavior:
- clustered cells can alternate stably between `clusteredTile` and `clusteredAlternateTile` using a deterministic tile-id hash
- dense cells can alternate stably between `denseTile` and `denseAlternateTile` using a deterministic tile-id hash
- isolated cells currently have only one image
- stable quarter-turn and horizontal-mirror transforms add more variation after the sprite is selected

Current non-board UI icon behavior:
- representative player mold icons are sourced from `GridVisualizer.GetMoldIconTileForPlayer` and `GridVisualizer.GetMoldIconTileForMoldIndex`
- UI currently prefers the clustered representative tile for mold icons and falls back to the legacy base tile if no alive-state variant is configured

## Canonical Files

Core runtime / wiring files:
- `FungusToast.Unity/Assets/Scripts/Unity/Grid/GridVisualizer.cs`
- `FungusToast.Unity/Assets/Scripts/Unity/Grid/GridVisualizer.Reclaim.cs`
- `FungusToast.Unity/Assets/Editor/MoldSpriteImporter.cs`
- `FungusToast.Unity/Assets/Scenes/SampleScene.unity`

Representative UI icon consumers:
- `FungusToast.Unity/Assets/Scripts/Unity/UI/MutationTree/UI_MutationManager.cs`
- `FungusToast.Unity/Assets/Scripts/Unity/UI/UI_MoldProfileRoot.cs`
- `FungusToast.Unity/Assets/Scripts/Unity/UI/GameStart/UI_StartGamePanel.cs`
- `FungusToast.Unity/Assets/Scripts/Unity/UI/Campaign/UI_CampaignPanelController.cs`

Representative current tile assets:
- `FungusToast.Unity/Assets/Tiles/Mold/red_mold_pilot_isolated_64x64.asset`
- `FungusToast.Unity/Assets/Tiles/Mold/red_mold_pilot_clustered_64x64.asset`
- `FungusToast.Unity/Assets/Tiles/Mold/red_mold_pilot_clustered_alt_64x64.asset`
- `FungusToast.Unity/Assets/Tiles/Mold/red_mold_pilot_dense_64x64.asset`
- `FungusToast.Unity/Assets/Tiles/Mold/red_mold_pilot_dense_alt_64x64.asset`

Sprite source folder:
- `FungusToast.Unity/Assets/Sprites/Tiles/Mold/`

Legacy tile asset folder:
- `FungusToast.Unity/Assets/Tiles/Mold/`

Palette baseline:
- `FungusToast.Core/docs/UI_STYLE_GUIDE.md`

## Naming Rules

### PNG files

Place gameplay PNGs under:
- `FungusToast.Unity/Assets/Sprites/Tiles/Mold/`

Preferred runtime naming pattern:
- `{mold_name}_pilot_{state}_64x64.png`

For the remaining rollout, keep the `_pilot_` infix for consistency with the red baseline unless the whole set is renamed in one deliberate cleanup.

Examples:
- `red_mold_pilot_isolated_64x64.png`
- `red_mold_pilot_clustered_64x64.png`
- `red_mold_pilot_clustered_alt_64x64.png`
- `red_mold_pilot_dense_64x64.png`
- `red_mold_pilot_dense_alt_64x64.png`

### Tile assets

Keep matching Tile assets under:
- `FungusToast.Unity/Assets/Tiles/Mold/`

Examples:
- `red_mold_pilot_isolated_64x64.asset`
- `red_mold_pilot_clustered_64x64.asset`
- `red_mold_pilot_clustered_alt_64x64.asset`
- `red_mold_pilot_dense_64x64.asset`
- `red_mold_pilot_dense_alt_64x64.asset`

## AI Generation Workflow

Use AI to generate the art, but do not rely on a blind downscale from a highly detailed image.

Correct workflow:
1. Generate a transparent-background square sprite at `512x512` or `1024x1024`.
2. Tell the model explicitly the sprite must remain readable at `64x64`.
3. Tell the model explicitly to output a real alpha channel, not a checkerboard preview.
4. Download the large version.
5. Resize to `64x64`.
6. Judge the `64x64`, not just the large original.
7. If the `64x64` is muddy, regenerate with fewer details and a stronger silhouette.

Wrong workflow:
1. Generate a beautiful high-detail mold image.
2. Shrink it blindly to `64x64`.
3. Accept it because the large version looked good.

## Core Art Direction Rules

Use these rules for every mold variant:
- top-down only
- transparent background
- real alpha channel only
- no perspective
- no cast shadow
- no glow / bloom / halo
- no hard black outline
- no border or frame
- no bread or environment background
- no text, symbols, numbers, or UI iconography
- no pixels touching the image border

Prefer:
- strong silhouette first
- moderate interior texture second
- realism only insofar as it improves small-size readability
- asymmetry in every variant
- species consistency across isolated, clustered, and dense states
- a distinct species identity per mold slot, not just a color change

Required transparency instruction for prompts:
- `Generate as a PNG with a real alpha channel. The background must be fully transparent alpha, not a checkerboard transparency preview. Do not render gray-and-white checker squares. Only the mold pixels should be opaque; all non-mold pixels must have 0% opacity.`

## Border / Margin Rules

Do not let the mold touch the edge of the PNG.

Hard rule:
- no non-transparent pixels in the outermost `2 px` of the final `64x64` image

Preferred margin targets:
- isolated: about `5-8 px`
- clustered: about `4-6 px`
- dense: about `2-4 px`

Important nuance:
- dense should not have even centered padding on all sides
- dense should press outward asymmetrically toward the border
- the goal is irregular near-contact, not full contact and not roomy centered spacing

## What Makes Dense Work

Dense is the hardest state.

Dense should:
- feel broad, mature, and fused
- read as part of a contiguous patch when repeated
- have an irregular perimeter with bays / notches / uneven outward pressure
- have a darker, heavier center than isolated or clustered

Dense should not:
- look like a centered flower medallion
- fill the tile as a flat square pancake
- have the same spacing on every side
- rely on tiny texture to carry the image at gameplay scale

When dense patches still look fake, the usual fix is not more scale. It is more shape variation.

## Evaluation Checklist

Reject a candidate if:
- it touches or nearly touches the border
- it becomes muddy at `64x64`
- it looks like a centered stamp or badge
- it becomes too symmetrical
- it reads as a soft red cloud instead of a colony
- isolated / clustered / dense feel like scale-only copies of the same silhouette
- dense reads as a repeated flower stamp in a patch

Keep a candidate if:
- the silhouette is obvious at a glance
- the center / edge hierarchy still reads after downscale
- it looks like the same species as the other states
- dense areas visually fuse into a patch instead of a grid of icons

## Reusable Prompt Template: Clustered Alt

Use this when generating a clustered alternate for any mold after the main clustered tile is working.

Recommended filename:
- `red_mold_pilot_clustered_alt_64x64.png`

Prompt:

Create a single top-down semi-realistic sprite of an alternate medium-growth vermilion-red bread mold colony for a strategy game tile. Transparent background. This image is intended to be resized to 64x64 pixels, so silhouette and large-shape readability are more important than fine detail. This must be the same species as the current red pilot mold, but it should be a different clustered-growth expression designed specifically to break up repetition in the medium-density transition band.

Generate as a PNG with a real alpha channel. The background must be fully transparent alpha, not a checkerboard transparency preview. Do not render gray-and-white checker squares. Only the mold pixels should be opaque; all non-mold pixels must have 0% opacity.

The colony should occupy roughly 60 percent to 72 percent of the canvas and remain fully contained inside the image with about 4 to 6 px of transparent space on most sides. Do not center it evenly. Make the silhouette asymmetrical and less medallion-like than the current clustered version. It should still feel like an interlocking lobed colony, but with a different perimeter rhythm and different distribution of weight so repeated neighboring copies do not read as the same flower stamp.

Use the same species identity: dominant vermilion-red color, darker maroon-brown shadowed core, lighter desaturated red highlights, subtle fissures, velvety biomass, irregular living perimeter. The perimeter should feel mature and slightly crowded, but not fully fused like the dense state. It should look like a mold colony in an active thickening phase between isolated and dense. Strong silhouette first, moderate texture second.

Do not generate any background of any kind. Do not show bread, toast, table, plate, kitchen, spores floating far away, fog, smoke, halo, bloom, glow, rim light, side view, angled view, mushroom caps, stems, cartoon features, mascot styling, letters, numbers, icons, hard black outlines, perfect circles, perfect squares, centered geometric badges, clipped edges, cropped forms that continue off canvas, or any pixels touching the border. Do not make it a neat rosette, a centered blossom, or the isolated version merely scaled up.

## Delivery Checklist For A New Mold Iteration

When iterating on a mold:
1. Work one mold slot at a time.
2. Define the mold's species identity in one sentence before prompting.
3. Build isolated, clustered, and dense first.
4. Add `denseAlternateTile` if dense patches still read as repeated stamps.
5. Add `clusteredAlternateTile` if the medium-density transition band still looks repetitive.
6. Save PNGs under `Assets/Sprites/Tiles/Mold`.
7. Create matching Tile assets under `Assets/Tiles/Mold`.
8. Wire the mold slot in `playerMoldAliveVariantTiles`.
9. Playtest in Unity and review both board cells and shared UI icons at real gameplay scale.
10. Judge patch readability and cross-mold distinctness, not just close-up beauty.

## How To Ask Copilot For The Next Wiring Step

Useful examples:
- "Wire this new clustered alternate into the pilot mold."
- "Add a clusteredAlternateTile to the pilot system and alternate it deterministically."
- "The dense patch still looks repetitive; add one more dense variant and hook it up."
- "This mold looks too airy in dense patches; adjust the scale slightly but do not change the art yet."

## Future Expansion Notes

The runtime and art now cover all eight molds with five images per mold. The next planned expansion is one new isolated, clustered, and dense image per mold, as specified below.

## Eight Images Per Mold Expansion Plan

### Objective and Scope

Add exactly 24 new source PNGs: one isolated, one clustered (medium-density), and one dense variant for each of the eight molds. After the expansion, every mold has:

- 2 isolated images
- 3 clustered images
- 3 dense images
- 8 alive-state images total

This task includes art generation, deterministic runtime selection, Unity Tile assets, scene wiring, automated asset validation, and gameplay-scale visual review. It does not replace or repaint any of the existing 40 PNGs unless a separate cleanup is approved.

### Verified Current Implementation

The current implementation is concentrated in:

- `GridVisualizer.MoldAliveVisualTiles`, which serializes five explicit Tile fields per mold
- `GridVisualizer.ClassifyAliveVisualState`, which maps `0` friendly orthogonal neighbors to isolated, `1-2` to clustered, and `3+` to dense
- `ResolveClusteredVariantTile` and `ResolveDenseVariantTile`, which choose between two images with a stable coordinate/tile-id hash
- `BuildAliveMoldVisualMatrix`, which independently adds stable quarter-turn and mirror variation
- `SampleScene.unity`, which wires eight `playerMoldAliveVariantTiles` entries in this order: red, yellow, cyan, aqua, green, dark blue, purple, orange-red
- `MoldSpriteImporter`, which applies Sprite/64 PPU/Point/Clamp/uncompressed/full-rect import settings and updates a matching Tile asset if that Tile asset already exists; it does not create a missing Tile asset
- `UI_ModeSelectPanelController`, which collects all five current images for ambient menu decoration
- `GetMoldIconTileForMoldIndex`, which deliberately uses the primary clustered image as the stable representative UI icon

The runtime change is therefore additive. Do not change the three neighbor-count bands or the existing visual scale values as part of this work.

### Asset Names and Runtime Fields

Use these new state suffixes consistently for PNGs and matching Tile assets:

- `isolated_alt`
- `clustered_alt_2`
- `dense_alt_2`

Examples:

- `red_mold_pilot_isolated_alt_64x64.png`
- `red_mold_pilot_clustered_alt_2_64x64.png`
- `red_mold_pilot_dense_alt_2_64x64.png`

Add these serialized fields without renaming the five existing fields, so Unity preserves existing scene references:

- `isolatedAlternateTile`
- `clusteredSecondAlternateTile`
- `denseSecondAlternateTile`

Do not convert the existing explicit fields to arrays in this slice. That would create avoidable Unity serialization/migration risk for a fixed three-variant requirement.

### Canonical Reference Matrix

For each generation call, use all five existing PNGs for that same mold as reference images. Label their roles explicitly: isolated anchor, primary clustered anchor, alternate clustered anchor, primary dense anchor, and alternate dense anchor. Do not use another player's mold as the species reference.

Preserve these visible identities from the current assets:

| Scene slot | File prefix | Species identity to preserve |
|---|---|---|
| 0 | `red` | vermilion-red velvety lobes/rosettes, darker maroon folds and cores |
| 1 | `yellow` | mustard-yellow/olive fuzzy clustered nodules with darker granular centers |
| 2 | `cyan` | bright cyan-blue branching coral/frond growth with deep-blue structure |
| 3 | `aqua` | pale aqua/mint porous blister-pad colonies with darker turquoise pits |
| 4 | `green` | olive-green mossy, chunky lobes with deep green-brown recesses |
| 5 | `dark_blue` | very dark navy/indigo folded velvety biomass with blue highlights |
| 6 | `purple` | lavender-purple soft radial filaments and fuzzy cloudlike lobes with darker violet cores |
| 7 | `orange_red` | shaggy woolly rusty orange/apricot masses, pale fringe, and ember/cinnamon core pockets |

The reference PNGs, not the flat UI token palette, are canonical for art color. The UI palette remains useful for player distinction checks, but must not be used to recolor the biological texture into a flat token color.

### Footprint Definitions and Acceptance Bands

Use two separate measurements; do not use the ambiguous word "occupancy" by itself:

1. **Alpha footprint**: area of the smallest bounding rectangle containing every pixel whose alpha is greater than zero, divided by total canvas area.
2. **Visible-pixel coverage**: percentage of all canvas pixels whose alpha is greater than zero. Record alpha-at-least-128 coverage as a secondary edge-softness check.

Target alpha-footprint bands at the final `64x64` size:

| State | Target alpha footprint | Preferred transparent margin | Visual intent |
|---|---:|---:|---|
| Isolated | 45%-65% | 5-8 px on most sides | one small colony or a few connected masses, clearly surrounded by toast |
| Clustered | 65%-78% | 4-6 px on most sides | medium growth, broader than isolated but not fused to neighboring tiles |
| Dense | 78%-88% | 2-4 px on most sides | mature fused patch pressing outward asymmetrically |

These footprint bands are prompt and review targets, not permission to fill the bounding box with a solid shape. The visible-pixel coverage must remain appropriate to the species: an airy cyan frond and a porous aqua pad should not be forced to match the solid-pixel count of a velvety red lobe.

Before generation, run the validator against the current five images for the selected mold and record their alpha footprint and visible-pixel coverage. Use the appropriate existing state images as the local comparison. Several current assets reach or cross the border and one source named `aqua_mold_pilot_clustered_64x64.png` is actually `85x85`; treat those as known baseline defects, not standards to reproduce. Do not alter them without separate approval.

### Prompt Construction

Create three separate prompts per mold rather than requesting a sheet or three subjects in one image. Each prompt must contain:

1. the target state and the fact that this is a new silhouette, not a copy or simple rotation of an existing image
2. the five same-mold references and their labeled roles
3. the species identity and color/material language from the reference matrix, refined by any original prompt Jake supplies
4. the target alpha-footprint band, final margin, and `64x64` readability requirement
5. top-down, semi-realistic, asymmetrical, fully contained composition
6. the common negative constraints from Core Art Direction Rules
7. explicit instructions to avoid a rendered checkerboard, cast shadow, glow, background, border contact, and cross-player palette drift

State-specific shape direction:

- **New isolated**: change mass count, lobe orientation, and center placement while staying sparse; it must not be the existing isolated sprite rotated, mirrored, or scaled.
- **New clustered**: use a third perimeter rhythm and distribution of weight; it must sit between isolated and dense and differ materially from both existing clustered silhouettes.
- **New dense**: use off-center core pockets, asymmetric near-contact spacing, bays/notches, and fused masses; it must blend into a patch without becoming a square pancake or centered medallion.

The orange dense prompt supplied by Jake is a strong dense template. Reuse its structure and transparency warnings, but substitute each mold's own references, material language, colors, and silhouette rules. Do not reuse its orange-specific biological description for other players.

### Transparency Workflow and Checkerboard Prevention

An instruction in the prompt is not sufficient proof of transparency. Every candidate must pass file-level alpha validation.

At execution time, choose one of these paths before the first generation call:

1. **Built-in image generation plus chroma-key removal**: generate on a single flat key color that does not occur in that mold, remove it locally with the installed image-generation helper, use soft matte/despill, then validate the result. This is the default tool path, but fuzzy hyphae can retain color spill or lose fine fringe.
2. **True native alpha via the CLI `gpt-image-1.5` path**: use `--background transparent --output-format png`. This is safer for fuzzy/hairlike mold edges, but requires Jake's explicit approval for the model/path change and requires `OPENAI_API_KEY`. Never switch to it silently.

For chroma keying, select the key per mold rather than assuming green is always safe. Never choose a key near the subject palette. Keep the key perfectly flat with no shadows, gradient, floor plane, texture, or lighting variation. Retain the unprocessed generated source outside `Assets/`; only validated `64x64` alpha PNGs enter the Unity sprite folder.

Reject a candidate automatically if any of these are true:

- it is not exactly `64x64` after final downsample
- it lacks an alpha channel
- all pixels are opaque
- any of the four corners is opaque
- any nontransparent pixel occurs in the outermost 2 px border
- the alpha footprint is outside the approved state band
- a gray/white checker pattern is baked into opaque pixels
- chroma-key colored fringe remains on the mold edge

Also inspect each candidate composited over at least three backgrounds: light neutral, dark neutral, and representative toast brown. File-level tests catch opaque checkerboards; compositing catches halos, missing fringe, and bad despill.

### Generation and Approval Sequence

Work one mold at a time so species drift is caught early:

1. Resolve the execution decisions listed in Inputs Required From Jake.
2. Implement the runtime fields and three-choice resolver with null-safe fallbacks before bulk art wiring.
3. Select one pilot mold. Orange-red is efficient if Jake provides its original prompts; red is the cleanest established baseline. Do not choose between them without Jake's direction.
4. Generate multiple candidates for that mold's isolated variant only, validate them, and present the best `64x64` candidates for approval.
5. Repeat for clustered, then dense. Review the full eight-image family and a repeated-tile board patch before accepting the pilot mold.
6. Repeat the approved workflow for the other seven molds, with an approval checkpoint after each mold rather than generating all remaining assets blindly.
7. Preserve final prompts beside the handoff record in this document or another indexed project document so future variants can be reproduced. Do not store secrets, API keys, or generated scratch files in the repo.

### Runtime Selection Plan

In `GridVisualizer.cs`:

1. Add the three serialized fields listed above.
2. Add `ResolveIsolatedVariantTile` and route isolated state through it.
3. Extend clustered and dense resolution to three non-null choices.
4. Use a stable full-width hash and state-specific salts, then choose `hash % availableVariantCount` so each configured image receives approximately equal representation.
5. Preserve the exact current two-image selection path whenever the third clustered/dense field is null. Existing scenes or partial configurations must retain their old behavior.
6. For isolated, return the original tile when the alternate is null; when both exist, use a separate salt and a stable 50/50 selection.
7. Keep selection dependent only on stable board data (`X`, `Y`, and `TileId`), never Unity random state, animation frame, render order, or player turn.
8. Keep representative UI icons pinned to the primary clustered tile. Add the three new sprites to ambient menu collection, but do not randomize profile/start/campaign icons.

This behavior must remain deterministic across rerenders, save/resume, reclaim animations, and different machines.

### Unity Asset and Scene Wiring

For each accepted final PNG:

1. Downsample once to final `64x64` with a high-quality alpha-aware filter; judge this file, not the large source.
2. Save it under `FungusToast.Unity/Assets/Sprites/Tiles/Mold/` with the approved suffix.
3. Let `MoldSpriteImporter` apply the established import settings.
4. Create a matching Tile asset under `FungusToast.Unity/Assets/Tiles/Mold/`; do not assume the importer creates it.
5. Reimport the PNG after the Tile asset exists if necessary so the importer assigns the Sprite.
6. Wire all three new Tile fields for all eight entries in `SampleScene.unity` through Unity serialization, preserving `.meta` GUIDs.
7. Confirm no prefab or alternate scene owns another `GridVisualizer` configuration that needs the same wiring.

### Validation and Definition of Done

Add a repeatable Unity Editor validation command for the mold sprite folder. It should report, per PNG: dimensions, alpha-channel presence, corner alpha, outer-2-px violations, alpha footprint, visible-pixel coverage, and matching Tile asset existence. It should fail clearly on an opaque checkerboard-like background rather than trusting the filename or prompt.

Runtime validation:

- verify the same board state always resolves to the same image
- verify null fields fall back without exceptions
- verify 2-choice configurations preserve existing selection behavior
- verify all three configured choices appear across a representative coordinate sample without row, column, or checkerboard banding
- verify the neighbor thresholds remain `0`, `1-2`, and `3+`
- verify ambient menu sprite collection includes all eight images per selected mold without duplicates
- verify stable representative UI icons are unchanged

Unity visual validation at actual gameplay scale:

- isolated cells read as sparse and do not look like miniature dense stamps
- clustered bands show three distinct silhouettes and remain visually between isolated and dense
- dense repeated patches feel fused, do not expose a strong icon grid, and do not become flat squares
- rotations and mirrors do not reveal lighting or perspective inconsistencies
- each mold remains recognizable against toast and distinguishable from the other seven molds
- light/dark/toast composites show no checkerboard, chroma fringe, halo, or accidental opaque background

The slice is complete only when all 24 final PNGs and Tile assets are present, all 24 scene fields are wired, automated validation passes, Unity compilation succeeds, and Jake approves gameplay-scale screenshots of every mold in isolated/clustered/dense board situations.

### Inputs Required From Jake Before Execution

Do not begin image generation until these are resolved:

1. **Pilot mold**: choose orange-red (best prompt provenance if the remaining prompts exist) or red (cleanest original baseline).
2. **Transparency path**: approve either the default chroma-key/removal path or the true-native-alpha CLI path. The latter requires explicit approval and an available `OPENAI_API_KEY`.
3. **Footprint policy**: confirm the documented consistent bands are authoritative for new art, even where an existing sprite is an outlier, or request per-player matching to those outliers.
4. **Review cadence**: confirm approval after each completed mold family, or explicitly authorize a larger batch.
5. **Original prompts**: provide any available prompts. They are helpful and should reduce species/color drift, but they are not essential because the five existing same-mold PNGs can serve as canonical visual references. The prompts become important if they contain intentional species or palette rules that are not reliably visible in the final `64x64` files.

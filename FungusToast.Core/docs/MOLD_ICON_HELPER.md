# Mold Icon Helper

Use this guide when creating or iterating on board mold sprites for gameplay tiles.

This helper covers:
- how to generate new mold tile art with AI
- how to name and place the PNG files
- how to evaluate whether a sprite survives the jump to 64x64 gameplay use
- how the current pilot runtime wiring works in Unity
- how to iterate from one mold to the next without re-learning the workflow

## Current Pilot Status

The current pilot is the red mold in mold slot 0.

This pilot is now the baseline template for the remaining 7 molds.

The runtime currently supports these alive-state variants for the pilot mold:
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

Current pilot tile assets:
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

For the remaining 7-mold rollout, keep the `_pilot_` infix for consistency with the red baseline unless the whole set is renamed in one deliberate cleanup.

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
2. Build isolated, clustered, and dense first.
3. Add `denseAlternateTile` if dense patches still read as repeated stamps.
4. Add `clusteredAlternateTile` if the medium-density transition band still looks repetitive.
5. Save PNGs under `Assets/Sprites/Tiles/Mold`.
6. Create matching Tile assets under `Assets/Tiles/Mold`.
7. Wire the mold slot in `playerMoldAliveVariantTiles`.
8. Playtest in Unity and review both board cells and shared UI icons at real gameplay scale.
9. Judge patch readability, not just close-up beauty.

## How To Ask Copilot For The Next Wiring Step

Useful examples:
- "Wire this new clustered alternate into the pilot mold."
- "Add a clusteredAlternateTile to the pilot system and alternate it deterministically."
- "The dense patch still looks repetitive; add one more dense variant and hook it up."
- "This mold looks too airy in dense patches; adjust the scale slightly but do not change the art yet."

## Future Expansion Notes

The runtime is now ready for a full 8-mold rollout.

Recommended next expansions, in order:
1. roll out the remaining 7 molds using the red baseline as the quality bar
2. add more visually distinct dense alternates only for molds that still read repetitively in screenshots
3. consider an enemy-contested border state only if playtests show that adjacency readability still needs more help